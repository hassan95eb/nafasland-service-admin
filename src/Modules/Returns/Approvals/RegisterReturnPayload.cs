using System.Text.Json;
using FluentValidation;
using NafasLand.Admin.Shared.Kernel.Errors;

namespace NafasLand.Admin.Modules.Returns.Approvals;

/// <summary>
/// Everything the browser sends for a return (ADR-054): which order, why, and
/// when it came back. The order's contents are never taken from the browser —
/// the executor re-reads them from the portal. Mirrored by the frontend's zod
/// schema in features/returns/schemas.
/// </summary>
internal sealed record RegisterReturnPayload(long OrderId, string? Reason, DateOnly? ReturnDate)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public const int ReasonMaxLength = 2000;

    /// <exception cref="CommandValidationException">The payload is malformed or breaks a field rule.</exception>
    public static RegisterReturnPayload Parse(string payloadJson, IValidator<RegisterReturnPayload> validator)
    {
        RegisterReturnPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<RegisterReturnPayload>(payloadJson, JsonOptions);
        }
        catch (JsonException)
        {
            payload = null;
        }

        if (payload is null)
        {
            throw new CommandValidationException(new Dictionary<string, string[]>
            {
                ["payload"] = ["بدنهٔ درخواست مرجوعی نامعتبر است."],
            });
        }

        var result = validator.Validate(payload);
        if (!result.IsValid)
        {
            throw new CommandValidationException(result.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray()));
        }

        return payload with { Reason = payload.Reason!.Trim() };
    }
}

internal sealed class RegisterReturnPayloadValidator : AbstractValidator<RegisterReturnPayload>
{
    public RegisterReturnPayloadValidator(TimeProvider timeProvider)
    {
        RuleFor(payload => payload.OrderId)
            .GreaterThan(0).WithMessage("شمارهٔ سفارش باید یک عدد صحیح مثبت باشد.");

        RuleFor(payload => payload.Reason)
            .Must(reason => !string.IsNullOrWhiteSpace(reason)).WithMessage("علت مرجوعی الزامی است.")
            .MaximumLength(RegisterReturnPayload.ReasonMaxLength).WithMessage("علت مرجوعی حداکثر ۲۰۰۰ کاراکتر است.");

        RuleFor(payload => payload.ReturnDate)
            .NotNull().WithMessage("تاریخ عودت الزامی است.")
            .Must(date => date is null || date.Value <= ReturnRules.TehranToday(timeProvider))
            .WithMessage("تاریخ عودت نمی‌تواند در آینده باشد.");
    }
}
