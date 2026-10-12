Wardrobe & Weather Outfits — бази даних для мікросервісної архітектури

Практичне заняття №01 · .NET / ASP.NET · Проєктування баз даних для мікросервісів

## 1. Опис предметної області

Застосунок допомагає користувачеві щодня обирати одяг під погоду. Користувач веде свій **гардероб** (речі з теплотою, температурним діапазоном, водостійкістю). Система бере прогноз погоди, враховує чутливість людини до холоду та вимкнені слоти (наприклад, «без сумки»), і **підбирає образи** у вигляді каруселі. Користувач обирає образ, одягає його й залишає **відгук** («було холодно / комфортно / жарко»). Відгуки впливають на майбутні рекомендації, а також обговорюються іншими користувачами.

### Bounded Contexts

| # | Контекст | Сервіс | БД | Технологія | Лаб. |
|---|----------|--------|----|-----------|------|
| 1 | Рекомендації образів | Recommendations | `OutfitRecommendationsDb` | SQL Server + ADO.NET/Dapper | 02 |
| 2 | Гардероб (каталог речей) | Wardrobe | `WardrobeDb` | SQL Server + EF Core (Code First) | 03 |
| 3 | Відгуки й обговорення | Feedback | `outfitFeedbackDb` | MongoDB | 04 |

![Діаграма контекстів](docs/context-map.png)

Пунктир — лише логічні посилання за ідентифікатором. **Між базами немає жодного FOREIGN KEY, спільних таблиць і міжсервісних JOIN-ів.**

### Власники сутностей

| Контекст | Сутності, якими він володіє |
|----------|----------------------------|
| Рекомендації | Users, UserProfiles, Slots, UserSlotPreferences, Recommendations, WeatherSnapshots, Outfits, OutfitItems, OutfitStatusTransitions, OutfitStatusHistory |
| Гардероб | WardrobeItems (оригінал), ItemDetails, ItemImages, Categories, ItemCategories |
| Відгуки | outfitFeedback, discussions, discussionMessages, outfitRatings |

### Агрегати

- **Recommendation** = Recommendations + WeatherSnapshots (1:1) + Outfits (1:N) + OutfitItems + OutfitStatusHistory. Образи й позиції існують лише в межах рекомендації й видаляються каскадом.
- **WardrobeItem** = WardrobeItems + ItemDetails (1:1) + ItemImages (1:N) + ItemCategories (M:N).
- **Feedback** = відгук з вбудованими `author`, `itemSnapshot`, `comments`; **Discussion** = обговорення + окрема колекція повідомлень (необмежена кількість → reference).

---

## 2. Проєкт №1 — `OutfitRecommendationsDb` (SQL Server + Dapper)

### ERD

![ERD Recommendations](docs/erd-recommendations.png)

### Зв'язки

| Тип | Приклад | Реалізація |
|-----|---------|-----------|
| 1:1 | Recommendations ↔ WeatherSnapshots; Users ↔ UserProfiles | FK є одночасно PK залежної таблиці |
| 1:N | Users → Recommendations; Recommendations → Outfits; Outfits → OutfitStatusHistory | FK на боці «багатьох» |
| M:N | Outfits ↔ WardrobeItems через **OutfitItems**; Users ↔ Slots через **UserSlotPreferences** (власне поле `IsEnabled`) | проміжна таблиця з двома FK |

### Статуси образу

![Діаграма статусів образу](docs/outfit-statuses.png)

Допустимі переходи зберігаються в таблиці `OutfitStatusTransitions`, тому правила можна змінювати даними, без перекомпіляції. Скасувати образ можна лише до того, як його одягнули.

### Обмеження

