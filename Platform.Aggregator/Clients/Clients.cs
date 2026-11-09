using Grpc.Core;
using Platform.Aggregator.Contracts;
using Wearcast.Contracts.Feedback;
using Wearcast.Contracts.Recommendations;
using Wearcast.Contracts.Wardrobe;

namespace Platform.Aggregator.Clients;

// Лаб. 06: клієнти Aggregator переведено з HTTP на gRPC. Клас-обгортка ховає згенерованого gRPC-клієнта
// і повертає ті самі контракти, що й у Лаб. 05, тому RecommendationCardService майже не змінився.
// CorrelationId додає CorrelationIdHandler із ServiceDefaults (gRPC-клієнти створює та сама фабрика HttpClient).

/// <summary>recommendations-api. Повертає null, якщо рекомендації немає (RpcException NotFound).</summary>
public sealed class RecommendationsClient(RecommendationsService.RecommendationsServiceClient grpc)
{
    public async Task<RecommendationDto?> GetAsync(long id, CancellationToken ct)
    {
        try
        {
            var r = await grpc.GetRecommendationAsync(new GetRecommendationRequest { Id = id }, cancellationToken: ct);
            return Map(r);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
    }

    private static RecommendationDto Map(RecommendationReply r) => new(
        r.Id, r.UserId, r.ForDate.ToDateTime(), r.CreatedAt.ToDateTime(),
        r.Weather is null ? null : new WeatherDto(
            r.Weather.City, (decimal)r.Weather.TempMin, (decimal)r.Weather.TempMax,
            (decimal)r.Weather.FeelsLikeMin, (decimal)r.Weather.FeelsLikeMax,
            (byte)r.Weather.PrecipProbability, (decimal)r.Weather.WindSpeedMs,
            r.Weather.HasUvIndex ? (decimal)r.Weather.UvIndex : null, r.Weather.Condition),
        r.Outfits.Select(o => new OutfitDto(
            o.Id, o.RecommendationId, (byte)o.CarouselPosition, (decimal)o.FitScore, o.Status,
            o.Items.Select(i => new OutfitItemDto(
                i.WardrobeItemId, i.SlotCode, i.Layer, i.ItemName,
                i.HasImageUrl ? i.ImageUrl : null, i.IsRemovable)).ToList())).ToList());
}

/// <summary>wardrobe-api: усі потрібні речі одним gRPC-викликом (відсутні у відповіді не повертаються).</summary>
public sealed class WardrobeClient(WardrobeService.WardrobeServiceClient grpc)
{
    public async Task<IReadOnlyList<WardrobeItemDetailsDto>> GetItemsAsync(IReadOnlyCollection<long> ids, CancellationToken ct)
    {
        if (ids.Count == 0) return [];
        var r = await grpc.GetItemsByIdsAsync(new GetItemsByIdsRequest { Ids = { ids } }, cancellationToken: ct);
        return r.Items.Select(Map).ToList();
    }

    private static WardrobeItemDetailsDto Map(ItemReply i) => new(
        i.Id, i.OwnerUserId, i.Name, i.Slot, i.Layer, i.IsAvailable,
        i.CreatedAt.ToDateTime(), i.UpdatedAt?.ToDateTime(),
        i.Detail is null ? null : new ItemDetailDto(
            (byte)i.Detail.Warmth, (short)i.Detail.MinTemp, (short)i.Detail.MaxTemp, (byte)i.Detail.WaterResistance,
            i.Detail.IsWindproof,
            i.Detail.HasMaterial ? i.Detail.Material : null,
            i.Detail.HasColor ? i.Detail.Color : null,
            i.Detail.HasNotes ? i.Detail.Notes : null),
        i.Images.Select(x => new ItemImageDto(x.Id, x.Url, x.IsPrimary, x.SortOrder)).ToList(),
        i.Categories.Select(c => new CategoryRefDto(c.Id, c.Name, c.AddedAt.ToDateTime())).ToList());
}

/// <summary>feedback-api: відгуки на всі образи одним gRPC-викликом.</summary>
public sealed class FeedbackClient(FeedbackService.FeedbackServiceClient grpc)
{
    public async Task<IReadOnlyList<FeedbackDto>> GetByOutfitsAsync(IReadOnlyCollection<long> outfitIds, CancellationToken ct)
    {
        if (outfitIds.Count == 0) return [];
        var r = await grpc.GetByOutfitsAsync(new GetFeedbackByOutfitsRequest { OutfitIds = { outfitIds } }, cancellationToken: ct);
        return r.Items.Select(f => new FeedbackDto(
            f.Id, f.OutfitId, f.AuthorUserId, f.AuthorName, f.Feeling,
            f.HasRating ? f.Rating : null, f.HasText ? f.Text : null,
            f.CommentsCount, f.Version, f.CreatedAt.ToDateTime())).ToList();
    }
}
