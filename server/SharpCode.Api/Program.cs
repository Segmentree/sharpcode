using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using SharpCode.Api;
using SharpCode.Api.Data;
using SharpCode.Api.Judge;

var builder = WebApplication.CreateBuilder(args);

var dbPath = builder.Configuration["Database:Path"] ?? Path.Combine(builder.Environment.ContentRootPath, "sharpcode.db");
builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite($"Data Source={dbPath}"));
builder.Services.AddSingleton<Compiler>();
builder.Services.AddSingleton<JudgeService>();
builder.Services.AddSingleton<CodeCompletion>();
builder.Services.AddSingleton<SandboxService>();
builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    o.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddSharpCodeAuth();

var app = builder.Build();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    RetireLegacyDatabase(dbPath, app.Logger);
    db.Database.Migrate();
    Seed.Apply(db, app.Logger);
    if (string.IsNullOrEmpty(app.Configuration["Admin:Token"]))
        app.Logger.LogWarning("Admin:Token is not configured; problem editing is disabled");
    if (!scope.ServiceProvider.GetRequiredService<AuthService>().Enabled)
        app.Logger.LogWarning("Google:ClientId is not configured; sign-in is disabled");
}

var api = app.MapGroup("/api");
// Everything a signed-in user owns (submissions, drafts, scripts, running code) lives under this group.
var mine = app.MapGroup("/api").RequireAuthorization();

// ---------- Config & auth ----------

api.MapGet("/config", (AuthService auth) => new ConfigDto(auth.GoogleClientId));

// Exchanges a Google ID token for this app's bearer token, creating the account on first sign-in.
api.MapPost("/auth/google", async (GoogleSignInInput input, AuthService auth, AppDbContext db, CancellationToken ct) =>
{
    if (!auth.Enabled) return Results.Json(new { error = "Sign-in is not configured." }, statusCode: StatusCodes.Status503ServiceUnavailable);
    var payload = await auth.ValidateGoogleTokenAsync(input.IdToken);
    if (payload is null) return Results.Json(new { error = "Google sign-in could not be verified." }, statusCode: StatusCodes.Status401Unauthorized);

    var user = await db.Users.FirstOrDefaultAsync(u => u.GoogleSubject == payload.Subject, ct);
    if (user is null)
    {
        user = new User { GoogleSubject = payload.Subject, Email = payload.Email ?? "", Name = payload.Name ?? payload.Email ?? "User" };
        db.Users.Add(user);
    }
    user.Email = payload.Email ?? user.Email;
    user.Name = string.IsNullOrWhiteSpace(payload.Name) ? user.Name : payload.Name;
    user.Picture = payload.Picture;
    user.LastLoginAt = DateTime.UtcNow;
    await db.SaveChangesAsync(ct);
    return Results.Ok(new SessionDto(user.ToDto(), auth.CreateToken(user)));
});

// Who am I. Also hands back a fresh token when the current one is getting old, so active users stay signed in.
mine.MapGet("/auth/me", async (HttpContext ctx, AuthService auth, AppDbContext db, CancellationToken ct) =>
{
    var user = await db.CurrentUserAsync(ctx.User, ct);
    if (user is null) return Results.Unauthorized();
    var issued = ctx.User.IssuedAt();
    var refresh = issued is null || DateTime.UtcNow - issued > AuthService.RefreshAfter;
    return Results.Ok(new SessionDto(user.ToDto(), refresh ? auth.CreateToken(user) : null));
});

// ---------- Admin ----------

api.MapGet("/admin/check", () => Results.Ok(new { ok = true })).RequireAdmin();

// ---------- Problems ----------

// Public. The "solved" tick is per user, so it is always false for anonymous visitors.
api.MapGet("/problems", async (string? lang, HttpContext ctx, AppDbContext db) =>
{
    var userId = ctx.User.UserId();
    var solved = userId is null
        ? []
        : await db.Submissions.Where(s => s.UserId == userId && s.Verdict == Verdict.Accepted).Select(s => s.ProblemId).Distinct().ToListAsync();
    var problems = await db.Problems.Include(p => p.Translations).Include(p => p.TestCases).OrderBy(p => p.Id).ToListAsync();
    return problems.Select(p => new ProblemSummaryDto(p.Id, p.Slug, Mapping.Localize(p, lang).Title, p.Difficulty, p.Category,
        p.TestCases.Count, solved.Contains(p.Id)));
});

