SET QUOTED_IDENTIFIER ON; 
SET ANSI_NULLS ON;
GO
USE OutfitRecommendationsDb;
GO

SELECT fk.name AS ForeignKey, 
       OBJECT_NAME(fk.parent_object_id) AS ChildTable, 
       OBJECT_NAME(fk.referenced_object_id) AS ParentTable
FROM sys.foreign_keys fk
ORDER BY ChildTable;
GO

DECLARE @RecSnow BIGINT = (SELECT Id FROM dbo.Recommendations WHERE UserId = 1 AND ForDate = '2026-12-15');
EXEC dbo.usp_GetEligibleItems @RecommendationId = @RecSnow;
GO

DECLARE @RecSnow BIGINT = (SELECT Id FROM dbo.Recommendations WHERE UserId = 1 AND ForDate = '2026-12-15');
EXEC dbo.usp_SetSlotEnabled @UserId = 1, @SlotCode = N'Accessory', @IsEnabled = 0;
EXEC dbo.usp_GetEligibleItems @RecommendationId = @RecSnow;
EXEC dbo.usp_SetSlotEnabled @UserId = 1, @SlotCode = N'Accessory', @IsEnabled = 1;   
GO

DECLARE @RecSnow BIGINT = (SELECT Id FROM dbo.Recommendations WHERE UserId = 1 AND ForDate = '2026-12-15');
EXEC dbo.usp_UpsertWardrobeItemCopy @Id = 9, @OwnerUserId = 1, @Name = N'Зимові черевики', @SlotCode = N'Shoes',
     @Layer = N'Base', @Warmth = 5, @MinTemp = -30, @MaxTemp = 8, @WaterResistance = 3, @IsAvailable = 0,
     @SourceUpdatedAt = '2099-01-01';
EXEC dbo.usp_GetEligibleItems @RecommendationId = @RecSnow;   

EXEC dbo.usp_UpsertWardrobeItemCopy @Id = 9, @OwnerUserId = 1, @Name = N'Зимові черевики', @SlotCode = N'Shoes',
     @Layer = N'Base', @Warmth = 5, @MinTemp = -30, @MaxTemp = 8, @WaterResistance = 3, @IsAvailable = 1,
     @SourceUpdatedAt = '2099-01-02';                          
GO

DECLARE @RecSnow BIGINT = (SELECT Id FROM dbo.Recommendations WHERE UserId = 1 AND ForDate = '2026-12-15');
DECLARE @Out1 BIGINT = (SELECT TOP 1 Id FROM dbo.Outfits WHERE RecommendationId = @RecSnow ORDER BY CarouselPosition);

BEGIN TRY
    EXEC dbo.usp_ChangeOutfitStatus @OutfitId = @Out1, @NewStatus = N'Worn', @ChangedBy = N'test';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;  
    SELECT ERROR_NUMBER() AS ErrNo, ERROR_MESSAGE() AS ErrMsg;     
END CATCH;
GO

DECLARE @RecSnow BIGINT = (SELECT Id FROM dbo.Recommendations WHERE UserId = 1 AND ForDate = '2026-12-15');
DECLARE @Out1 BIGINT = (SELECT TOP 1 Id FROM dbo.Outfits WHERE RecommendationId = @RecSnow ORDER BY CarouselPosition);

EXEC dbo.usp_ChangeOutfitStatus @OutfitId = @Out1, @NewStatus = N'Chosen', @ChangedBy = N'test';

DECLARE @Out2 BIGINT = (SELECT TOP 1 Id FROM dbo.Outfits WHERE RecommendationId = @RecSnow AND Id <> @Out1);

BEGIN TRY
    EXEC dbo.usp_ChangeOutfitStatus @OutfitId = @Out2, @NewStatus = N'Chosen', @ChangedBy = N'test';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;   
    SELECT ERROR_NUMBER() AS ErrNo, ERROR_MESSAGE() AS ErrMsg;     
