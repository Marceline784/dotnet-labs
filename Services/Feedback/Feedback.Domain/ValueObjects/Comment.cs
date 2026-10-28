using Feedback.Domain.Common;

namespace Feedback.Domain.ValueObjects;

/// <summary>
/// Вкладений коментар у відгуку (embedded). Незмінний. У відгуку зберігається не більше кількох останніх
/// коментарів; довге обговорення живе окремо (колекція discussions і повідомлення за посиланням).
/// </summary>
public sealed class Comment : ValueObject
{
    public const int AuthorMaxLength = 100;
    public const int TextMaxLength = 1000;

    public string Author { get; }
    public string Text { get; }
    public DateTime CreatedAt { get; }

    public Comment(string author, string text, DateTime createdAt)
    {
        Author = Guard.Required(author, "Comment author", AuthorMaxLength);
        Text = Guard.Required(text, "Comment text", TextMaxLength);
        CreatedAt = createdAt;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Author;
        yield return Text;
        yield return CreatedAt;
    }
}