- **PK / FK:** усі таблиці, каскади лише там, де дані належать агрегату (`Outfits`, `OutfitItems`, `OutfitStatusHistory`, `WeatherSnapshots`). Користувачі, речі й слоти **без** каскаду, щоб не втратити історію.
- **UNIQUE:** `Users.Email`, `Slots.SortOrder`, `(RecommendationId, CarouselPosition)`, `(RecommendationId, ItemsHash)` (однакові образи в каруселі заборонені), `(OutfitId, SlotCode, Layer)` (дві речі в одному слоті й шарі заборонені).
- **CHECK:** `Warmth 1–5`, `MaxTemp > MinTemp`, `WaterResistance 0–3`, `Layer`, `Condition`, `Status`, `PrecipProbability 0–100`, `FitScore 0–100`, `ColdSensitivity −3…3`, координати, `SlotCode = 'Top' OR Layer = 'Base'` (шари лише для верхнього одягу).
- **DEFAULT:** `Layer = 'Base'`, `Status = 'Generated'`, `IsAvailable = 1`, `CreatedAt = SYSUTCDATETIME()`, `IsDeleted = 0`.
- **Аудит і м'яке видалення:** `CreatedAt/By`, `UpdatedAt/By`, `IsDeleted` у бізнес-таблицях (Users, UserProfiles, Recommendations, Outfits). У довідниках і зв'язкових таблицях аудиту немає свідомо.
- **Конкурентність:** `ROWVERSION` в `Users`, `UserProfiles`, `Recommendations`, `Outfits`. Це оптимістична конкурентність: два одночасні редагування одного рядка не затруть одне одного «тихо». Для змін статусу додатково використовується `UPDLOCK`.

### Індекси

| Індекс | Під який запит |
|--------|----------------|
| `IX_WardrobeItems_Eligible` (фільтрований: `IsAvailable=1 AND IsDeleted=0`, покриваючий) | `usp_GetEligibleItems`: доступні речі користувача за слотом |
| `IX_Recommendations_User_Date` (`UserId, ForDate DESC`, фільтрований) | історія рекомендацій користувача з пагінацією |
| `IX_Outfits_ItemsHash` (INCLUDE `RecommendationId, Status`) | перевірка «чи вдягали цей образ нещодавно» (помилка 50014) |
| `UX_Outfits_OneActivePerRecommendation` (унікальний фільтрований) | гарантія: в одній рекомендації лише один обраний/одягнений образ |
| `IX_OutfitItems_WardrobeItemId` | пошук образів за річчю; індекс під FK |
| `IX_OSH_Outfit_ChangedAt` | історія статусів образу у хронологічному порядку |

### Збережувані процедури

| Процедура | Операція | Що робить | Помилки |
|-----------|----------|-----------|---------|
| `usp_CreateRecommendation` | Create | Рекомендація + знімок погоди в одній транзакції | 50001 |
| `usp_GetEligibleItems` | Read | Речі, придатні для погоди: враховує відчутну температуру, `ColdSensitivity`, вимкнені слоти й водостійкість при опадах | 50003 |
| `usp_AddOutfit` | Create (транзакція) | Образ з JSON-списку речей: перевіряє власника, доступність, слоти, дублікати, не-повтор за N днів; пише `ItemsHash`, знімок позицій і початкову історію | 50003, 50011–50015 |
| `usp_ChangeOutfitStatus` | Update (транзакція) | Зміна статусу з `UPDLOCK`, перевіркою переходу за таблицею й історією | 50021–50023 |
| `usp_SetSlotEnabled` | Update (upsert) | Вмикає або вимикає слот для користувача | 50001, 50031 |
| `usp_UpsertWardrobeItemCopy` | Upsert | Ідемпотентна синхронізація копії речі з Гардеробу, застарілі події ігноруються за `SourceUpdatedAt` | 50041 |

Видалення виконується м'яко (`IsDeleted`), окремої процедури видалення немає.

**Коди помилок:** 50001 користувача не знайдено · 50003 рекомендацію не знайдено · 50011 порожній образ · 50012 речі чужі/недоступні/слот вимкнено · 50013 дві речі в одному слоті й шарі · 50014 образ уже одягали нещодавно · 50015 такий образ уже в каруселі · 50021 образ не знайдено · 50022 недопустимий перехід статусу · 50023 інший образ уже обрано · 50031 слот не знайдено · 50041 власника речі не знайдено.

---

## 3. Проєкт №2 — `WardrobeDb` (SQL Server + EF Core)

### ERD

![ERD Wardrobe](docs/erd-wardrobe.png)

### Зв'язки

| Тип | Приклад |
|-----|---------|
| 1:1 | WardrobeItems ↔ ItemDetails (`ItemId` = PK і FK) |
| 1:N | WardrobeItems → ItemImages |
| M:N | WardrobeItems ↔ Categories через **ItemCategories** (власне поле `AddedAt`) |

`OwnerUserId` — звичайний стовпець **без FK**: користувачі належать іншому контексту.

### Обмеження й індекси

