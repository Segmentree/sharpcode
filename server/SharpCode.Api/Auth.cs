using System.Security.Claims;
using System.Text;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SharpCode.Api.Data;

namespace SharpCode.Api;

/// <summary>
/// Sign-in flow: the client gets a Google ID token from Google Identity Services, posts it to /api/auth/google,
/// the server verifies it against Google's keys, upserts the user and issues its own bearer token (HS256 JWT)
/// that the client sends on every call. Tokens last <see cref="TokenLifetime"/> and are re-issued on use.
/// </summary>
public sealed class AuthService(IConfiguration config, IWebHostEnvironment env, ILogger<AuthService> logger)
{
    public static readonly TimeSpan TokenLifetime = TimeSpan.FromDays(30);
    /// <summary>A token older than this is replaced on the next /auth/me so active users never get logged out.</summary>
    public static readonly TimeSpan RefreshAfter = TimeSpan.FromDays(7);

    private readonly JsonWebTokenHandler _handler = new();
    private readonly SymmetricSecurityKey _key = new(Encoding.UTF8.GetBytes(ResolveSecret(config, env, logger)));

    public string? GoogleClientId => config["Google:ClientId"];

    public bool Enabled => !string.IsNullOrWhiteSpace(GoogleClientId);

    public SymmetricSecurityKey SigningKey => _key;

    private static string ResolveSecret(IConfiguration config, IWebHostEnvironment env, ILogger logger)
    {
        var secret = config["Auth:Secret"];
        if (!string.IsNullOrWhiteSpace(secret) && secret.Length >= 32) return secret;
        if (env.IsDevelopment())
        {
            logger.LogWarning("Auth:Secret is not configured; using a fixed development secret");
            return "sharpcode-development-secret-do-not-use-in-production";
        }
        throw new InvalidOperationException("Auth:Secret must be configured (at least 32 characters) outside Development.");
    }

    /// <summary>Verifies a Google ID token for this app's client id. Returns null when it is not valid.</summary>
    public async Task<GoogleJsonWebSignature.Payload?> ValidateGoogleTokenAsync(string idToken)
    {
        if (!Enabled) return null;
        try
        {
            return await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [GoogleClientId!],
            });
        }
        catch (InvalidJwtException ex)
        {
            logger.LogInformation("Rejected Google token: {Reason}", ex.Message);
            return null;
        }
    }

    public string CreateToken(User user)
    {
        var now = DateTime.UtcNow;
        return _handler.CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([new Claim("sub", user.Id.ToString()), new Claim("name", user.Name)]),
            IssuedAt = now,
            NotBefore = now,
            Expires = now + TokenLifetime,
            Issuer = "sharpcode",
            Audience = "sharpcode",
            SigningCredentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256),
        });
    }

    public static TokenValidationParameters ValidationParameters(SymmetricSecurityKey key) => new()
    {
        ValidIssuer = "sharpcode",
        ValidAudience = "sharpcode",
        IssuerSigningKey = key,
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1),
        NameClaimType = "name",
    };
}

public static class AuthExtensions
{
    public static IServiceCollection AddSharpCodeAuth(this IServiceCollection services)
    {
        services.AddSingleton<AuthService>();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<AuthService>((o, auth) =>
            {
                o.MapInboundClaims = false; // keep "sub" as "sub"
                o.TokenValidationParameters = AuthService.ValidationParameters(auth.SigningKey);
            });
        services.AddAuthorization();
        return services;
    }

    /// <summary>The signed-in user's id, or null for anonymous requests.</summary>
    public static int? UserId(this ClaimsPrincipal principal) =>
        int.TryParse(principal.FindFirstValue("sub"), out var id) ? id : null;

    /// <summary>The signed-in user's id; endpoints that call this are behind RequireAuthorization so it is always present.</summary>
    public static int RequiredUserId(this ClaimsPrincipal principal) =>
        principal.UserId() ?? throw new InvalidOperationException("No authenticated user.");

    /// <summary>When the token was issued, from its "iat" claim.</summary>
    public static DateTime? IssuedAt(this ClaimsPrincipal principal) =>
        long.TryParse(principal.FindFirstValue("iat"), out var iat) ? DateTimeOffset.FromUnixTimeSeconds(iat).UtcDateTime : null;

    public static async Task<User?> CurrentUserAsync(this AppDbContext db, ClaimsPrincipal principal, CancellationToken ct = default)
    {
        var id = principal.UserId();
        return id is null ? null : await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
    }
}
