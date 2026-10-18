using System.ComponentModel.DataAnnotations;

namespace Recommendations.Bll.Dtos;

public record WardrobeItemDto(long Id, long OwnerUserId, string Name, string SlotCode, string Layer,
    byte Warmth, short MinTemp, short MaxTemp, byte WaterResistance, bool IsAvailable, string? PrimaryImageUrl);

/// <summary>Речі, відібрані для каруселі: лише потрібні для показу поля.</summary>
public record EligibleItemDto(long Id, string Name, string SlotCode, string Layer,
    byte Warmth, byte WaterResistance, string? PrimaryImageUrl);

/// <summary>
/// Додати або оновити локальну копію речі. Власник даних — сервіс «Гардероб»;
/// в Лаб. 02 цей запит імітує подію від нього.
/// </summary>
public class UpsertWardrobeItemDto
{
    [Range(1, long.MaxValue)] public long Id { get; set; }
    [Range(1, long.MaxValue)] public long OwnerUserId { get; set; }

    [Required, StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = "";

    /// <summary>Top | Bottom | Shoes | Bag | Accessory</summary>
    [Required] public string SlotCode { get; set; } = "";

    /// <summary>Base | Mid | Outer (шари лише для слота Top)</summary>
    [Required] public string Layer { get; set; } = "Base";

    [Range(1, 5)] public byte Warmth { get; set; } = 1;
    public short MinTemp { get; set; }
    public short MaxTemp { get; set; }
    [Range(0, 3)] public byte WaterResistance { get; set; }
    public bool IsAvailable { get; set; } = true;

    [StringLength(500)] public string? PrimaryImageUrl { get; set; }
}
