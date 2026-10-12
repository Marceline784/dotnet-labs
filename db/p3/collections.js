
const database = db.getSiblingDB("outfitFeedbackDb");
const idType = ["int", "long"];

function ensureCollection(name, validator) {
  const exists = database.getCollectionNames().includes(name);
  if (!exists) {
    database.createCollection(name, { validator, validationLevel: "strict", validationAction: "error" });
  } else {
    database.runCommand({ collMod: name, validator, validationLevel: "strict", validationAction: "error" });
  }
}

ensureCollection("outfitFeedback", {
  $jsonSchema: {
    bsonType: "object",
    required: ["outfitId", "author", "feeling", "createdAt"],
    properties: {
      outfitId: { bsonType: idType, description: "Id образу в БД Рекомендацій (без FK)" },
      author: {
        bsonType: "object",
        required: ["userId", "displayName"],
        properties: { userId: { bsonType: idType }, displayName: { bsonType: "string" } }
      },
      feeling: { enum: ["Cold", "Comfortable", "Hot"] },
      rating: { bsonType: "int", minimum: 1, maximum: 5 },
      text: { bsonType: "string", maxLength: 2000 },
      photos: { bsonType: "array", items: { bsonType: "string" }, maxItems: 10 },
      itemSnapshot: {
        bsonType: "array",
        items: { bsonType: "object", required: ["itemId", "name"],
                 properties: { itemId: { bsonType: idType }, name: { bsonType: "string" }, slot: { bsonType: "string" } } }
      },
      comments: { bsonType: "array", maxItems: 5,
                  items: { bsonType: "object", required: ["author", "text", "createdAt"] } },
      discussionId: { bsonType: "objectId" },
      createdAt: { bsonType: "date" }
    }
  }
});

ensureCollection("discussions", {
  $jsonSchema: {
    bsonType: "object",
    required: ["title", "createdBy", "createdAt"],
    properties: {
      title: { bsonType: "string", minLength: 3, maxLength: 200 },
      feedbackId: { bsonType: "objectId" },
      createdBy: { bsonType: "object", required: ["userId", "displayName"] },
      tags: { bsonType: "array", items: { bsonType: "string" } },
      messagesCount: { bsonType: ["int", "long"], minimum: 0 },
      lastMessageAt: { bsonType: "date" },
      createdAt: { bsonType: "date" }
    }
  }
});

ensureCollection("discussionMessages", {
  $jsonSchema: {
    bsonType: "object",
    required: ["discussionId", "author", "text", "createdAt"],
    properties: {
      discussionId: { bsonType: "objectId" },
      replyTo: { bsonType: "objectId" },
      author: { bsonType: "object", required: ["userId", "displayName"] },
      text: { bsonType: "string", minLength: 1, maxLength: 2000 },
      createdAt: { bsonType: "date" }
    }
  }
});

ensureCollection("outfitRatings", {
  $jsonSchema: {
    bsonType: "object",
    required: ["outfitId", "userId", "score", "createdAt"],
    properties: {
      outfitId: { bsonType: idType },
      userId: { bsonType: idType },
      score: { bsonType: "int", minimum: 1, maximum: 5 },
      createdAt: { bsonType: "date" }
    }
  }
});


database.outfitFeedback.createIndex({ "author.userId": 1, createdAt: -1 }, { name: "ix_feedback_user_date" });
database.outfitFeedback.createIndex({ outfitId: 1, "author.userId": 1 }, { unique: true, name: "ux_feedback_outfit_user" });
database.outfitFeedback.createIndex({ "author.userId": 1, feeling: 1, createdAt: -1 }, { name: "ix_feedback_user_feeling" });
database.outfitFeedback.createIndex({ createdAt: -1 }, { partialFilterExpression: { photos: { $exists: true } }, name: "ix_feedback_with_photos" });
database.outfitFeedback.createIndex({ text: "text" }, { default_language: "none", name: "tx_feedback_text" });
database.discussionMessages.createIndex({ discussionId: 1, createdAt: 1 }, { name: "ix_messages_discussion_date" });
database.discussions.createIndex({ lastMessageAt: -1 }, { name: "ix_discussions_last" });
database.outfitRatings.createIndex({ outfitId: 1, userId: 1 }, { unique: true, name: "ux_ratings_outfit_user" });

print("outfitFeedbackDb: колекції та індекси готові");