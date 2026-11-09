using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Feedback.Application.Caching;

/// <summary>
/// Cache-aside з двома рівнями. L1 (пам'ять процесу) має КОРОТШИЙ TTL, ніж L2 (Redis, спільний для реплік).
/// Якщо Redis недоступний, сервіс не падає: працює через L1 і базу, а Redis пропускається на 15 с.
/// </summary>
public sealed class TwoLevelCache(
    IMemoryCache memory, IDistributedCache redis, CacheMetrics metrics, ILogger<TwoLevelCache> logger) : ICacheService
{
    private static readonly TimeSpan L1Sliding = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan L1Absolute = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan L2Absolute = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan L2Cooldown = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan VersionL1 = TimeSpan.FromSeconds(5);
    private long _l2DownUntil;

    private bool L2Available => Environment.TickCount64 >= Interlocked.Read(ref _l2DownUntil);

    public async Task<T?> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T?>> factory, CancellationToken ct, TimeSpan? l2Ttl = null)
    {
        var l2 = l2Ttl ?? L2Absolute;                                   // TTL залежить від типу даних
        var l1Ttl = l2 < TimeSpan.FromMinutes(2) ? l2 / 2 : L1Absolute;    // L1 завжди коротший за L2
        var start = Stopwatch.GetTimestamp();
        try
        {
            if (memory.TryGetValue(key, out T? l1) && l1 is not null)
            {
                metrics.Hit("L1");
                logger.LogDebug("Cache hit {Key} level {Level}", key, "L1");
                return l1;
            }
            metrics.Miss("L1");

            if (L2Available)
            {
                try
                {
                    var bytes = await redis.GetAsync(key, ct);
                    if (bytes is not null)
                    {
                        var v = JsonSerializer.Deserialize<T>(bytes);
                        if (v is not null)
                        {
                            SetL1(key, v, l1Ttl);
                            metrics.Hit("L2");
                            logger.LogDebug("Cache hit {Key} level {Level} size {Size} bytes", key, "L2", bytes.Length);
                            return v;
                        }
                    }
                    metrics.Miss("L2");
                    logger.LogDebug("Cache miss {Key}", key);
                }
                catch (Exception ex) when (ex is not OperationCanceledException) { MarkL2Down(ex); }
            }

            var value = await factory(ct);   // база даних
            if (value is null) return default;

            SetL1(key, value, l1Ttl);
            if (L2Available)
            {
                try
                {
                    await redis.SetAsync(key, JsonSerializer.SerializeToUtf8Bytes(value),
                        new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = l2 }, ct);
                }
                catch (Exception ex) when (ex is not OperationCanceledException) { MarkL2Down(ex); }
            }
            return value;
        }
        finally { metrics.Duration(Stopwatch.GetElapsedTime(start).TotalMilliseconds); }
    }

    public async Task RemoveAsync(string key, string reason, CancellationToken ct)
    {
        memory.Remove(key);
        if (L2Available)
        {
            try { await redis.RemoveAsync(key, ct); }
            catch (Exception ex) when (ex is not OperationCanceledException) { MarkL2Down(ex); }
        }
        metrics.Invalidated();
        logger.LogInformation("Cache invalidated {Key}, reason {Reason}", key, reason);
    }

    public async Task<long> GetVersionAsync(string scope, CancellationToken ct)
    {
        var key = $"{scope}:version";
        if (memory.TryGetValue(key, out long cached)) return cached;
        long version = 1;
        if (L2Available)
        {
            try
            {
                var s = await redis.GetStringAsync(key, ct);
                if (long.TryParse(s, out var p)) version = p;
            }
            catch (Exception ex) when (ex is not OperationCanceledException) { MarkL2Down(ex); }
        }
        memory.Set(key, version, new MemoryCacheEntryOptions { Size = 1, AbsoluteExpirationRelativeToNow = VersionL1 });
        return version;
    }

    public async Task BumpVersionAsync(string scope, string reason, CancellationToken ct)
    {
        var key = $"{scope}:version";
        var next = DateTime.UtcNow.Ticks;
        memory.Set(key, next, new MemoryCacheEntryOptions { Size = 1, AbsoluteExpirationRelativeToNow = VersionL1 });
        if (L2Available)
        {
            try { await redis.SetStringAsync(key, next.ToString(), ct); }
            catch (Exception ex) when (ex is not OperationCanceledException) { MarkL2Down(ex); }
        }
        metrics.Invalidated();
        logger.LogInformation("Cache invalidated scope {Scope}, new version {Version}, reason {Reason}", scope, next, reason);
    }

    private void SetL1<T>(string key, T value, TimeSpan absolute)
    {
        var o = new MemoryCacheEntryOptions
        {
            Size = 1,
            Priority = CacheItemPriority.Normal,
            SlidingExpiration = absolute < L1Sliding ? absolute : L1Sliding,
            AbsoluteExpirationRelativeToNow = absolute
        };
        o.RegisterPostEvictionCallback(OnEvicted, metrics);
        memory.Set(key, value, o);
    }

    private static void OnEvicted(object key, object? value, EvictionReason reason, object? state)
    {
        if (reason is EvictionReason.Capacity or EvictionReason.Expired or EvictionReason.TokenExpired)
            (state as CacheMetrics)?.Evicted(reason.ToString());
    }

    private void MarkL2Down(Exception ex)
    {
        Interlocked.Exchange(ref _l2DownUntil, Environment.TickCount64 + (long)L2Cooldown.TotalMilliseconds);
        logger.LogWarning(ex, "Redis (L2) unavailable, using L1 and database for {Seconds} s", L2Cooldown.TotalSeconds);
    }
}
