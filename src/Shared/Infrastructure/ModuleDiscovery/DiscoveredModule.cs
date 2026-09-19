using NafasLand.Admin.Shared.Kernel.Modules;

namespace NafasLand.Admin.Shared.Infrastructure.ModuleDiscovery;

public sealed record DiscoveredModule(string Name, IModule Module);
