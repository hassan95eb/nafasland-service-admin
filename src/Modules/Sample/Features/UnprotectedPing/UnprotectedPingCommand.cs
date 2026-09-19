using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Modules.Sample.Features.UnprotectedPing;

/// <summary>
/// عمداً <see cref="NafasLand.Admin.Shared.Kernel.Permissions.IRequiresPermission"/>
/// را پیاده نمی‌کند تا ثابت شود AuthorizationBehavior چنین commandی را طبق
/// پیش‌فرض بسته رد می‌کند، صرف‌نظر از دسترسی‌های کاربر (ADR-006).
/// </summary>
internal sealed record UnprotectedPingCommand : ICommand<UnprotectedPingResult>;

internal sealed record UnprotectedPingResult(string Message);
