using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
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
builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    o.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseCors();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    Seed.Apply(db);
}

var api = app.MapGroup("/api");

// ---------- Problems ----------

api.MapGet("/problems", async (AppDbContext db) =>
{
    var solved = await db.Submissions.Where(s => s.Verdict == Verdict.Accepted).Select(s => s.ProblemId).Distinct().ToListAsync();
    var problems = await db.Problems.OrderBy(p => p.Id)
        .Select(p => new ProblemSummaryDto(p.Id, p.Slug, p.Title, p.Difficulty, p.TestCases.Count, false))
        .ToListAsync();
    return problems.Select(p => p with { Solved = solved.Contains(p.Id) });
});

api.MapGet("/problems/{idOrSlug}", async (string idOrSlug, AppDbContext db) =>
{
    var p = await FindProblem(db, idOrSlug);
    return p is null ? Results.NotFound() : Results.Ok(p.ToDto(includeHiddenDetails: false));
});

// Admin view: includes the hidden test cases' input/expected.
api.MapGet("/problems/{idOrSlug}/full", async (string idOrSlug, AppDbContext db) =>
{
    var p = await FindProblem(db, idOrSlug);
    return p is null ? Results.NotFound() : Results.Ok(p.ToDto(includeHiddenDetails: true));
});

api.MapPost("/problems", async (ProblemInput input, AppDbContext db) =>
{
    if (Validate(input) is { } error) return Results.BadRequest(new { error });
    var slug = string.IsNullOrWhiteSpace(input.Slug) ? Mapping.Slugify(input.Title) : Mapping.Slugify(input.Slug);
    if (await db.Problems.AnyAsync(p => p.Slug == slug)) return Results.Conflict(new { error = $"Slug '{slug}' already exists." });

    var problem = new Problem
    {
        Slug = slug, Title = input.Title.Trim(), Description = input.Description, Difficulty = input.Difficulty,
        StarterCode = input.StarterCode, MethodName = input.MethodName.Trim(),
        ClassName = string.IsNullOrWhiteSpace(input.ClassName) ? "Solution" : input.ClassName.Trim(),
        TimeLimitMs = input.TimeLimitMs ?? 2000,
        TestCases = (input.TestCases ?? []).Select((t, i) => new TestCase
            { Input = t.Input, Expected = t.Expected, IsHidden = t.IsHidden, Order = i }).ToList(),
    };
    db.Problems.Add(problem);
    await db.SaveChangesAsync();
    return Results.Created($"/api/problems/{problem.Id}", problem.ToDto(includeHiddenDetails: true));
});

api.MapPut("/problems/{id:int}", async (int id, ProblemInput input, AppDbContext db) =>
{
    if (Validate(input) is { } error) return Results.BadRequest(new { error });
    var problem = await db.Problems.Include(p => p.TestCases).FirstOrDefaultAsync(p => p.Id == id);
    if (problem is null) return Results.NotFound();
    var slug = string.IsNullOrWhiteSpace(input.Slug) ? problem.Slug : Mapping.Slugify(input.Slug);
    if (await db.Problems.AnyAsync(p => p.Slug == slug && p.Id != id)) return Results.Conflict(new { error = $"Slug '{slug}' already exists." });

    problem.Slug = slug;
    problem.Title = input.Title.Trim();
    problem.Description = input.Description;
    problem.Difficulty = input.Difficulty;
    problem.StarterCode = input.StarterCode;
    problem.MethodName = input.MethodName.Trim();
    problem.ClassName = string.IsNullOrWhiteSpace(input.ClassName) ? "Solution" : input.ClassName.Trim();
    problem.TimeLimitMs = input.TimeLimitMs ?? problem.TimeLimitMs;
    if (input.TestCases is not null)
    {
        // Replace the whole set: simplest consistent semantics for an editor that submits the full list.
        db.TestCases.RemoveRange(problem.TestCases);
        problem.TestCases = input.TestCases.Select((t, i) => new TestCase
            { Input = t.Input, Expected = t.Expected, IsHidden = t.IsHidden, Order = i }).ToList();
    }
    await db.SaveChangesAsync();
    return Results.Ok(problem.ToDto(includeHiddenDetails: true));
});

api.MapDelete("/problems/{id:int}", async (int id, AppDbContext db) =>
{
    var deleted = await db.Problems.Where(p => p.Id == id).ExecuteDeleteAsync();
    return deleted == 0 ? Results.NotFound() : Results.NoContent();
});

// ---------- Test cases ----------

api.MapPost("/problems/{id:int}/testcases", async (int id, TestCaseInput input, AppDbContext db) =>
{
    if (ValidateTestCase(input) is { } error) return Results.BadRequest(new { error });
    var problem = await db.Problems.Include(p => p.TestCases).FirstOrDefaultAsync(p => p.Id == id);
    if (problem is null) return Results.NotFound();
    var tc = new TestCase
    {
        Input = input.Input, Expected = input.Expected, IsHidden = input.IsHidden,
        Order = problem.TestCases.Count == 0 ? 0 : problem.TestCases.Max(t => t.Order) + 1,
    };
    problem.TestCases.Add(tc);
    await db.SaveChangesAsync();
    return Results.Created($"/api/testcases/{tc.Id}", tc.ToDto(true));
});

