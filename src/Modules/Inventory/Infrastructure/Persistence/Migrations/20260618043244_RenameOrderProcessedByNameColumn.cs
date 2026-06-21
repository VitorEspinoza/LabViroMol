using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabViroMol.Modules.Inventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameOrderProcessedByNameColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Processing_ProcessedByName",
                schema: "inventory",
                table: "InventoryOrders",
                newName: "ProcessedByName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ProcessedByName",
                schema: "inventory",
                table: "InventoryOrders",
                newName: "Processing_ProcessedByName");
        }
    }
}
