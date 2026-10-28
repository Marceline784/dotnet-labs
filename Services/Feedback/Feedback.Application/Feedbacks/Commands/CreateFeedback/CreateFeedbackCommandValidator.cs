using Feedback.Application.Common.Validation;
using Feedback.Domain.Entities;
using Feedback.Domain.Interfaces;
using Feedback.Domain.ValueObjects;
using Feedback.Application.Feedbacks.Commands;
using FluentValidation;

namespace Feedback.Application.Feedbacks.Commands.CreateFeedback;

public class CreateFeedbackCommandValidator : AbstractValidator<CreateFeedbackCommand>
{
    public CreateFeedbackCommandValidator(IFeedbackRepository repository)
    {
        RuleFor(x => x.OutfitId).GreaterThan(0);
        RuleFor(x => x.AuthorUserId).GreaterThan(0);
        RuleFor(x => x.AuthorName).NotEmpty().MaximumLength(Author.DisplayNameMaxLength);

        RuleFor(x => x.Feeling).Must(FeelingParser.IsValid)
            .WithMessage("Feeling must be one of: Cold, Comfortable, Hot");

        RuleFor(x => x.Rating!.Value)
            .InclusiveBetween(OutfitFeedback.MinRating, OutfitFeedback.MaxRating)
            .When(x => x.Rating.HasValue)
            .OverridePropertyName("Rating");

        RuleFor(x => x.Text).MaximumLength(OutfitFeedback.MaxTextLength);

        RuleFor(x => x.Items!.Count).LessThanOrEqualTo(OutfitFeedback.MaxItems)
            .When(x => x.Items is not null).OverridePropertyName("Items");
        RuleForEach(x => x.Items!).SetValidator(new ItemSnapshotInputValidator()).When(x => x.Items is not null);

        RuleFor(x => x.TemperatureNote!).SetValidator(new TemperatureNoteInputValidator())
            .When(x => x.TemperatureNote is not null);

        RuleFor(x => x.Photos!.Count).LessThanOrEqualTo(OutfitFeedback.MaxPhotos)
            .When(x => x.Photos is not null).OverridePropertyName("Photos");
        RuleForEach(x => x.Photos!).NotEmpty().MaximumLength(500).When(x => x.Photos is not null);

        // Асинхронна валідація із запитом до MongoDB: один відгук від автора на образ.
        // Запит виконується, лише якщо ідентифікатори коректні (зайвих звернень до БД немає).
        RuleFor(x => x)
            .MustAsync(async (cmd, ct) => !await repository.ExistsAsync(cmd.OutfitId, cmd.AuthorUserId, ct))
            .When(x => x.OutfitId > 0 && x.AuthorUserId > 0)
            .WithMessage("You have already left feedback for this outfit")
            .OverridePropertyName("AuthorUserId");
    }
}

public class ItemSnapshotInputValidator : AbstractValidator<ItemSnapshotInput>
{
    public ItemSnapshotInputValidator()
    {
        RuleFor(x => x.ItemId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(ItemSnapshot.NameMaxLength);
        RuleFor(x => x.Slot).Must(s => s is null || new[] { "Top", "Bottom", "Shoes", "Bag", "Accessory" }.Contains(s))
            .WithMessage("Slot must be one of: Top, Bottom, Shoes, Bag, Accessory");
    }
}

public class TemperatureNoteInputValidator : AbstractValidator<TemperatureNoteInput>
{
    public TemperatureNoteInputValidator()
    {
        RuleFor(x => x.Felt).InclusiveBetween(-80, 60);
        RuleFor(x => x.Forecast).InclusiveBetween(-80, 60);
    }
}
