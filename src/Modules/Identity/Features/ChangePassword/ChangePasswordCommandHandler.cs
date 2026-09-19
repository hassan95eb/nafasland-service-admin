using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Modules.Identity.Persistence;
using NafasLand.Admin.Modules.Identity.Security;
using NafasLand.Admin.Shared.Kernel.Errors;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Modules.Identity.Features.ChangePassword;

internal sealed class ChangePasswordCommandHandler(
    IdentityDbContext dbContext,
    IPasswordHasher passwordHasher,
    IHttpContextAccessor httpContextAccessor)
    : ICommandHandler<ChangePasswordCommand, ChangePasswordResult>
{
    public async Task<ChangePasswordResult> HandleAsync(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        var userId = CurrentUserAccessor.GetUserId(httpContextAccessor.HttpContext!.User);
        var user = await dbContext.Users.SingleAsync(u => u.Id == userId, cancellationToken);

        if (!passwordHasher.Verify(command.CurrentPassword, user.PasswordHash))
        {
            throw new InvalidCredentialsException("رمز عبور فعلی اشتباه است.");
        }

        var newHash = passwordHasher.Hash(command.NewPassword);
        user.SetPassword(newHash, passwordHasher.Algorithm, mustChangePassword: false);

        return new ChangePasswordResult();
    }
}