api.MapGet("/problems/{idOrSlug}", async (string idOrSlug, string? lang, AppDbContext db) =>
{
    var p = await FindProblem(db, idOrSlug);
    return p is null ? Results.NotFound() : Results.Ok(p.ToDto(includeHiddenDetails: false, lang));
});

// Admin view: hidden test case details and every translation.
api.MapGet("/problems/{idOrSlug}/full", async (string idOrSlug, AppDbContext db) =>
{
    var p = await FindProblem(db, idOrSlug);
    return p is null ? Results.NotFound() : Results.Ok(p.ToDto(includeHiddenDetails: true, lang: null, includeTranslations: true));
}).RequireAdmin();

api.MapPost("/problems", async (ProblemInput input, AppDbContext db) =>
{
    if (Validate(input, requireTestCases: true) is { } error) return Results.BadRequest(new { error });
    var slug = string.IsNullOrWhiteSpace(input.Slug) ? Mapping.Slugify(input.Title) : Mapping.Slugify(input.Slug);
    if (await db.Problems.AnyAsync(p => p.Slug == slug)) return Results.Conflict(new { error = $"Slug '{slug}' already exists." });

    var problem = new Problem { Slug = slug, Title = "", Description = "", StarterCode = "", MethodName = "" };
    ApplyInput(problem, input);
    db.Problems.Add(problem);
    await db.SaveChangesAsync();
    return Results.Created($"/api/problems/{problem.Id}", problem.ToDto(includeHiddenDetails: true, lang: null, includeTranslations: true));
}).RequireAdmin();

api.MapPut("/problems/{id:int}", async (int id, ProblemInput input, AppDbContext db) =>
{
    if (Validate(input, requireTestCases: false) is { } error) return Results.BadRequest(new { error });
    var problem = await db.Problems.Include(p => p.TestCases).Include(p => p.Translations).FirstOrDefaultAsync(p => p.Id == id);
    if (problem is null) return Results.NotFound();
    var slug = string.IsNullOrWhiteSpace(input.Slug) ? problem.Slug : Mapping.Slugify(input.Slug);
    if (await db.Problems.AnyAsync(p => p.Slug == slug && p.Id != id)) return Results.Conflict(new { error = $"Slug '{slug}' already exists." });

    problem.Slug = slug;
    if (input.TestCases is not null) db.TestCases.RemoveRange(problem.TestCases);
    db.ProblemTranslations.RemoveRange(problem.Translations);
    ApplyInput(problem, input);
    await db.SaveChangesAsync();
    return Results.Ok(problem.ToDto(includeHiddenDetails: true, lang: null, includeTranslations: true));
}).RequireAdmin();

api.MapDelete("/problems/{id:int}", async (int id, AppDbContext db) =>
{
    var deleted = await db.Problems.Where(p => p.Id == id).ExecuteDeleteAsync();
    return deleted == 0 ? Results.NotFound() : Results.NoContent();
}).RequireAdmin();

// ---------- Judge ----------

// Run: executes only the visible (sample) cases, nothing is persisted. Quick feedback loop while coding.
mine.MapPost("/problems/{id:int}/run", async (int id, CodeInput input, AppDbContext db, JudgeService judge, CancellationToken ct) =>
{
    var problem = await db.Problems.Include(p => p.TestCases).FirstOrDefaultAsync(p => p.Id == id, ct);
    if (problem is null) return Results.NotFound();
    var cases = problem.TestCases.Where(t => !t.IsHidden).OrderBy(t => t.Order).ThenBy(t => t.Id).ToList();
    var result = await judge.JudgeAsync(problem, cases, input.Code, revealHidden: false, ct);
    return Results.Ok(result);
});

