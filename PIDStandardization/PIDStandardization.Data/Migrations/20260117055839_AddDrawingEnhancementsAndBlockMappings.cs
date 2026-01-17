using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PIDStandardization.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDrawingEnhancementsAndBlockMappings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DrawingId",
                table: "Equipment",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceBlockName",
                table: "Equipment",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileHash",
                table: "Drawings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "Drawings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "FileSizeBytes",
                table: "Drawings",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ImportDate",
                table: "Drawings",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "ImportedBy",
                table: "Drawings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StoredFilePath",
                table: "Drawings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VersionNumber",
                table: "Drawings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "BlockMappings",
                columns: table => new
                {
                    BlockMappingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BlockName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EquipmentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UsageCount = table.Column<int>(type: "int", nullable: false),
                    FirstUsedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUsedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConfidenceScore = table.Column<double>(type: "float", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsUserConfirmed = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlockMappings", x => x.BlockMappingId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_DrawingId",
                table: "Equipment",
                column: "DrawingId");

            migrationBuilder.CreateIndex(
                name: "IX_BlockMappings_BlockName",
                table: "BlockMappings",
                column: "BlockName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BlockMappings_EquipmentType",
                table: "BlockMappings",
                column: "EquipmentType");

            migrationBuilder.AddForeignKey(
                name: "FK_Equipment_Drawings_DrawingId",
                table: "Equipment",
                column: "DrawingId",
                principalTable: "Drawings",
                principalColumn: "DrawingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Equipment_Drawings_DrawingId",
                table: "Equipment");

            migrationBuilder.DropTable(
                name: "BlockMappings");

            migrationBuilder.DropIndex(
                name: "IX_Equipment_DrawingId",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "DrawingId",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "SourceBlockName",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "FileHash",
                table: "Drawings");

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "Drawings");

            migrationBuilder.DropColumn(
                name: "FileSizeBytes",
                table: "Drawings");

            migrationBuilder.DropColumn(
                name: "ImportDate",
                table: "Drawings");

            migrationBuilder.DropColumn(
                name: "ImportedBy",
                table: "Drawings");

            migrationBuilder.DropColumn(
                name: "StoredFilePath",
                table: "Drawings");

            migrationBuilder.DropColumn(
                name: "VersionNumber",
                table: "Drawings");
        }
    }
}
