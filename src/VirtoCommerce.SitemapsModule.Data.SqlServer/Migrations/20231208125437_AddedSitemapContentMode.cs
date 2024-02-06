using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VirtoCommerce.SitemapsModule.Data.SqlServer.Migrations
{
    public partial class AddedSitemapContentMode : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SitemapMode",
                table: "Sitemap",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SitemapMode",
                table: "Sitemap");
        }
    }
}
