using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabViroMol.Modules.Inventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Inventory_Fix_KitItems_CompositePK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_KitItems",
                schema: "inventory",
                table: "KitItems");

            migrationBuilder.DropIndex(
                name: "IX_KitItems_KitId",
                schema: "inventory",
                table: "KitItems");

            migrationBuilder.AddPrimaryKey(
                name: "PK_KitItems",
                schema: "inventory",
                table: "KitItems",
                columns: new[] { "KitId", "MaterialId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_KitItems",
                schema: "inventory",
                table: "KitItems");

            migrationBuilder.AddPrimaryKey(
                name: "PK_KitItems",
                schema: "inventory",
                table: "KitItems",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_KitItems_KitId",
                schema: "inventory",
                table: "KitItems",
                column: "KitId");
        }
    }
}
