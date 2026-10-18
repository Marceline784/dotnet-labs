using Recommendations.Domain.Constants;

namespace Recommendations.Domain.Models;

public class Outfit
{
    public long Id { get; set; }
    public long RecommendationId { get; set; }
    public byte CarouselPosition { get; set; }
    public decimal FitScore { get; set; }           // 0..100
    public string ItemsHash { get; set; } = "";
    public string Status { get; set; } = OutfitStatuses.Generated;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<OutfitItem> Items { get; set; } = new();
}
