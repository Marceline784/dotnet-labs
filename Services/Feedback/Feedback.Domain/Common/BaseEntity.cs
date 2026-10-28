using MongoDB.Bson;

namespace Feedback.Domain.Common;

/// <summary>
/// Базова сутність, адаптована для MongoDB: Id (ObjectId), аудит і Version для оптимістичної конкурентності.
/// Відображення на BSON налаштовується в Infrastructure (BsonClassMap), без атрибутів у домені.
/// </summary>
public abstract class BaseEntity
{
    public ObjectId Id { get; protected set; } = ObjectId.GenerateNewId();
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; protected set; }

    /// <summary>Версія документа: оновлення застосовується лише до документа з очікуваною версією.</summary>
    public int Version { get; protected set; }

    protected void Touch() => UpdatedAt = DateTime.UtcNow;

    public void BumpVersion() => Version++;
}
