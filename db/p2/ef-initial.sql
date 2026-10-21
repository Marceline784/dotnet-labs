IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021202046_InitialCreate'
)
BEGIN
    CREATE TABLE [Categories] (
        [Id] bigint NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_Categories] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021202046_InitialCreate'
)
BEGIN
    CREATE TABLE [WardrobeItems] (
        [Id] bigint NOT NULL IDENTITY,
        [OwnerUserId] bigint NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Slot] nvarchar(20) NOT NULL,
        [Layer] nvarchar(10) NOT NULL,
        [IsAvailable] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_WardrobeItems] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_WardrobeItems_Layer] CHECK ([Layer] IN ('Base', 'Mid', 'Outer')),
        CONSTRAINT [CK_WardrobeItems_LayerSlot] CHECK ([Slot] = 'Top' OR [Layer] = 'Base'),
        CONSTRAINT [CK_WardrobeItems_Slot] CHECK ([Slot] IN ('Top', 'Bottom', 'Shoes', 'Bag', 'Accessory'))
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021202046_InitialCreate'
)
BEGIN
    CREATE TABLE [ItemCategories] (
        [ItemId] bigint NOT NULL,
        [CategoryId] bigint NOT NULL,
        [AddedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_ItemCategories] PRIMARY KEY ([ItemId], [CategoryId]),
        CONSTRAINT [FK_ItemCategories_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ItemCategories_WardrobeItems_ItemId] FOREIGN KEY ([ItemId]) REFERENCES [WardrobeItems] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021202046_InitialCreate'
)
BEGIN
    CREATE TABLE [ItemDetails] (
        [ItemId] bigint NOT NULL,
        [Material] nvarchar(100) NULL,
        [Color] nvarchar(50) NULL,
        [Warmth] tinyint NOT NULL,
        [MinTemp] smallint NOT NULL,
        [MaxTemp] smallint NOT NULL,
        [WaterResistance] tinyint NOT NULL,
        [IsWindproof] bit NOT NULL,
        [Notes] nvarchar(500) NULL,
        CONSTRAINT [PK_ItemDetails] PRIMARY KEY ([ItemId]),
        CONSTRAINT [CK_ItemDetails_TempRange] CHECK ([MaxTemp] > [MinTemp]),
        CONSTRAINT [CK_ItemDetails_Warmth] CHECK ([Warmth] BETWEEN 1 AND 5),
        CONSTRAINT [CK_ItemDetails_Water] CHECK ([WaterResistance] BETWEEN 0 AND 3),
        CONSTRAINT [FK_ItemDetails_WardrobeItems_ItemId] FOREIGN KEY ([ItemId]) REFERENCES [WardrobeItems] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021202046_InitialCreate'
)
BEGIN
    CREATE TABLE [ItemImages] (
        [Id] bigint NOT NULL IDENTITY,
        [ItemId] bigint NOT NULL,
        [Url] nvarchar(500) NOT NULL,
        [IsPrimary] bit NOT NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_ItemImages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ItemImages_WardrobeItems_ItemId] FOREIGN KEY ([ItemId]) REFERENCES [WardrobeItems] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021202046_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name') AND [object_id] = OBJECT_ID(N'[Categories]'))
        SET IDENTITY_INSERT [Categories] ON;
    EXEC(N'INSERT INTO [Categories] ([Id], [Name])
    VALUES (CAST(1 AS bigint), N''Outerwear''),
    (CAST(2 AS bigint), N''Sweaters & hoodies''),
    (CAST(3 AS bigint), N''T-shirts''),
    (CAST(4 AS bigint), N''Trousers''),
    (CAST(5 AS bigint), N''Shorts''),
    (CAST(6 AS bigint), N''Footwear''),
    (CAST(7 AS bigint), N''Bags''),
    (CAST(8 AS bigint), N''Headwear''),
    (CAST(9 AS bigint), N''Scarves''),
    (CAST(10 AS bigint), N''Winter''),
    (CAST(11 AS bigint), N''Demi-season''),
    (CAST(12 AS bigint), N''Summer'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name') AND [object_id] = OBJECT_ID(N'[Categories]'))
        SET IDENTITY_INSERT [Categories] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021202046_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Categories_Name] ON [Categories] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021202046_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ItemCategories_CategoryId] ON [ItemCategories] ([CategoryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021202046_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ItemImages_Item_Sort] ON [ItemImages] ([ItemId], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021202046_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_WardrobeItems_Name] ON [WardrobeItems] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021202046_InitialCreate'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_WardrobeItems_Owner_Slot] ON [WardrobeItems] ([OwnerUserId], [Slot]) INCLUDE ([Name], [IsAvailable]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021202046_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261021202046_InitialCreate', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021203748_AddUniqueItemNamePerOwner'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_WardrobeItems_Owner_Name] ON [WardrobeItems] ([OwnerUserId], [Name]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021203748_AddUniqueItemNamePerOwner'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261021203748_AddUniqueItemNamePerOwner', N'10.0.13');
END;

COMMIT;
GO