// Submit: executes every case (hidden ones included) and records the submission.
mine.MapPost("/problems/{id:int}/submit", async (int id, CodeInput input, HttpContext ctx, AppDbContext db, JudgeService judge, CancellationToken ct) =>
{
    var problem = await db.Problems.Include(p => p.TestCases).FirstOrDefaultAsync(p => p.Id == id, ct);
    if (problem is null) return Results.NotFound();
    var cases = problem.TestCases.OrderBy(t => t.Order).ThenBy(t => t.Id).ToList();
    var result = await judge.JudgeAsync(problem, cases, input.Code, revealHidden: false, ct);

    var submission = new Submission
    {
        ProblemId = problem.Id, UserId = ctx.User.RequiredUserId(), Code = input.Code, Verdict = result.Verdict,
        PassedCount = result.PassedCount, TotalCount = result.TotalCount,
        ResultJson = JsonSerializer.Serialize(result, JsonOpts),
    };
    db.Submissions.Add(submission);
    await db.SaveChangesAsync(ct);
    return Results.Ok(new SubmissionDto(submission.Id, submission.ProblemId, submission.Verdict, submission.PassedCount,
        submission.TotalCount, submission.CreatedAt, submission.Code, result));
});

mine.MapGet("/problems/{id:int}/submissions", async (int id, HttpContext ctx, AppDbContext db) =>
{
    var userId = ctx.User.RequiredUserId();
    return await db.Submissions.Where(s => s.ProblemId == id && s.UserId == userId).OrderByDescending(s => s.Id).Take(50)
        .Select(s => new SubmissionDto(s.Id, s.ProblemId, s.Verdict, s.PassedCount, s.TotalCount, s.CreatedAt, s.Code, null))
        .ToListAsync();
});

mine.MapGet("/submissions/{id:int}", async (int id, HttpContext ctx, AppDbContext db) =>
{
    var userId = ctx.User.RequiredUserId();
    var s = await db.Submissions.FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);
    if (s is null) return Results.NotFound();
    var result = JsonSerializer.Deserialize<JudgeResult>(s.ResultJson, JsonOpts);
    return Results.Ok(new SubmissionDto(s.Id, s.ProblemId, s.Verdict, s.PassedCount, s.TotalCount, s.CreatedAt, s.Code, result));
});

// ---------- Problem drafts (autosaved editor contents, one per user and problem) ----------

mine.MapGet("/problems/{id:int}/draft", async (int id, HttpContext ctx, AppDbContext db) =>
{
    var userId = ctx.User.RequiredUserId();
    var d = await db.ProblemDrafts.FirstOrDefaultAsync(d => d.ProblemId == id && d.UserId == userId);
    return d is null ? Results.NoContent() : Results.Ok(new DraftDto(d.Code, d.UpdatedAt));
});

mine.MapPut("/problems/{id:int}/draft", async (int id, DraftInput input, HttpContext ctx, AppDbContext db) =>
{
    if (!await db.Problems.AnyAsync(p => p.Id == id)) return Results.NotFound();
    var userId = ctx.User.RequiredUserId();
    var d = await db.ProblemDrafts.FirstOrDefaultAsync(d => d.ProblemId == id && d.UserId == userId);
    if (d is null) db.ProblemDrafts.Add(d = new ProblemDraft { ProblemId = id, UserId = userId, Code = input.Code });
    d.Code = input.Code;
    d.UpdatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync();
    return Results.Ok(new DraftDto(d.Code, d.UpdatedAt));
});

mine.MapDelete("/problems/{id:int}/draft", async (int id, HttpContext ctx, AppDbContext db) =>
{
    var userId = ctx.User.RequiredUserId();
    await db.ProblemDrafts.Where(d => d.ProblemId == id && d.UserId == userId).ExecuteDeleteAsync();
    return Results.NoContent();
});

// ---------- Editor support ----------

mine.MapPost("/complete", async (CompleteInput input, CodeCompletion completion, CancellationToken ct) =>
    Results.Ok(await completion.CompleteAsync(input.Code ?? "", input.Position, ct)));

// ---------- Sandbox ----------

mine.MapPost("/sandbox/run", async (SandboxInput input, SandboxService sandbox, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(input.Code)) return Results.BadRequest(new { error = "Code is required." });
    return Results.Ok(await sandbox.RunAsync(input.Code, input.Stdin ?? "", ct));
});

