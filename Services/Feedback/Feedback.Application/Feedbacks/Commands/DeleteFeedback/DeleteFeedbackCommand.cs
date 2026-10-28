using Feedback.Application.Common.Interfaces;

namespace Feedback.Application.Feedbacks.Commands.DeleteFeedback;

public record DeleteFeedbackCommand(string Id) : ICommand;