- **CHECK:** `Slot ∈ {Top, Bottom, Shoes, Bag, Accessory}`, `Layer ∈ {Base, Mid, Outer}`, `Warmth 1–5`, `WaterResistance 0–3`, `MaxTemp > MinTemp`, `SlotCode = 'Top' OR Layer = 'Base'`.
- **UNIQUE:** `Categories.Name`.
- **Каскади:** деталі, зображення й зв'язки видаляються разом з річчю. `ItemCategories → Categories` **без** каскаду: категорію з речами не можна видалити.
- **Аудит, soft delete, `ROWVERSION`:** у `WardrobeItems`.

| Індекс | Під який запит |
|--------|----------------|
| `IX_WardrobeItems_Owner_Slot` (`OwnerUserId, SlotCode`, INCLUDE, `IsDeleted=0`) | гардероб користувача, відфільтрований за слотом |
| `IX_ItemCategories_CategoryId` | зворотний пошук: усі речі категорії «Зима» |
| `UX_ItemImages_OnePrimary` (унікальний фільтрований `IsPrimary=1`) | у речі лише одне головне фото |
| `IX_ItemImages_Item_Sort` | галерея фото речі у потрібному порядку |

**Міграції.** Схема має ідемпотентні скрипти (`IF OBJECT_ID… IS NULL`) і готова до EF Core Code First: у Лаб. 03 структура відтворюється міграціями.

**Довідникові дані:** 12 категорій (типи одягу та сезони), 21 річ для двох користувачів, деталі, головні фото, зв'язки з категоріями. Ідентифікатори речей **збігаються** з копією в `OutfitRecommendationsDb`.

---

## 4. Проєкт №3 — `outfitFeedbackDb` (MongoDB)

### Колекції та зв'язки

![Діаграма колекцій MongoDB](docs/mongodb-collections.png)

| Прийом | Де застосовано |
|--------|----------------|
| **Embedding** | `author`, `itemSnapshot[]`, `comments[]` (до 5), `photos[]`, бо дані завжди потрібні разом із відгуком |
| **Referencing** | `discussionMessages.discussionId`, `replyTo`: повідомлень можуть бути тисячі |
| **Гібрид** | відгук містить 5 останніх коментарів **і** посилання `discussionId` на повне обговорення (`$lookup`) |
| **Гнучка схема** | в одній колекції `outfitFeedback` є документи з різними наборами полів: `photos`, `temperatureNote`, `weatherAtTime`, `itemNotes` або без жодного з них |
| **Денормалізація** | `author.displayName`, `itemSnapshot[].name` |

Приклади всіх документів: [`feedback-db/sample-documents.json`](feedback-db/sample-documents.json). Це Extended JSON, дати й ObjectId записані як `{"$date": …}` і `{"$oid": …}`.

### Валідація схеми (`$jsonSchema`)

Для кожної колекції задано `validationLevel: strict`, `validationAction: error`. Приклад: `feeling ∈ {Cold, Comfortable, Hot}`, `rating` — int 1–5, `text` ≤ 2000, `photos` ≤ 10, `comments` ≤ 5. Додаткові поля дозволені, бо `additionalProperties` не заборонено, і це дає гнучку схему.

### Індекси

| Індекс | Під який запит |
|--------|----------------|
| `ix_feedback_user_date` (`author.userId, createdAt −1`) | відгуки користувача, найновіші першими, пагінація |
| `ux_feedback_outfit_user` (унікальний `outfitId, author.userId`) | один відгук користувача на образ |
| `ix_feedback_user_feeling` | розподіл відчуттів користувача (оновлення `ColdSensitivity`) |
| `ix_feedback_with_photos` (частковий, `photos` існує) | стрічка відгуків з фото |
| `tx_feedback_text` (text, `default_language: none`) | повнотекстовий пошук по тексту відгуків |
| `ix_messages_discussion_date` | повідомлення обговорення за часом, пагінація |
| `ix_discussions_last` | список обговорень за останньою активністю |
| `ux_ratings_outfit_user` (унікальний) | одна оцінка користувача на образ |

### Запити й агрегації (`queries.js`)

1. Документи з різними наборами полів.
2. Відгуки з фото (частковий індекс).
3. Відгуки користувача з пагінацією (`skip/limit`).
4. Гібрид: відгук + обговорення через `$lookup`.
5. Повідомлення обговорення з пагінацією.
6. Розподіл відчуттів за користувачем за останні ~2 тижні (`$group`).
7. Середня оцінка кожного образу.
8. Найчастіші речі у відгуках (`$unwind` по `itemSnapshot`).
9. Повнотекстовий пошук.
10. Перевірка валідації: вставка з `rating = 9` має завершитися помилкою.

