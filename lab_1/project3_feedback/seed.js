const database = db.getSiblingDB("outfitFeedbackDb");

const FB1 = ObjectId("66f0c2a1b2c3d4e5f6000001");
const FB2 = ObjectId("66f0c2a1b2c3d4e5f6000002");
const FB3 = ObjectId("66f0c2a1b2c3d4e5f6000003");
const FB4 = ObjectId("66f0c2a1b2c3d4e5f6000004");
const D1  = ObjectId("66f0c3a1b2c3d4e5f6000001");
const D2  = ObjectId("66f0c3a1b2c3d4e5f6000002");

function put(coll, doc) {
  database[coll].updateOne({ _id: doc._id }, { $setOnInsert: doc }, { upsert: true });
}

put("outfitFeedback", {
  _id: FB1, outfitId: 1,
  author: { userId: 1, displayName: "Олена" },
  feeling: "Comfortable", rating: NumberInt(5),
  text: "Вітровка і светр виручили: зранку прохолодно, а вдень светр лишився в рюкзаку.",
  photos: ["fb1_a.jpg", "fb1_b.jpg"],
  itemSnapshot: [
    { itemId: 2, name: "Легка вітровка", slot: "Top" },
    { itemId: 3, name: "Вовняний светр", slot: "Top" },
    { itemId: 7, name: "Класичні джинси", slot: "Bottom" },
    { itemId: 10, name: "Білі кросівки", slot: "Shoes" }
  ],
  comments: [
    { author: "Андрій", text: "Класний образ", createdAt: ISODate("2026-10-08T19:10:00Z") },
    { author: "Марія", text: "Де такий светр?", createdAt: ISODate("2026-10-08T20:02:00Z") }
  ],
  discussionId: D1,
  createdAt: ISODate("2026-10-08T18:30:00Z")
});

put("outfitFeedback", {
  _id: FB2, outfitId: 4,
  author: { userId: 2, displayName: "Макс" },
  feeling: "Cold",
  text: "Було холодніше, ніж показувала погода.",
  temperatureNote: { felt: -2, forecast: 3 },
  createdAt: ISODate("2026-10-09T08:15:00Z")
});

put("outfitFeedback", {
  _id: FB3, outfitId: 5,
  author: { userId: 1, displayName: "Олена" },
  feeling: "Hot", rating: NumberInt(3),
  weatherAtTime: { condition: "Clear", feelsLike: 17 },
  itemNotes: [{ itemId: 3, note: "Светр завтеплий для 17 градусів" }],
  createdAt: ISODate("2026-10-09T15:40:00Z")
});

put("outfitFeedback", {
  _id: FB4, outfitId: 6,
  author: { userId: 2, displayName: "Макс" },
  feeling: "Comfortable", rating: NumberInt(4),
  createdAt: ISODate("2026-10-09T17:00:00Z")
});

put("discussions", {
  _id: D1, title: "Як носити шари восени", feedbackId: FB1,
  createdBy: { userId: 1, displayName: "Олена" },
  tags: ["шари", "осінь"], messagesCount: NumberInt(3),
  lastMessageAt: ISODate("2026-10-09T09:00:00Z"), createdAt: ISODate("2026-10-08T18:35:00Z")
});
put("discussions", {
  _id: D2, title: "Зимові черевики: що обрати", 
  createdBy: { userId: 2, displayName: "Макс" },
  tags: ["зима", "взуття"], messagesCount: NumberInt(1),
  lastMessageAt: ISODate("2026-10-09T10:00:00Z"), createdAt: ISODate("2026-10-09T10:00:00Z")
});

const M1 = ObjectId("66f0c4a1b2c3d4e5f6000001");
put("discussionMessages", { _id: M1, discussionId: D1, author: { userId: 1, displayName: "Олена" },
  text: "Беру светр на випадок похолодання, а вітровку зверху.", createdAt: ISODate("2026-10-08T18:36:00Z") });
put("discussionMessages", { _id: ObjectId("66f0c4a1b2c3d4e5f6000002"), discussionId: D1, replyTo: M1,
  author: { userId: 2, displayName: "Макс" }, text: "Я теж так роблю, зручно.", createdAt: ISODate("2026-10-08T21:00:00Z") });
put("discussionMessages", { _id: ObjectId("66f0c4a1b2c3d4e5f6000003"), discussionId: D1,
  author: { userId: 1, displayName: "Олена" }, text: "Дякую за поради!", createdAt: ISODate("2026-10-09T09:00:00Z") });
put("discussionMessages", { _id: ObjectId("66f0c4a1b2c3d4e5f6000004"), discussionId: D2,
  author: { userId: 2, displayName: "Макс" }, text: "Шукаю черевики з мембраною до -15.", createdAt: ISODate("2026-10-09T10:00:00Z") });

[
  { _id: ObjectId("66f0c5a1b2c3d4e5f6000001"), outfitId: 1, userId: 1, score: NumberInt(5), createdAt: ISODate("2026-10-08T18:31:00Z") },
  { _id: ObjectId("66f0c5a1b2c3d4e5f6000002"), outfitId: 1, userId: 2, score: NumberInt(4), createdAt: ISODate("2026-10-08T19:00:00Z") },
  { _id: ObjectId("66f0c5a1b2c3d4e5f6000003"), outfitId: 5, userId: 1, score: NumberInt(3), createdAt: ISODate("2026-10-09T15:41:00Z") }
].forEach(d => put("outfitRatings", d));

print("Seed виконано. Документів у outfitFeedback: " + database.outfitFeedback.countDocuments());
