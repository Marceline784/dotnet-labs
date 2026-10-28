using Feedback.Domain.Common;
using Feedback.Domain.Exceptions;
using Feedback.Domain.ValueObjects;
using MongoDB.Bson;

namespace Feedback.Domain.Entities;

/// <summary>
/// Тема обговорення (колекція discussions). Повідомлення можуть бути необмежено довгими, тому живуть окремо
/// в колекції discussionMessages і посилаються на тему за ідентифікатором (reference).
/// </summary>
public class Discussion : BaseEntity
{
    public const int TitleMinLength = 3;
    public const int TitleMaxLength = 200;
    public const int MaxTags = 10;

    private readonly List<string> _tags = new();

    public string Title { get; private set; } = "";
    public ObjectId? FeedbackId { get; private set; }       // відгук, з якого почалось обговорення
    public Author CreatedBy { get; private set; } = null!;
    public int MessagesCount { get; private set; }
    public DateTime? LastMessageAt { get; private set; }
    public IReadOnlyCollection<string> Tags => _tags.AsReadOnly();

    private Discussion() { }                                // для десеріалізації

    public Discussion(string title, Author createdBy, ObjectId? feedbackId = null, IEnumerable<string>? tags = null)
    {
        var trimmed = Guard.Required(title, "Title", TitleMaxLength);
        if (trimmed.Length < TitleMinLength)
            throw new DomainException($"Title must be at least {TitleMinLength} characters long");

        Title = trimmed;
        CreatedBy = createdBy ?? throw new DomainException("Creator is required");
        FeedbackId = feedbackId;

        if (tags is not null)
            foreach (var tag in tags)
                AddTag(tag);
    }

    public void AddTag(string tag)
    {
        if (_tags.Count >= MaxTags)
            throw new DomainException($"A discussion can have at most {MaxTags} tags");

        var value = Guard.Required(tag, "Tag", 50);
        if (!_tags.Contains(value))
            _tags.Add(value);
    }

    /// <summary>Облік нового повідомлення (саме повідомлення зберігається в окремій колекції).</summary>
    public void RegisterMessage()
    {
        MessagesCount++;
        LastMessageAt = DateTime.UtcNow;
        Touch();
    }
}
