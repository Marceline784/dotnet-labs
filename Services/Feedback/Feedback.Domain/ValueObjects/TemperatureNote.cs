using Feedback.Domain.Common;
using Feedback.Domain.Exceptions;

namespace Feedback.Domain.ValueObjects;

/// <summary>Відчута та прогнозована температура (°C). Приклад гнучкої схеми: є не в усіх відгуках.</summary>
public sealed class TemperatureNote : ValueObject
{
    public double Felt { get; }
    public double Forecast { get; }

    public TemperatureNote(double felt, double forecast)
    {
        EnsureRealistic(felt, nameof(felt));
        EnsureRealistic(forecast, nameof(forecast));

        Felt = felt;
        Forecast = forecast;
    }

    private static void EnsureRealistic(double value, string field)
    {
        if (double.IsNaN(value) || value is < -80 or > 60)
            throw new DomainException($"Temperature '{field}' must be between -80 and 60 degrees");
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Felt;
        yield return Forecast;
    }
}
