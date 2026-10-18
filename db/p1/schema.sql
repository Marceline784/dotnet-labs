
SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;
GO
IF DB_ID(N'OutfitRecommendationsDb') IS NULL CREATE DATABASE OutfitRecommendationsDb;
GO
USE OutfitRecommendationsDb;
GO

IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
CREATE TABLE dbo.Users (
    Id          BIGINT IDENTITY(1,1) CONSTRAINT PK_Users PRIMARY KEY,
    Email       NVARCHAR(256) NOT NULL CONSTRAINT UQ_Users_Email UNIQUE,   
    DisplayName NVARCHAR(100) NOT NULL,
    CreatedAt   DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy   NVARCHAR(100) NULL,
    UpdatedAt   DATETIME2 NULL,
    UpdatedBy   NVARCHAR(100) NULL,
    IsDeleted   BIT NOT NULL DEFAULT 0,
    RowVersion  ROWVERSION
);
GO

IF OBJECT_ID(N'dbo.UserProfiles', N'U') IS NULL
CREATE TABLE dbo.UserProfiles (
    UserId          BIGINT NOT NULL CONSTRAINT PK_UserProfiles PRIMARY KEY
                    CONSTRAINT FK_UserProfiles_Users REFERENCES dbo.Users(Id) ON DELETE CASCADE,
    City            NVARCHAR(100) NOT NULL,
    Latitude        DECIMAL(9,6) NULL CONSTRAINT CK_UserProfiles_Lat CHECK (Latitude BETWEEN -90 AND 90),
    Longitude       DECIMAL(9,6) NULL CONSTRAINT CK_UserProfiles_Lon CHECK (Longitude BETWEEN -180 AND 180),
    ColdSensitivity DECIMAL(3,1) NOT NULL DEFAULT 0
                    CONSTRAINT CK_UserProfiles_Cold CHECK (ColdSensitivity BETWEEN -3 AND 3),
    StylePreference NVARCHAR(30) NULL
                    CONSTRAINT CK_UserProfiles_Style CHECK (StylePreference IN (N'Casual', N'Sport', N'Classic', N'Street')),
    CreatedAt       DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy       NVARCHAR(100) NULL,
    UpdatedAt       DATETIME2 NULL,
    UpdatedBy       NVARCHAR(100) NULL,
    IsDeleted       BIT NOT NULL DEFAULT 0,
    RowVersion      ROWVERSION   
);
GO

IF OBJECT_ID(N'dbo.Slots', N'U') IS NULL
CREATE TABLE dbo.Slots (
    Code      NVARCHAR(20) NOT NULL CONSTRAINT PK_Slots PRIMARY KEY,
    Name      NVARCHAR(50) NOT NULL,
    SortOrder TINYINT NOT NULL CONSTRAINT UQ_Slots_SortOrder UNIQUE
);
GO

IF OBJECT_ID(N'dbo.UserSlotPreferences', N'U') IS NULL
CREATE TABLE dbo.UserSlotPreferences (
    UserId    BIGINT NOT NULL CONSTRAINT FK_USP_Users REFERENCES dbo.Users(Id) ON DELETE CASCADE,
    SlotCode  NVARCHAR(20) NOT NULL CONSTRAINT FK_USP_Slots REFERENCES dbo.Slots(Code),
    IsEnabled BIT NOT NULL DEFAULT 1,
    UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_UserSlotPreferences PRIMARY KEY (UserId, SlotCode)
);
GO

IF OBJECT_ID(N'dbo.WardrobeItems', N'U') IS NULL
CREATE TABLE dbo.WardrobeItems (
    Id              BIGINT NOT NULL CONSTRAINT PK_WardrobeItems PRIMARY KEY,
    OwnerUserId     BIGINT NOT NULL CONSTRAINT FK_WardrobeItems_Users REFERENCES dbo.Users(Id),
    Name            NVARCHAR(200) NOT NULL,
    SlotCode        NVARCHAR(20) NOT NULL CONSTRAINT FK_WardrobeItems_Slots REFERENCES dbo.Slots(Code),
    Layer           NVARCHAR(10) NOT NULL DEFAULT N'Base'
                    CONSTRAINT CK_WardrobeItems_Layer CHECK (Layer IN (N'Base', N'Mid', N'Outer')),
    Warmth          TINYINT NOT NULL CONSTRAINT CK_WardrobeItems_Warmth CHECK (Warmth BETWEEN 1 AND 5),
    MinTemp         SMALLINT NOT NULL,
    MaxTemp         SMALLINT NOT NULL,
    WaterResistance TINYINT NOT NULL DEFAULT 0 CONSTRAINT CK_WardrobeItems_Water CHECK (WaterResistance BETWEEN 0 AND 3),
    IsAvailable     BIT NOT NULL DEFAULT 1,          
    PrimaryImageUrl NVARCHAR(500) NULL,
    SourceUpdatedAt DATETIME2 NOT NULL,             
    SyncedAt        DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    IsDeleted       BIT NOT NULL DEFAULT 0,
    CONSTRAINT CK_WardrobeItems_TempRange CHECK (MaxTemp > MinTemp),
    CONSTRAINT CK_WardrobeItems_LayerSlot CHECK (SlotCode = N'Top' OR Layer = N'Base') 
);
GO

