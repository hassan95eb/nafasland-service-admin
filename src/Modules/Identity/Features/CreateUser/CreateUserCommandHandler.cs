using Microsoft.AspNetCore.Http;
using NafasLand.Admin.Modules.Identity.Persistence;
using NafasLand.Admin.Modules.Identity.Security;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Modules.Identity.Features.CreateUser;

internal sealed class CreateUserCommandHandler(
    IdentityDbContext dbContext,
    IPasswordHasher passwordHasher,
    TimeProvider timeProvider,
    IHttpContextAccessor httpContextAccessor)
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

        return Task.FromResult(new CreateUserResult(user.Id, user.Username));
    }
}
