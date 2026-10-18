
SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;
GO
USE OutfitRecommendationsDb;
GO

INSERT dbo.Slots (Code, Name, SortOrder)
SELECT v.Code, v.Name, v.SortOrder
FROM (VALUES (N'Top', N'Верх', 1), (N'Bottom', N'Низ', 2), (N'Shoes', N'Взуття', 3),
             (N'Bag', N'Сумка', 4), (N'Accessory', N'Аксесуари', 5)) v(Code, Name, SortOrder)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Slots s WHERE s.Code = v.Code);

INSERT dbo.OutfitStatusTransitions (FromStatus, ToStatus)
SELECT v.F, v.T
FROM (VALUES (N'Generated', N'Chosen'), (N'Generated', N'Cancelled'),
             (N'Chosen', N'Worn'),      (N'Chosen', N'Cancelled'),
             (N'Worn', N'Reviewed')) v(F, T)
WHERE NOT EXISTS (SELECT 1 FROM dbo.OutfitStatusTransitions t WHERE t.FromStatus = v.F AND t.ToStatus = v.T);

SET IDENTITY_INSERT dbo.Users ON;
INSERT dbo.Users (Id, Email, DisplayName, CreatedBy)
SELECT v.Id, v.Email, v.DisplayName, N'seed'
FROM (VALUES (1, N'olena@example.com', N'Олена'), (2, N'max@example.com', N'Макс')) v(Id, Email, DisplayName)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Users u WHERE u.Id = v.Id);
SET IDENTITY_INSERT dbo.Users OFF;

INSERT dbo.UserProfiles (UserId, City, Latitude, Longitude, ColdSensitivity, StylePreference, CreatedBy)
SELECT v.UserId, v.City, v.Lat, v.Lon, v.Cold, v.Style, N'seed'
FROM (VALUES (1, N'Тернопіль', 49.553516, 25.594767, 1.0, N'Casual'),
             (2, N'Львів',     49.839683, 24.029717, -0.5, N'Street')) v(UserId, City, Lat, Lon, Cold, Style)
WHERE NOT EXISTS (SELECT 1 FROM dbo.UserProfiles p WHERE p.UserId = v.UserId);

INSERT dbo.UserSlotPreferences (UserId, SlotCode, IsEnabled)
SELECT v.UserId, v.SlotCode, v.IsEnabled
FROM (VALUES (2, N'Bag', 0), (2, N'Accessory', 0)) v(UserId, SlotCode, IsEnabled)
WHERE NOT EXISTS (SELECT 1 FROM dbo.UserSlotPreferences p WHERE p.UserId = v.UserId AND p.SlotCode = v.SlotCode);

