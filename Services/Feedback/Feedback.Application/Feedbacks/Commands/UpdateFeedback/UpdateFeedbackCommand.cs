using Feedback.Application.Common.Interfaces;
using Feedback.Application.Feedbacks.Commands;

namespace Feedback.Application.Feedbacks.Commands.UpdateFeedback;

/// <summary>
/// Змінити відгук. ExpectedVersion це версія, яку бачив клієнт (заголовок If-Match): якщо документ уже змінили,
/// буде конфлікт (409). Повертає НОВУ версію документа.
/// </summary>
public record UpdateFeedbackCommand(
    string Id,
    string Feeling,
    int? Rating = null,
    string? Text = null,
    TemperatureNoteInput? TemperatureNote = null,
    int? ExpectedVersion = null) : ICommand<int>;
