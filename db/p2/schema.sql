SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;
GO
IF DB_ID(N'WardrobeDb') IS NULL CREATE DATABASE WardrobeDb;
GO
USE WardrobeDb;
GO

IF OBJECT_ID(N'dbo.Categories', N'U') IS NULL
CREATE TABLE dbo.Categories (
    Id   INT IDENTITY(1,1) CONSTRAINT PK_Categories PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL CONSTRAINT UQ_Categories_Name UNIQUE  
);
GO

IF OBJECT_ID(N'dbo.WardrobeItems', N'U') IS NULL
CREATE TABLE dbo.WardrobeItems (
    Id          BIGINT IDENTITY(1,1) CONSTRAINT PK_WardrobeItems PRIMARY KEY,
    OwnerUserId BIGINT NOT NULL,       
    Name        NVARCHAR(200) NOT NULL,
    SlotCode    NVARCHAR(20) NOT NULL
                CONSTRAINT CK_WardrobeItems_Slot CHECK (SlotCode IN (N'Top', N'Bottom', N'Shoes', N'Bag', N'Accessory')),
    Layer       NVARCHAR(10) NOT NULL DEFAULT N'Base'
                CONSTRAINT CK_WardrobeItems_Layer CHECK (Layer IN (N'Base', N'Mid', N'Outer')),
    IsAvailable BIT NOT NULL DEFAULT 1,  
    CreatedAt   DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy   NVARCHAR(100) NULL,
    UpdatedAt   DATETIME2 NULL,
    UpdatedBy   NVARCHAR(100) NULL,
    IsDeleted   BIT NOT NULL DEFAULT 0,
    RowVersion  ROWVERSION,              
    CONSTRAINT CK_WardrobeItems_LayerSlot CHECK (SlotCode = N'Top' OR Layer = N'Base')
);
GO

IF OBJECT_ID(N'dbo.ItemDetails', N'U') IS NULL
CREATE TABLE dbo.ItemDetails (
    ItemId          BIGINT NOT NULL CONSTRAINT PK_ItemDetails PRIMARY KEY
                    CONSTRAINT FK_ItemDetails_Items REFERENCES dbo.WardrobeItems(Id) ON DELETE CASCADE,
    Material        NVARCHAR(100) NULL,
    Color           NVARCHAR(50) NULL,
    Warmth          TINYINT NOT NULL CONSTRAINT CK_ItemDetails_Warmth CHECK (Warmth BETWEEN 1 AND 5),
    MinTemp         SMALLINT NOT NULL,
    MaxTemp         SMALLINT NOT NULL,
    WaterResistance TINYINT NOT NULL DEFAULT 0 CONSTRAINT CK_ItemDetails_Water CHECK (WaterResistance BETWEEN 0 AND 3),
    IsWindproof     BIT NOT NULL DEFAULT 0,
    Notes           NVARCHAR(500) NULL,
    CONSTRAINT CK_ItemDetails_Range CHECK (MaxTemp > MinTemp)
);
GO

IF OBJECT_ID(N'dbo.ItemImages', N'U') IS NULL
CREATE TABLE dbo.ItemImages (
    Id        BIGINT IDENTITY(1,1) CONSTRAINT PK_ItemImages PRIMARY KEY,
    ItemId    BIGINT NOT NULL CONSTRAINT FK_ItemImages_Items REFERENCES dbo.WardrobeItems(Id) ON DELETE CASCADE,
    Url       NVARCHAR(500) NOT NULL,
    IsPrimary BIT NOT NULL DEFAULT 0,
    SortOrder TINYINT NOT NULL DEFAULT 0
);
GO

IF OBJECT_ID(N'dbo.ItemCategories', N'U') IS NULL
CREATE TABLE dbo.ItemCategories (
    ItemId     BIGINT NOT NULL CONSTRAINT FK_ItemCategories_Items REFERENCES dbo.WardrobeItems(Id) ON DELETE CASCADE,
    CategoryId INT NOT NULL CONSTRAINT FK_ItemCategories_Categories REFERENCES dbo.Categories(Id),  -- без CASCADE
    AddedAt    DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_ItemCategories PRIMARY KEY (ItemId, CategoryId)
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_WardrobeItems_Owner_Slot' AND object_id = OBJECT_ID(N'dbo.WardrobeItems'))
CREATE INDEX IX_WardrobeItems_Owner_Slot ON dbo.WardrobeItems (OwnerUserId, SlotCode) INCLUDE (Name, IsAvailable) WHERE IsDeleted = 0;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ItemCategories_CategoryId' AND object_id = OBJECT_ID(N'dbo.ItemCategories'))
CREATE INDEX IX_ItemCategories_CategoryId ON dbo.ItemCategories (CategoryId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_ItemImages_OnePrimary' AND object_id = OBJECT_ID(N'dbo.ItemImages'))
CREATE UNIQUE INDEX UX_ItemImages_OnePrimary ON dbo.ItemImages (ItemId) WHERE IsPrimary = 1;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ItemImages_Item_Sort' AND object_id = OBJECT_ID(N'dbo.ItemImages'))
CREATE INDEX IX_ItemImages_Item_Sort ON dbo.ItemImages (ItemId, SortOrder);
GO
