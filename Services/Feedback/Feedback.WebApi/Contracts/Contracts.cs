using Feedback.Application.Feedbacks.Commands;

namespace Feedback.WebApi.Contracts;

// Вхідні контракти API. До команд MediatR вони перетворюються AutoMapper-ом (див. FeedbackProfile).

public class CreateFeedbackRequest
{
    public long OutfitId { get; set; }
    public long AuthorUserId { get; set; }
    public string AuthorName { get; set; } = "";

    /// <summary>Cold | Comfortable | Hot</summary>
    public string Feeling { get; set; } = "";

    public int? Rating { get; set; }
    public string? Text { get; set; }
    public List<ItemSnapshotInput>? Items { get; set; }
    public TemperatureNoteInput? TemperatureNote { get; set; }
    public List<string>? Photos { get; set; }
}

public class UpdateFeedbackRequest
{
    public string Feeling { get; set; } = "";
    public int? Rating { get; set; }
    public string? Text { get; set; }
    public TemperatureNoteInput? TemperatureNote { get; set; }
}

public class AddCommentRequest
{
    public string AuthorName { get; set; } = "";
    public string Text { get; set; } = "";
}

/// <summary>Відповідь на створення: ObjectId документа рядком (не сирий BSON).</summary>
public record IdResponse(string Id);

/// <summary>Нова версія документа після зміни (дублюється в заголовку ETag).</summary>
public record VersionResponse(int Version);
