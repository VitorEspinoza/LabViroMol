using LabViroMol.Modules.Scheduling.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabViroMol.Modules.Scheduling.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(SchedulingDbContext))]
    [Migration("20260620204500_Add_Dashboard_Schedule_Indexes")]
    public partial class Add_Dashboard_Schedule_Indexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Schedules_Status_SchedulingStartHour",
                schema: "scheduling",
                table: "Schedules",
                columns: new[] { "Status", "SchedulingStartHour" });

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_Status_SchedulingDate",
                schema: "scheduling",
                table: "Schedules",
                columns: new[] { "Status", "SchedulingDate" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Schedules_Status_SchedulingStartHour",
                schema: "scheduling",
                table: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_Status_SchedulingDate",
                schema: "scheduling",
                table: "Schedules");
        }
    }
}
