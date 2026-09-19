namespace NafasLand.Admin.Shared.Kernel.Errors;

/// <summary>
/// A command's validation error, broken down by field. Kernel does not depend on
/// FluentValidation; ValidationBehavior in Shared.Infrastructure maps
/// FluentValidation's result into this shape so it reaches 400 with a list of
/// errors (ADR-036).
/// </summary>
public sealed class CommandValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("درخواست نامعتبر است.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
