using FluentValidation;

namespace NafasLand.Admin.Modules.Approvals.Features.RejectApprovalRequest;

internal sealed class RejectApprovalRequestCommandValidator : AbstractValidator<RejectApprovalRequestCommand>
{
    public RejectApprovalRequestCommandValidator()
    {
        RuleFor(command => command.Note).NotEmpty().WithMessage("یادداشت رد الزامی است.").MaximumLength(2000);
    }
}
