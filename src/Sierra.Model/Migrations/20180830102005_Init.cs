using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Sierra.Model.Migrations
{
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Code = table.Column<string>(maxLength: 6, nullable: false),
                    Name = table.Column<string>(maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "Forks",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ForkVstsId = table.Column<Guid>(nullable: false),
                    SourceRepositoryName = table.Column<string>(nullable: false),
                    TenantCode = table.Column<string>(maxLength: 6, nullable: false),
                    State = table.Column<int>(nullable: false),
                    ProjectType = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Forks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Forks_Tenants_TenantCode",
                        column: x => x.TenantCode,
                        principalTable: "Tenants",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BuildDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    SourceCodeId = table.Column<Guid>(nullable: false),
                    TenantCode = table.Column<string>(nullable: false),
                    VstsBuildDefinitionId = table.Column<string>(nullable: true),
                    State = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuildDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BuildDefinitions_Forks_SourceCodeId",
                        column: x => x.SourceCodeId,
                        principalTable: "Forks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BuildDefinitions_Tenants_TenantCode",
                        column: x => x.TenantCode,
                        principalTable: "Tenants",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BuildDefinitions_SourceCodeId",
                table: "BuildDefinitions",
                column: "SourceCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_BuildDefinitions_TenantCode",
                table: "BuildDefinitions",
                column: "TenantCode");

            migrationBuilder.CreateIndex(
                name: "IX_Forks_TenantCode",
                table: "Forks",
                column: "TenantCode");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BuildDefinitions");

            migrationBuilder.DropTable(
                name: "Forks");

            migrationBuilder.DropTable(
                name: "Tenants");
        }
    }
}
