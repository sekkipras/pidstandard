using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PIDStandardization.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessParametersAndConnectivity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LineId",
                table: "Instruments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DesignPressure",
                table: "Equipment",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DesignPressureUnit",
                table: "Equipment",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DesignTemperature",
                table: "Equipment",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DesignTemperatureUnit",
                table: "Equipment",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DownstreamEquipmentId",
                table: "Equipment",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FlowRate",
                table: "Equipment",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FlowRateUnit",
                table: "Equipment",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OperatingPressure",
                table: "Equipment",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OperatingPressureUnit",
                table: "Equipment",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OperatingTemperature",
                table: "Equipment",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OperatingTemperatureUnit",
                table: "Equipment",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PowerOrCapacity",
                table: "Equipment",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PowerOrCapacityUnit",
                table: "Equipment",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpstreamEquipmentId",
                table: "Equipment",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Instruments_LineId",
                table: "Instruments",
                column: "LineId");

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_DownstreamEquipmentId",
                table: "Equipment",
                column: "DownstreamEquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_UpstreamEquipmentId",
                table: "Equipment",
                column: "UpstreamEquipmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Equipment_Equipment_DownstreamEquipmentId",
                table: "Equipment",
                column: "DownstreamEquipmentId",
                principalTable: "Equipment",
                principalColumn: "EquipmentId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Equipment_Equipment_UpstreamEquipmentId",
                table: "Equipment",
                column: "UpstreamEquipmentId",
                principalTable: "Equipment",
                principalColumn: "EquipmentId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Instruments_Lines_LineId",
                table: "Instruments",
                column: "LineId",
                principalTable: "Lines",
                principalColumn: "LineId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Equipment_Equipment_DownstreamEquipmentId",
                table: "Equipment");

            migrationBuilder.DropForeignKey(
                name: "FK_Equipment_Equipment_UpstreamEquipmentId",
                table: "Equipment");

            migrationBuilder.DropForeignKey(
                name: "FK_Instruments_Lines_LineId",
                table: "Instruments");

            migrationBuilder.DropIndex(
                name: "IX_Instruments_LineId",
                table: "Instruments");

            migrationBuilder.DropIndex(
                name: "IX_Equipment_DownstreamEquipmentId",
                table: "Equipment");

            migrationBuilder.DropIndex(
                name: "IX_Equipment_UpstreamEquipmentId",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "LineId",
                table: "Instruments");

            migrationBuilder.DropColumn(
                name: "DesignPressure",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "DesignPressureUnit",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "DesignTemperature",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "DesignTemperatureUnit",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "DownstreamEquipmentId",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "FlowRate",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "FlowRateUnit",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "OperatingPressure",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "OperatingPressureUnit",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "OperatingTemperature",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "OperatingTemperatureUnit",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "PowerOrCapacity",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "PowerOrCapacityUnit",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "UpstreamEquipmentId",
                table: "Equipment");
        }
    }
}