END CATCH;

EXEC dbo.usp_ChangeOutfitStatus @OutfitId = @Out1, @NewStatus = N'Cancelled', @ChangedBy = N'test';

BEGIN TRY
    EXEC dbo.usp_ChangeOutfitStatus @OutfitId = @Out1, @NewStatus = N'Chosen', @ChangedBy = N'test';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;  
    SELECT ERROR_NUMBER() AS ErrNo, ERROR_MESSAGE() AS ErrMsg;    
END CATCH;
GO

DECLARE @RecNew BIGINT, @OutX BIGINT;
EXEC dbo.usp_CreateRecommendation @UserId = 1, @ForDate = '2026-10-10', @City = N'Тернопіль',
     @TempMin = 4, @TempMax = 12, @FeelsLikeMin = 3, @FeelsLikeMax = 11, @PrecipProbability = 10,
     @WindSpeedMs = 3, @Condition = N'Cloudy', @RecommendationId = @RecNew OUTPUT;

BEGIN TRY
    EXEC dbo.usp_AddOutfit @RecommendationId = @RecNew, @FitScore = 80, @ItemIdsJson = N'[2,3,4,7,10,12]', @OutfitId = @OutX OUTPUT;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;   
    SELECT ERROR_NUMBER() AS ErrNo, ERROR_MESSAGE() AS ErrMsg;   
END CATCH;

DELETE FROM dbo.Recommendations WHERE Id = @RecNew;
GO

DECLARE @RecMax BIGINT, @OutM BIGINT;
EXEC dbo.usp_CreateRecommendation @UserId = 2, @ForDate = '2026-11-01', @City = N'Львів',
     @TempMin = 5, @TempMax = 10, @FeelsLikeMin = 4, @FeelsLikeMax = 9, @PrecipProbability = 0,
     @WindSpeedMs = 2, @Condition = N'Clear', @RecommendationId = @RecMax OUTPUT;

BEGIN TRY
    EXEC dbo.usp_AddOutfit @RecommendationId = @RecMax, @FitScore = 70, @ItemIdsJson = N'[17,18,19,12]', @OutfitId = @OutM OUTPUT;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;   
    SELECT ERROR_NUMBER() AS ErrNo, ERROR_MESSAGE() AS ErrMsg;     
END CATCH;

DELETE FROM dbo.Recommendations WHERE Id = @RecMax;
GO

DECLARE @RecSnow BIGINT = (SELECT Id FROM dbo.Recommendations WHERE UserId = 1 AND ForDate = '2026-12-15');
SELECT o.Id AS OutfitId, o.CarouselPosition, o.FitScore, o.Status, oi.SlotCode, oi.Layer, oi.ItemName, oi.IsRemovable
FROM dbo.Outfits o
JOIN dbo.OutfitItems oi ON oi.OutfitId = o.Id
JOIN dbo.Slots s ON s.Code = oi.SlotCode
WHERE o.RecommendationId = @RecSnow
ORDER BY o.CarouselPosition, s.SortOrder, oi.Layer;
GO

SELECT h.OutfitId, h.FromStatus, h.ToStatus, h.ChangedAt, h.ChangedBy 
FROM dbo.OutfitStatusHistory h 
ORDER BY h.OutfitId, h.Id;
GO

SELECT r.Id, r.ForDate, w.Condition, w.FeelsLikeMin, w.FeelsLikeMax
FROM dbo.Recommendations r 
JOIN dbo.WeatherSnapshots w ON w.RecommendationId = r.Id
WHERE r.UserId = 1 AND r.IsDeleted = 0
ORDER BY r.ForDate DESC
OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY;
GO

SELECT t.name AS TableName, i.name AS IndexName, i.is_unique, i.filter_definition
FROM sys.indexes i 
JOIN sys.tables t ON t.object_id = i.object_id
WHERE i.name IS NOT NULL AND i.is_primary_key = 0
ORDER BY t.name, i.name;
GO