using Feedback.Domain.Entities;
using Feedback.Domain.Enums;
using Feedback.Domain.Exceptions;
using Feedback.Domain.ValueObjects;
using Xunit;

namespace Feedback.Tests;

/// <summary>
/// Unit-тести доменного шару: жодних звернень до БД, лише чиста логіка.
/// Контрольна точка лаби: створення відгуку з Rating = 10 кидає DomainException.
/// </summary>
public class DomainTests
{
    private static Author Anna() => new(1, "Anna");

    [Fact]
    public void Rating_ten_throws_DomainException_without_touching_database()
    {
        var ex = Assert.Throws<DomainException>(() =>
            new OutfitFeedback(1, Anna(), Feeling.Comfortable, rating: 10));

        Assert.Contains("between 1 and 5", ex.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(6)]
    public void Rating_outside_range_is_rejected(int rating)
    {
        Assert.Throws<DomainException>(() => new OutfitFeedback(1, Anna(), Feeling.Hot, rating));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void Rating_inside_range_is_accepted(int rating)
    {
        var feedback = new OutfitFeedback(1, Anna(), Feeling.Cold, rating, "Chilly in the morning");

        Assert.Equal(rating, feedback.Rating);
        Assert.Equal(0, feedback.Version);
    }

    [Fact]
    public void Rating_is_optional()
    {
        var feedback = new OutfitFeedback(1, Anna(), Feeling.Comfortable);

        Assert.Null(feedback.Rating);
    }

    [Fact]
    public void Unknown_feeling_is_rejected()
    {
        Assert.Throws<DomainException>(() => new OutfitFeedback(1, Anna(), (Feeling)99));
    }

    [Fact]
    public void Non_positive_outfit_id_is_rejected()
    {
        Assert.Throws<DomainException>(() => new OutfitFeedback(0, Anna(), Feeling.Hot));
    }

    [Fact]
    public void Too_long_text_is_rejected()
    {
        var text = new string('x', OutfitFeedback.MaxTextLength + 1);

        Assert.Throws<DomainException>(() => new OutfitFeedback(1, Anna(), Feeling.Hot, text: text));
    }

    [Fact]
    public void Sixth_embedded_comment_is_rejected()
    {
        var feedback = new OutfitFeedback(1, Anna(), Feeling.Comfortable);
        for (var i = 1; i <= OutfitFeedback.MaxComments; i++)
            feedback.AddComment("Max", $"Comment {i}");

        Assert.Equal(OutfitFeedback.MaxComments, feedback.Comments.Count);
        Assert.Throws<DomainException>(() => feedback.AddComment("Max", "One too many"));
    }

    [Fact]
    public void Empty_comment_text_is_rejected()
    {
        var feedback = new OutfitFeedback(1, Anna(), Feeling.Comfortable);

        Assert.Throws<DomainException>(() => feedback.AddComment("Max", "   "));
    }

    [Fact]
    public void Update_changes_state_and_sets_UpdatedAt()
    {
        var feedback = new OutfitFeedback(1, Anna(), Feeling.Cold, 2, "Too thin");

        feedback.Update(Feeling.Comfortable, 5, "Perfect", new TemperatureNote(-2, 3));

        Assert.Equal(Feeling.Comfortable, feedback.Feeling);
        Assert.Equal(5, feedback.Rating);
        Assert.NotNull(feedback.UpdatedAt);
        Assert.Equal(-2, feedback.TemperatureNote!.Felt);
    }

    [Fact]
    public void Second_discussion_cannot_be_attached()
    {
        var feedback = new OutfitFeedback(1, Anna(), Feeling.Comfortable);
        feedback.AttachDiscussion(MongoDB.Bson.ObjectId.GenerateNewId());

        Assert.Throws<DomainException>(() => feedback.AttachDiscussion(MongoDB.Bson.ObjectId.GenerateNewId()));
    }

    // ---------- Value Objects ----------

    [Fact]
    public void Value_objects_are_equal_by_value_not_by_reference()
    {
        var a = new Author(1, "Anna");
        var b = new Author(1, "Anna");
        var c = new Author(2, "Anna");

        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.NotEqual(a, c);
        Assert.True(a != c);
    }

    [Fact]
    public void Author_requires_positive_id_and_name()
    {
        Assert.Throws<DomainException>(() => new Author(0, "Anna"));
        Assert.Throws<DomainException>(() => new Author(1, " "));
    }

    [Fact]
    public void ItemSnapshot_rejects_unknown_slot()
    {
        Assert.Throws<DomainException>(() => new ItemSnapshot(1, "Wool sweater", "Hat"));
        var ok = new ItemSnapshot(1, "Wool sweater", "Top");
        Assert.Equal("Top", ok.Slot);
    }

    [Fact]
    public void TemperatureNote_rejects_absurd_values()
    {
        Assert.Throws<DomainException>(() => new TemperatureNote(-200, 3));
        Assert.Throws<DomainException>(() => new TemperatureNote(3, 500));
    }

    // ---------- Discussion ----------

    [Fact]
    public void Discussion_title_must_be_long_enough()
    {
        Assert.Throws<DomainException>(() => new Discussion("ab", Anna()));
    }

    [Fact]
    public void Registering_a_message_updates_counters()
    {
        var discussion = new Discussion("Layering in autumn", Anna(), tags: new[] { "layers", "autumn", "layers" });

        discussion.RegisterMessage();
        discussion.RegisterMessage();

        Assert.Equal(2, discussion.MessagesCount);
        Assert.NotNull(discussion.LastMessageAt);
        Assert.Equal(2, discussion.Tags.Count);          // дубль тега не додається
    }
}
