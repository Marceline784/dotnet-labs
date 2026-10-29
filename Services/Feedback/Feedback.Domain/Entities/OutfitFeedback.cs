
using Feedback.Domain.Common;
using Feedback.Domain.Enums;
using Feedback.Domain.Exceptions;
using Feedback.Domain.ValueObjects;
using MongoDB.Bson;

namespace Feedback.Domain.Entities;

/// <summary>
/// Відгук на образ: корінь агрегату колекції outfitFeedback.
/// </summary>
public class OutfitFeedback : BaseEntity
{
    public const int MaxTextLength = 2000;
    public const int MaxComments = 5;
    public const int MaxPhotos = 10;
    public const int MaxItems = 20;
    public const int MinRating = 1;
    public const int MaxRating = 5;

    private List<Comment> _comments = new();
    private List<string> _photos = new();
    private List<ItemSnapshot> _items = new();

    public long OutfitId { get; private set; }
    public Author Author { get; private set; } = null!;
    public Feeling Feeling { get; private set; }
    public int? Rating { get; private set; }
    public string? Text { get; private set; }
    public TemperatureNote? TemperatureNote { get; private set; }
    public ObjectId? DiscussionId { get; private set; }

    public IReadOnlyCollection<Comment> Comments => _comments.AsReadOnly();
    public IReadOnlyCollection<string> Photos => _photos.AsReadOnly();
    public IReadOnlyCollection<ItemSnapshot> Items => _items.AsReadOnly();

    private OutfitFeedback() { }

    public OutfitFeedback(
        long outfitId,
        Author author,
        Feeling feeling,
        int? rating = null,
        string? text = null,
        IEnumerable<ItemSnapshot>? items = null,
        TemperatureNote? temperatureNote = null)
    {
        if (outfitId <= 0)
            throw new DomainException("OutfitId must be positive");

        OutfitId = outfitId;
        Author = author ?? throw new DomainException("Author is required");
        Feeling = EnsureFeeling(feeling);
        Rating = EnsureRating(rating);
        Text = Guard.Optional(text, "Text", MaxTextLength);
        TemperatureNote = temperatureNote;

        if (items is not null)
        {
            foreach (var item in items)
                AddItem(item);
        }
    }

    /// <summary>
    /// Зміна основних даних відгуку.
    /// </summary>
    public void Update(
        Feeling feeling,
        int? rating,
        string? text,
        TemperatureNote? temperatureNote)
    {
        Feeling = EnsureFeeling(feeling);
        Rating = EnsureRating(rating);
        Text = Guard.Optional(text, "Text", MaxTextLength);
        TemperatureNote = temperatureNote;
        Touch();
    }

    public void AddComment(string authorName, string text)
    {
        if (_comments.Count >= MaxComments)
        {
            throw new DomainException(
                $"A feedback keeps at most {MaxComments} embedded comments; continue in the discussion");
        }

        _comments.Add(new Comment(authorName, text, DateTime.UtcNow));
        Touch();
    }

    public void AddPhoto(string url)
    {
        if (_photos.Count >= MaxPhotos)
            throw new DomainException($"A feedback can have at most {MaxPhotos} photos");

        _photos.Add(Guard.Required(url, "Photo url", 500));
        Touch();
    }

    public void AttachDiscussion(ObjectId discussionId)
    {
        if (DiscussionId is not null)
            throw new DomainException("A discussion is already attached to this feedback");

        if (discussionId == ObjectId.Empty)
            throw new DomainException("Discussion id is required");

        DiscussionId = discussionId;
        Touch();
    }

    private void AddItem(ItemSnapshot item)
    {
        if (item is null)
            throw new DomainException("Item snapshot must not be null");

        if (_items.Count >= MaxItems)
            throw new DomainException($"A feedback can describe at most {MaxItems} items");

        _items.Add(item);
    }

    private static Feeling EnsureFeeling(Feeling feeling)
    {
        if (!Enum.IsDefined(feeling))
            throw new DomainException("Feeling must be Cold, Comfortable or Hot");

        return feeling;
    }

    private static int? EnsureRating(int? rating)
    {
        if (rating is < MinRating or > MaxRating)
            throw new DomainException($"Rating must be between {MinRating} and {MaxRating}");

        return rating;
    }
}
