using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Modules.Sample.Features.UnprotectedPing;

/// <summary>
/// Deliberately does not implement
/// <see cref="NafasLand.Admin.Shared.Kernel.Permissions.IRequiresPermission"/>, to
/// prove that AuthorizationBehavior rejects such a command under the
/// default-closed rule, regardless of the user's permissions (ADR-006).
/// </summary>
internal sealed record UnprotectedPingCommand : ICommand<UnprotectedPingResult>;

internal sealed record UnprotectedPingResult(string Message);