IF OBJECT_ID(N'dbo.Recommendations', N'U') IS NULL
CREATE TABLE dbo.Recommendations (
    Id         BIGINT IDENTITY(1,1) CONSTRAINT PK_Recommendations PRIMARY KEY,
    UserId     BIGINT NOT NULL CONSTRAINT FK_Recommendations_Users REFERENCES dbo.Users(Id),  
    ForDate    DATE NOT NULL,
    CreatedAt  DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy  NVARCHAR(100) NULL,
    UpdatedAt  DATETIME2 NULL,
    UpdatedBy  NVARCHAR(100) NULL,
    IsDeleted  BIT NOT NULL DEFAULT 0,
    RowVersion ROWVERSION
);
GO

IF OBJECT_ID(N'dbo.WeatherSnapshots', N'U') IS NULL
CREATE TABLE dbo.WeatherSnapshots (
    RecommendationId  BIGINT NOT NULL CONSTRAINT PK_WeatherSnapshots PRIMARY KEY
                      CONSTRAINT FK_WeatherSnapshots_Rec REFERENCES dbo.Recommendations(Id) ON DELETE CASCADE,
    City              NVARCHAR(100) NOT NULL,
    TempMin           DECIMAL(4,1) NOT NULL,
    TempMax           DECIMAL(4,1) NOT NULL,
    FeelsLikeMin      DECIMAL(4,1) NOT NULL,   
    FeelsLikeMax      DECIMAL(4,1) NOT NULL,   
    PrecipProbability TINYINT NOT NULL DEFAULT 0 CONSTRAINT CK_Weather_Precip CHECK (PrecipProbability BETWEEN 0 AND 100),
    WindSpeedMs       DECIMAL(4,1) NOT NULL DEFAULT 0 CONSTRAINT CK_Weather_Wind CHECK (WindSpeedMs >= 0),
    UvIndex           DECIMAL(3,1) NULL CONSTRAINT CK_Weather_Uv CHECK (UvIndex >= 0),
    Condition         NVARCHAR(20) NOT NULL
                      CONSTRAINT CK_Weather_Condition CHECK (Condition IN (N'Clear', N'Cloudy', N'Rain', N'Snow', N'Storm', N'Fog')),
    Source            NVARCHAR(50) NOT NULL DEFAULT N'open-meteo',
    FetchedAt         DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT CK_Weather_Range CHECK (TempMax >= TempMin AND FeelsLikeMax >= FeelsLikeMin)
);
GO

IF OBJECT_ID(N'dbo.Outfits', N'U') IS NULL
CREATE TABLE dbo.Outfits (
    Id               BIGINT IDENTITY(1,1) CONSTRAINT PK_Outfits PRIMARY KEY,
    RecommendationId BIGINT NOT NULL CONSTRAINT FK_Outfits_Rec REFERENCES dbo.Recommendations(Id) ON DELETE CASCADE,
    CarouselPosition TINYINT NOT NULL CONSTRAINT CK_Outfits_Pos CHECK (CarouselPosition >= 1),
    FitScore         DECIMAL(5,2) NOT NULL CONSTRAINT CK_Outfits_Fit CHECK (FitScore BETWEEN 0 AND 100),
    ItemsHash        CHAR(64) NOT NULL,   
    Status           NVARCHAR(20) NOT NULL DEFAULT N'Generated'
                     CONSTRAINT CK_Outfits_Status CHECK (Status IN (N'Generated', N'Chosen', N'Worn', N'Reviewed', N'Cancelled')),
    CreatedAt        DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy        NVARCHAR(100) NULL,
    UpdatedAt        DATETIME2 NULL,
    UpdatedBy        NVARCHAR(100) NULL,
    IsDeleted        BIT NOT NULL DEFAULT 0,
    RowVersion       ROWVERSION,
    CONSTRAINT UQ_Outfits_Rec_Position UNIQUE (RecommendationId, CarouselPosition),
    CONSTRAINT UQ_Outfits_Rec_Hash UNIQUE (RecommendationId, ItemsHash)  
);
GO

