using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Wardrobe.Dal.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WardrobeItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerUserId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Slot = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Layer = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WardrobeItems", x => x.Id);
                    table.CheckConstraint("CK_WardrobeItems_Layer", "[Layer] IN ('Base', 'Mid', 'Outer')");
                    table.CheckConstraint("CK_WardrobeItems_LayerSlot", "[Slot] = 'Top' OR [Layer] = 'Base'");
                    table.CheckConstraint("CK_WardrobeItems_Slot", "[Slot] IN ('Top', 'Bottom', 'Shoes', 'Bag', 'Accessory')");
                });

            migrationBuilder.CreateTable(
                name: "ItemCategories",
                columns: table => new
                {
                    ItemId = table.Column<long>(type: "bigint", nullable: false),
                    CategoryId = table.Column<long>(type: "bigint", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemCategories", x => new { x.ItemId, x.CategoryId });
                    table.ForeignKey(
                        name: "FK_ItemCategories_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ItemCategories_WardrobeItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "WardrobeItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemDetails",
                columns: table => new
                {
                    ItemId = table.Column<long>(type: "bigint", nullable: false),
                    Material = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Color = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Warmth = table.Column<byte>(type: "tinyint", nullable: false),
                    MinTemp = table.Column<short>(type: "smallint", nullable: false),
                    MaxTemp = table.Column<short>(type: "smallint", nullable: false),
                    WaterResistance = table.Column<byte>(type: "tinyint", nullable: false),
                    IsWindproof = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemDetails", x => x.ItemId);
                    table.CheckConstraint("CK_ItemDetails_TempRange", "[MaxTemp] > [MinTemp]");
                    table.CheckConstraint("CK_ItemDetails_Warmth", "[Warmth] BETWEEN 1 AND 5");
                    table.CheckConstraint("CK_ItemDetails_Water", "[WaterResistance] BETWEEN 0 AND 3");
                    table.ForeignKey(
                        name: "FK_ItemDetails_WardrobeItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "WardrobeItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemImages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ItemId = table.Column<long>(type: "bigint", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemImages_WardrobeItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "WardrobeItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1L, "Outerwear" },
                    { 2L, "Sweaters & hoodies" },
                    { 3L, "T-shirts" },
                    { 4L, "Trousers" },
                    { 5L, "Shorts" },
                    { 6L, "Footwear" },
                    { 7L, "Bags" },
                    { 8L, "Headwear" },
                    { 9L, "Scarves" },
                    { 10L, "Winter" },
                    { 11L, "Demi-season" },
                    { 12L, "Summer" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemCategories_CategoryId",
                table: "ItemCategories",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemImages_Item_Sort",
                table: "ItemImages",
                columns: new[] { "ItemId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_WardrobeItems_Name",
                table: "WardrobeItems",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_WardrobeItems_Owner_Slot",
                table: "WardrobeItems",
                columns: new[] { "OwnerUserId", "Slot" },
                filter: "[IsDeleted] = 0")
                .Annotation("SqlServer:Include", new[] { "Name", "IsAvailable" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItemCategories");

            migrationBuilder.DropTable(
                name: "ItemDetails");

            migrationBuilder.DropTable(
                name: "ItemImages");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "WardrobeItems");
        }
    }
}
