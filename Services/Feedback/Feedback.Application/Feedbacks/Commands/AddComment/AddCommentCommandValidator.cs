using Feedback.Application.Common.Validation;
using Feedback.Domain.ValueObjects;
using FluentValidation;

namespace Feedback.Application.Feedbacks.Commands.AddComment;

public class AddCommentCommandValidator : AbstractValidator<AddCommentCommand>
{
    public AddCommentCommandValidator()
    {
        RuleFor(x => x.Id).Must(ObjectIdRules.IsValid).WithMessage("Id must be a valid ObjectId");
        RuleFor(x => x.AuthorName).NotEmpty().MaximumLength(Comment.AuthorMaxLength);
        RuleFor(x => x.Text).NotEmpty().MaximumLength(Comment.TextMaxLength);
    }
}
