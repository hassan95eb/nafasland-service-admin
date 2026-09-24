namespace NafasLand.Admin.Shared.Kernel.Errors;

public sealed class PortalUnavailableException : Exception
{
    public PortalUnavailableException(Exception? innerException = null)
        : base("ارتباط با سامانهٔ نفس‌لند ممکن نیست. لطفاً دوباره تلاش کنید.", innerException)
    {
    }
}
