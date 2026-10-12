SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;
GO
USE WardrobeDb;
GO

INSERT dbo.Categories (Name)
SELECT v.Name
FROM (VALUES (N'Верхній одяг'), (N'Светри та худі'), (N'Футболки'), (N'Штани'), (N'Шорти'),
             (N'Взуття'), (N'Сумки'), (N'Головні убори'), (N'Шарфи'),
             (N'Зима'), (N'Демісезон'), (N'Літо')) v(Name)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Categories c WHERE c.Name = v.Name);

SET IDENTITY_INSERT dbo.WardrobeItems ON;
INSERT dbo.WardrobeItems (Id, OwnerUserId, Name, SlotCode, Layer, IsAvailable, CreatedBy)
SELECT v.Id, v.Owner, v.Name, v.Slot, v.Layer, 1, N'seed'
FROM (VALUES
 (1,  1, N'Пухова куртка',        N'Top',       N'Outer'),
 (2,  1, N'Легка вітровка',       N'Top',       N'Outer'),
 (3,  1, N'Вовняний светр',       N'Top',       N'Mid'),
 (4,  1, N'Біла футболка',        N'Top',       N'Base'),
 (5,  1, N'Термо-лонгслів',       N'Top',       N'Base'),
 (6,  1, N'Утеплені джинси',      N'Bottom',    N'Base'),
 (7,  1, N'Класичні джинси',      N'Bottom',    N'Base'),
 (8,  1, N'Джинсові шорти',       N'Bottom',    N'Base'),
 (9,  1, N'Зимові черевики',      N'Shoes',     N'Base'),
 (10, 1, N'Білі кросівки',        N'Shoes',     N'Base'),
 (11, 1, N'Вовняна шапка',        N'Accessory', N'Base'),
 (12, 1, N'Сумка-шопер',          N'Bag',       N'Base'),
 (13, 1, N'Рюкзак водостійкий',   N'Bag',       N'Base'),
 (14, 1, N'Сонцезахисні окуляри', N'Accessory', N'Base'),
 (15, 1, N'Вовняний шарф',        N'Accessory', N'Base'),
 (16, 2, N'Парка',                N'Top',       N'Outer'),
 (17, 2, N'Худі',                 N'Top',       N'Mid'),
 (18, 2, N'Чорна футболка',       N'Top',       N'Base'),
 (19, 2, N'Чінос',                N'Bottom',    N'Base'),
 (20, 2, N'Зимові кросівки',      N'Shoes',     N'Base'),
 (21, 2, N'Бейсболка',            N'Accessory', N'Base')
) v(Id, Owner, Name, Slot, Layer)
WHERE NOT EXISTS (SELECT 1 FROM dbo.WardrobeItems w WHERE w.Id = v.Id);
SET IDENTITY_INSERT dbo.WardrobeItems OFF;

INSERT dbo.ItemDetails (ItemId, Material, Color, Warmth, MinTemp, MaxTemp, WaterResistance, IsWindproof)
SELECT v.Id, v.Material, v.Color, v.Warmth, v.MinT, v.MaxT, v.Water, v.Wind
FROM (VALUES
 (1,  N'Пух/поліестер',    N'Чорний',     5, -30,  5, 2, 1),
 (2,  N'Нейлон',           N'Оливковий',  2,   8, 20, 2, 1),
 (3,  N'Вовна',            N'Бежевий',    4, -10, 14, 0, 0),
 (4,  N'Бавовна',          N'Білий',      1, -20, 40, 0, 0),
 (5,  N'Мериносова вовна', N'Сірий',      3, -25, 10, 0, 0),
 (6,  N'Деним з флісом',   N'Синій',      4, -15,  8, 0, 0),
 (7,  N'Деним',            N'Синій',      2,   0, 22, 0, 0),
 (8,  N'Деним',            N'Блакитний',  1,  20, 40, 0, 0),
 (9,  N'Шкіра/мембрана',   N'Коричневий', 5, -30,  8, 3, 1),
 (10, N'Текстиль',         N'Білий',      2,   4, 30, 1, 0),
 (11, N'Вовна',            N'Сірий',      4, -30,  8, 0, 1),
 (12, N'Канвас',           N'Бежевий',    1, -30, 40, 0, 0),
 (13, N'Нейлон',           N'Чорний',     1, -30, 40, 2, 0),
 (14, N'Пластик',          N'Чорний',     1,  12, 40, 0, 0),
 (15, N'Вовна',            N'Червоний',   3, -30, 10, 0, 0),
 (16, N'Поліестер',        N'Хакі',       5, -30,  3, 2, 1),
 (17, N'Фліс',             N'Сірий',      3,  -5, 16, 0, 0),
 (18, N'Бавовна',          N'Чорний',     1, -20, 40, 0, 0),
 (19, N'Бавовна',          N'Бежевий',    2,   0, 25, 0, 0),
 (20, N'Мембрана',         N'Чорний',     4, -20,  8, 2, 1),
 (21, N'Бавовна',          N'Чорний',     1,  10, 40, 0, 0)
) v(Id, Material, Color, Warmth, MinT, MaxT, Water, Wind)
WHERE NOT EXISTS (SELECT 1 FROM dbo.ItemDetails d WHERE d.ItemId = v.Id);

