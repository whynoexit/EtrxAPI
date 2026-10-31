using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using Etrx.API.Security;
using Etrx.Domain.Models;
using Etrx.Persistence.Databases;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Etrx.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(EtrxDbContext db, IPasswordHasher<UserCredential> hasher, IConfiguration config) : ControllerBase
{
    public record HandleRequest(
    [Required, StringLength(100)] string Handle);

    public record PasswordRequest(
    [Required, StringLength(100)] string Handle,
    [Required, StringLength(128, MinimumLength = 10)] string Password,
    bool Confirmed = false,
    [StringLength(512)] string? ActivationCode = null);

    private Task<User?> FindUser(string handle) => db.Users.AsNoTracking()
        .SingleOrDefaultAsync(u => u.Handle.ToLower() == handle.Trim().ToLower());

    [HttpPost("lookup"), AllowAnonymous, EnableRateLimiting("auth")]
    public async Task<IActionResult> Lookup(HandleRequest request)
    {
        var user = await FindUser(request.Handle);
        if (user == null) return NotFound(new { message = "Пользователь с таким хэндлом не найден." });
        var existing = await db.UserCredentials.AsNoTracking().SingleOrDefaultAsync(c => c.UserId == user.Id);
        var registered = existing != null;
        return Ok(new { user.Handle, hasPassword = registered,
            firstName = registered ? null : user.FirstName, lastName = registered ? null : user.LastName,
            grade = registered ? (int?)null : user.Grade,
            requiresActivationCode = AuthSecurity.IsAdmin(user.Handle, config) && existing?.AdminActivated != true });
    }

    [HttpPost("setup"), AllowAnonymous, EnableRateLimiting("auth")]
    public async Task<IActionResult> Setup(PasswordRequest request)
    {
        var user = await FindUser(request.Handle);
        if (user == null) return NotFound(new { message = "Пользователь не найден." });
        if (!request.Confirmed) return BadRequest(new { message = "Сначала подтвердите свои данные." });
        if (await db.UserCredentials.AnyAsync(c => c.UserId == user.Id))
            return Conflict(new { message = "Пароль уже создан. Выполните вход." });
        if (AuthSecurity.IsAdmin(user.Handle, config))
        {
            if (!ValidActivationCode(request.ActivationCode))
                return StatusCode(403, new { message = "Неверный код активации администратора или он не настроен на сервере." });
        }
        var account = new UserCredential { UserId = user.Id, AdminActivated = AuthSecurity.IsAdmin(user.Handle, config) };
        account.PasswordHash = hasher.HashPassword(account, request.Password);
        db.UserCredentials.Add(account);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        { return Conflict(new { message = "Пароль уже создан. Выполните вход." }); }
        return await SignIn(user, account);
    }

    [HttpPost("login"), AllowAnonymous, EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(PasswordRequest request)
    {
        var user = await FindUser(request.Handle);
        var account = user == null ? null : await db.UserCredentials.SingleOrDefaultAsync(c => c.UserId == user.Id);
        if (account == null) return Unauthorized(new { message = "Неверный хэндл или пароль." });
        if (account.LockedUntil > DateTimeOffset.UtcNow)
            return StatusCode(429, new { message = "Слишком много попыток. Повторите через 15 минут." });
        var verified = hasher.VerifyHashedPassword(account, account.PasswordHash, request.Password);
        if (verified == PasswordVerificationResult.Failed)
        {
            // Atomic increment also counts concurrent attempts.
            await db.UserCredentials.Where(c => c.UserId == account.UserId).ExecuteUpdateAsync(set => set
                .SetProperty(c => c.FailedAttempts, c => c.FailedAttempts + 1)
                .SetProperty(c => c.LockedUntil, c => c.FailedAttempts >= 4 ? (DateTimeOffset?)DateTimeOffset.UtcNow.AddMinutes(15) : c.LockedUntil));
            return Unauthorized(new { message = "Неверный хэндл или пароль." });
        }
        if (AuthSecurity.IsAdmin(user!.Handle, config) && !account.AdminActivated)
        {
            if (!ValidActivationCode(request.ActivationCode))
                return StatusCode(403, new { message = "Неверный код активации администратора." });
            account.AdminActivated = true;
        }
        account.FailedAttempts = 0; account.LockedUntil = null;
        if (verified == PasswordVerificationResult.SuccessRehashNeeded)
            account.PasswordHash = hasher.HashPassword(account, request.Password);
        await db.SaveChangesAsync();
        return await SignIn(user!, account);
    }

    [HttpGet("me"), AllowAnonymous]
    public IActionResult Me() => Ok(new { authenticated = User.Identity?.IsAuthenticated == true,
        handle = User.Identity?.Name, isAdmin = User.IsInRole("Admin") });

    [HttpPost("logout"), AllowAnonymous]
    public async Task<IActionResult> Logout()
    {
        var newStamp = Guid.NewGuid().ToString("N");
        if (Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var id))
            await db.UserCredentials.Where(c => c.UserId == id).ExecuteUpdateAsync(set => set
                .SetProperty(c => c.SecurityStamp, newStamp));
        await HttpContext.SignOutAsync(AuthSecurity.Scheme);
        return NoContent();
    }

    private bool ValidActivationCode(string? code)
    {
        var expected = config["Auth:AdminActivationCode"];
        return !string.IsNullOrWhiteSpace(expected) && expected.Length >= 32 &&
            CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(expected)),
                SHA256.HashData(Encoding.UTF8.GetBytes(code ?? "")));
    }

    private async Task<IActionResult> SignIn(User user, UserCredential account)
    {
        await HttpContext.SignInAsync(AuthSecurity.Scheme, AuthSecurity.CreatePrincipal(user, account, config),
            new AuthenticationProperties { IsPersistent = true });
        return Ok(new { authenticated = true, handle = user.Handle, isAdmin = account.AdminActivated && AuthSecurity.IsAdmin(user.Handle, config) });
    }
}
