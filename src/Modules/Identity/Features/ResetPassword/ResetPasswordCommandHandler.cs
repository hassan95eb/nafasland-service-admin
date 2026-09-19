using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Modules.Identity.Persistence;
using NafasLand.Admin.Modules.Identity.Security;
using NafasLand.Admin.Shared.Kernel.Errors;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Modules.Identity.Features.ResetPassword;

internal sealed class ResetPasswordCommandHandler(IdentityDbContext dbContext, IPasswordHasher passwordHasher)
    : ICommandHandler<ResetPasswordCommand, ResetPasswordResult>
{
    public async Task<ResetPasswordResult> HandleAsync(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.SingleOrDefaultAsync(u => u.Id == command.TargetUserId, cancellationToken)
            ?? throw new CommandValidationException(new Dictionary<string, string[]>
            {
                [nameof(command.TargetUserId)] = ["کاربر یافت نشد."],
            });

        var newHash = passwordHasher.Hash(command.NewPassword);
        user.SetPassword(newHash, passwordHasher.Algorithm, mustChangePassword: true);
        // A reset is also this team's recovery path back into a locked account.
        user.RegisterSuccessfulLogin();

        return new ResetPasswordResult();
    }
}
