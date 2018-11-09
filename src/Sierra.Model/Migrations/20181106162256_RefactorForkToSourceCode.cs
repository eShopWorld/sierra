using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Sierra.Model.Migrations
{
    public partial class RefactorForkToSourceCode : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn("ForkVstsId", "Forks", "RepoVstsId");
            migrationBuilder.RenameTable("Forks", "dbo", "SourceCodeRepositories");
            migrationBuilder.AddColumn<bool>("Fork", "SourceCodeRepositories", defaultValue: false);
            migrationBuilder.Sql("UPDATE dbo.SourceCodeRepositories SET Fork = 1;");
            migrationBuilder.DropForeignKey("FK_BuildDefinitions_Forks_SourceCodeId", "BuildDefinitions", "dbo");
            migrationBuilder.AddForeignKey("FK_BuildDefinitions_SourceCodeRepositories_SourceCodeId",
                "BuildDefinitions", "SourceCodeId", "SourceCodeRepositories", "dbo", "dbo", "Id");

        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn("Fork", "SourceCodeRepositories");
            migrationBuilder.RenameTable("SourceCodeRepositories", "dbo", "Forks");
            migrationBuilder.RenameColumn("RepoVstsId", "Forks", "ForkVstsId");
            migrationBuilder.DropForeignKey("FK_BuildDefinitions_SourceCodeRepositories_SourceCodeId",
                "BuildDefinitions", "dbo");
            migrationBuilder.AddForeignKey("FK_BuildDefinitions_Forks_SourceCodeId", "BuildDefinitions", "SourceCodeId",
                "Forks", "dbo", "dbo", "Id");
        }
    }
}
