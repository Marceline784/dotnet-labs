namespace Recommendations.Domain.Models;

public class UserProfile
{
    public long UserId { get; set; }
    public string City { get; set; } = "";
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    public decimal ColdSensitivity { get; set; }

    public string? StylePreference { get; set; }   
}
