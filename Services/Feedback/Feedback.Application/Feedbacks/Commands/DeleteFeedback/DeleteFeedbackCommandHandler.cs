using Feedback.Domain.Exceptions;
using Feedback.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace Feedback.Application.Feedbacks.Commands.DeleteFeedback;

public class DeleteFeedbackCommandHandler(IFeedbackRepository repository, ILogger<DeleteFeedbackCommandHandler> logger)
    : IRequestHandler<DeleteFeedbackCommand>
{
    public async Task Handle(DeleteFeedbackCommand cmd, CancellationToken ct)
    {
        logger.LogInformation("Handler: deleting feedback {FeedbackId}", cmd.Id);

        if (!await repository.DeleteAsync(ObjectId.Parse(cmd.Id), ct))
            throw new NotFoundException("Feedback", cmd.Id);
    }
}
