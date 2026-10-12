/* =====================================================================
   ПРОЄКТ №1. Збережувані процедури (6 шт.)
   Коди помилок:
   50001 користувача не знайдено      50003 рекомендацію не знайдено
   50011 порожній образ               50012 речі недоступні/чужі/слот вимкнено
   50013 дві речі в одному слоті+шарі 50014 образ вже одягали нещодавно
   50015 такий образ уже є в каруселі 50021 образ не знайдено
   50022 недопустимий перехід статусу 50023 інший образ уже обрано
   50031 слот не знайдено             50041 власника речі не знайдено
   ===================================================================== */
SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;
GO
USE OutfitRecommendationsDb;
GO

CREATE OR ALTER PROCEDURE dbo.usp_CreateRecommendation
    @UserId            BIGINT,
    @ForDate           DATE,
    @City              NVARCHAR(100),
    @TempMin           DECIMAL(4,1),
    @TempMax           DECIMAL(4,1),
    @FeelsLikeMin      DECIMAL(4,1),
    @FeelsLikeMax      DECIMAL(4,1),
    @PrecipProbability TINYINT,
    @WindSpeedMs       DECIMAL(4,1),
    @UvIndex           DECIMAL(3,1) = NULL,
    @Condition         NVARCHAR(20),
    @CreatedBy         NVARCHAR(100) = NULL,
    @RecommendationId  BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Id = @UserId AND IsDeleted = 0)
        THROW 50001, 'User not found', 1;

    BEGIN TRANSACTION;

    INSERT dbo.Recommendations (UserId, ForDate, CreatedBy) VALUES (@UserId, @ForDate, @CreatedBy);
    SET @RecommendationId = SCOPE_IDENTITY();

    INSERT dbo.WeatherSnapshots
        (RecommendationId, City, TempMin, TempMax, FeelsLikeMin, FeelsLikeMax, PrecipProbability, WindSpeedMs, UvIndex, Condition)
    VALUES
        (@RecommendationId, @City, @TempMin, @TempMax, @FeelsLikeMin, @FeelsLikeMax, @PrecipProbability, @WindSpeedMs, @UvIndex, @Condition);

    COMMIT TRANSACTION;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_GetEligibleItems
    @RecommendationId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @UserId BIGINT, @FlMin DECIMAL(4,1), @FlMax DECIMAL(4,1), @Precip TINYINT, @Cond NVARCHAR(20);

    SELECT @UserId = r.UserId, @FlMin = w.FeelsLikeMin, @FlMax = w.FeelsLikeMax,
           @Precip = w.PrecipProbability, @Cond = w.Condition
    FROM dbo.Recommendations r
    JOIN dbo.WeatherSnapshots w ON w.RecommendationId = r.Id
    WHERE r.Id = @RecommendationId AND r.IsDeleted = 0;

    IF @UserId IS NULL
        THROW 50003, 'Recommendation not found', 1;

    DECLARE @Sens DECIMAL(3,1) = ISNULL((SELECT ColdSensitivity FROM dbo.UserProfiles WHERE UserId = @UserId), 0);
    DECLARE @EffMin DECIMAL(5,1) = @FlMin - @Sens * 2;
    DECLARE @EffMax DECIMAL(5,1) = @FlMax - @Sens * 2;
    DECLARE @NeedWater BIT = CASE WHEN @Precip >= 50 OR @Cond IN (N'Rain', N'Snow', N'Storm') THEN 1 ELSE 0 END;

    SELECT i.Id, i.Name, i.SlotCode, i.Layer, i.Warmth, i.WaterResistance, i.PrimaryImageUrl
    FROM dbo.WardrobeItems i
    JOIN dbo.Slots s ON s.Code = i.SlotCode
    LEFT JOIN dbo.UserSlotPreferences p ON p.UserId = i.OwnerUserId AND p.SlotCode = i.SlotCode
    WHERE i.OwnerUserId = @UserId
      AND i.IsAvailable = 1 AND i.IsDeleted = 0
      AND ISNULL(p.IsEnabled, 1) = 1
      AND i.MinTemp <= @EffMax AND i.MaxTemp >= @EffMin                                   
      AND (i.SlotCode NOT IN (N'Bottom', N'Shoes') OR i.MinTemp <= @EffMin + 3)           
      AND (@NeedWater = 0
           OR NOT (i.SlotCode = N'Shoes' OR (i.SlotCode = N'Top' AND i.Layer = N'Outer'))
           OR i.WaterResistance >= 2)                                                      
    ORDER BY s.SortOrder, i.Layer, i.Warmth DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_AddOutfit
    @RecommendationId BIGINT,
    @FitScore         DECIMAL(5,2),
    @ItemIdsJson      NVARCHAR(MAX),
    @CreatedBy        NVARCHAR(100) = NULL,
    @NoRepeatDays     INT = 3,
    @OutfitId         BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    DECLARE @UserId BIGINT, @ForDate DATE;
    SELECT @UserId = UserId, @ForDate = ForDate
    FROM dbo.Recommendations WITH (UPDLOCK)          -- серіалізуємо додавання образів у межах однієї рекомендації
    WHERE Id = @RecommendationId AND IsDeleted = 0;

    IF @UserId IS NULL
        THROW 50003, 'Recommendation not found', 1;

    DECLARE @Items TABLE (ItemId BIGINT PRIMARY KEY);
    INSERT @Items (ItemId) SELECT DISTINCT CAST([value] AS BIGINT) FROM OPENJSON(@ItemIdsJson);

    DECLARE @Cnt INT = (SELECT COUNT(*) FROM @Items);
    IF @Cnt = 0
        THROW 50011, 'Outfit must contain at least one item', 1;

    IF (SELECT COUNT(*)
        FROM @Items it
        JOIN dbo.WardrobeItems w ON w.Id = it.ItemId
        LEFT JOIN dbo.UserSlotPreferences p ON p.UserId = w.OwnerUserId AND p.SlotCode = w.SlotCode
        WHERE w.OwnerUserId = @UserId AND w.IsAvailable = 1 AND w.IsDeleted = 0 AND ISNULL(p.IsEnabled, 1) = 1) <> @Cnt
        THROW 50012, 'Some items are not owned, unavailable or belong to a disabled slot', 1;

    IF EXISTS (SELECT 1 FROM @Items it JOIN dbo.WardrobeItems w ON w.Id = it.ItemId
               GROUP BY w.SlotCode, w.Layer HAVING COUNT(*) > 1)
        THROW 50013, 'Two items in the same slot and layer', 1;

    DECLARE @Hash CHAR(64) = CONVERT(CHAR(64),
        HASHBYTES('SHA2_256', (SELECT STRING_AGG(CAST(ItemId AS NVARCHAR(20)), N',') WITHIN GROUP (ORDER BY ItemId) FROM @Items)), 2);

    IF EXISTS (SELECT 1
               FROM dbo.Outfits o
               JOIN dbo.Recommendations r ON r.Id = o.RecommendationId
               WHERE r.UserId = @UserId AND o.ItemsHash = @Hash AND o.IsDeleted = 0
                 AND o.Status IN (N'Worn', N'Reviewed')
                 AND r.ForDate >= DATEADD(DAY, -@NoRepeatDays, @ForDate) AND r.ForDate < @ForDate)
    BEGIN
        DECLARE @Msg NVARCHAR(200) = CONCAT(N'This outfit was already worn in the last ', @NoRepeatDays, N' days');
        THROW 50014, @Msg, 1;
    END

    IF EXISTS (SELECT 1 FROM dbo.Outfits WHERE RecommendationId = @RecommendationId AND ItemsHash = @Hash)
        THROW 50015, 'This outfit is already in the carousel', 1;

    DECLARE @Pos TINYINT = ISNULL((SELECT MAX(CarouselPosition) FROM dbo.Outfits WHERE RecommendationId = @RecommendationId), 0) + 1;

    INSERT dbo.Outfits (RecommendationId, CarouselPosition, FitScore, ItemsHash, CreatedBy)
    VALUES (@RecommendationId, @Pos, @FitScore, @Hash, @CreatedBy);
    SET @OutfitId = SCOPE_IDENTITY();

    INSERT dbo.OutfitItems (OutfitId, WardrobeItemId, SlotCode, Layer, ItemName, ImageUrl, IsRemovable)
    SELECT @OutfitId, w.Id, w.SlotCode, w.Layer, w.Name, w.PrimaryImageUrl,
           CASE WHEN w.SlotCode = N'Top' AND w.Layer IN (N'Mid', N'Outer') THEN 1 ELSE 0 END
    FROM @Items it JOIN dbo.WardrobeItems w ON w.Id = it.ItemId;

    INSERT dbo.OutfitStatusHistory (OutfitId, FromStatus, ToStatus, ChangedBy)
    VALUES (@OutfitId, NULL, N'Generated', @CreatedBy);

    COMMIT TRANSACTION;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_ChangeOutfitStatus
    @OutfitId  BIGINT,
    @NewStatus NVARCHAR(20),
    @ChangedBy NVARCHAR(100) = NULL,
    @Note      NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;   -- будь-яка помилка відкочує транзакцію

    BEGIN TRANSACTION;

    DECLARE @Current NVARCHAR(20), @RecId BIGINT;
    SELECT @Current = Status, @RecId = RecommendationId
    FROM dbo.Outfits WITH (UPDLOCK)                -- блокуємо рядок до кінця транзакції
    WHERE Id = @OutfitId AND IsDeleted = 0;

    IF @Current IS NULL
        THROW 50021, 'Outfit not found', 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.OutfitStatusTransitions WHERE FromStatus = @Current AND ToStatus = @NewStatus)
    BEGIN
        DECLARE @Msg NVARCHAR(200) = CONCAT(N'Invalid status transition: ', @Current, N' -> ', @NewStatus);
        THROW 50022, @Msg, 1;
    END

    IF @NewStatus = N'Chosen'
       AND EXISTS (SELECT 1 FROM dbo.Outfits
                   WHERE RecommendationId = @RecId AND Id <> @OutfitId AND IsDeleted = 0
                     AND Status IN (N'Chosen', N'Worn', N'Reviewed'))
        THROW 50023, 'Another outfit is already chosen for this recommendation', 1;

    UPDATE dbo.Outfits
    SET Status = @NewStatus, UpdatedAt = SYSUTCDATETIME(), UpdatedBy = @ChangedBy
    WHERE Id = @OutfitId;

    INSERT dbo.OutfitStatusHistory (OutfitId, FromStatus, ToStatus, ChangedBy, Note)
    VALUES (@OutfitId, @Current, @NewStatus, @ChangedBy, @Note);

    COMMIT TRANSACTION;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_SetSlotEnabled
    @UserId    BIGINT,
    @SlotCode  NVARCHAR(20),
    @IsEnabled BIT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Id = @UserId AND IsDeleted = 0)
        THROW 50001, 'User not found', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.Slots WHERE Code = @SlotCode)
        THROW 50031, 'Slot not found', 1;

    BEGIN TRANSACTION;

    UPDATE dbo.UserSlotPreferences WITH (UPDLOCK, HOLDLOCK)
    SET IsEnabled = @IsEnabled, UpdatedAt = SYSUTCDATETIME()
    WHERE UserId = @UserId AND SlotCode = @SlotCode;

    IF @@ROWCOUNT = 0
        INSERT dbo.UserSlotPreferences (UserId, SlotCode, IsEnabled) VALUES (@UserId, @SlotCode, @IsEnabled);

    COMMIT TRANSACTION;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_UpsertWardrobeItemCopy
    @Id              BIGINT,
    @OwnerUserId     BIGINT,
    @Name            NVARCHAR(200),
    @SlotCode        NVARCHAR(20),
    @Layer           NVARCHAR(10),
    @Warmth          TINYINT,
    @MinTemp         SMALLINT,
    @MaxTemp         SMALLINT,
    @WaterResistance TINYINT,
    @IsAvailable     BIT,
    @PrimaryImageUrl NVARCHAR(500) = NULL,
    @IsDeleted       BIT = 0,
    @SourceUpdatedAt DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Id = @OwnerUserId)
        THROW 50041, 'Item owner not found', 1;

    BEGIN TRANSACTION;

    UPDATE dbo.WardrobeItems WITH (UPDLOCK, HOLDLOCK)
    SET Name = @Name, SlotCode = @SlotCode, Layer = @Layer, Warmth = @Warmth, MinTemp = @MinTemp, MaxTemp = @MaxTemp,
        WaterResistance = @WaterResistance, IsAvailable = @IsAvailable, PrimaryImageUrl = @PrimaryImageUrl,
        IsDeleted = @IsDeleted, SourceUpdatedAt = @SourceUpdatedAt, SyncedAt = SYSUTCDATETIME()
    WHERE Id = @Id AND SourceUpdatedAt < @SourceUpdatedAt;

    IF @@ROWCOUNT = 0 AND NOT EXISTS (SELECT 1 FROM dbo.WardrobeItems WHERE Id = @Id)
        INSERT dbo.WardrobeItems
            (Id, OwnerUserId, Name, SlotCode, Layer, Warmth, MinTemp, MaxTemp, WaterResistance, IsAvailable, PrimaryImageUrl, IsDeleted, SourceUpdatedAt)
        VALUES
            (@Id, @OwnerUserId, @Name, @SlotCode, @Layer, @Warmth, @MinTemp, @MaxTemp, @WaterResistance, @IsAvailable, @PrimaryImageUrl, @IsDeleted, @SourceUpdatedAt);

    COMMIT TRANSACTION;
END
GO
