using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Sierra.Model.Migrations
{
    public partial class ManagedIdentityModel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ManagedIdentity",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    TenantCode = table.Column<string>(maxLength: 6, nullable: false),
                    EnvironmentName = table.Column<string>(maxLength: 10, nullable: false),
                    IdentityName = table.Column<string>(maxLength: 50, nullable: false),
                    IdentityId = table.Column<string>(maxLength: 500, nullable: true),
                    ResourceGroupName = table.Column<string>(maxLength: 90, nullable: false),
                    State = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManagedIdentity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ManagedIdentity_Tenants_TenantCode",
                        column: x => x.TenantCode,
                        principalTable: "Tenants",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ManagedIdentity_TenantCode",
                table: "ManagedIdentity",
                column: "TenantCode");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ManagedIdentity");
        }
    }
}
