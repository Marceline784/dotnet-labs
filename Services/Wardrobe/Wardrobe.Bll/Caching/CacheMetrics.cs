using System.Diagnostics.Metrics;
using Microsoft.Extensions.Caching.Memory;

namespace Wardrobe.Bll.Caching;

/// <summary>Метрики кешу. Meter «Wearcast.*» уже підписаний у ServiceDefaults.</summary>
public sealed class CacheMetrics
{
    public const string MeterName = "Wearcast.Wardrobe.Cache";
    private readonly Counter<long> _hits, _misses, _invalidations, _evictions;
    private readonly Histogram<double> _duration;

    public CacheMetrics(IMeterFactory meterFactory, IMemoryCache memory)
    {
        var meter = meterFactory.Create(MeterName);
        _hits = meter.CreateCounter<long>("cache.hits");
        _misses = meter.CreateCounter<long>("cache.misses");
        _invalidations = meter.CreateCounter<long>("cache.invalidations");
        _evictions = meter.CreateCounter<long>("cache.evictions");
        _duration = meter.CreateHistogram<double>("cache.duration", "ms");
        meter.CreateObservableGauge("cache.l1.size", () => memory.GetCurrentStatistics()?.CurrentEstimatedSize ?? 0L);
    }

    public void Hit(string level) => _hits.Add(1, new KeyValuePair<string, object?>("cache.level", level));
    public void Miss(string level) => _misses.Add(1, new KeyValuePair<string, object?>("cache.level", level));
    public void Invalidated() => _invalidations.Add(1);
    public void Evicted(string reason) => _evictions.Add(1, new KeyValuePair<string, object?>("reason", reason));
    public void Duration(double ms) => _duration.Record(ms);
}
