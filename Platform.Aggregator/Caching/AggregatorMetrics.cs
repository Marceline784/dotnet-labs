using System.Diagnostics.Metrics;

namespace Platform.Aggregator.Caching;

/// <summary>Метрики кешу агрегованих відповідей (Meter «Wearcast.*» підписаний у ServiceDefaults).</summary>
public sealed class AggregatorMetrics
{
    public const string MeterName = "Wearcast.Aggregator.Cache";
    private readonly Counter<long> _hits, _misses;

    public AggregatorMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _hits = meter.CreateCounter<long>("cache.hits");
        _misses = meter.CreateCounter<long>("cache.misses");
    }

    public void Hit() => _hits.Add(1, new KeyValuePair<string, object?>("cache.level", "L2"));
    public void Miss() => _misses.Add(1, new KeyValuePair<string, object?>("cache.level", "L2"));
}