> Демо-дані містять `outfitId` 4, 5, 6, яких немає в Проєкті 1 (там образи 1–3). Це допустимо: FK між базами заборонені, а цілісність такого посилання перевіряє прикладний код.

---

## 5. Політика дублювання даних і узгодженості

| Поле | Де зберігається | Власник даних | Політика узгодженості |
|------|-----------------|---------------|-----------------------|
| `WardrobeItems.Name, SlotCode, Layer, Warmth, MinTemp, MaxTemp, WaterResistance, IsAvailable, IsDeleted` | Рекомендації (копія) | Гардероб (`WardrobeItems` + `ItemDetails`) | **Копія для читання.** Оновлюється подією `WardrobeItemChanged` через `usp_UpsertWardrobeItemCopy`; застарілі події ігноруються за `SourceUpdatedAt` |
| `WardrobeItems.PrimaryImageUrl` | Рекомендації (копія) | Гардероб (`ItemImages`, `IsPrimary = 1`) | Копія для читання, оновлюється тією ж подією |
| `WardrobeItems.OwnerUserId` | Гардероб | Рекомендації (`Users`) | Лише посилання за Id, без FK; не змінюється |
| `OutfitItems.ItemName, ImageUrl, SlotCode, Layer` | Рекомендації | Копія речі в Рекомендаціях → Гардероб | **Знімок на момент створення образу** — не оновлюється (історія образу має лишатися незмінною) |
| `outfitFeedback.outfitId`, `outfitRatings.outfitId` | Відгуки | Рекомендації (`Outfits.Id`) | Лише посилання без FK; існування перевіряє застосунок |
| `author.userId` | Відгуки | Рекомендації (`Users.Id`) | Лише посилання без FK |
| `author.displayName`, `createdBy.displayName` | Відгуки | Рекомендації (`Users.DisplayName`) | **Копія для читання.** Оновлюється подією `UserDisplayNameChanged` (масовий `updateMany` за `author.userId`) |
| `outfitFeedback.itemSnapshot[]` (`itemId, name, slot`) | Відгуки | Рекомендації (`OutfitItems`) | **Знімок** на момент відгуку — не оновлюється |
| `outfitFeedback.weatherAtTime` | Відгуки | Рекомендації (`WeatherSnapshots`; джерело open-meteo) | **Знімок** — не оновлюється |
| `outfitFeedback.rating` | Відгуки | `outfitRatings.score` (та сама БД) | Копія для читання; записується тією ж операцією, власник — `outfitRatings` |
| `UserProfiles.ColdSensitivity` ← `feeling` | Рекомендації | Рекомендації | Відгуки лише **публікують** подію `FeedbackCreated`. Перерахунок і запис виконує тільки Рекомендації, тож dual write немає |
| `Outfits.Status = Reviewed` ← відгук | Рекомендації | Рекомендації | Змінюється через `usp_ChangeOutfitStatus` після події `FeedbackCreated` |

Dual write відсутній: кожне поле має рівно одного власника. Механізм подій реалізується в Лаб. 08.

---

## 6. Первинні ключі та цілісність

- **`BIGINT IDENTITY`** для Проєктів 1 і 2: послідовна вставка з одного джерела, компактний кластерний ключ.
- **`Id` без IDENTITY** у копії `WardrobeItems` (Проєкт 1): ідентифікатор присвоює власник (Гардероб).
- **`ObjectId`** у MongoDB: генерується клієнтом, без координації.
- **Каскадне видалення** лише для частин агрегату (позиції образу, деталі речі, фото). Для користувачів, слотів і категорій каскаду немає свідомо.

## 7. Безпека (принцип найменших привілеїв)

Приклад окремого логіна застосунку лише для своєї БД:

```sql
CREATE LOGIN app_recommendations WITH PASSWORD = '<StrongPassword!>';
USE OutfitRecommendationsDb;
CREATE USER app_recommendations FOR LOGIN app_recommendations;
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::dbo TO app_recommendations;
GRANT EXECUTE ON SCHEMA::dbo TO app_recommendations;
```

Для `WardrobeDb` і MongoDB створюються окремі облікові записи з доступом лише до своєї бази.

---

## 8. Розгортання з нуля

### Вимоги

SQL Server **2017+** (потрібні `STRING_AGG`, `OPENJSON`, `CREATE OR ALTER`), SSMS, MongoDB 6+ (локально або в Docker), `mongosh` та за потреби MongoDB Database Tools (`mongoimport`).

