using Feedback.Application.Common.Interfaces;
using Feedback.Application.Feedbacks.Commands;

namespace Feedback.Application.Feedbacks.Commands.CreateFeedback;

/// <summary>Створити відгук на образ. Повертає ObjectId нового документа рядком.</summary>
public record CreateFeedbackCommand(
    long OutfitId,
    long AuthorUserId,
    string AuthorName,
    string Feeling,
    int? Rating = null,
    string? Text = null,
    IReadOnlyList<ItemSnapshotInput>? Items = null,
    TemperatureNoteInput? TemperatureNote = null,
    IReadOnlyList<string>? Photos = null) : ICommand<string>;
