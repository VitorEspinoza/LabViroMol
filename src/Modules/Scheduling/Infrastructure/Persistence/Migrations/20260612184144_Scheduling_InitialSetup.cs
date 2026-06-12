using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabViroMol.Modules.Scheduling.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Scheduling_InitialSetup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "scheduling");

            migrationBuilder.CreateTable(
                name: "Schedules",
                schema: "scheduling",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchedulerName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SchedulerCourse = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SchedulerEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SchedulingDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SchedulingStartHour = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SchedulingEndHour = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AcceptTerm = table.Column<bool>(type: "bit", nullable: false),
                    AdvisorProfessor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProjectTitle = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApprovedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RefusedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TermUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RefuseJustification = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", maxLength: 15, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Schedules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScheduleEquipments",
                schema: "scheduling",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EquipmentName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ScheduleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleEquipments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduleEquipments_Schedules_ScheduleId",
                        column: x => x.ScheduleId,
                        principalSchema: "scheduling",
                        principalTable: "Schedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleEquipments_ScheduleId",
                schema: "scheduling",
                table: "ScheduleEquipments",
                column: "ScheduleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScheduleEquipments",
                schema: "scheduling");

            migrationBuilder.DropTable(
                name: "Schedules",
                schema: "scheduling");
        }
    }
}
