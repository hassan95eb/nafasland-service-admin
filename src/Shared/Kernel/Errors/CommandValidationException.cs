namespace NafasLand.Admin.Shared.Kernel.Errors;

/// <summary>
/// خطای اعتبارسنجی یک command، به تفکیک فیلد. Kernel به FluentValidation
/// وابسته نیست؛ ValidationBehavior در Shared.Infrastructure نتیجهٔ
/// FluentValidation را به این شکل map می‌کند تا به ۴۰۰ با فهرست خطا برسد
/// (ADR-036).
/// </summary>
public sealed class CommandValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("درخواست نامعتبر است.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
