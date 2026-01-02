using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrickDex.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class ConnectUserSetToRebrickableSet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserSets_LegoSets_LegoSetId",
                table: "UserSets");

            migrationBuilder.DropTable(
                name: "LegoSets");

            migrationBuilder.DropIndex(
                name: "IX_UserSets_LegoSetId",
                table: "UserSets");

            migrationBuilder.DropIndex(
                name: "IX_UserSets_UserId_LegoSetId",
                table: "UserSets");

            migrationBuilder.DropColumn(
                name: "LegoSetId",
                table: "UserSets");

            migrationBuilder.AddColumn<string>(
                name: "SetNumber",
                table: "UserSets",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_UserSets_SetNumber",
                table: "UserSets",
                column: "SetNumber");

            migrationBuilder.CreateIndex(
                name: "IX_UserSets_UserId_SetNumber",
                table: "UserSets",
                columns: new[] { "UserId", "SetNumber" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_UserSets_RebrickableSets_SetNumber",
                table: "UserSets",
                column: "SetNumber",
                principalTable: "RebrickableSets",
                principalColumn: "SetNum",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserSets_RebrickableSets_SetNumber",
                table: "UserSets");

            migrationBuilder.DropIndex(
                name: "IX_UserSets_SetNumber",
                table: "UserSets");

            migrationBuilder.DropIndex(
                name: "IX_UserSets_UserId_SetNumber",
                table: "UserSets");

            migrationBuilder.DropColumn(
                name: "SetNumber",
                table: "UserSets");

            migrationBuilder.AddColumn<Guid>(
                name: "LegoSetId",
                table: "UserSets",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "LegoSets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<long>(type: "bigint", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    NumParts = table.Column<int>(type: "int", nullable: false),
                    SetNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SetUrl = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    ThemeName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    UpdatedAt = table.Column<long>(type: "bigint", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegoSets", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserSets_LegoSetId",
                table: "UserSets",
                column: "LegoSetId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSets_UserId_LegoSetId",
                table: "UserSets",
                columns: new[] { "UserId", "LegoSetId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LegoSets_SetNumber",
                table: "LegoSets",
                column: "SetNumber",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_UserSets_LegoSets_LegoSetId",
                table: "UserSets",
                column: "LegoSetId",
                principalTable: "LegoSets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
