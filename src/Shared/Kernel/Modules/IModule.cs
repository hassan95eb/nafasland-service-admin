using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NafasLand.Admin.Shared.Kernel.Permissions;

namespace NafasLand.Admin.Shared.Kernel.Modules;

/// <summary>
/// Automatic registration point for each module (ADR-005). Each module has one
/// implementation of this interface, discovered and activated by the host (Api)
/// via assembly scanning.
/// </summary>
public interface IModule
{
    void RegisterServices(IServiceCollection services, IConfiguration config);

    void MapEndpoints(IEndpointRouteBuilder app);

    IReadOnlyList<PermissionDefinition> Permissions { get; }
}
