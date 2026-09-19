namespace NafasLand.Admin.Shared.Kernel.Permissions;

/// <summary>
/// معرفی یک permission توسط ماژول صاحبش (ADR-001، ADR-005). کلید به فرمت
/// <c>&lt;resource&gt;.&lt;action&gt;</c> است و به‌صورت const داخل خود ماژول تعریف می‌شود.
/// </summary>
public sealed record PermissionDefinition(string Key, string DisplayName);

/// <summary>
/// یک command با پیاده‌سازی این رابط اعلام می‌کند به چه permission ای نیاز دارد.
/// commandی که این رابط را پیاده نکند، طبق پیش‌فرض بسته (ADR-006) توسط
/// AuthorizationBehavior رد می‌شود.
/// </summary>
public interface IRequiresPermission
{
    string RequiredPermission { get; }
}
