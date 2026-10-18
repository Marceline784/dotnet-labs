namespace Recommendations.Domain.Models;

public class WeatherSnapshot
{
    public long RecommendationId { get; set; }
    public string City { get; set; } = "";
    public decimal TempMin { get; set; }
    public decimal TempMax { get; set; }
    public decimal FeelsLikeMin { get; set; }
    public decimal FeelsLikeMax { get; set; }
    public byte PrecipProbability { get; set; }     // 0..100
    public decimal WindSpeedMs { get; set; }
    public decimal? UvIndex { get; set; }
    public string Condition { get; set; } = "Clear"; 
}