// What is in the sandbox editor right now (autosaved).
mine.MapGet("/sandbox/draft", async (HttpContext ctx, AppDbContext db) =>
{
    var d = await db.SandboxDrafts.FindAsync(ctx.User.RequiredUserId());
    return d is null ? Results.NoContent() : Results.Ok(new SandboxDraftDto(d.Code, d.Stdin, d.UpdatedAt));
});

mine.MapPut("/sandbox/draft", async (SandboxDraftInput input, HttpContext ctx, AppDbContext db) =>
{
    var userId = ctx.User.RequiredUserId();
    var d = await db.SandboxDrafts.FindAsync(userId);
    if (d is null) db.SandboxDrafts.Add(d = new SandboxDraft { UserId = userId, Code = input.Code });
    d.Code = input.Code;
    d.Stdin = input.Stdin ?? "";
    d.UpdatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync();
    return Results.Ok(new SandboxDraftDto(d.Code, d.Stdin, d.UpdatedAt));
});

// Named scripts the user keeps.
mine.MapGet("/sandbox/scripts", async (HttpContext ctx, AppDbContext db) =>
{
    var userId = ctx.User.RequiredUserId();
    return await db.SandboxScripts.Where(s => s.UserId == userId).OrderByDescending(s => s.UpdatedAt)
        .Select(s => new SandboxScriptDto(s.Id, s.Name, s.Code, s.Stdin, s.CreatedAt, s.UpdatedAt)).ToListAsync();
});

mine.MapPost("/sandbox/scripts", async (SandboxScriptInput input, HttpContext ctx, AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(input.Name)) return Results.BadRequest(new { error = "Name is required." });
    var s = new SandboxScript { UserId = ctx.User.RequiredUserId(), Name = input.Name.Trim(), Code = input.Code, Stdin = input.Stdin ?? "" };
    db.SandboxScripts.Add(s);
    await db.SaveChangesAsync();
    return Results.Created($"/api/sandbox/scripts/{s.Id}", new SandboxScriptDto(s.Id, s.Name, s.Code, s.Stdin, s.CreatedAt, s.UpdatedAt));
});

mine.MapPut("/sandbox/scripts/{id:int}", async (int id, SandboxScriptInput input, HttpContext ctx, AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(input.Name)) return Results.BadRequest(new { error = "Name is required." });
    var userId = ctx.User.RequiredUserId();
    var s = await db.SandboxScripts.FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);
    if (s is null) return Results.NotFound();
    s.Name = input.Name.Trim();
    s.Code = input.Code;
    s.Stdin = input.Stdin ?? "";
    s.UpdatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync();
    return Results.Ok(new SandboxScriptDto(s.Id, s.Name, s.Code, s.Stdin, s.CreatedAt, s.UpdatedAt));
});

mine.MapDelete("/sandbox/scripts/{id:int}", async (int id, HttpContext ctx, AppDbContext db) =>
{
    var userId = ctx.User.RequiredUserId();
    var deleted = await db.SandboxScripts.Where(s => s.Id == id && s.UserId == userId).ExecuteDeleteAsync();
    return deleted == 0 ? Results.NotFound() : Results.NoContent();
});

app.Run();

/// <summary>
/// Databases created before EF migrations were introduced (EnsureCreated) have the tables but no migration
/// history, so Migrate() would fail. Such a file is renamed aside once and a fresh database is created.
/// </summary>
static void RetireLegacyDatabase(string dbPath, ILogger logger)
{
    if (!File.Exists(dbPath)) return;
    var tables = new HashSet<string>();
    // Pooling=false so the probe's file handle is really closed before the file is moved.
    using (var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath};Pooling=false"))
    {
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name IN ('Problems','__EFMigrationsHistory')";
        using var reader = cmd.ExecuteReader();
        while (reader.Read()) tables.Add(reader.GetString(0));
    }
    if (!tables.Contains("Problems") || tables.Contains("__EFMigrationsHistory")) return;

    var retired = $"{dbPath}.legacy-{DateTime.UtcNow:yyyyMMddHHmmss}";
    File.Move(dbPath, retired);
    foreach (var suffix in new[] { "-shm", "-wal" })
        if (File.Exists(dbPath + suffix)) File.Move(dbPath + suffix, retired + suffix);
    logger.LogWarning("Legacy database without migration history moved to {Path}; starting fresh", retired);
}

