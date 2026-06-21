using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabViroMol.Modules.Inventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialOrderForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_OrderId",
                schema: "inventory",
                table: "StockTransactions",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_KitItems_MaterialId",
                schema: "inventory",
                table: "KitItems",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryOrders_MaterialId",
                schema: "inventory",
                table: "InventoryOrders",
                column: "MaterialId");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryOrders_Materials_MaterialId",
                schema: "inventory",
                table: "InventoryOrders",
                column: "MaterialId",
                principalSchema: "inventory",
                principalTable: "Materials",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KitItems_Materials_MaterialId",
                schema: "inventory",
                table: "KitItems",
                column: "MaterialId",
                principalSchema: "inventory",
                principalTable: "Materials",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockTransactions_InventoryOrders_OrderId",
                schema: "inventory",
                table: "StockTransactions",
                column: "OrderId",
                principalSchema: "inventory",
                principalTable: "InventoryOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryOrders_Materials_MaterialId",
                schema: "inventory",
                table: "InventoryOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_KitItems_Materials_MaterialId",
                schema: "inventory",
                table: "KitItems");

            migrationBuilder.DropForeignKey(
                name: "FK_StockTransactions_InventoryOrders_OrderId",
                schema: "inventory",
                table: "StockTransactions");

            migrationBuilder.DropIndex(
                name: "IX_StockTransactions_OrderId",
                schema: "inventory",
                table: "StockTransactions");

            migrationBuilder.DropIndex(
                name: "IX_KitItems_MaterialId",
                schema: "inventory",
                table: "KitItems");

            migrationBuilder.DropIndex(
                name: "IX_InventoryOrders_MaterialId",
                schema: "inventory",
                table: "InventoryOrders");
        }
    }
}
