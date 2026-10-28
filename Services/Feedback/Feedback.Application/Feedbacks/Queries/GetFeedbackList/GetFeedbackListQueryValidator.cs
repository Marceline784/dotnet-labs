using Feedback.Application.Common.Validation;
using FluentValidation;

namespace Feedback.Application.Feedbacks.Queries.GetFeedbackList;

public class GetFeedbackListQueryValidator : AbstractValidator<GetFeedbackListQuery>
{
    public GetFeedbackListQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.Feeling).Must(FeelingParser.IsValid).When(x => x.Feeling is not null)
            .WithMessage("Feeling must be one of: Cold, Comfortable, Hot");
        RuleFor(x => x.OutfitId!.Value).GreaterThan(0).When(x => x.OutfitId.HasValue).OverridePropertyName("OutfitId");
        RuleFor(x => x.AuthorUserId!.Value).GreaterThan(0).When(x => x.AuthorUserId.HasValue).OverridePropertyName("AuthorUserId");
    }
}