INSERT dbo.WardrobeItems (Id, OwnerUserId, Name, SlotCode, Layer, Warmth, MinTemp, MaxTemp, WaterResistance, IsAvailable, PrimaryImageUrl, SourceUpdatedAt)
SELECT v.Id, v.Owner, v.Name, v.Slot, v.Layer, v.Warmth, v.MinT, v.MaxT, v.Water, 1, CONCAT(N'/img/wardrobe/', v.Id, N'.jpg'), SYSUTCDATETIME()
FROM (VALUES
 (1,  1, N'Пухова куртка',         N'Top',       N'Outer', 5, -30,  5, 2),
 (2,  1, N'Легка вітровка',        N'Top',       N'Outer', 2,   8, 20, 2),
 (3,  1, N'Вовняний светр',        N'Top',       N'Mid',   4, -10, 14, 0),
 (4,  1, N'Біла футболка',         N'Top',       N'Base',  1, -20, 40, 0),
 (5,  1, N'Термо-лонгслів',        N'Top',       N'Base',  3, -25, 10, 0),
 (6,  1, N'Утеплені джинси',       N'Bottom',    N'Base',  4, -15,  8, 0),
 (7,  1, N'Класичні джинси',       N'Bottom',    N'Base',  2,   0, 22, 0),
 (8,  1, N'Джинсові шорти',        N'Bottom',    N'Base',  1,  20, 40, 0),
 (9,  1, N'Зимові черевики',       N'Shoes',     N'Base',  5, -30,  8, 3),
 (10, 1, N'Білі кросівки',         N'Shoes',     N'Base',  2,   4, 30, 1),
 (11, 1, N'Вовняна шапка',         N'Accessory', N'Base',  4, -30,  8, 0),
 (12, 1, N'Сумка-шопер',           N'Bag',       N'Base',  1, -30, 40, 0),
 (13, 1, N'Рюкзак водостійкий',    N'Bag',       N'Base',  1, -30, 40, 2),
 (14, 1, N'Сонцезахисні окуляри',  N'Accessory', N'Base',  1,  12, 40, 0),
 (15, 1, N'Вовняний шарф',         N'Accessory', N'Base',  3, -30, 10, 0),
 (16, 2, N'Парка',                 N'Top',       N'Outer', 5, -30,  3, 2),
 (17, 2, N'Худі',                  N'Top',       N'Mid',   3,  -5, 16, 0),
 (18, 2, N'Чорна футболка',        N'Top',       N'Base',  1, -20, 40, 0),
 (19, 2, N'Чінос',                 N'Bottom',    N'Base',  2,   0, 25, 0),
 (20, 2, N'Зимові кросівки',       N'Shoes',     N'Base',  4, -20,  8, 2),
 (21, 2, N'Бейсболка',             N'Accessory', N'Base',  1,  10, 40, 0)
) v(Id, Owner, Name, Slot, Layer, Warmth, MinT, MaxT, Water)
WHERE NOT EXISTS (SELECT 1 FROM dbo.WardrobeItems w WHERE w.Id = v.Id);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Recommendations WHERE UserId = 1 AND ForDate = '2026-10-08')
BEGIN
    DECLARE @RecA BIGINT, @OutA BIGINT;
    EXEC dbo.usp_CreateRecommendation @UserId = 1, @ForDate = '2026-10-08', @City = N'Тернопіль',
         @TempMin = 4, @TempMax = 12, @FeelsLikeMin = 3, @FeelsLikeMax = 11, @PrecipProbability = 20,
         @WindSpeedMs = 4.5, @UvIndex = 2, @Condition = N'Cloudy', @CreatedBy = N'seed', @RecommendationId = @RecA OUTPUT;
    EXEC dbo.usp_AddOutfit @RecommendationId = @RecA, @FitScore = 88, @ItemIdsJson = N'[2,3,4,7,10,12]',
         @CreatedBy = N'seed', @OutfitId = @OutA OUTPUT;
    EXEC dbo.usp_ChangeOutfitStatus @OutA, N'Chosen',   N'seed';
    EXEC dbo.usp_ChangeOutfitStatus @OutA, N'Worn',     N'seed';
    EXEC dbo.usp_ChangeOutfitStatus @OutA, N'Reviewed', N'seed';
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Recommendations WHERE UserId = 1 AND ForDate = '2026-12-15')
BEGIN
    DECLARE @RecB BIGINT, @OutB1 BIGINT, @OutB2 BIGINT;
    EXEC dbo.usp_CreateRecommendation @UserId = 1, @ForDate = '2026-12-15', @City = N'Тернопіль',
         @TempMin = -6, @TempMax = -1, @FeelsLikeMin = -9, @FeelsLikeMax = -3, @PrecipProbability = 80,
         @WindSpeedMs = 6, @UvIndex = 1, @Condition = N'Snow', @CreatedBy = N'seed', @RecommendationId = @RecB OUTPUT;
    EXEC dbo.usp_AddOutfit @RecommendationId = @RecB, @FitScore = 95, @ItemIdsJson = N'[1,5,6,9,11,13]',
         @CreatedBy = N'seed', @OutfitId = @OutB1 OUTPUT;
    EXEC dbo.usp_AddOutfit @RecommendationId = @RecB, @FitScore = 90, @ItemIdsJson = N'[1,3,5,6,9,15,12]',
         @CreatedBy = N'seed', @OutfitId = @OutB2 OUTPUT;
END
GO