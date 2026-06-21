using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabViroMol.Modules.Inventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_Inventory_Report_Indexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_ProjectId_Type_TransactedAt",
                schema: "inventory",
                table: "StockTransactions",
                columns: new[] { "ProjectId", "Type", "TransactedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_TransactedAt",
                schema: "inventory",
                table: "StockTransactions",
                column: "TransactedAt");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_Type_TransactedAt_MaterialId",
                schema: "inventory",
                table: "StockTransactions",
                columns: new[] { "Type", "TransactedAt", "MaterialId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StockTransactions_ProjectId_Type_TransactedAt",
                schema: "inventory",
                table: "StockTransactions");

            migrationBuilder.DropIndex(
                name: "IX_StockTransactions_TransactedAt",
                schema: "inventory",
                table: "StockTransactions");

            migrationBuilder.DropIndex(
                name: "IX_StockTransactions_Type_TransactedAt_MaterialId",
                schema: "inventory",
                table: "StockTransactions");
        }
    }
}
