
const database = db.getSiblingDB("outfitFeedbackDb");

print("\n--- 1. Документи з РІЗНИМИ наборами полів (гнучка схема) ---");
printjson(database.outfitFeedback.find({}, { feeling: 1, rating: 1, photos: 1, temperatureNote: 1, weatherAtTime: 1, itemNotes: 1 }).toArray());

print("\n--- 2. Лише відгуки з фото (частковий індекс ix_feedback_with_photos) ---");
printjson(database.outfitFeedback.find({ photos: { $exists: true } }, { outfitId: 1, photos: 1 }).toArray());

print("\n--- 3. Відгуки користувача 1, найновіші першими, сторінка 1 по 10 (ix_feedback_user_date) ---");
printjson(database.outfitFeedback.find({ "author.userId": 1 }).sort({ createdAt: -1 }).skip(0).limit(10).toArray());

print("\n--- 4. Гібрид: відгук + повне обговорення ($lookup за discussionId) ---");
printjson(database.outfitFeedback.aggregate([
  { $match: { _id: ObjectId("66f0c2a1b2c3d4e5f6000001") } },
  { $lookup: { from: "discussions", localField: "discussionId", foreignField: "_id", as: "discussion" } },
  { $unwind: "$discussion" },
  { $project: { text: 1, comments: 1, "discussion.title": 1, "discussion.messagesCount": 1 } }
]).toArray());

print("\n--- 5. Повідомлення обговорення з пагінацією (ix_messages_discussion_date) ---");
printjson(database.discussionMessages.find({ discussionId: ObjectId("66f0c3a1b2c3d4e5f6000001") })
  .sort({ createdAt: 1 }).skip(0).limit(20).toArray());

print("\n--- 6. Розподіл відчуттів за користувачем (для оновлення ColdSensitivity) ---");
printjson(database.outfitFeedback.aggregate([
  { $match: { createdAt: { $gte: ISODate("2026-09-25T00:00:00Z") } } },   // останні ~2 тижні
  { $group: { _id: { userId: "$author.userId", feeling: "$feeling" }, count: { $sum: 1 } } },
  { $sort: { "_id.userId": 1, "_id.feeling": 1 } }
]).toArray());

print("\n--- 7. Середня оцінка кожного образу (outfitRatings) ---");
printjson(database.outfitRatings.aggregate([
  { $group: { _id: "$outfitId", avgScore: { $avg: "$score" }, votes: { $sum: 1 } } },
  { $sort: { avgScore: -1 } }
]).toArray());

print("\n--- 8. Найчастіші речі у відгуках «Cold» (розгортаємо вбудований масив itemSnapshot) ---");
printjson(database.outfitFeedback.aggregate([
  { $unwind: "$itemSnapshot" },
  { $group: { _id: "$itemSnapshot.name", feedbacks: { $sum: 1 } } },
  { $sort: { feedbacks: -1 } }
]).toArray());

print("\n--- 9. Повнотекстовий пошук ---");
printjson(database.outfitFeedback.find({ $text: { $search: "светр" } }, { text: 1 }).toArray());

print("\n--- 10. Перевірка валідації схеми: очікується помилка (rating = 9) ---");
try {
  database.outfitFeedback.insertOne({ outfitId: 99, author: { userId: 1, displayName: "Тест" },
    feeling: "Hot", rating: NumberInt(9), createdAt: new Date() });
} catch (e) { print("OK, валідація спрацювала: " + e.codeName); }
