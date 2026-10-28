using Feedback.Application.Common.Validation;
using Feedback.Application.Feedbacks.Commands;
using Feedback.Domain.Entities;
using Feedback.Domain.Interfaces;
using Feedback.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Feedback.Application.Feedbacks.Commands.CreateFeedback;

public class CreateFeedbackCommandHandler(IFeedbackRepository repository, ILogger<CreateFeedbackCommandHandler> logger)
    : IRequestHandler<CreateFeedbackCommand, string>
{
    public async Task<string> Handle(CreateFeedbackCommand cmd, CancellationToken ct)
    {
        logger.LogInformation("Handler: creating feedback for outfit {OutfitId} by user {UserId}", cmd.OutfitId, cmd.AuthorUserId);

        var author = new Author(cmd.AuthorUserId, cmd.AuthorName);
        var items = cmd.Items?.Select(i => new ItemSnapshot(i.ItemId, i.Name, i.Slot)).ToList();
        var note = cmd.TemperatureNote is null
            ? null
            : new TemperatureNote(cmd.TemperatureNote.Felt, cmd.TemperatureNote.Forecast);

        var feedback = new OutfitFeedback(
            cmd.OutfitId, author, FeelingParser.Parse(cmd.Feeling), cmd.Rating, cmd.Text, items, note);

        if (cmd.Photos is not null)
            foreach (var photo in cmd.Photos)
                feedback.AddPhoto(photo);

        await repository.AddAsync(feedback, ct);
        return feedback.Id.ToString();
    }
}
