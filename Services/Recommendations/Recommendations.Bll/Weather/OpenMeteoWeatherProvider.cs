using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Recommendations.Domain.Exceptions;
using Recommendations.Domain.Models;

namespace Recommendations.Bll.Weather;

public sealed class OpenMeteoWeatherProvider(HttpClient http, ILogger<OpenMeteoWeatherProvider> logger) : IWeatherProvider
{
    public async Task<WeatherSnapshot> GetDailyAsync(string city, decimal latitude, decimal longitude, DateTime date,
        CancellationToken ct = default)
    {
        var day = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var lat = latitude.ToString(CultureInfo.InvariantCulture);
        var lon = longitude.ToString(CultureInfo.InvariantCulture);
        var url = "v1/forecast?latitude=" + lat + "&longitude=" + lon +
            "&daily=temperature_2m_max,temperature_2m_min,apparent_temperature_max,apparent_temperature_min," +
            "precipitation_probability_max,wind_speed_10m_max,uv_index_max,weather_code" +
            "&wind_speed_unit=ms&timezone=auto&start_date=" + day + "&end_date=" + day;

        try
        {
            using var response = await http.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            var daily = doc.RootElement.GetProperty("daily");

            var tempMax = Value(daily, "temperature_2m_max") ?? throw new KeyNotFoundException("temperature_2m_max");
            var tempMin = Value(daily, "temperature_2m_min") ?? throw new KeyNotFoundException("temperature_2m_min");
            var feelsMax = Value(daily, "apparent_temperature_max") ?? tempMax;
            var feelsMin = Value(daily, "apparent_temperature_min") ?? tempMin;
            var precip = Value(daily, "precipitation_probability_max") ?? 0m;
            var wind = Value(daily, "wind_speed_10m_max") ?? 0m;
            var uv = Value(daily, "uv_index_max");
            var code = (int)(Value(daily, "weather_code") ?? 0m);

            return new WeatherSnapshot
            {
                City = city,
                TempMin = Round(tempMin),
                TempMax = Round(tempMax),
                FeelsLikeMin = Round(feelsMin),
                FeelsLikeMax = Round(feelsMax),
                PrecipProbability = (byte)Math.Clamp((int)Math.Round(precip), 0, 100),
                WindSpeedMs = Math.Max(0m, Round(wind)),
                UvIndex = uv is null ? null : Math.Max(0m, Round(uv.Value)),
                Condition = MapWeatherCode(code)
            };
        }
        catch (Exception ex) when (!ct.IsCancellationRequested &&
            ex is HttpRequestException or TaskCanceledException or JsonException or KeyNotFoundException or InvalidOperationException)
        {
            logger.LogWarning(ex, "Weather request failed for {City}", city);
            throw new ExternalServiceException("Weather service is currently unavailable");
        }
    }

    private static decimal Round(decimal v) => Math.Round(v, 1);

    private static decimal? Value(JsonElement daily, string name) =>
        daily.TryGetProperty(name, out var arr) && arr.ValueKind == JsonValueKind.Array && arr.GetArrayLength() > 0
        && arr[0].ValueKind == JsonValueKind.Number
            ? arr[0].GetDecimal()
            : null;

    /// <summary>Код WMO -> наш тип погоди (Clear | Cloudy | Rain | Snow | Storm | Fog).</summary>
    private static string MapWeatherCode(int code) => code switch
    {
        0 or 1 => "Clear",
        2 or 3 => "Cloudy",
        45 or 48 => "Fog",
        >= 51 and <= 67 => "Rain",
        >= 71 and <= 77 => "Snow",
        >= 80 and <= 82 => "Rain",
        85 or 86 => "Snow",
        >= 95 => "Storm",
        _ => "Cloudy"
    };
}
