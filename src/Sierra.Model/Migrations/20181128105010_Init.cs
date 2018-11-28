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
                    Name = table.Column<string>(maxLength: 100, nullable: false),
                    TenantSize = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "ManagedIdentities",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    TenantCode = table.Column<string>(maxLength: 6, nullable: false),
                    Environment = table.Column<int>(nullable: false),
                    IdentityName = table.Column<string>(maxLength: 50, nullable: false),
                    IdentityId = table.Column<string>(maxLength: 500, nullable: true),
                    ResourceGroupName = table.Column<string>(maxLength: 90, nullable: false),
                    State = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManagedIdentities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ManagedIdentities_Tenants_TenantCode",
                        column: x => x.TenantCode,
                        principalTable: "Tenants",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResourceGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    TenantCode = table.Column<string>(maxLength: 6, nullable: false),
                    Environment = table.Column<int>(nullable: false),
                    Name = table.Column<string>(nullable: false),
                    ResourceId = table.Column<string>(nullable: true),
                    State = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResourceGroups_Tenants_TenantCode",
                        column: x => x.TenantCode,
                        principalTable: "Tenants",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SourceCodeRepositories",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    RepoVstsId = table.Column<Guid>(nullable: false),
                    SourceRepositoryName = table.Column<string>(nullable: false),
                    TenantCode = table.Column<string>(maxLength: 6, nullable: false),
                    State = table.Column<int>(nullable: false),
                    ProjectType = table.Column<int>(nullable: false),
                    Fork = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SourceCodeRepositories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SourceCodeRepositories_Tenants_TenantCode",
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
                    VstsBuildDefinitionId = table.Column<int>(nullable: false),
                    State = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuildDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BuildDefinitions_SourceCodeRepositories_SourceCodeId",
                        column: x => x.SourceCodeId,
                        principalTable: "SourceCodeRepositories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BuildDefinitions_Tenants_TenantCode",
                        column: x => x.TenantCode,
                        principalTable: "Tenants",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReleaseDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    BuildDefinitionId = table.Column<Guid>(nullable: false),
                    TenantCode = table.Column<string>(nullable: false),
                    TenantSize = table.Column<int>(nullable: false),
                    State = table.Column<int>(nullable: false),
                    VstsReleaseDefinitionId = table.Column<int>(nullable: false),
                    RingBased = table.Column<bool>(nullable: false)
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
                name: "IX_BuildDefinitions_SourceCodeId",
                table: "BuildDefinitions",
                column: "SourceCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_BuildDefinitions_TenantCode",
                table: "BuildDefinitions",
                column: "TenantCode");

            migrationBuilder.CreateIndex(
                name: "IX_ManagedIdentities_TenantCode",
                table: "ManagedIdentities",
                column: "TenantCode");

            migrationBuilder.CreateIndex(
                name: "IX_ReleaseDefinitions_BuildDefinitionId",
                table: "ReleaseDefinitions",
                column: "BuildDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReleaseDefinitions_TenantCode",
                table: "ReleaseDefinitions",
                column: "TenantCode");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceGroups_TenantCode",
                table: "ResourceGroups",
                column: "TenantCode");

            migrationBuilder.CreateIndex(
                name: "IX_SourceCodeRepositories_TenantCode",
                table: "SourceCodeRepositories",
                column: "TenantCode");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ManagedIdentities");

            migrationBuilder.DropTable(
                name: "ReleaseDefinitions");

            migrationBuilder.DropTable(
                name: "ResourceGroups");

            migrationBuilder.DropTable(
                name: "BuildDefinitions");

            migrationBuilder.DropTable(
                name: "SourceCodeRepositories");

            migrationBuilder.DropTable(
                name: "Tenants");
        }
    }
}
