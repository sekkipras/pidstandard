using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PIDStandardization.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveBlockMappings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlockMappings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
                name: "IX_BlockMappings_BlockName",
                table: "BlockMappings",
                column: "BlockName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BlockMappings_EquipmentType",
                table: "BlockMappings",
                column: "EquipmentType");
        }
    }
}
