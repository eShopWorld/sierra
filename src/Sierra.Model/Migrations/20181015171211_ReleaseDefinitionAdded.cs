using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Sierra.Model.Migrations
{
    public partial class ReleaseDefinitionAdded : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReleaseDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    BuildDefinitionId = table.Column<Guid>(nullable: false),
                    TenantCode = table.Column<string>(nullable: false),
                    State = table.Column<int>(nullable: false),
                    VstsReleaseDefinitionId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReleaseDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReleaseDefinitions_BuildDefinitions_BuildDefinitionId",
                        column: x => x.BuildDefinitionId,
                        principalTable: "BuildDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReleaseDefinitions_Tenants_TenantCode",
                        column: x => x.TenantCode,
                        principalTable: "Tenants",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReleaseDefinitions_BuildDefinitionId",
                table: "ReleaseDefinitions",
                column: "BuildDefinitionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReleaseDefinitions_TenantCode",
                table: "ReleaseDefinitions",
                column: "TenantCode");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReleaseDefinitions");
        }
    }
}
