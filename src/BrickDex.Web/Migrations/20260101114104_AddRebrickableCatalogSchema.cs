using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrickDex.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddRebrickableCatalogSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RebrickableMinifigs",
                columns: table => new
                {
                    FigNum = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NumParts = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RebrickableMinifigs", x => x.FigNum);
                });

            migrationBuilder.CreateTable(
                name: "RebrickableThemes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ParentId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RebrickableThemes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RebrickableThemes_RebrickableThemes_ParentId",
                        column: x => x.ParentId,
                        principalTable: "RebrickableThemes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RebrickableSets",
                columns: table => new
                {
                    SetNum = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    ThemeId = table.Column<int>(type: "int", nullable: false),
                    NumParts = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RebrickableSets", x => x.SetNum);
                    table.ForeignKey(
                        name: "FK_RebrickableSets_RebrickableThemes_ThemeId",
                        column: x => x.ThemeId,
                        principalTable: "RebrickableThemes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RebrickableInventories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    SetNum = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RebrickableInventories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RebrickableInventories_RebrickableSets_SetNum",
                        column: x => x.SetNum,
                        principalTable: "RebrickableSets",
                        principalColumn: "SetNum",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RebrickableInventoryMinifigs",
                columns: table => new
                {
                    InventoryId = table.Column<int>(type: "int", nullable: false),
                    FigNum = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RebrickableInventoryMinifigs", x => new { x.InventoryId, x.FigNum });
                    table.ForeignKey(
                        name: "FK_RebrickableInventoryMinifigs_RebrickableInventories_InventoryId",
                        column: x => x.InventoryId,
                        principalTable: "RebrickableInventories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RebrickableInventoryMinifigs_RebrickableMinifigs_FigNum",
                        column: x => x.FigNum,
                        principalTable: "RebrickableMinifigs",
                        principalColumn: "FigNum",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RebrickableInventorySets",
                columns: table => new
                {
                    InventoryId = table.Column<int>(type: "int", nullable: false),
                    SetNum = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RebrickableInventorySets", x => new { x.InventoryId, x.SetNum });
                    table.ForeignKey(
                        name: "FK_RebrickableInventorySets_RebrickableInventories_InventoryId",
                        column: x => x.InventoryId,
                        principalTable: "RebrickableInventories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RebrickableInventorySets_RebrickableSets_SetNum",
                        column: x => x.SetNum,
                        principalTable: "RebrickableSets",
                        principalColumn: "SetNum",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RebrickableInventories_SetNum",
                table: "RebrickableInventories",
                column: "SetNum");

            migrationBuilder.CreateIndex(
                name: "IX_RebrickableInventoryMinifigs_FigNum",
                table: "RebrickableInventoryMinifigs",
                column: "FigNum");

            migrationBuilder.CreateIndex(
                name: "IX_RebrickableInventorySets_SetNum",
                table: "RebrickableInventorySets",
                column: "SetNum");

            migrationBuilder.CreateIndex(
                name: "IX_RebrickableSets_ThemeId",
                table: "RebrickableSets",
                column: "ThemeId");

            migrationBuilder.CreateIndex(
                name: "IX_RebrickableThemes_ParentId",
                table: "RebrickableThemes",
                column: "ParentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RebrickableInventoryMinifigs");

            migrationBuilder.DropTable(
                name: "RebrickableInventorySets");

            migrationBuilder.DropTable(
                name: "RebrickableMinifigs");

            migrationBuilder.DropTable(
                name: "RebrickableInventories");

            migrationBuilder.DropTable(
                name: "RebrickableSets");

            migrationBuilder.DropTable(
                name: "RebrickableThemes");
        }
    }
}
