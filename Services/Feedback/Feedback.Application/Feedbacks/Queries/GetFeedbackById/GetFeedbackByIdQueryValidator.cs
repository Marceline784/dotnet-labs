using Feedback.Application.Common.Validation;
using FluentValidation;

namespace Feedback.Application.Feedbacks.Queries.GetFeedbackById;

public class GetFeedbackByIdQueryValidator : AbstractValidator<GetFeedbackByIdQuery>
{
    public GetFeedbackByIdQueryValidator()
        => RuleFor(x => x.Id).Must(ObjectIdRules.IsValid).WithMessage("Id must be a valid ObjectId");
}
