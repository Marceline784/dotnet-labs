namespace Platform.Aggregator.Contracts;

// Мінімальні копії контрактів сервісів (лише потрібні агрегатору поля). Сервіси між собою не залежать:
// агрегатор не посилається на їхні збірки, а читає JSON за HTTP.

// ---- Recommendations: GET /api/recommendations/{id} ----
public record WeatherDto(string City, decimal TempMin, decimal TempMax, decimal FeelsLikeMin, decimal FeelsLikeMax,
    byte PrecipProbability, decimal WindSpeedMs, decimal? UvIndex, string Condition);

public record OutfitItemDto(long WardrobeItemId, string SlotCode, string Layer, string ItemName,
    string? ImageUrl, bool IsRemovable);

public record OutfitDto(long Id, long RecommendationId, byte CarouselPosition, decimal FitScore, string Status,
    IReadOnlyList<OutfitItemDto> Items);

public record RecommendationDto(long Id, long UserId, DateTime ForDate, DateTime CreatedAt,
    WeatherDto? Weather, IReadOnlyList<OutfitDto> Outfits);

// ---- Wardrobe: GET /api/wardrobe-items/{id} ----
public record ItemDetailDto(byte Warmth, short MinTemp, short MaxTemp, byte WaterResistance,
    bool IsWindproof, string? Material, string? Color, string? Notes);

public record ItemImageDto(long Id, string Url, bool IsPrimary, int SortOrder);

public record CategoryRefDto(long Id, string Name, DateTime AddedAt);

public record WardrobeItemDetailsDto(long Id, long OwnerUserId, string Name, string Slot, string Layer,
    bool IsAvailable, DateTime CreatedAt, DateTime? UpdatedAt,
    ItemDetailDto? Detail, IReadOnlyList<ItemImageDto> Images, IReadOnlyList<CategoryRefDto> Categories);

// ---- Feedback: GET /api/feedback?outfitId=... ----
public record FeedbackDto(string Id, long OutfitId, long AuthorUserId, string AuthorName, string Feeling,
    int? Rating, string? Text, int CommentsCount, int Version, DateTime CreatedAt);

public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);

// ---- Відповідь агрегатора: «повна картка рекомендації» ----
public record RecommendationCardDto(
    RecommendationDto Recommendation,
    IReadOnlyList<WardrobeItemDetailsDto> WardrobeItems,
    IReadOnlyList<FeedbackDto> Feedback,
    IReadOnlyList<string> Warnings);
