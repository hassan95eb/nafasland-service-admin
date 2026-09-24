namespace NafasLand.Admin.Modules.Returns;

/// <summary>
/// Permission keys for the Returns module (ADR-005, ADR-054). Seeing your own
/// returns is not a permission — like "my approval requests" (ADR-010) every
/// signed-in user has it, so the list/detail endpoints scope by
/// RegisteredByUserId instead of rejecting.
/// </summary>
internal static class ReturnsPermissions
{
    public const string Request = "returns.request";
    public const string Review = "returns.review";
    public const string ReadAll = "returns.read.all";
}
