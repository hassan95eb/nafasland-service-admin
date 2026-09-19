namespace NafasLand.Admin.Modules.Sample;

/// <summary>
/// کلید permission های ماژول Sample، به‌صورت const داخل خود ماژول (ADR-005)،
/// نه در یک enum مرکزی.
/// </summary>
internal static class SamplePermissions
{
    public const string Ping = "sample.ping";
}
