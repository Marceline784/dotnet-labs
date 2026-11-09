using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Recommendations.Bll.Dtos;
using Recommendations.Bll.Services;
using Recommendations.Domain.Exceptions;
using Wearcast.Contracts.Recommendations;

namespace Recommendations.Api.GrpcServices;

/// <summary>gRPC-адаптер над IRecommendationService (як контролер).</summary>
public class RecommendationsGrpcService(IRecommendationService recommendations)
    : RecommendationsService.RecommendationsServiceBase
{
    public override async Task<RecommendationReply> GetRecommendation(
        GetRecommendationRequest request, ServerCallContext context)
    {
        if (request.Id <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Id must be positive"));
        try
        {
            return ToReply(await recommendations.GetAsync(request.Id, context.CancellationToken));
        }
        catch (NotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    private static RecommendationReply ToReply(RecommendationDto d)
    {
        var r = new RecommendationReply
        {
            Id = d.Id,
            UserId = d.UserId,
            ForDate = Utc(d.ForDate),
            CreatedAt = Utc(d.CreatedAt)
        };

        if (d.Weather is { } w)
        {
            var wr = new WeatherReply
            {
                City = w.City,
                TempMin = (double)w.TempMin, TempMax = (double)w.TempMax,
                FeelsLikeMin = (double)w.FeelsLikeMin, FeelsLikeMax = (double)w.FeelsLikeMax,
                PrecipProbability = w.PrecipProbability,
                WindSpeedMs = (double)w.WindSpeedMs,
                Condition = w.Condition
            };
            if (w.UvIndex is { } uv) wr.UvIndex = (double)uv;
            r.Weather = wr;
        }

        r.Outfits.AddRange(d.Outfits.Select(o =>
        {
            var or = new OutfitReply
            {
                Id = o.Id, RecommendationId = o.RecommendationId, CarouselPosition = o.CarouselPosition,
                FitScore = (double)o.FitScore, Status = o.Status
            };
            or.Items.AddRange(o.Items.Select(i =>
            {
                var ir = new OutfitItemReply
                {
                    WardrobeItemId = i.WardrobeItemId, SlotCode = i.SlotCode, Layer = i.Layer,
                    ItemName = i.ItemName, IsRemovable = i.IsRemovable
                };
                if (i.ImageUrl is not null) ir.ImageUrl = i.ImageUrl;
                return ir;
            }));
            return or;
        }));
        return r;
    }

    private static Timestamp Utc(DateTime value)
        => Timestamp.FromDateTime(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
