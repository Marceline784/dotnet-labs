using System.ComponentModel.DataAnnotations;

namespace Recommendations.Bll.Dtos;

public record UserDto(long Id, string Email, string DisplayName);

/// <summary>Вхідний контракт створення користувача. DataAnnotations перевіряються ще на рівні API.</summary>
public class CreateUserDto
{
    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = "";

    [Required, StringLength(100, MinimumLength = 1)]
    public string DisplayName { get; set; } = "";
}

public record UserProfileDto(long UserId, string City, decimal? Latitude, decimal? Longitude,
    decimal ColdSensitivity, string? StylePreference);

/// <summary>
/// Профіль: місто й координати користувач вказує один раз. Погоду він НЕ вводить:
/// її система сама отримує за цими координатами.
/// </summary>
public class SaveUserProfileDto
{
    [Required, StringLength(100, MinimumLength = 1)]
    public string City { get; set; } = "";

    [Required, Range(typeof(decimal), "-90", "90")]
    public decimal? Latitude { get; set; }

    [Required, Range(typeof(decimal), "-180", "180")]
    public decimal? Longitude { get; set; }

    /// <summary>-3 (спека) ... +3 (мерзлякувата).</summary>
    [Range(typeof(decimal), "-3", "3")]
    public decimal ColdSensitivity { get; set; }

    [StringLength(30)]
    public string? StylePreference { get; set; }
}
