using NafasLand.Admin.Shared.Kernel.Messaging;

// namespace ساختگی برای شبیه‌سازی یک ماژول واقعی در تست TransactionBehavior،
// چون ModuleNameResolver نام ماژول را از پیشوند "NafasLand.Admin.Modules." استخراج می‌کند.
namespace NafasLand.Admin.Modules.FakeModuleForTests;

internal sealed record FakeModuleCommand : ICommand<string>;