IF OBJECT_ID(N'dbo.OutfitItems', N'U') IS NULL
CREATE TABLE dbo.OutfitItems (
    OutfitId       BIGINT NOT NULL CONSTRAINT FK_OutfitItems_Outfits REFERENCES dbo.Outfits(Id) ON DELETE CASCADE,
    WardrobeItemId BIGINT NOT NULL CONSTRAINT FK_OutfitItems_Items REFERENCES dbo.WardrobeItems(Id),
    SlotCode       NVARCHAR(20) NOT NULL CONSTRAINT FK_OutfitItems_Slots REFERENCES dbo.Slots(Code),
    Layer          NVARCHAR(10) NOT NULL CONSTRAINT CK_OutfitItems_Layer CHECK (Layer IN (N'Base', N'Mid', N'Outer')),
    ItemName       NVARCHAR(200) NOT NULL,   
    ImageUrl       NVARCHAR(500) NULL,       
    IsRemovable    BIT NOT NULL DEFAULT 0,  
    CONSTRAINT PK_OutfitItems PRIMARY KEY (OutfitId, WardrobeItemId),
    CONSTRAINT UQ_OutfitItems_Slot_Layer UNIQUE (OutfitId, SlotCode, Layer)  
);
GO

IF OBJECT_ID(N'dbo.OutfitStatusTransitions', N'U') IS NULL
CREATE TABLE dbo.OutfitStatusTransitions (
    FromStatus NVARCHAR(20) NOT NULL,
    ToStatus   NVARCHAR(20) NOT NULL,
    CONSTRAINT PK_OutfitStatusTransitions PRIMARY KEY (FromStatus, ToStatus)
);
GO

IF OBJECT_ID(N'dbo.OutfitStatusHistory', N'U') IS NULL
CREATE TABLE dbo.OutfitStatusHistory (
    Id         BIGINT IDENTITY(1,1) CONSTRAINT PK_OutfitStatusHistory PRIMARY KEY,
    OutfitId   BIGINT NOT NULL CONSTRAINT FK_OSH_Outfits REFERENCES dbo.Outfits(Id) ON DELETE CASCADE,
    FromStatus NVARCHAR(20) NULL,
    ToStatus   NVARCHAR(20) NOT NULL,
    ChangedAt  DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    ChangedBy  NVARCHAR(100) NULL,
    Note       NVARCHAR(500) NULL
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_WardrobeItems_Eligible' AND object_id = OBJECT_ID(N'dbo.WardrobeItems'))
CREATE INDEX IX_WardrobeItems_Eligible ON dbo.WardrobeItems (OwnerUserId, SlotCode)
    INCLUDE (Name, Layer, Warmth, MinTemp, MaxTemp, WaterResistance, PrimaryImageUrl)
    WHERE IsAvailable = 1 AND IsDeleted = 0;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Recommendations_User_Date' AND object_id = OBJECT_ID(N'dbo.Recommendations'))
CREATE INDEX IX_Recommendations_User_Date ON dbo.Recommendations (UserId, ForDate DESC) WHERE IsDeleted = 0;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Outfits_ItemsHash' AND object_id = OBJECT_ID(N'dbo.Outfits'))
CREATE INDEX IX_Outfits_ItemsHash ON dbo.Outfits (ItemsHash) INCLUDE (RecommendationId, Status) WHERE IsDeleted = 0;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Outfits_OneActivePerRecommendation' AND object_id = OBJECT_ID(N'dbo.Outfits'))
CREATE UNIQUE INDEX UX_Outfits_OneActivePerRecommendation ON dbo.Outfits (RecommendationId)
    WHERE Status <> N'Generated' AND Status <> N'Cancelled' AND IsDeleted = 0;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OutfitItems_WardrobeItemId' AND object_id = OBJECT_ID(N'dbo.OutfitItems'))
CREATE INDEX IX_OutfitItems_WardrobeItemId ON dbo.OutfitItems (WardrobeItemId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OSH_Outfit_ChangedAt' AND object_id = OBJECT_ID(N'dbo.OutfitStatusHistory'))
CREATE INDEX IX_OSH_Outfit_ChangedAt ON dbo.OutfitStatusHistory (OutfitId, ChangedAt);
GO
