using Recommendations.Domain.Models;

namespace Recommendations.Bll.Weather;

/// <summary>
/// Детермінований «прогноз» для демонстрації й тестів без інтернету.
/// Сценарій залежить від дати, тож різні дати дають різну погоду: сніг, хмарно, спека, дощ.
/// Вмикається налаштуванням Weather:Provider = Fake.
/// </summary>
public sealed class FakeWeatherProvider : IWeatherProvider
{
    public Task<WeatherSnapshot> GetDailyAsync(string city, decimal latitude, decimal longitude, DateTime date,
        CancellationToken ct = default)
    {
        var s = (date.DayOfYear % 4) switch
        {
            0 => (Min: -7m, Max: -2m, FlMin: -12m, FlMax: -5m, Precip: (byte)80, Wind: 6m, Uv: 1m, Cond: "Snow"),
            1 => (Min: 4m, Max: 12m, FlMin: 2m, FlMax: 10m, Precip: (byte)20, Wind: 4m, Uv: 2m, Cond: "Cloudy"),
            2 => (Min: 18m, Max: 28m, FlMin: 19m, FlMax: 29m, Precip: (byte)0, Wind: 3m, Uv: 7m, Cond: "Clear"),
            _ => (Min: 8m, Max: 14m, FlMin: 6m, FlMax: 11m, Precip: (byte)85, Wind: 7m, Uv: 1m, Cond: "Rain")
        };

        return Task.FromResult(new WeatherSnapshot
        {
            City = city,
            TempMin = s.Min, TempMax = s.Max,
            FeelsLikeMin = s.FlMin, FeelsLikeMax = s.FlMax,
            PrecipProbability = s.Precip,
            WindSpeedMs = s.Wind,
            UvIndex = s.Uv,
            Condition = s.Cond
        });
    }
}
