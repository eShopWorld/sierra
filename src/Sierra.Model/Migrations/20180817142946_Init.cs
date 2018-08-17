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
                    ForkVstsId = table.Column<Guid>(nullable: false),
                    SourceRepositoryName = table.Column<string>(nullable: false),
                    TenantCode = table.Column<string>(maxLength: 6, nullable: false),
                    State = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Forks", x => new { x.SourceRepositoryName, x.TenantCode });
                    table.ForeignKey(
                        name: "FK_Forks_Tenants_TenantCode",
                        column: x => x.TenantCode,
                        principalTable: "Tenants",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Forks_TenantCode",
                table: "Forks",
                column: "TenantCode");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Forks");

            migrationBuilder.DropTable(
                name: "Tenants");
        }
    }
}
