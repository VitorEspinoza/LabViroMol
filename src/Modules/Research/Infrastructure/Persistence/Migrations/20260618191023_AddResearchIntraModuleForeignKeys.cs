using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabViroMol.Modules.Research.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddResearchIntraModuleForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Researchers_PositionId",
                schema: "research",
                table: "Researchers",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicationResearchers_ResearcherId",
                schema: "research",
                table: "PublicationResearchers",
                column: "ResearcherId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_PartnerId",
                schema: "research",
                table: "Projects",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMembers_ResearcherId",
                schema: "research",
                table: "ProjectMembers",
                column: "ResearcherId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectMembers_Researchers_ResearcherId",
                schema: "research",
                table: "ProjectMembers",
                column: "ResearcherId",
                principalSchema: "research",
                principalTable: "Researchers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Partners_PartnerId",
                schema: "research",
                table: "Projects",
                column: "PartnerId",
                principalSchema: "research",
                principalTable: "Partners",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PublicationResearchers_Researchers_ResearcherId",
                schema: "research",
                table: "PublicationResearchers",
                column: "ResearcherId",
                principalSchema: "research",
                principalTable: "Researchers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Researchers_Positions_PositionId",
                schema: "research",
                table: "Researchers",
                column: "PositionId",
                principalSchema: "research",
                principalTable: "Positions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectMembers_Researchers_ResearcherId",
                schema: "research",
                table: "ProjectMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Partners_PartnerId",
                schema: "research",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_PublicationResearchers_Researchers_ResearcherId",
                schema: "research",
                table: "PublicationResearchers");

            migrationBuilder.DropForeignKey(
                name: "FK_Researchers_Positions_PositionId",
                schema: "research",
                table: "Researchers");

            migrationBuilder.DropIndex(
                name: "IX_Researchers_PositionId",
                schema: "research",
                table: "Researchers");

            migrationBuilder.DropIndex(
                name: "IX_PublicationResearchers_ResearcherId",
                schema: "research",
                table: "PublicationResearchers");

            migrationBuilder.DropIndex(
                name: "IX_Projects_PartnerId",
                schema: "research",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_ProjectMembers_ResearcherId",
                schema: "research",
                table: "ProjectMembers");
        }
    }
}
