using FluentValidation;
using Wardrobe.Bll.Common;
using Wardrobe.Bll.Dtos;
using Wardrobe.Domain.Enums;

namespace Wardrobe.Bll.Validators;

// FluentValidation: ЛИШЕ складні правила. Прості (Required, довжини, діапазони) вже задані атрибутами на DTO,
// тому тут вони не повторюються. Виклик валідаторів явний (у контролері), як рекомендує сучасна документація.

public class ItemDetailInputDtoValidator : AbstractValidator<ItemDetailInputDto>
{
    public ItemDetailInputDtoValidator()
    {
        RuleFor(x => x.MaxTemp).GreaterThan(x => x.MinTemp)
            .WithMessage("MaxTemp must be greater than MinTemp");
    }
}

public class CreateWardrobeItemDtoValidator : AbstractValidator<CreateWardrobeItemDto>
{
    public CreateWardrobeItemDtoValidator()
    {
        RuleFor(x => x.Name).Must(n => !string.IsNullOrWhiteSpace(n)).WithMessage("Name must not be blank");

        RuleFor(x => x.Slot).Must(v => EnumParser.TryParse<Slot>(v, out _))
            .WithMessage($"Slot must be one of: {string.Join(", ", Enum.GetNames<Slot>())}");
        RuleFor(x => x.Layer).Must(v => EnumParser.TryParse<Layer>(v, out _))
            .WithMessage($"Layer must be one of: {string.Join(", ", Enum.GetNames<Layer>())}");

        // залежність між полями: шари Mid/Outer лише для верху
        RuleFor(x => x).Must(x => LayerRule.IsAllowed(x.Slot, x.Layer))
            .OverridePropertyName("Layer").WithMessage("Layers other than Base are allowed only for the Top slot");

        When(x => x.Detail is not null, () =>
            RuleFor(x => x.Detail!).SetValidator(new ItemDetailInputDtoValidator()));
    }
}

public class UpdateWardrobeItemDtoValidator : AbstractValidator<UpdateWardrobeItemDto>
{
    public UpdateWardrobeItemDtoValidator()
    {
        RuleFor(x => x.Name).Must(n => !string.IsNullOrWhiteSpace(n)).WithMessage("Name must not be blank");

        RuleFor(x => x.Slot).Must(v => EnumParser.TryParse<Slot>(v, out _))
            .WithMessage($"Slot must be one of: {string.Join(", ", Enum.GetNames<Slot>())}");
        RuleFor(x => x.Layer).Must(v => EnumParser.TryParse<Layer>(v, out _))
            .WithMessage($"Layer must be one of: {string.Join(", ", Enum.GetNames<Layer>())}");

        RuleFor(x => x).Must(x => LayerRule.IsAllowed(x.Slot, x.Layer))
            .OverridePropertyName("Layer").WithMessage("Layers other than Base are allowed only for the Top slot");
    }
}

public class AddImageDtoValidator : AbstractValidator<AddImageDto>
{
    public AddImageDtoValidator()
    {
        RuleFor(x => x.Url).Must(u => Uri.IsWellFormedUriString(u, UriKind.RelativeOrAbsolute))
            .WithMessage("Url must be a valid absolute or relative URL");
    }
}

public class WardrobeItemQueryValidator : AbstractValidator<WardrobeItemQuery>
{
    private static readonly string[] SortFields = { "name", "slot", "created" };

    public WardrobeItemQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.MinWarmth!.Value).InclusiveBetween((byte)1, (byte)5).When(x => x.MinWarmth is not null)
            .OverridePropertyName("MinWarmth");
        RuleFor(x => x.SortBy).Must(s => SortFields.Contains(s?.ToLowerInvariant()))
            .WithMessage($"SortBy must be one of: {string.Join(", ", SortFields)}");
        RuleFor(x => x.SortDir).Must(s => s is not null
                && (s.Equals("asc", StringComparison.OrdinalIgnoreCase) || s.Equals("desc", StringComparison.OrdinalIgnoreCase)))
            .WithMessage("SortDir must be 'asc' or 'desc'");
    }
}

public class CategoryInputDtoValidator : AbstractValidator<CategoryInputDto>
{
    public CategoryInputDtoValidator()
    {
        RuleFor(x => x.Name).Must(n => !string.IsNullOrWhiteSpace(n)).WithMessage("Name must not be blank");
    }
}

internal static class LayerRule
{
    /// <summary>Шар, відмінний від Base, можливий лише для слота Top (якщо значення некоректні, інші правила їх відхилять).</summary>
    public static bool IsAllowed(string slot, string layer)
        => !EnumParser.TryParse<Slot>(slot, out var s) || !EnumParser.TryParse<Layer>(layer, out var l)
           || s == Slot.Top || l == Layer.Base;
}
