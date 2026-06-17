using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabViroMol.Modules.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Identity_RoleSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "Identity",
                table: "Roles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RemovedAt",
                schema: "Identity",
                table: "Roles",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RemovedBy",
                schema: "Identity",
                table: "Roles",
                type: "uniqueidentifier",
                maxLength: 15,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "Identity",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "RemovedAt",
                schema: "Identity",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "RemovedBy",
                schema: "Identity",
                table: "Roles");
        }
    }
}
