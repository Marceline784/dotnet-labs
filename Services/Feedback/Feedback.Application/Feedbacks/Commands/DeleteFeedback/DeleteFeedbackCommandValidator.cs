using Feedback.Application.Common.Validation;
using FluentValidation;

namespace Feedback.Application.Feedbacks.Commands.DeleteFeedback;

public class DeleteFeedbackCommandValidator : AbstractValidator<DeleteFeedbackCommand>
{
    public DeleteFeedbackCommandValidator()
        => RuleFor(x => x.Id).Must(ObjectIdRules.IsValid).WithMessage("Id must be a valid ObjectId");
}
