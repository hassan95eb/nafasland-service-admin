using FluentValidation;

namespace NafasLand.Admin.Modules.Auditing.Features.ExportAuditLog;

internal sealed class ExportAuditLogCommandValidator : AbstractValidator<ExportAuditLogCommand>
{
    public ExportAuditLogCommandValidator()
    {
        RuleFor(command => command.Format)
            .Must(format => format is "csv" or "xlsx")
            .WithMessage("format باید csv یا xlsx باشد.");
    }
}
