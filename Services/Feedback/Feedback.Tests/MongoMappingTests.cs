using Feedback.Domain.Entities;
using Feedback.Domain.Enums;
using Feedback.Domain.ValueObjects;
using Feedback.Infrastructure.Persistence;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Xunit;

namespace Feedback.Tests;

/// <summary>
/// Перевіряє BSON-відображення БЕЗ запущеної MongoDB: як виглядає документ і чи читається він назад.
/// Це контрольна точка «структура документа з Лаб. 01, без витоку внутрішніх полів .NET».
/// </summary>
public class MongoMappingTests
{
    public MongoMappingTests() => MongoMappings.Register();

    private static OutfitFeedback Full()
    {
        var feedback = new OutfitFeedback(
            1, new Author(1, "Олена"), Feeling.Comfortable, 5, "Чудово",
            new[] { new ItemSnapshot(2, "Легка вітровка", "Top") },
            new TemperatureNote(-2, 3));
        feedback.AddPhoto("a.jpg");
        feedback.AddComment("Андрій", "Класний образ");
        feedback.AttachDiscussion(ObjectId.GenerateNewId());
        return feedback;
    }

    [Fact]
    public void Document_has_camelCase_fields_and_no_dotnet_internals()
    {
        var doc = Full().ToBsonDocument();

        Assert.True(doc["_id"].IsObjectId);
        Assert.Equal("Comfortable", doc["feeling"].AsString);          // enum рядком
        Assert.Equal(1, doc["author"]["userId"].ToInt64());
        Assert.Equal("Олена", doc["author"]["displayName"].AsString);
        Assert.True(doc["comments"].IsBsonArray);                      // приватне поле _comments під ім'ям comments
        Assert.True(doc["itemSnapshot"].IsBsonArray);
        Assert.True(doc["photos"].IsBsonArray);
        Assert.Equal(0, doc["version"].ToInt32());

        Assert.False(doc.Contains("_comments"));
        Assert.False(doc.Contains("Comments"));
        Assert.False(doc.Contains("Id"));
        Assert.False(doc.Contains("_t"));                              // без дискримінатора типу
        Assert.All(doc.Names, name => Assert.True(char.IsLower(name[0]) || name == "_id"));
    }

    [Fact]
    public void Null_optional_fields_are_not_stored()
    {
        var doc = new OutfitFeedback(1, new Author(1, "Anna"), Feeling.Cold).ToBsonDocument();

        Assert.False(doc.Contains("rating"));
        Assert.False(doc.Contains("text"));
        Assert.False(doc.Contains("temperatureNote"));
        Assert.False(doc.Contains("discussionId"));
    }

    [Fact]
    public void Round_trip_preserves_value_objects_and_private_collections()
    {
        var original = Full();

        var restored = BsonSerializer.Deserialize<OutfitFeedback>(original.ToBsonDocument());

        Assert.Equal(original.Id, restored.Id);
        Assert.Equal(original.Author, restored.Author);                 // Value Object: рівність за значенням
        Assert.Equal(Feeling.Comfortable, restored.Feeling);
        Assert.Equal(5, restored.Rating);
        Assert.Single(restored.Comments);
        Assert.Equal("Андрій", restored.Comments.First().Author);
        Assert.Single(restored.Items);
        Assert.Equal(original.Items.First(), restored.Items.First());
        Assert.Equal(new[] { "a.jpg" }, restored.Photos);
        Assert.Equal(original.TemperatureNote, restored.TemperatureNote);
        Assert.Equal(original.DiscussionId, restored.DiscussionId);
    }

    [Fact]
    public void Legacy_document_with_extra_and_missing_fields_is_readable()
    {
        // документ у стилі Лаб. 01: немає version, є зайві поля (гнучка схема), коментар з рядковим автором
        var legacy = BsonDocument.Parse("""
        {
          "_id": { "$oid": "66f0c2a1b2c3d4e5f6000001" },
          "outfitId": 1,
          "author": { "userId": 1, "displayName": "Олена" },
          "feeling": "Hot",
          "rating": 3,
          "weatherAtTime": { "condition": "Clear", "feelsLike": 17 },
          "itemNotes": [ { "itemId": 3, "note": "Светр завтеплий" } ],
          "comments": [ { "author": "Андрій", "text": "Згоден", "createdAt": { "$date": "2026-10-08T19:10:00Z" } } ],
          "createdAt": { "$date": "2026-10-09T15:40:00Z" }
        }
        """);

        var feedback = BsonSerializer.Deserialize<OutfitFeedback>(legacy);

        Assert.Equal(Feeling.Hot, feedback.Feeling);
        Assert.Equal(0, feedback.Version);
        Assert.Single(feedback.Comments);
        Assert.Empty(feedback.Photos);
    }

    [Fact]
    public void Discussion_round_trips_with_private_tags()
    {
        var discussion = new Discussion("Layers in autumn", new Author(1, "Anna"), ObjectId.GenerateNewId(), new[] { "layers" });
        discussion.RegisterMessage();

        var doc = discussion.ToBsonDocument();
        var restored = BsonSerializer.Deserialize<Discussion>(doc);

        Assert.True(doc["tags"].IsBsonArray);
        Assert.Equal(1, restored.MessagesCount);
        Assert.Equal(new[] { "layers" }, restored.Tags);
    }
}
