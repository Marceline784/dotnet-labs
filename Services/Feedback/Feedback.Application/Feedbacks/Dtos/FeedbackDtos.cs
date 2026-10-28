namespace Feedback.Application.Feedbacks.Dtos;

/// <summary>Елемент списку: легка проєкція (з БД вибираються лише потрібні поля).</summary>
public record FeedbackDto(string Id, long OutfitId, long AuthorUserId, string AuthorName, string Feeling,
    int? Rating, string? Text, int CommentsCount, int Version, DateTime CreatedAt);

public record CommentDto(string Author, string Text, DateTime CreatedAt);

public record ItemSnapshotDto(long ItemId, string Name, string? Slot);

public record TemperatureNoteDto(double Felt, double Forecast);

/// <summary>Повний відгук з вкладеними масивами й об'єктами (для GetById). Version віддається клієнту як ETag.</summary>
public record FeedbackDetailsDto(string Id, long OutfitId, long AuthorUserId, string AuthorName, string Feeling,
    int? Rating, string? Text, TemperatureNoteDto? TemperatureNote,
    IReadOnlyList<string> Photos, IReadOnlyList<ItemSnapshotDto> Items, IReadOnlyList<CommentDto> Comments,
    string? DiscussionId, int Version, DateTime CreatedAt, DateTime? UpdatedAt);
