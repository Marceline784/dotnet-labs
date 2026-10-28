using Feedback.Application.Common.Validation;
using Feedback.Domain.Exceptions;
using Feedback.Domain.Interfaces;
using Feedback.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace Feedback.Application.Feedbacks.Commands.UpdateFeedback;

public class UpdateFeedbackCommandHandler(IFeedbackRepository repository, ILogger<UpdateFeedbackCommandHandler> logger)
    : IRequestHandler<UpdateFeedbackCommand, int>
{
    public async Task<int> Handle(UpdateFeedbackCommand cmd, CancellationToken ct)
    {
        logger.LogInformation("Handler: updating feedback {FeedbackId}", cmd.Id);

        var feedback = await repository.GetByIdAsync(ObjectId.Parse(cmd.Id), ct)
            ?? throw new NotFoundException("Feedback", cmd.Id);

        VersionGuard.Ensure(cmd.ExpectedVersion, feedback.Version, cmd.Id);   // ранній конфлікт версій

        var note = cmd.TemperatureNote is null
            ? null
            : new TemperatureNote(cmd.TemperatureNote.Felt, cmd.TemperatureNote.Forecast);

        feedback.Update(FeelingParser.Parse(cmd.Feeling), cmd.Rating, cmd.Text, note);

        // репозиторій оновлює документ лише з очікуваною версією (захист від гонки між читанням і записом)
        await repository.UpdateAsync(feedback, ct);
        return feedback.Version;
    }
}
