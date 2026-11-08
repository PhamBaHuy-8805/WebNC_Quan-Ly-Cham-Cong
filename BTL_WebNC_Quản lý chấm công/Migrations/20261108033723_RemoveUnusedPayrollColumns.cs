using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BTL_WebNC_Quản_lý_chấm_công.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnusedPayrollColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "Payrolls");

            migrationBuilder.DropColumn(
                name: "ApprovedBy",
                table: "Payrolls");

            migrationBuilder.DropColumn(
                name: "CalculatedAt",
                table: "Payrolls");

            migrationBuilder.DropColumn(
                name: "CalculationDetails",
                table: "Payrolls");

            migrationBuilder.DropColumn(
                name: "OvertimeMultiplier",
                table: "Payrolls");

            migrationBuilder.DropColumn(
                name: "OvertimePay",
                table: "Payrolls");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "Payrolls");

            migrationBuilder.DropColumn(
                name: "StandardDays",
                table: "Payrolls");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "Payrolls",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApprovedBy",
                table: "Payrolls",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CalculatedAt",
                table: "Payrolls",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CalculationDetails",
                table: "Payrolls",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "OvertimeMultiplier",
                table: "Payrolls",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OvertimePay",
                table: "Payrolls",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "Payrolls",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StandardDays",
                table: "Payrolls",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
