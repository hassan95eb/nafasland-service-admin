using FluentValidation;

namespace NafasLand.Admin.Modules.Catalog.Features.UpdateProduct;

internal sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(command => command.ProductId).NotEmpty();
        RuleFor(command => command.LastKnownVersion).NotEmpty().WithMessage("نسخهٔ آخرین مشاهدهٔ محصول الزامی است.");
        RuleFor(command => command.Title).NotEmpty().MaximumLength(500);
        RuleFor(command => command.Contents)
            .Must(values => values.Select(value => value.Name).Distinct(StringComparer.Ordinal).Count() == values.Count)
            .WithMessage("نام بخش‌های محتوا باید یکتا باشد.");
        RuleFor(command => command.Fields)
            .Must(values => values.Select(value => value.Name).Distinct(StringComparer.Ordinal).Count() == values.Count)
            .WithMessage("نام فیلدها باید یکتا باشد.");
        RuleForEach(command => command.CategoryIds).GreaterThan(0);
        RuleForEach(command => command.FilterIds).GreaterThan(0);
    }
}
