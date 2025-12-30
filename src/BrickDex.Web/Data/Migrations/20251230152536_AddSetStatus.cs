using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrickDex.Web.Data.Migrations;

/// <inheritdoc />
public partial class AddSetStatus : Migration {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.CreateTable(
            name: "LegoSets",
            columns: table => new {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                SetNumber = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                Year = table.Column<int>(type: "INTEGER", nullable: false),
                NumParts = table.Column<int>(type: "INTEGER", nullable: false),
                ThemeName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                ImageUrl = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                SetUrl = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                Notes = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: true),
                IsWishlist = table.Column<bool>(type: "INTEGER", nullable: false),
                Status = table.Column<int>(type: "INTEGER", nullable: false),
                CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_LegoSets", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_LegoSets_SetNumber",
            table: "LegoSets",
            column: "SetNumber");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropTable(
            name: "LegoSets");
    }
}
