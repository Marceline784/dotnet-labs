using Feedback.Application.Common.Validation;
using Feedback.Application.Feedbacks.Commands.CreateFeedback;
using Feedback.Domain.Entities;
using FluentValidation;

namespace Feedback.Application.Feedbacks.Commands.UpdateFeedback;

public class UpdateFeedbackCommandValidator : AbstractValidator<UpdateFeedbackCommand>
{
    public UpdateFeedbackCommandValidator()
    {
        RuleFor(x => x.Id).Must(ObjectIdRules.IsValid).WithMessage("Id must be a valid ObjectId");

        RuleFor(x => x.Feeling).Must(FeelingParser.IsValid)
            .WithMessage("Feeling must be one of: Cold, Comfortable, Hot");

        RuleFor(x => x.Rating!.Value)
            .InclusiveBetween(OutfitFeedback.MinRating, OutfitFeedback.MaxRating)
            .When(x => x.Rating.HasValue)
            .OverridePropertyName("Rating");

        RuleFor(x => x.Text).MaximumLength(OutfitFeedback.MaxTextLength);

        RuleFor(x => x.TemperatureNote!).SetValidator(new TemperatureNoteInputValidator())
            .When(x => x.TemperatureNote is not null);

        RuleFor(x => x.ExpectedVersion!.Value).GreaterThanOrEqualTo(0)
            .When(x => x.ExpectedVersion.HasValue).OverridePropertyName("ExpectedVersion");
    }
}
