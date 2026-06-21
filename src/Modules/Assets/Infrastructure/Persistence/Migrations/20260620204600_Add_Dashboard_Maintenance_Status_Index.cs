using LabViroMol.Modules.Assets.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabViroMol.Modules.Assets.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(AssetsDbContext))]
    [Migration("20260620204600_Add_Dashboard_Maintenance_Status_Index")]
    public partial class Add_Dashboard_Maintenance_Status_Index : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRequests_Status",
                schema: "assets",
                table: "MaintenanceRequests",
                column: "Status");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MaintenanceRequests_Status",
                schema: "assets",
                table: "MaintenanceRequests");
        }
    }
}
