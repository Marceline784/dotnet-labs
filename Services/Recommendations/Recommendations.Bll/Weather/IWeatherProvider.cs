using Recommendations.Domain.Models;

namespace Recommendations.Bll.Weather;

/// <summary>
/// Джерело погоди. Користувач погоду не вводить: сервіс бере прогноз за координатами з профілю.
/// Реалізації: OpenMeteoWeatherProvider (реальний прогноз) і FakeWeatherProvider (демо/тести без інтернету).
/// </summary>
public interface IWeatherProvider
{
    Task<WeatherSnapshot> GetDailyAsync(string city, decimal latitude, decimal longitude, DateTime date,
        CancellationToken ct = default);
}
