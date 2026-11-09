namespace Feedback.Application.Caching;

public static class CacheKeys
{
    /// <summary>Усі ключі відгуків живуть під однією версією: будь-який запис її збільшує (масова інвалідація).</summary>
    public const string Scope = "feedback:data";
}

/// <summary>Запит, результат якого кешується (cache-aside у MediatR pipeline).</summary>
public interface ICacheableQuery
{
    string CacheKey { get; }

    /// <summary>TTL у L2 (Redis) залежно від типу даних; L1 завжди коротший.</summary>
    TimeSpan Ttl => TimeSpan.FromMinutes(5);
}

/// <summary>Команда запису: після успішного виконання кеш відгуків інвалідується.</summary>
public interface ICacheInvalidatingCommand { }
