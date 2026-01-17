using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PIDStandardization.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLineUnitFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DesignPressureUnit",
                table: "Lines",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DesignTemperatureUnit",
                table: "Lines",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DesignPressureUnit",
                table: "Lines");

            migrationBuilder.DropColumn(
                name: "DesignTemperatureUnit",
                table: "Lines");
        }
    }
}
