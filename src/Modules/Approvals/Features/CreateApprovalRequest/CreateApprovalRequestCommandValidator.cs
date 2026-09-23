using FluentValidation;

namespace NafasLand.Admin.Modules.Approvals.Features.CreateApprovalRequest;

internal sealed class CreateApprovalRequestCommandValidator : AbstractValidator<CreateApprovalRequestCommand>
{
    public CreateApprovalRequestCommandValidator()
    {
        RuleFor(command => command.RequestType).NotEmpty();
        RuleFor(command => command.TargetEntityType).NotEmpty();
        RuleFor(command => command.TargetEntityId).NotEmpty();
        RuleFor(command => command.Reason).NotEmpty().WithMessage("دلیل درخواست الزامی است.").MaximumLength(2000);
        RuleFor(command => command.PayloadJson).NotEmpty();
    }
}
