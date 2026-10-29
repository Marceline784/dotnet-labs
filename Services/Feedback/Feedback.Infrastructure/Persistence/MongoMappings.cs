
using Feedback.Domain.Common;
using Feedback.Domain.Entities;
using Feedback.Domain.Enums;
using Feedback.Domain.ValueObjects;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

namespace Feedback.Infrastructure.Persistence;

/// <summary>
/// Відображення доменних типів на BSON без BSON-атрибутів у Domain.
/// Використовує camelCase, enum як рядок і приватні поля колекцій.
/// </summary>
public static class MongoMappings
{
    private static readonly object Sync = new();
    private static bool _registered;

    public static void Register()
    {
        lock (Sync)
        {
            if (_registered)
                return;

            ConventionRegistry.Register(
                "feedback-conventions",
                new ConventionPack
                {
                    new CamelCaseElementNameConvention(),
                    new IgnoreExtraElementsConvention(true),
                    new IgnoreIfNullConvention(true)
                },
                type => type.Namespace is not null &&
                        type.Namespace.StartsWith("Feedback.Domain"));

            RegisterBase();
            RegisterValueObjects();
            RegisterEntities();

            _registered = true;
        }
    }

    private static void RegisterBase()
    {
        BsonClassMap.RegisterClassMap<BaseEntity>(cm =>
        {
            cm.MapIdMember(e => e.Id);
            cm.MapProperty(e => e.CreatedAt)
                .SetElementName("createdAt");
            cm.MapProperty(e => e.UpdatedAt)
                .SetElementName("updatedAt")
                .SetIgnoreIfNull(true);
            cm.MapProperty(e => e.Version)
                .SetElementName("version");

            cm.SetIgnoreExtraElements(true);
        });
    }

    private static void RegisterValueObjects()
    {
        BsonClassMap.RegisterClassMap<Author>(cm =>
        {
            cm.MapProperty(a => a.UserId)
                .SetElementName("userId");
            cm.MapProperty(a => a.DisplayName)
                .SetElementName("displayName");

            cm.MapCreator(a => new Author(a.UserId, a.DisplayName));
            cm.SetIgnoreExtraElements(true);
        });

        BsonClassMap.RegisterClassMap<ItemSnapshot>(cm =>
        {
            cm.MapProperty(i => i.ItemId)
                .SetElementName("itemId");
            cm.MapProperty(i => i.Name)
                .SetElementName("name");
            cm.MapProperty(i => i.Slot)
                .SetElementName("slot")
                .SetIgnoreIfNull(true);

            cm.MapCreator(i => new ItemSnapshot(i.ItemId, i.Name, i.Slot));
            cm.SetIgnoreExtraElements(true);
        });

        BsonClassMap.RegisterClassMap<TemperatureNote>(cm =>
        {
            cm.MapProperty(t => t.Felt)
                .SetElementName("felt");
            cm.MapProperty(t => t.Forecast)
                .SetElementName("forecast");

            cm.MapCreator(t => new TemperatureNote(t.Felt, t.Forecast));
            cm.SetIgnoreExtraElements(true);
        });

        BsonClassMap.RegisterClassMap<Comment>(cm =>
        {
            cm.MapProperty(c => c.Author)
                .SetElementName("author");
            cm.MapProperty(c => c.Text)
                .SetElementName("text");
            cm.MapProperty(c => c.CreatedAt)
                .SetElementName("createdAt");

            cm.MapCreator(c => new Comment(c.Author, c.Text, c.CreatedAt));
            cm.SetIgnoreExtraElements(true);
        });
    }

    private static void RegisterEntities()
    {
        BsonClassMap.RegisterClassMap<OutfitFeedback>(cm =>
        {
            cm.MapProperty(f => f.OutfitId)
                .SetElementName("outfitId");
            cm.MapProperty(f => f.Author)
                .SetElementName("author");
            cm.MapProperty(f => f.Feeling)
                .SetElementName("feeling")
                .SetSerializer(new EnumSerializer<Feeling>(BsonType.String));
            cm.MapProperty(f => f.Rating)
                .SetElementName("rating")
                .SetIgnoreIfNull(true);
            cm.MapProperty(f => f.Text)
                .SetElementName("text")
                .SetIgnoreIfNull(true);
            cm.MapProperty(f => f.TemperatureNote)
                .SetElementName("temperatureNote")
                .SetIgnoreIfNull(true);
            cm.MapProperty(f => f.DiscussionId)
                .SetElementName("discussionId")
                .SetIgnoreIfNull(true);

            cm.MapField("_comments")
                .SetElementName("comments");
            cm.MapField("_photos")
                .SetElementName("photos");
            cm.MapField("_items")
                .SetElementName("itemSnapshot");

            cm.SetIgnoreExtraElements(true);
        });

        BsonClassMap.RegisterClassMap<Discussion>(cm =>
        {
            cm.MapProperty(d => d.Title)
                .SetElementName("title");
            cm.MapProperty(d => d.FeedbackId)
                .SetElementName("feedbackId")
                .SetIgnoreIfNull(true);
            cm.MapProperty(d => d.CreatedBy)
                .SetElementName("createdBy");
            cm.MapProperty(d => d.MessagesCount)
                .SetElementName("messagesCount");
            cm.MapProperty(d => d.LastMessageAt)
                .SetElementName("lastMessageAt")
                .SetIgnoreIfNull(true);

            cm.MapField("_tags")
                .SetElementName("tags");

            cm.SetIgnoreExtraElements(true);
        });
    }
}
