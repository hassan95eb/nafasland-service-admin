using Microsoft.AspNetCore.Http;
using NafasLand.Admin.Modules.Identity.Persistence;
using NafasLand.Admin.Modules.Identity.Security;
using NafasLand.Admin.Shared.Kernel.Auditing;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Modules.Identity.Features.CreateUser;

internal sealed class CreateUserCommandHandler(
    IdentityDbContext dbContext,
    IPasswordHasher passwordHasher,
    TimeProvider timeProvider,
    IHttpContextAccessor httpContextAccessor,
    IAuditContext auditContext)
    : ICommandHandler<CreateUserCommand, CreateUserResult>
{
    public Task<CreateUserResult> HandleAsync(CreateUserCommand command, CancellationToken cancellationToken)
    {
        var createdByUserId = CurrentUserAccessor.GetUserId(httpContextAccessor.HttpContext!.User);
        var passwordHash = passwordHasher.Hash(command.InitialPassword);

        var user = AppUser.Create(
            command.Username,
            passwordHash,
            passwordHasher.Algorithm,
            timeProvider.GetUtcNow(),
            mustChangePassword: true,
            isProtected: false,
            createdByUserId);

        dbContext.Users.Add(user);
        auditContext.SetEntityId(user.Id.ToString());
        auditContext.SetAfter(new { user.Username, user.IsActive, user.IsProtected, user.MustChangePassword });

        return Task.FromResult(new CreateUserResult(user.Id, user.Username));
    }
}
