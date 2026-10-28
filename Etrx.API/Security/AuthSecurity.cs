using System.Security.Claims;
using Etrx.Domain.Models;
using Etrx.Persistence.Databases;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;

namespace Etrx.API.Security;

public static class AuthSecurity
{
    public const string Scheme = "EtrxSession";
    public static bool IsAdmin(string handle, IConfiguration config) =>
        !string.IsNullOrWhiteSpace(config["Auth:AdminHandle"]) &&
        string.Equals(handle, config["Auth:AdminHandle"]?.Trim(), StringComparison.OrdinalIgnoreCase);

    public static IServiceCollection AddEtrxAuthentication(this IServiceCollection services, IConfiguration config, bool development)
    {
        services.AddScoped<IPasswordHasher<UserCredential>, PasswordHasher<UserCredential>>();
        services.Configure<PasswordHasherOptions>(o => o.IterationCount = 210_000);
        services.AddAuthentication(Scheme).AddCookie(Scheme, options =>
        {
            options.Cookie.Name = "etrx.session";
            options.Cookie.HttpOnly = true;
            options.Cookie.Path = "/";
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = development ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
            options.ExpireTimeSpan = TimeSpan.FromHours(12);
            options.SlidingExpiration = false;
            options.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = 401; return Task.CompletedTask; };
            options.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = 403; return Task.CompletedTask; };
            options.Events.OnValidatePrincipal = async ctx =>
            {
                var db = ctx.HttpContext.RequestServices.GetRequiredService<EtrxDbContext>();
                if (!Guid.TryParse(ctx.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
                { ctx.RejectPrincipal(); return; }
                var account = await db.UserCredentials.AsNoTracking()
                    .Where(c => c.UserId == id).FirstOrDefaultAsync(ctx.HttpContext.RequestAborted);
                var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ctx.HttpContext.RequestAborted);
                if (account == null || user == null || account.SecurityStamp != ctx.Principal?.FindFirstValue("stamp"))
                { ctx.RejectPrincipal(); await ctx.HttpContext.SignOutAsync(Scheme); return; }
                ctx.ReplacePrincipal(CreatePrincipal(user, account, config));
            };
        });
        services.AddAuthorization(options => options.AddPolicy("Admin", policy => policy.RequireRole("Admin")));
        var origins = config.GetSection("Auth:AllowedOrigins").Get<string[]>() ?? [];
        services.AddCors(options => options.AddDefaultPolicy(policy =>
        {
            if (origins.Length > 0) policy.WithOrigins(origins).AllowCredentials().AllowAnyHeader().AllowAnyMethod();
        }));
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = 429;
            options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
                { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
        });
        return services;
    }

    public static ClaimsPrincipal CreatePrincipal(User user, UserCredential account, IConfiguration config)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Handle), new("stamp", account.SecurityStamp) };
        if (account.AdminActivated && IsAdmin(user.Handle, config)) claims.Add(new(ClaimTypes.Role, "Admin"));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme));
    }
}