INSERT dbo.ItemImages (ItemId, Url, IsPrimary, SortOrder)
SELECT w.Id, CONCAT(N'/img/wardrobe/', w.Id, N'.jpg'), 1, 0
FROM dbo.WardrobeItems w
WHERE NOT EXISTS (SELECT 1 FROM dbo.ItemImages i WHERE i.ItemId = w.Id AND i.IsPrimary = 1);

INSERT dbo.ItemCategories (ItemId, CategoryId)
SELECT m.ItemId, c.Id
FROM (VALUES
 (1, N'Верхній одяг'), (1, N'Зима'),
 (2, N'Верхній одяг'), (2, N'Демісезон'),
 (3, N'Светри та худі'), (3, N'Демісезон'), (3, N'Зима'),
 (4, N'Футболки'), (4, N'Літо'), (4, N'Демісезон'),
 (5, N'Футболки'), (5, N'Зима'),
 (6, N'Штани'), (6, N'Зима'),
 (7, N'Штани'), (7, N'Демісезон'),
 (8, N'Шорти'), (8, N'Літо'),
 (9, N'Взуття'), (9, N'Зима'),
 (10, N'Взуття'), (10, N'Демісезон'), (10, N'Літо'),
 (11, N'Головні убори'), (11, N'Зима'),
 (12, N'Сумки'), (13, N'Сумки'),
 (14, N'Літо'),
 (15, N'Шарфи'), (15, N'Зима'),
 (16, N'Верхній одяг'), (16, N'Зима'),
 (17, N'Светри та худі'), (17, N'Демісезон'),
 (18, N'Футболки'), (19, N'Штани'), (19, N'Демісезон'),
 (20, N'Взуття'), (20, N'Зима'),
 (21, N'Головні убори'), (21, N'Літо')
) m(ItemId, CatName)
JOIN dbo.Categories c ON c.Name = m.CatName
WHERE NOT EXISTS (SELECT 1 FROM dbo.ItemCategories ic WHERE ic.ItemId = m.ItemId AND ic.CategoryId = c.Id);
GO

SELECT w.SlotCode, w.Layer, w.Name, d.Warmth, d.MinTemp, d.MaxTemp, w.IsAvailable
FROM dbo.WardrobeItems w JOIN dbo.ItemDetails d ON d.ItemId = w.Id
WHERE w.OwnerUserId = 1 AND w.IsDeleted = 0 ORDER BY w.SlotCode, w.Layer;

SELECT w.Name, ic.AddedAt FROM dbo.ItemCategories ic
JOIN dbo.Categories c ON c.Id = ic.CategoryId JOIN dbo.WardrobeItems w ON w.Id = ic.ItemId
WHERE c.Name = N'Зима';

SELECT fk.name, OBJECT_NAME(fk.parent_object_id) AS ChildTable, OBJECT_NAME(fk.referenced_object_id) AS ParentTable FROM sys.foreign_keys fk;