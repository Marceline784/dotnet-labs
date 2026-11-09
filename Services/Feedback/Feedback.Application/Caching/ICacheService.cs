namespace Feedback.Application.Caching;

/// <summary>Дворівневий кеш: L1 (IMemoryCache) -> L2 (Redis) -> джерело даних.</summary>
public interface ICacheService
{
    Task<T?> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T?>> factory, CancellationToken ct, TimeSpan? l2Ttl = null);
    Task RemoveAsync(string key, string reason, CancellationToken ct);

    /// <summary>Версія групи ключів (для масової інвалідації списків).</summary>
    Task<long> GetVersionAsync(string scope, CancellationToken ct);
    Task BumpVersionAsync(string scope, string reason, CancellationToken ct);
}
