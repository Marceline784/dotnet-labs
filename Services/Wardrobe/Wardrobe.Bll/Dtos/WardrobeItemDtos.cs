using System.ComponentModel.DataAnnotations;
using Wardrobe.Domain.Enums;
using Wardrobe.Domain.Rules;

namespace Wardrobe.Bll.Dtos;

// ---------- вихідні контракти (лише дані, без логіки) ----------

public record ItemDetailDto(byte Warmth, short MinTemp, short MaxTemp, byte WaterResistance,
    bool IsWindproof, string? Material, string? Color, string? Notes);

public record ItemImageDto(long Id, string Url, bool IsPrimary, int SortOrder);

public record CategoryRefDto(long Id, string Name, DateTime AddedAt);

/// <summary>Елемент списку: легка форма (без важких зв'язків).</summary>
public record WardrobeItemListDto(long Id, long OwnerUserId, string Name, string Slot, string Layer,
    bool IsAvailable, byte? Warmth, string? PrimaryImageUrl);

/// <summary>Картка речі з усім пов'язаним (завантажується одним eager-запитом).</summary>
public record WardrobeItemDetailsDto(long Id, long OwnerUserId, string Name, string Slot, string Layer,
    bool IsAvailable, DateTime CreatedAt, DateTime? UpdatedAt,
    ItemDetailDto? Detail, IReadOnlyList<ItemImageDto> Images, IReadOnlyList<CategoryRefDto> Categories);

// ---------- вхідні контракти ----------
// Прості правила (Required, StringLength, Range) задані атрибутами DataAnnotations;
// складні (enum, залежності між полями) перевіряє FluentValidation. Правила не дублюються.

public class ItemDetailInputDto
{
    [Range(ItemRules.MinWarmth, ItemRules.MaxWarmth)]
    public byte Warmth { get; set; } = 1;

    public short MinTemp { get; set; }
    public short MaxTemp { get; set; }

    [Range(0, ItemRules.MaxWaterResistance)]
    public byte WaterResistance { get; set; }

    public bool IsWindproof { get; set; }

    [StringLength(ItemRules.MaterialMaxLength)] public string? Material { get; set; }
    [StringLength(ItemRules.ColorMaxLength)] public string? Color { get; set; }
    [StringLength(ItemRules.NotesMaxLength)] public string? Notes { get; set; }
}

public class CreateWardrobeItemDto
{
    [Range(1, long.MaxValue)]
    public long OwnerUserId { get; set; }

    [Required, StringLength(ItemRules.NameMaxLength)]
    public string Name { get; set; } = "";

    /// <summary>Top | Bottom | Shoes | Bag | Accessory</summary>
    [Required] public string Slot { get; set; } = "";

    /// <summary>Base | Mid | Outer (шари лише для Top)</summary>
    [Required] public string Layer { get; set; } = "Base";

    public ItemDetailInputDto? Detail { get; set; }
}

public class UpdateWardrobeItemDto
{
    [Required, StringLength(ItemRules.NameMaxLength)]
    public string Name { get; set; } = "";

    [Required] public string Slot { get; set; } = "";
    [Required] public string Layer { get; set; } = "Base";
}

public class AddImageDto
{
    [Required, StringLength(ItemRules.UrlMaxLength)]
    public string Url { get; set; } = "";

    public bool IsPrimary { get; set; }
}

public class AddToCategoryDto
{
    [Range(1, long.MaxValue)]
    public long CategoryId { get; set; }
}

/// <summary>
/// Параметри запиту колекції: пагінація, фільтри, сортування.
/// Виконуються на рівні SQL (через специфікації), а не в пам'яті.
/// </summary>
public record WardrobeItemQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    long? OwnerUserId = null,
    Slot? Slot = null,
    bool? IsAvailable = null,
    string? Category = null,
    byte? MinWarmth = null,
    string SortBy = "name",
    string SortDir = "asc");
