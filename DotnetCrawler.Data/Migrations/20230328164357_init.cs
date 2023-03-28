using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DotnetCrawler.Data.Migrations
{
    public partial class init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CategoryDb",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    CreateDate = table.Column<DateTime>(nullable: false),
                    UpdateDate = table.Column<DateTime>(nullable: false),
                    Domain = table.Column<string>(nullable: true),
                    Titlte = table.Column<string>(nullable: true),
                    Url = table.Column<string>(nullable: true),
                    Slug = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryDb", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChapDb",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    CreateDate = table.Column<DateTime>(nullable: false),
                    UpdateDate = table.Column<DateTime>(nullable: false),
                    Titlte = table.Column<string>(nullable: true),
                    Content = table.Column<string>(nullable: true),
                    Slug = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChapDb", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PostDb",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    CreateDate = table.Column<DateTime>(nullable: false),
                    UpdateDate = table.Column<DateTime>(nullable: false),
                    Titlte = table.Column<string>(nullable: true),
                    Description = table.Column<string>(nullable: true),
                    Slug = table.Column<string>(nullable: true),
                    Avatar = table.Column<string>(nullable: true),
                    Taxonomies = table.Column<string>(nullable: true),
                    Metadata = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostDb", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SettingDb",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    CreateDate = table.Column<DateTime>(nullable: false),
                    UpdateDate = table.Column<DateTime>(nullable: false),
                    CheckDuplicateUrlPost = table.Column<bool>(nullable: false),
                    CheckDuplicateTitlePost = table.Column<bool>(nullable: false),
                    CheckDuplicateUrlChapter = table.Column<bool>(nullable: false),
                    CheckDuplicateTitleChapter = table.Column<bool>(nullable: false),
                    IsThuThap = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SettingDb", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CategoryDb");

            migrationBuilder.DropTable(
                name: "ChapDb");

            migrationBuilder.DropTable(
                name: "PostDb");

            migrationBuilder.DropTable(
                name: "SettingDb");
        }
    }
}
