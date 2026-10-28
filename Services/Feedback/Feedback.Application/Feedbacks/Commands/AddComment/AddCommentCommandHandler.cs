using Feedback.Application.Common.Validation;
using Feedback.Domain.Exceptions;
using Feedback.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace Feedback.Application.Feedbacks.Commands.AddComment;

public class AddCommentCommandHandler(IFeedbackRepository repository, ILogger<AddCommentCommandHandler> logger)
    : IRequestHandler<AddCommentCommand, int>
{
    public async Task<int> Handle(AddCommentCommand cmd, CancellationToken ct)
    {
        logger.LogInformation("Handler: adding a comment to feedback {FeedbackId}", cmd.Id);

        var feedback = await repository.GetByIdAsync(ObjectId.Parse(cmd.Id), ct)
            ?? throw new NotFoundException("Feedback", cmd.Id);

        VersionGuard.Ensure(cmd.ExpectedVersion, feedback.Version, cmd.Id);

        feedback.AddComment(cmd.AuthorName, cmd.Text);      // правило «не більше 5» перевіряє сутність
        await repository.UpdateAsync(feedback, ct);
        return feedback.Version;
    }
}
