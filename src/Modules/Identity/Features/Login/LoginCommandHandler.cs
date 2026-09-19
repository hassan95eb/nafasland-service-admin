using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Modules.Identity.Persistence;
using NafasLand.Admin.Modules.Identity.Security;
using NafasLand.Admin.Shared.Kernel.Errors;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Modules.Identity.Features.Login;

internal sealed class LoginCommandHandler(
    IdentityDbContext dbContext,
    IPasswordHasher passwordHasher,
    TimeProvider timeProvider,
    IHttpContextAccessor httpContextAccessor,
    IAntiforgery antiforgery)
    : ICommandHandler<LoginCommand, LoginResult>
{
    public async Task<LoginResult> HandleAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Username == command.Username, cancellationToken);

        // Deliberately the same message for "no such user" and "wrong password"
        // below, so a client can never tell which one it was.
        if (user is null)
        {
            throw new InvalidCredentialsException("نام کاربری یا رمز عبور اشتباه است.");
        }

        if (!user.IsActive)
        {
            throw new AccountInactiveException("این حساب غیرفعال شده است.");
        }

        var now = timeProvider.GetUtcNow();
        if (user.IsLocked(now))
        {
            throw new AccountLockedException("حساب به دلیل تلاش‌های ناموفق پیاپی قفل شده است؛ بعداً دوباره تلاش کنید.");
        }

        if (!passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            user.RegisterFailedLogin(now, AccountLockoutPolicy.MaxFailedAttempts, AccountLockoutPolicy.LockDuration);

            // Must survive even though this handler is about to throw: a normal
            // throw here would let TransactionBehavior roll the whole thing back,
            // silently discarding the failed-attempt counter and defeating the
            // lockout. Committing early first, then throwing, leaves
            // TransactionBehavior's own rollback a no-op (its transaction field
            // is already null by the time it runs).
            await dbContext.CommitTransactionAsync(cancellationToken);
            throw new InvalidCredentialsException("نام کاربری یا رمز عبور اشتباه است.");
        }

        user.RegisterSuccessfulLogin();
        await dbContext.SaveChangesAsync(cancellationToken);

        var httpContext = httpContextAccessor.HttpContext!;
        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
            ],
            CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        // SignInAsync only writes the response cookie; it does not update
        // HttpContext.User for the rest of THIS request (that only happens on
        // the next request, once the cookie is read back). The antiforgery
        // token generator embeds a hash of HttpContext.User's identity, so
        // without this line the token would be bound to the old anonymous
        // principal and every later request would fail antiforgery validation
        // with "meant for a different claims-based user".
        httpContext.User = principal;

        var antiforgeryTokens = antiforgery.GetAndStoreTokens(httpContext);

        return new LoginResult(user.Id, user.Username, user.MustChangePassword, antiforgeryTokens.RequestToken!);
    }
}
