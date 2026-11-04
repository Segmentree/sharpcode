namespace SharpCode.Api;

/// <summary>
/// Minimal admin protection: mutating endpoints require the shared secret configured as Admin:Token
/// (environment variable Admin__Token) in the X-Admin-Token header.
/// </summary>
public static class AdminAuth
{
    public const string Header = "X-Admin-Token";

    public static bool IsAuthorized(HttpContext ctx, IConfiguration config)
    {
        var expected = config["Admin:Token"];
        if (string.IsNullOrEmpty(expected)) return false;
        if (!ctx.Request.Headers.TryGetValue(Header, out var provided)) return false;
        var a = System.Text.Encoding.UTF8.GetBytes(expected);
        var b = System.Text.Encoding.UTF8.GetBytes(provided.ToString());
        return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(a, b);
    }

    public static TBuilder RequireAdmin<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder =>
        builder.AddEndpointFilter(async (ctx, next) =>
        {
            var config = ctx.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            if (!IsAuthorized(ctx.HttpContext, config))
                return Results.Json(new { error = "Admin token missing or invalid." }, statusCode: StatusCodes.Status401Unauthorized);
            return await next(ctx);
        });
}
