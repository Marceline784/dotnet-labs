using System.Net;
using Platform.Aggregator.Contracts;

namespace Platform.Aggregator.Clients;

/// <summary>recommendations-api (Лаб. 02). Повертає null, якщо рекомендації немає (404).</summary>
public sealed class RecommendationsClient(HttpClient http)
{
    public async Task<RecommendationDto?> GetAsync(long id, CancellationToken ct)
    {
        using var response = await http.GetAsync($"/api/recommendations/{id}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RecommendationDto>(ct);
    }
}

/// <summary>wardrobe-api (Лаб. 03). Повертає null, якщо речі немає (404).</summary>
public sealed class WardrobeClient(HttpClient http)
{
    public async Task<WardrobeItemDetailsDto?> GetItemAsync(long id, CancellationToken ct)
    {
        using var response = await http.GetAsync($"/api/wardrobe-items/{id}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<WardrobeItemDetailsDto>(ct);
    }
}

/// <summary>feedback-api (Лаб. 04).</summary>
public sealed class FeedbackClient(HttpClient http)
{
    public async Task<IReadOnlyList<FeedbackDto>> GetByOutfitAsync(long outfitId, CancellationToken ct)
    {
        var page = await http.GetFromJsonAsync<PagedResult<FeedbackDto>>(
            $"/api/feedback?outfitId={outfitId}&page=1&pageSize=50", ct);
        return page?.Items ?? [];
    }
}
