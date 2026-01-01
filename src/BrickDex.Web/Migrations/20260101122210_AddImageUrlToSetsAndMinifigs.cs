using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrickDex.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddImageUrlToSetsAndMinifigs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "RebrickableSets",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "RebrickableMinifigs",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "RebrickableSets");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "RebrickableMinifigs");
        }
    }
}
