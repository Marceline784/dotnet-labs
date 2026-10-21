using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wardrobe.Dal.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueItemNamePerOwner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "UX_WardrobeItems_Owner_Name",
                table: "WardrobeItems",
                columns: new[] { "OwnerUserId", "Name" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_WardrobeItems_Owner_Name",
                table: "WardrobeItems");
        }
    }
}
