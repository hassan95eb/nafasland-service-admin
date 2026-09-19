using Microsoft.AspNetCore.Authorization;

namespace NafasLand.Admin.Shared.Infrastructure.Authorization;

public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}