static async Task<Problem?> FindProblem(AppDbContext db, string idOrSlug)
{
    var query = db.Problems.Include(p => p.TestCases).Include(p => p.Translations);
    return int.TryParse(idOrSlug, out var id)
        ? await query.FirstOrDefaultAsync(p => p.Id == id)
        : await query.FirstOrDefaultAsync(p => p.Slug == idOrSlug);
}

static void ApplyInput(Problem problem, ProblemInput input)
{
    problem.Title = input.Title.Trim();
    problem.Description = input.Description;
    problem.Difficulty = input.Difficulty;
    problem.Category = input.Category;
    problem.StarterCode = input.StarterCode;
    problem.MethodName = input.MethodName.Trim();
    problem.ClassName = string.IsNullOrWhiteSpace(input.ClassName) ? "Solution" : input.ClassName.Trim();
    problem.TimeLimitMs = input.TimeLimitMs ?? problem.TimeLimitMs;
    if (input.DocLinks is not null)
        problem.DocLinksJson = JsonSerializer.Serialize(input.DocLinks.Select(d => d.Trim()).Where(d => d.Length > 0).Distinct().ToList());
    if (input.TestCases is not null)
        problem.TestCases = input.TestCases.Select((t, i) => new TestCase
            { Input = t.Input, Expected = t.Expected, IsHidden = t.IsHidden, Order = i }).ToList();
    problem.Translations = (input.Translations ?? [])
        .Where(kv => !string.IsNullOrWhiteSpace(kv.Value.Title) || !string.IsNullOrWhiteSpace(kv.Value.Description))
        .Select(kv => new ProblemTranslation
        {
            Locale = kv.Key.Trim().ToLowerInvariant(),
            Title = string.IsNullOrWhiteSpace(kv.Value.Title) ? input.Title.Trim() : kv.Value.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(kv.Value.Description) ? input.Description : kv.Value.Description,
        }).ToList();
}

static string? Validate(ProblemInput input, bool requireTestCases)
{
    if (string.IsNullOrWhiteSpace(input.Title)) return "Title is required.";
    if (string.IsNullOrWhiteSpace(input.Description)) return "Description is required.";
    if (string.IsNullOrWhiteSpace(input.StarterCode)) return "Starter code is required.";
    if (string.IsNullOrWhiteSpace(input.MethodName)) return "Method name is required.";
    if (input.TimeLimitMs is < 100 or > 30_000) return "Time limit must be between 100 and 30000 ms.";
    if (!Enum.IsDefined(input.Category)) return "Category must be School or Gym.";
    if (!Enum.IsDefined(input.Difficulty)) return "Difficulty must be Easy, Medium or Hard.";
    if (input.TestCases is null)
        return requireTestCases ? "Test cases are required." : null;

    var visible = input.TestCases.Count(t => !t.IsHidden);
    var hidden = input.TestCases.Count(t => t.IsHidden);
    if (visible != Mapping.VisibleCases || hidden != Mapping.HiddenCases)
        return $"A problem needs exactly {Mapping.VisibleCases} visible and {Mapping.HiddenCases} hidden test cases (got {visible} visible, {hidden} hidden).";
    foreach (var (t, i) in input.TestCases.Select((t, i) => (t, i)))
        if (ValidateTestCase(t) is { } error) return $"Test case {i + 1}: {error}";
    foreach (var (locale, _) in input.Translations ?? [])
        if (locale.Trim().Length is < 2 or > 5) return $"Translation locale '{locale}' is not valid.";
    return null;
}

static string? ValidateTestCase(TestCaseInput t)
{
    try
    {
        if (JsonNode.Parse(t.Input) is not JsonArray) return "Input must be a JSON array of arguments, e.g. [[1,2,3], 4].";
    }
    catch (JsonException ex) { return $"Input is not valid JSON: {ex.Message}"; }
    try { JsonNode.Parse(t.Expected); }
    catch (JsonException ex) { return $"Expected output is not valid JSON: {ex.Message}"; }
    return null;
}

static partial class Program
{
    internal static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };
}
