using System.Diagnostics;
using System.Text.Json;
using Grpc.Core;
using Microsoft.Extensions.Caching.Distributed;
using Platform.Aggregator.Caching;
using Platform.Aggregator.Clients;
using Platform.Aggregator.Contracts;

namespace Platform.Aggregator.Services;

public sealed class RecommendationCardService(
    RecommendationsClient recommendations,
    WardrobeClient wardrobe,
    FeedbackClient feedback,
    IDistributedCache cache,
    AggregatorMetrics metrics,
    ILogger<RecommendationCardService> logger)
{
    private static readonly ActivitySource Source = new("Wearcast.Aggregator");

    // короткий TTL: агрегована відповідь не має бути «свіжішою», ніж кеші сервісів, з яких її зібрано
    private static readonly DistributedCacheEntryOptions Ttl =
        new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30) };

    /// <summary>Кеш Redis перевіряється ДО gRPC-викликів. Часткові відповіді (з попередженнями) не кешуються.</summary>
    public async Task<RecommendationCardDto?> GetAsync(long id, CancellationToken ct)
    {
        var key = $"aggregator:recommendation:{id}:full";

        try
        {
            var bytes = await cache.GetAsync(key, ct);
            if (bytes is not null)
            {
                metrics.Hit();
                logger.LogInformation("Cache hit {Key}", key);
                return JsonSerializer.Deserialize<RecommendationCardDto>(bytes);
            }
            metrics.Miss();
            logger.LogInformation("Cache miss {Key}", key);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Redis unavailable, building card without cache");
        }

        var card = await BuildAsync(id, ct);

        if (card is not null && card.Warnings.Count == 0)
        {
            try { await cache.SetAsync(key, JsonSerializer.SerializeToUtf8Bytes(card), Ttl, ct); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Cannot write {Key} to Redis", key);
            }
        }
        return card;
    }

    /// <summary>
    /// Картка рекомендації: сама рекомендація (обов'язкова) + речі з гардероба й відгуки на образи (необов'язкові).
    /// Речі та відгуки запитуються ПАРАЛЕЛЬНО через gRPC; недоступність wardrobe-api чи feedback-api дає попередження, а не 500.
    /// </summary>
    private async Task<RecommendationCardDto?> BuildAsync(long id, CancellationToken ct)
    {
        using var activity = Source.StartActivity("GetRecommendationCard");
        activity?.SetTag("recommendation.id", id);

        // без рекомендації агрегувати нічого; якщо recommendations-api недоступний, виняток піде в контролер (503)
        var sw = Stopwatch.StartNew();
        logger.LogInformation("Calling recommendations-api via gRPC for recommendation {RecommendationId}", id);
        var recommendation = await recommendations.GetAsync(id, ct);
        logger.LogInformation("recommendations-api responded in {Elapsed} ms", sw.ElapsedMilliseconds);
        if (recommendation is null) return null;

        var warnings = new List<string>();
        var itemIds = recommendation.Outfits.SelectMany(o => o.Items).Select(i => i.WardrobeItemId).Distinct().ToList();
        var outfitIds = recommendation.Outfits.Select(o => o.Id).ToList();

        var itemsTask = SafeAsync("wardrobe-api", () => FetchItemsAsync(itemIds, recommendation.UserId, warnings, ct), warnings, ct);
        var feedbackTask = SafeAsync("feedback-api", () => FetchFeedbackAsync(outfitIds, ct), warnings, ct);

        await Task.WhenAll(itemsTask, feedbackTask);   // паралельно, без Task.WaitAll і .Result

        return new RecommendationCardDto(recommendation, await itemsTask ?? [], await feedbackTask ?? [], warnings);
    }

    private async Task<IReadOnlyList<WardrobeItemDetailsDto>> FetchItemsAsync(
        List<long> itemIds, long userId, List<string> warnings, CancellationToken ct)
    {
        var found = (await wardrobe.GetItemsAsync(itemIds, ct)).ToList();

        if (found.Count < itemIds.Count)
            lock (warnings) warnings.Add($"wardrobe-api: {itemIds.Count - found.Count} item(s) not found");

        // узгодженість: речі в картці мають належати власнику рекомендації
        var foreign = found.Where(i => i.OwnerUserId != userId).ToList();
        if (foreign.Count > 0)
        {
            lock (warnings) warnings.Add($"wardrobe-api: {foreign.Count} item(s) belong to another user and were skipped");
            found = found.Except(foreign).ToList();
        }
        return found;
    }

    private async Task<IReadOnlyList<FeedbackDto>> FetchFeedbackAsync(List<long> outfitIds, CancellationToken ct)
    {
        var all = await feedback.GetByOutfitsAsync(outfitIds, ct);
        // узгодженість: відгуки лише на образи цієї рекомендації
        return all.Where(f => outfitIds.Contains(f.OutfitId)).ToList();
    }

    private async Task<T?> SafeAsync<T>(string service, Func<Task<T>> call, List<string> warnings, CancellationToken ct)
        where T : class
    {
        var sw = Stopwatch.StartNew();
        logger.LogInformation("Calling {Service} via gRPC", service);
        try
        {
            var result = await call();
            logger.LogInformation("{Service} responded in {Elapsed} ms", service, sw.ElapsedMilliseconds);
            return result;
        }
        catch (RpcException ex) when (!ct.IsCancellationRequested)   // Unavailable, DeadlineExceeded та інші статуси gRPC
        {
            logger.LogWarning(ex, "{Service} is unavailable via gRPC: {Status}", service, ex.StatusCode);
            lock (warnings) warnings.Add($"{service} is unavailable");
            return null;
        }
        catch (Exception ex) when (ex is HttpRequestException or TimeoutException or OperationCanceledException && !ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "{Service} is unavailable after {Elapsed} ms", service, sw.ElapsedMilliseconds);
            lock (warnings) warnings.Add($"{service} is unavailable");
            return null;
        }
    }
}
