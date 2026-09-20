namespace NafasLand.Admin.Shared.Kernel.Errors;

public sealed class PortalBusyException : Exception
{
    public PortalBusyException()
        : base("سامانه شلوغ است، دوباره تلاش کنید.")
    {
    }
}
