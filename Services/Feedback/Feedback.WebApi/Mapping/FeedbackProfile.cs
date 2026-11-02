using AutoMapper;
using Feedback.Application.Feedbacks.Commands.AddComment;
using Feedback.Application.Feedbacks.Commands.CreateFeedback;
using Feedback.Application.Feedbacks.Commands.UpdateFeedback;
using Feedback.WebApi.Contracts;

namespace Feedback.WebApi.Mapping;

/// <summary>
/// Мапінг API-контрактів у команди MediatR. Значення, яких немає в тілі запиту (Id з маршруту,
/// версія з заголовка If-Match), передаються через ResolutionContext.Items.
/// </summary>
public class FeedbackProfile : Profile
{
    public const string IdKey = "id";
    public const string VersionKey = "version";

    public FeedbackProfile()
    {
        CreateMap<CreateFeedbackRequest, CreateFeedbackCommand>();

        CreateMap<UpdateFeedbackRequest, UpdateFeedbackCommand>()
            .ForCtorParam(nameof(UpdateFeedbackCommand.Id),
                o => o.MapFrom((_, ctx) => (string)ctx.Items[IdKey]))
            .ForCtorParam(nameof(UpdateFeedbackCommand.ExpectedVersion),
                o => o.MapFrom((_, ctx) => ctx.Items.ContainsKey(VersionKey) ? (int?)ctx.Items[VersionKey] : null));

        CreateMap<AddCommentRequest, AddCommentCommand>()
            .ForCtorParam(nameof(AddCommentCommand.Id),
                o => o.MapFrom((_, ctx) => (string)ctx.Items[IdKey]))
            .ForCtorParam(nameof(AddCommentCommand.ExpectedVersion),
                o => o.MapFrom((_, ctx) => ctx.Items.ContainsKey(VersionKey) ? (int?)ctx.Items[VersionKey] : null));
    }
}
