namespace NafasLand.Admin.Shared.Infrastructure.Authorization;

public static class PermissionClaimTypes
{
    /// <summary>
    /// The claim type holding a user's effective permissions. Computing the real
    /// effective permission set (roles + Grant − Deny) is step 1's job; at this
    /// stage it is simply read from the claim.
    /// </summary>
    public const string Permission = "permission";
}
