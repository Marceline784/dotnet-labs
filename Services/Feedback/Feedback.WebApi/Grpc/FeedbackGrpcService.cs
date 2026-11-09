using Feedback.Application.Feedbacks.Dtos;
using Feedback.Application.Feedbacks.Queries.GetFeedbackList;
using FluentValidation;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using Wearcast.Contracts.Feedback;

namespace Feedback.WebApi.GrpcServices;

/// <summary>gRPC-адаптер над CQRS-запитами (MediatR), як контролер Лаб. 04. Кешування працює в pipeline.</summary>
public class FeedbackGrpcService(IMediator mediator) : FeedbackService.FeedbackServiceBase
{
    public override async Task<FeedbackListReply> GetByOutfits(GetFeedbackByOutfitsRequest request, ServerCallContext context)
    {
        if (request.OutfitIds.Count > 100)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Too many outfit ids (max 100)"));
        if (request.OutfitIds.Any(i => i <= 0))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Outfit ids must be positive"));

        var reply = new FeedbackListReply();
        try
        {
            foreach (var outfitId in request.OutfitIds.Distinct())
            {
                var page = await mediator.Send(
                    new GetFeedbackListQuery(OutfitId: outfitId, PageSize: 50), context.CancellationToken);
                reply.Items.AddRange(page.Items.Select(ToReply));
            }
        }
        catch (ValidationException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
        return reply;
    }

    private static FeedbackReply ToReply(FeedbackDto f)
    {
        var r = new FeedbackReply
        {
            Id = f.Id, OutfitId = f.OutfitId, AuthorUserId = f.AuthorUserId, AuthorName = f.AuthorName,
            Feeling = f.Feeling, CommentsCount = f.CommentsCount, Version = f.Version,
            CreatedAt = Timestamp.FromDateTime(DateTime.SpecifyKind(f.CreatedAt, DateTimeKind.Utc))
        };
        if (f.Rating is { } rating) r.Rating = rating;
        if (f.Text is not null) r.Text = f.Text;
        return r;
    }
}
