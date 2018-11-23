using Microsoft.EntityFrameworkCore.Migrations;

namespace Sierra.Model.Migrations
{
    public partial class RenameManagedIdentityTableToPlural : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ManagedIdentity_Tenants_TenantCode",
                table: "ManagedIdentity");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ManagedIdentity",
                table: "ManagedIdentity");

            migrationBuilder.RenameTable(
                name: "ManagedIdentity",
                newName: "ManagedIdentities");

            migrationBuilder.RenameIndex(
                name: "IX_ManagedIdentity_TenantCode",
                table: "ManagedIdentities",
                newName: "IX_ManagedIdentities_TenantCode");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ManagedIdentities",
                table: "ManagedIdentities",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ManagedIdentities_Tenants_TenantCode",
                table: "ManagedIdentities",
                column: "TenantCode",
                principalTable: "Tenants",
                principalColumn: "Code",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ManagedIdentities_Tenants_TenantCode",
                table: "ManagedIdentities");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ManagedIdentities",
                table: "ManagedIdentities");

            migrationBuilder.RenameTable(
                name: "ManagedIdentities",
                newName: "ManagedIdentity");

            migrationBuilder.RenameIndex(
                name: "IX_ManagedIdentities_TenantCode",
                table: "ManagedIdentity",
                newName: "IX_ManagedIdentity_TenantCode");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ManagedIdentity",
                table: "ManagedIdentity",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ManagedIdentity_Tenants_TenantCode",
                table: "ManagedIdentity",
                column: "TenantCode",
                principalTable: "Tenants",
                principalColumn: "Code",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
