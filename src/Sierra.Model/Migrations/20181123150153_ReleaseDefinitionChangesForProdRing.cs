using Microsoft.EntityFrameworkCore.Migrations;

namespace Sierra.Model.Migrations
{
    public partial class ReleaseDefinitionChangesForProdRing : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ReleaseDefinitions_BuildDefinitionId",
                table: "ReleaseDefinitions");

            migrationBuilder.AddColumn<bool>(
                name: "RingBased",
                table: "ReleaseDefinitions",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "TenantSize",
                table: "ReleaseDefinitions",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ReleaseDefinitions_BuildDefinitionId",
                table: "ReleaseDefinitions",
                column: "BuildDefinitionId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ReleaseDefinitions_BuildDefinitionId",
                table: "ReleaseDefinitions");

            migrationBuilder.DropColumn(
                name: "RingBased",
                table: "ReleaseDefinitions");

            migrationBuilder.DropColumn(
                name: "TenantSize",
                table: "ReleaseDefinitions");

            migrationBuilder.CreateIndex(
                name: "IX_ReleaseDefinitions_BuildDefinitionId",
                table: "ReleaseDefinitions",
                column: "BuildDefinitionId",
                unique: true);
        }
    }
}
