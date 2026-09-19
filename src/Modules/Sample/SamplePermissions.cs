namespace NafasLand.Admin.Modules.Sample;

/// <summary>
/// Permission keys for the Sample module, as consts inside the module itself
/// (ADR-005), not in a central enum.
/// </summary>
internal static class SamplePermissions
{
    public const string Ping = "sample.ping";
}
