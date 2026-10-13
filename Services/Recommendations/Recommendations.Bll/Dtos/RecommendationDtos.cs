using System.ComponentModel.DataAnnotations;

namespace Recommendations.Bll.Dtos;

public record WeatherDto(string City, decimal TempMin, decimal TempMax, decimal FeelsLikeMin, decimal FeelsLikeMax,
    byte PrecipProbability, decimal WindSpeedMs, decimal? UvIndex, string Condition);

public record OutfitItemDto(long WardrobeItemId, string SlotCode, string Layer, string ItemName,
    string? ImageUrl, bool IsRemovable);

public record OutfitDto(long Id, long RecommendationId, byte CarouselPosition, decimal FitScore, string Status,
    IReadOnlyList<OutfitItemDto> Items);

public record RecommendationDto(long Id, long UserId, DateTime ForDate, DateTime CreatedAt,
    WeatherDto? Weather, IReadOnlyList<OutfitDto> Outfits);

public record RecommendationSummaryDto(long Id, long UserId, DateTime ForDate, DateTime CreatedAt, WeatherDto? Weather);

/// <summary>Запит рекомендації: погоду клієнт не передає, її отримує сервіс сам.</summary>
public class CreateRecommendationDto
{
    [Range(1, long.MaxValue)]
    public long UserId { get; set; }

    /// <summary>Дата, на яку підбирається одяг (за замовчуванням сьогодні). До 7 днів уперед.</summary>
    public DateTime? Date { get; set; }

    /// <summary>Скільки образів показати в каруселі (1..5).</summary>
    [Range(1, 5)]
    public int OutfitCount { get; set; } = 3;
}

public class ChangeOutfitStatusDto
{
    /// <summary>Generated | Chosen | Worn | Reviewed | Cancelled</summary>
    [Required] public string Status { get; set; } = "";

    [StringLength(500)] public string? Note { get; set; }
}