api.MapPut("/testcases/{id:int}", async (int id, TestCaseInput input, AppDbContext db) =>
{
    if (ValidateTestCase(input) is { } error) return Results.BadRequest(new { error });
    var tc = await db.TestCases.FindAsync(id);
    if (tc is null) return Results.NotFound();
    tc.Input = input.Input;
    tc.Expected = input.Expected;
    tc.IsHidden = input.IsHidden;
    await db.SaveChangesAsync();
    return Results.Ok(tc.ToDto(true));
});

api.MapDelete("/testcases/{id:int}", async (int id, AppDbContext db) =>
{
    var deleted = await db.TestCases.Where(t => t.Id == id).ExecuteDeleteAsync();
    return deleted == 0 ? Results.NotFound() : Results.NoContent();
});

// ---------- Judge ----------

// Run: executes only the visible (sample) cases, nothing is persisted. Quick feedback loop while coding.
api.MapPost("/problems/{id:int}/run", async (int id, CodeInput input, AppDbContext db, JudgeService judge, CancellationToken ct) =>
{
    var problem = await db.Problems.Include(p => p.TestCases).FirstOrDefaultAsync(p => p.Id == id, ct);
    if (problem is null) return Results.NotFound();
    var cases = problem.TestCases.Where(t => !t.IsHidden).OrderBy(t => t.Order).ThenBy(t => t.Id).ToList();
    var result = await judge.JudgeAsync(problem, cases, input.Code, revealHidden: false, ct);
    return Results.Ok(result);
});

// Submit: executes every case (hidden ones included) and records the submission.
api.MapPost("/problems/{id:int}/submit", async (int id, CodeInput input, AppDbContext db, JudgeService judge, CancellationToken ct) =>
{
    var problem = await db.Problems.Include(p => p.TestCases).FirstOrDefaultAsync(p => p.Id == id, ct);
    if (problem is null) return Results.NotFound();
    var cases = problem.TestCases.OrderBy(t => t.Order).ThenBy(t => t.Id).ToList();
    var result = await judge.JudgeAsync(problem, cases, input.Code, revealHidden: false, ct);

    var submission = new Submission
    {
        ProblemId = problem.Id, Code = input.Code, Verdict = result.Verdict,
        PassedCount = result.PassedCount, TotalCount = result.TotalCount,
        ResultJson = JsonSerializer.Serialize(result, JsonOpts),
    };
    db.Submissions.Add(submission);
    await db.SaveChangesAsync(ct);
    return Results.Ok(new SubmissionDto(submission.Id, submission.ProblemId, submission.Verdict, submission.PassedCount,
        submission.TotalCount, submission.CreatedAt, submission.Code, result));
});

// ---------- Editor support ----------

api.MapPost("/complete", async (CompleteInput input, CodeCompletion completion, CancellationToken ct) =>
    Results.Ok(await completion.CompleteAsync(input.Code ?? "", input.Position, ct)));

api.MapGet("/problems/{id:int}/submissions", async (int id, AppDbContext db) =>
    await db.Submissions.Where(s => s.ProblemId == id).OrderByDescending(s => s.Id).Take(50)
        .Select(s => new SubmissionDto(s.Id, s.ProblemId, s.Verdict, s.PassedCount, s.TotalCount, s.CreatedAt, s.Code, null))
        .ToListAsync());

api.MapGet("/submissions/{id:int}", async (int id, AppDbContext db) =>
{
    var s = await db.Submissions.FindAsync(id);
    if (s is null) return Results.NotFound();
    var result = JsonSerializer.Deserialize<JudgeResult>(s.ResultJson, JsonOpts);
    return Results.Ok(new SubmissionDto(s.Id, s.ProblemId, s.Verdict, s.PassedCount, s.TotalCount, s.CreatedAt, s.Code, result));
});

app.Run();

static async Task<Problem?> FindProblem(AppDbContext db, string idOrSlug) =>
    int.TryParse(idOrSlug, out var id)
        ? await db.Problems.Include(p => p.TestCases).FirstOrDefaultAsync(p => p.Id == id)
        : await db.Problems.Include(p => p.TestCases).FirstOrDefaultAsync(p => p.Slug == idOrSlug);

static string? Validate(ProblemInput input)
{
    if (string.IsNullOrWhiteSpace(input.Title)) return "Title is required.";
    if (string.IsNullOrWhiteSpace(input.Description)) return "Description is required.";
    if (string.IsNullOrWhiteSpace(input.StarterCode)) return "Starter code is required.";
    if (string.IsNullOrWhiteSpace(input.MethodName)) return "Method name is required.";
    if (input.TimeLimitMs is < 100 or > 30_000) return "Time limit must be between 100 and 30000 ms.";
    foreach (var (t, i) in (input.TestCases ?? []).Select((t, i) => (t, i)))
        if (ValidateTestCase(t) is { } error) return $"Test case {i + 1}: {error}";
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
