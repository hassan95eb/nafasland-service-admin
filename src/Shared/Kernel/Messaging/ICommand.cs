namespace NafasLand.Admin.Shared.Kernel.Messaging;

/// <summary>
/// یک عملیات تغییردهنده که از pipeline اجباری عبور می‌کند (ADR-006).
/// </summary>
/// <typeparam name="TResponse">نوع پاسخ handler.</typeparam>
public interface ICommand<TResponse>
{
}
