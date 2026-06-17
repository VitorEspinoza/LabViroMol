using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabViroMol.Modules.Assets.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Assets_MaintenanceRequestSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "assets",
                table: "MaintenanceRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RemovedAt",
                schema: "assets",
                table: "MaintenanceRequests",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RemovedBy",
                schema: "assets",
                table: "MaintenanceRequests",
                type: "uniqueidentifier",
                maxLength: 15,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "assets",
                table: "MaintenanceRequests");

            migrationBuilder.DropColumn(
                name: "RemovedAt",
                schema: "assets",
                table: "MaintenanceRequests");

            migrationBuilder.DropColumn(
                name: "RemovedBy",
                schema: "assets",
                table: "MaintenanceRequests");
        }
    }
}
