namespace NafasLand.Admin.Shared.Kernel.Errors;

/// <summary>
/// یک command بدون permission تعریف‌شده یا کاربر بدون آن permission رد شده
/// است (پیش‌فرض بسته، ADR-006). به ۴۰۳ با ProblemDetails map می‌شود.
/// </summary>
public sealed class AuthorizationDeniedException(string message) : Exception(message)
{
}
