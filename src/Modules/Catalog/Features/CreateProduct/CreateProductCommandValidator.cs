using FluentValidation;

namespace NafasLand.Admin.Modules.Catalog.Features.CreateProduct;

internal sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(command => command.Title).NotEmpty().MaximumLength(500);
        RuleFor(command => command.Contents).Must(HaveUniqueNames).WithMessage("نام بخش‌های محتوا باید یکتا باشد.");
        RuleFor(command => command.Fields).Must(HaveUniqueNames).WithMessage("نام فیلدها باید یکتا باشد.");
        RuleFor(command => command.Attributes).Must(HaveUniqueAttributeNames).WithMessage("نام ویژگی‌ها باید یکتا باشد.");
        RuleForEach(command => command.CategoryIds).GreaterThan(0);
        RuleForEach(command => command.FilterIds).GreaterThan(0);
        RuleFor(command => command.Variants).NotEmpty().WithMessage("حداقل یک واریانت الزامی است.");
        RuleForEach(command => command.Variants).ChildRules(variant =>
        {
            variant.RuleFor(value => value.Title).NotEmpty().MaximumLength(500);
            variant.RuleFor(value => value.Price).GreaterThanOrEqualTo(0).When(value => value.Price.HasValue);
            variant.RuleFor(value => value.ComparePrice).GreaterThanOrEqualTo(0).When(value => value.ComparePrice.HasValue);
            variant.RuleFor(value => value.Stock).GreaterThanOrEqualTo(0).When(value => value.Stock.HasValue);
            variant.RuleFor(value => value.Minimum).GreaterThanOrEqualTo(0).When(value => value.Minimum.HasValue);
            variant.RuleFor(value => value.Maximum).GreaterThanOrEqualTo(0).When(value => value.Maximum.HasValue);
        });
        RuleFor(command => command).Must(command =>
                command.Attributes.Count > 0 ||
                command.Variants is [{ Title: "primary" }])
            .WithMessage("کالای ساده باید دقیقاً یک واریانت با عنوان primary داشته باشد.");
    }

    private static bool HaveUniqueNames(IReadOnlyList<Contracts.Models.PortalNameValue> values) =>
        values.Select(value => value.Name).Distinct(StringComparer.Ordinal).Count() == values.Count;

    private static bool HaveUniqueAttributeNames(IReadOnlyList<Contracts.Models.PortalAttribute> values) =>
        values.Select(value => value.Name).Distinct(StringComparer.Ordinal).Count() == values.Count;
}
