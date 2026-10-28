using Feedback.Application.Common.Interfaces;

namespace Feedback.Application.Feedbacks.Commands.AddComment;

/// <summary>Додати вкладений коментар до відгуку (не більше 5). Повертає нову версію документа.</summary>
public record AddCommentCommand(string Id, string AuthorName, string Text, int? ExpectedVersion = null) : ICommand<int>;
