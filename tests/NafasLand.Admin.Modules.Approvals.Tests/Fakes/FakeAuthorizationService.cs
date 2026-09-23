using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using NafasLand.Admin.Shared.Infrastructure.Authorization;

namespace NafasLand.Admin.Modules.Approvals.Tests.Fakes;

internal sealed class FakeAuthorizationService(Func<string, bool> isAuthorized) : IAuthorizationService
{
    public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, IEnumerable<IAuthorizationRequirement> requirements)
    {
        foreach (var requirement in requirements)
        {
            if (requirement is PermissionRequirement permissionRequirement && !isAuthorized(permissionRequirement.Permission))
            {
                return Task.FromResult(AuthorizationResult.Failed());
            }
        }

        return Task.FromResult(AuthorizationResult.Success());
    }

    public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, string policyName) =>
        Task.FromResult(AuthorizationResult.Success());
}
