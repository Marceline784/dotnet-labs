using System.Diagnostics;
using Platform.Aggregator.Clients;
using Platform.Aggregator.Contracts;

namespace Platform.Aggregator.Services;

public sealed class RecommendationCardService(
    RecommendationsClient recommendations,
    WardrobeClient wardrobe,
    FeedbackClient feedback,
    ILogger<RecommendationCardService> logger)
{
    private static readonly ActivitySource Source = new("Wearcast.Aggregator");

    /// <summary>
    /// Картка рекомендації: сама рекомендація (обов'язкова) + речі з гардероба й відгуки на образи (необов'язкові).
    /// Речі та відгуки запитуються ПАРАЛЕЛЬНО; недоступність wardrobe-api чи feedback-api дає попередження, а не 500.
    /// </summary>
    public async Task<RecommendationCardDto?> GetAsync(long id, CancellationToken ct)
    {
        using var activity = Source.StartActivity("GetRecommendationCard");
        activity?.SetTag("recommendation.id", id);

        // без рекомендації агрегувати нічого; якщо recommendations-api недоступний, виняток піде в контролер (503)
        var sw = Stopwatch.StartNew();
        logger.LogInformation("Calling recommendations-api for recommendation {RecommendationId}", id);
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
        var fetched = await Task.WhenAll(itemIds.Select(itemId => wardrobe.GetItemAsync(itemId, ct)));
        var found = fetched.OfType<WardrobeItemDetailsDto>().ToList();

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
        var pages = await Task.WhenAll(outfitIds.Select(outfitId => feedback.GetByOutfitAsync(outfitId, ct)));
        // узгодженість: відгуки лише на образи цієї рекомендації
        return pages.SelectMany(p => p).Where(f => outfitIds.Contains(f.OutfitId)).ToList();
    }

    private async Task<T?> SafeAsync<T>(string service, Func<Task<T>> call, List<string> warnings, CancellationToken ct)
        where T : class
    {
        var sw = Stopwatch.StartNew();
        logger.LogInformation("Calling {Service}", service);
        try
        {
            var result = await call();
            logger.LogInformation("{Service} responded in {Elapsed} ms", service, sw.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "{Service} is unavailable after {Elapsed} ms", service, sw.ElapsedMilliseconds);
            lock (warnings) warnings.Add($"{service} is unavailable");
            return null;
        }
    }
}