### Структура репозиторію

```
.
├── README.md
├── docs/                      context-map.png, erd-recommendations.png, erd-wardrobe.png, outfit-statuses.png, mongodb-collections.png
├── recommendations-db/        schema.sql, procedures.sql, seed.sql, tests.sql
├── wardrobe-db/               schema.sql, seed.sql
└── feedback-db/               init.js, seed.js, queries.js, sample-documents.json, samples/*.json
```

### Проєкт №1: `OutfitRecommendationsDb` (SSMS)

Файли виконуй **у такому порядку** (кожен у новому вікні запиту, `F5`):

1. `schema.sql`: створює БД, таблиці й індекси.
2. `procedures.sql`: 6 збережуваних процедур.
3. `seed.sql`: довідники, користувачі, речі, дві рекомендації (викликає процедури, тому йде **після** `procedures.sql`).
4. `tests.sql`: перевірки (див. розділ 9).

Те саме через `sqlcmd` (прапорець `-I` обов'язковий для `QUOTED_IDENTIFIER`):

```bash
sqlcmd -S localhost -E -I -i recommendations-db/schema.sql
sqlcmd -S localhost -E -I -i recommendations-db/procedures.sql
sqlcmd -S localhost -E -I -i recommendations-db/seed.sql
```

### Проєкт №2: `WardrobeDb`

1. `wardrobe-db/schema.sql`
2. `wardrobe-db/seed.sql`

### Проєкт №3: `outfitFeedbackDb`

```bash
docker run -d --name mongo -p 27017:27017 mongo:7
mongosh "mongodb://localhost:27017" --file feedback-db/init.js
mongosh "mongodb://localhost:27017" --file feedback-db/seed.js
mongosh "mongodb://localhost:27017" --file feedback-db/queries.js
```

Альтернатива кроку 2: імпорт JSON-файлів.

```bash
mongoimport --uri "mongodb://localhost:27017/outfitFeedbackDb" --collection outfitFeedback      --file feedback-db/samples/outfitFeedback.json      --jsonArray --mode upsert
mongoimport --uri "mongodb://localhost:27017/outfitFeedbackDb" --collection discussions         --file feedback-db/samples/discussions.json         --jsonArray --mode upsert
mongoimport --uri "mongodb://localhost:27017/outfitFeedbackDb" --collection discussionMessages  --file feedback-db/samples/discussionMessages.json  --jsonArray --mode upsert
mongoimport --uri "mongodb://localhost:27017/outfitFeedbackDb" --collection outfitRatings       --file feedback-db/samples/outfitRatings.json       --jsonArray --mode upsert
```

У MongoDB Compass: відкрий колекцію → **Add Data → Import JSON file**. Імпорт має виконуватися **після** `init.js`, інакше колекції створяться без валідації й індексів.

---

## 9. Перевірка

| Перевірка | Як | Очікувано |
|-----------|----|-----------|
| Немає FK за межі БД | `SELECT * FROM sys.foreign_keys;` в обох SQL-базах | усі FK лише всередині своєї бази |
| Недопустимий перехід статусу | `tests.sql`: `Generated → Worn` | керована помилка 50022, дія не виконується |
| Один обраний образ | другий образ → `Chosen` | помилка 50023 |
| Скасований образ не воскресає | `Cancelled → Chosen` | помилка 50022 |
| Повтор образу | `usp_AddOutfit` з тим самим набором речей | помилка 50014 |
| Чужі речі або вимкнений слот | `usp_AddOutfit` | помилка 50012 |
| Підбір речей під погоду | `usp_GetEligibleItems` для снігу 15.12 | лише теплі й водостійкі речі |
| Вимкнений слот | `usp_SetSlotEnabled` + `usp_GetEligibleItems` | аксесуарів у виборці немає |
| Сліпа синхронізація | `usp_UpsertWardrobeItemCopy` зі старішою датою | зміни ігноруються |
| Ідемпотентність seed | повторний запуск `seed.sql`, `seed.js` | дублікатів немає |
| Гнучка схема | `db.outfitFeedback.find()` | документи з різними наборами полів |
| Валідація Mongo | вставка з `rating = 9` | помилка `DocumentValidationFailure` |

> **Примітка щодо `tests.sql`.** Набір змінює дані (образ №1 стає `Cancelled`, а в речі №9 `SourceUpdatedAt` виставляється у 2099 рік), тому для повторного запуску перестворюй БД.
