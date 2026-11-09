using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Wardrobe.Bll.Dtos;
using Wardrobe.Bll.Services;
using Wardrobe.Domain.Exceptions;
using Wearcast.Contracts.Wardrobe;

namespace Wardrobe.Api.GrpcServices;

/// <summary>gRPC-адаптер над BLL (як контролер). Використовує той самий кешований IWardrobeItemService.</summary>
public class WardrobeGrpcService(IWardrobeItemService items) : WardrobeService.WardrobeServiceBase
{
    public override async Task<ItemReply> GetItem(GetItemRequest request, ServerCallContext context)
    {
        if (request.Id <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Id must be positive"));
        try
        {
            return ToReply(await items.GetByIdAsync(request.Id, context.CancellationToken));
        }
        catch (EntityNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<ItemsReply> GetItemsByIds(GetItemsByIdsRequest request, ServerCallContext context)
    {
        if (request.Ids.Count > 200)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Too many ids (max 200)"));
        if (request.Ids.Any(i => i <= 0))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Ids must be positive"));

        var reply = new ItemsReply();
        // послідовно: DbContext не потокобезпечний, а повторні читання йдуть із кешу
        foreach (var id in request.Ids.Distinct())
        {
            try { reply.Items.Add(ToReply(await items.GetByIdAsync(id, context.CancellationToken))); }
            catch (EntityNotFoundException) { /* відсутні речі пропускаємо */ }
        }
        return reply;
    }

    private static ItemReply ToReply(WardrobeItemDetailsDto d)
    {
        var r = new ItemReply
        {
            Id = d.Id,
            OwnerUserId = d.OwnerUserId,
            Name = d.Name,
            Slot = d.Slot,
            Layer = d.Layer,
            IsAvailable = d.IsAvailable,
            CreatedAt = Utc(d.CreatedAt)
        };
        if (d.UpdatedAt is { } updated) r.UpdatedAt = Utc(updated);

        if (d.Detail is { } det)
        {
            var dr = new ItemDetailReply
            {
                Warmth = det.Warmth, MinTemp = det.MinTemp, MaxTemp = det.MaxTemp,
                WaterResistance = det.WaterResistance, IsWindproof = det.IsWindproof
            };
            if (det.Material is not null) dr.Material = det.Material;
            if (det.Color is not null) dr.Color = det.Color;
            if (det.Notes is not null) dr.Notes = det.Notes;
            r.Detail = dr;
        }

        r.Images.AddRange(d.Images.Select(i => new ItemImageReply
        {
            Id = i.Id, Url = i.Url, IsPrimary = i.IsPrimary, SortOrder = i.SortOrder
        }));
        r.Categories.AddRange(d.Categories.Select(c => new CategoryRefReply
        {
            Id = c.Id, Name = c.Name, AddedAt = Utc(c.AddedAt)
        }));
        return r;
    }

    private static Timestamp Utc(DateTime value)
        => Timestamp.FromDateTime(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
