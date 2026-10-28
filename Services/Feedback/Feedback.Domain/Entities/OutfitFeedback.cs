using Feedback.Domain.Common;
using Feedback.Domain.Enums;
using Feedback.Domain.Exceptions;
using Feedback.Domain.ValueObjects;
using MongoDB.Bson;

namespace Feedback.Domain.Entities;

/// <summary>
/// Відгук на образ («замерз / комфортно / жарко»): корінь агрегату колекції outfitFeedback.
/// Інкапсуляція: приватні setter-и, зміни лише методами, інваріанти перевіряються в конструкторі й методах,
/// порушення правил дають DomainException (до будь-якого звернення до БД).
/// Особливості MongoDB: вкладені Value Objects і масиви, посилання на інший документ (DiscussionId), гнучка схема.
/// </summary>
public class OutfitFeedback : BaseEntity
{
    public const int MaxTextLength = 2000;
    public const int MaxComments = 5;        // вкладено лише кілька останніх, решта в окремій колекції
    public const int MaxPhotos = 10;
    public const int MaxItems = 20;
    public const int MinRating = 1;
    public const int MaxRating = 5;

    private readonly List<Comment> _comments = new();
    private readonly List<string> _photos = new();
    private readonly List<ItemSnapshot> _items = new();

    public long OutfitId { get; private set; }              // посилання на образ у сервісі «Рекомендації» (без FK)
    public Author Author { get; private set; } = null!;     // Value Object (вкладений документ)
    public Feeling Feeling { get; private set; }
    public int? Rating { get; private set; }                // необов'язкова оцінка 1..5
    public string? Text { get; private set; }
    public TemperatureNote? TemperatureNote { get; private set; }   // є не в усіх документах (гнучка схема)
    public ObjectId? DiscussionId { get; private set; }     // посилання на колекцію discussions (гібрид)

    public IReadOnlyCollection<Comment> Comments => _comments.AsReadOnly();    // вкладений масив
    public IReadOnlyCollection<string> Photos => _photos.AsReadOnly();
    public IReadOnlyCollection<ItemSnapshot> Items => _items.AsReadOnly();     // знімок речей образу

    private OutfitFeedback() { }                            // для десеріалізації

    public OutfitFeedback(long outfitId, Author author, Feeling feeling, int? rating = null, string? text = null,
        IEnumerable<ItemSnapshot>? items = null, TemperatureNote? temperatureNote = null)
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
            foreach (var item in items)
                AddItem(item);
    }

    /// <summary>Зміна основних даних відгуку. Версія документа збільшується в репозиторії під час збереження.</summary>
    public void Update(Feeling feeling, int? rating, string? text, TemperatureNote? temperatureNote)
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
            throw new DomainException(
                $"A feedback keeps at most {MaxComments} embedded comments; continue in the discussion");

        _comments.Add(new Comment(authorName, text, DateTime.UtcNow));   // вкладений документ
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
