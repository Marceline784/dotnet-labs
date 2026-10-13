using System.Data;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Recommendations.Bll.Dtos;
using Recommendations.Bll.Generation;
using Recommendations.Bll.Weather;
using Recommendations.Dal.Interfaces;
using Recommendations.Domain.Exceptions;
using Recommendations.Domain.Models;

namespace Recommendations.Bll.Services;

public sealed class RecommendationService(
    IUnitOfWork uow,
    IMapper mapper,
    IWeatherProvider weatherProvider,
    IOutfitGenerator generator,
    ILogger<RecommendationService> logger) : IRecommendationService
{
    private const int NoRepeatDays = 3;      // як за замовчуванням у usp_AddOutfit
    private const int MaxDaysAhead = 7;

    /// <summary>
    /// ТРАНЗАКЦІЙНА бізнес-операція кількох репозиторіїв:
    /// Recommendations (рекомендація + знімок погоди) -> WardrobeItems (відбір речей) -> Outfits (образи + позиції + історія).
    /// Якщо щось падає, відкочується ВСЕ: у БД не лишається рекомендації без образів.
    /// </summary>
    public async Task<RecommendationDto> CreateAsync(CreateRecommendationDto input, CancellationToken ct = default)
    {
        var date = ResolveDate(input.Date);

        var user = await uow.Users.GetByIdAsync(input.UserId, ct)
            ?? throw new NotFoundException($"User {input.UserId} not found");
        var profile = await uow.Users.GetProfileAsync(user.Id, ct)
            ?? throw new BusinessConflictException("Create the user profile (city and coordinates) first");
        if (profile.Latitude is null || profile.Longitude is null)
            throw new BusinessConflictException("User profile must contain coordinates to get the weather");

        // 1) Погоду отримує система сама. HTTP-виклик робимо ДО транзакції: не тримаємо блокування БД під час очікування.
        var weather = await weatherProvider.GetDailyAsync(profile.City, profile.Latitude.Value, profile.Longitude.Value, date, ct);

        // 2) Які образи користувач недавно одягав (їх не пропонуємо повторно)
        var worn = new HashSet<string>(await uow.Outfits.GetRecentlyWornHashesAsync(user.Id, date, NoRepeatDays, ct),
            StringComparer.OrdinalIgnoreCase);

        long recommendationId;
        await uow.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        try
        {
            recommendationId = await uow.Recommendations.AddAsync(
                new Recommendation { UserId = user.Id, ForDate = date, Weather = weather }, ct);

            // відбір за погодою робить процедура БД; повні дані речей беремо окремо (процедура повертає не всі стовпці)
            var eligibleIds = (await uow.WardrobeItems.GetEligibleAsync(recommendationId, ct)).Select(i => i.Id).ToHashSet();
            var eligible = (await uow.WardrobeItems.GetByOwnerAsync(user.Id, null, ct))
                .Where(i => eligibleIds.Contains(i.Id)).ToList();

            var outfits = generator.Generate(eligible, weather, profile, input.OutfitCount, worn);
            if (outfits.Count == 0)
                throw new BusinessConflictException(
                    "Not enough suitable items in the wardrobe for this weather: at least one top and one bottom are required");

            foreach (var outfit in outfits)
            {
                outfit.RecommendationId = recommendationId;
                await uow.Outfits.AddAsync(outfit, ct);
            }

            await uow.CommitAsync(ct);
        }
        catch
        {
            await uow.RollbackAsync(ct);
            throw;
        }

        logger.LogInformation("Recommendation {RecommendationId} created for user {UserId}", recommendationId, user.Id);
        return await GetAsync(recommendationId, ct);
    }

    public async Task<RecommendationDto> GetAsync(long id, CancellationToken ct = default)
    {
        var recommendation = await uow.Recommendations.GetWithWeatherAsync(id, ct)
            ?? throw new NotFoundException($"Recommendation {id} not found");
        recommendation.Outfits = (await uow.Outfits.GetByRecommendationAsync(id, ct)).ToList();
        return mapper.Map<RecommendationDto>(recommendation);
    }

    public async Task<IReadOnlyList<RecommendationSummaryDto>> GetByUserAsync(long userId, int page, int pageSize, CancellationToken ct = default)
    {
        if (page < 1) throw new ValidationException("Page must be 1 or greater");
        if (pageSize is < 1 or > 50) throw new ValidationException("Page size must be between 1 and 50");
        if (await uow.Users.GetByIdAsync(userId, ct) is null)
            throw new NotFoundException($"User {userId} not found");

        var items = await uow.Recommendations.GetByUserAsync(userId, (page - 1) * pageSize, pageSize, ct);
        return mapper.Map<IReadOnlyList<RecommendationSummaryDto>>(items);
    }

    private static DateTime ResolveDate(DateTime? requested)
    {
        var today = DateTime.UtcNow.Date;
        var date = requested?.Date ?? today;
        if (date < today.AddDays(-1) || date > today.AddDays(MaxDaysAhead))
            throw new ValidationException($"Date must be between today and {MaxDaysAhead} days ahead");
        return date;
    }
}
