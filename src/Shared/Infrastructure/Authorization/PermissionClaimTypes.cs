namespace NafasLand.Admin.Shared.Infrastructure.Authorization;

public static class PermissionClaimTypes
{
    /// <summary>
    /// نوع claim ای که permissionهای مؤثر کاربر در آن نگه داشته می‌شود.
    /// محاسبهٔ permission مؤثر واقعی (نقش‌ها + Grant − Deny) کار گام ۱ است؛
    /// در این مرحله فقط از روی claim خوانده می‌شود.
    /// </summary>
    public const string Permission = "permission";
}
