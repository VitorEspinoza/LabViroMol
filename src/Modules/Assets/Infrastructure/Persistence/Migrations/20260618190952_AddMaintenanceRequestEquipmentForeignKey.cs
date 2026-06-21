using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabViroMol.Modules.Assets.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMaintenanceRequestEquipmentForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRequests_EquipmentId",
                schema: "assets",
                table: "MaintenanceRequests",
                column: "EquipmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_MaintenanceRequests_Equipments_EquipmentId",
                schema: "assets",
                table: "MaintenanceRequests",
                column: "EquipmentId",
                principalSchema: "assets",
                principalTable: "Equipments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaintenanceRequests_Equipments_EquipmentId",
                schema: "assets",
                table: "MaintenanceRequests");

            migrationBuilder.DropIndex(
                name: "IX_MaintenanceRequests_EquipmentId",
                schema: "assets",
                table: "MaintenanceRequests");
        }
    }
}
