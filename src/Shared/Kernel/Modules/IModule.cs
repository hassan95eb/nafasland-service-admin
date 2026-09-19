using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NafasLand.Admin.Shared.Kernel.Permissions;

namespace NafasLand.Admin.Shared.Kernel.Modules;

/// <summary>
/// نقطهٔ ثبت خودکار هر ماژول (ADR-005). هر ماژول یک پیاده‌سازی از این رابط دارد
/// که با اسکن اسمبلی توسط میزبان (Api) پیدا و فعال می‌شود.
/// </summary>
public interface IModule
{
    void RegisterServices(IServiceCollection services, IConfiguration config);

    void MapEndpoints(IEndpointRouteBuilder app);

    IReadOnlyList<PermissionDefinition> Permissions { get; }
}
