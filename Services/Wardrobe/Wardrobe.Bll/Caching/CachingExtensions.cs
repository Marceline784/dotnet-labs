using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wardrobe.Bll.Services;

namespace Wardrobe.Bll.Caching;

public static class CachingExtensions
{
    /// <summary>IMemoryCache з обмеженням розміру + Redis (IDistributedCache через Aspire) + декоратори сервісів.</summary>
    public static TBuilder AddWardrobeCaching<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddMemoryCache(o =>
        {
            o.SizeLimit = 10_000;                           // у «одиницях» Size записів
            o.CompactionPercentage = 0.2;                   // при переповненні витісняється 20 % записів
            o.ExpirationScanFrequency = TimeSpan.FromMinutes(1);
            o.TrackStatistics = true;
        });
        builder.AddRedisDistributedCache("redis");          // ім'я збігається з ресурсом «redis» в AppHost
        builder.Services.AddSingleton<CacheMetrics>();
        builder.Services.AddSingleton<ICacheService, TwoLevelCache>();

        // сервіси Лаб. 03 лишаються без змін; назовні вони відкриваються через кешуючі декоратори
        builder.Services.AddScoped<WardrobeItemService>();
        builder.Services.AddScoped<CategoryService>();
        builder.Services.AddScoped<IWardrobeItemService, CachedWardrobeItemService>();
        builder.Services.AddScoped<ICategoryService, CachedCategoryService>();
        return builder;
    }
}
