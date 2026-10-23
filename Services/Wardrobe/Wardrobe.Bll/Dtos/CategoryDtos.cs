using System.ComponentModel.DataAnnotations;
using Wardrobe.Domain.Rules;

namespace Wardrobe.Bll.Dtos;

public record CategoryDto(long Id, string Name);

public record CategoryItemDto(long ItemId, string Name, string Slot, DateTime AddedAt);

/// <summary>Категорія з речами (речі довантажуються окремим explicit-запитом).</summary>
public record CategoryWithItemsDto(long Id, string Name, IReadOnlyList<CategoryItemDto> Items);

public class CategoryInputDto
{
    [Required, StringLength(ItemRules.CategoryNameMaxLength)]
    public string Name { get; set; } = "";
}
