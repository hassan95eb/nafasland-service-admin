using NafasLand.Admin.Shared.Kernel.Messaging;

// A fake namespace simulating a real module in the TransactionBehavior test,
// since ModuleNameResolver extracts the module name from the
// "NafasLand.Admin.Modules." prefix.
namespace NafasLand.Admin.Modules.FakeModuleForTests;

internal sealed record FakeModuleCommand : ICommand<string>;
