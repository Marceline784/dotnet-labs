namespace Recommendations.Domain.Models;

public class Recommendation
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public DateTime ForDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public WeatherSnapshot? Weather { get; set; }
    public List<Outfit> Outfits { get; set; } = new();
}
