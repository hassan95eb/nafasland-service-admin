using FluentValidation;

namespace NafasLand.Admin.Modules.Catalog.Features.UpdateVariantPriceAndInventory;

internal sealed class UpdateVariantPriceAndInventoryCommandValidator
    : AbstractValidator<UpdateVariantPriceAndInventoryCommand>
{
    public UpdateVariantPriceAndInventoryCommandValidator()
    {
        RuleFor(command => command.VariantId)
            .NotEmpty().WithMessage("شناسهٔ واریانت الزامی است.");
        RuleFor(command => command.NewPrice)
            .GreaterThanOrEqualTo(0).WithMessage("قیمت نمی‌تواند منفی باشد.");
        RuleFor(command => command.NewStock)
            .GreaterThanOrEqualTo(0).WithMessage("موجودی نمی‌تواند منفی باشد.");
    }
}
