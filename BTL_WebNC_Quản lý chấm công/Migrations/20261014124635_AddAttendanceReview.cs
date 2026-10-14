using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BTL_WebNC_Quản_lý_chấm_công.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT EmployeeID, CAST(WorkDate AS date)
                    FROM Attendances
                    GROUP BY EmployeeID, CAST(WorkDate AS date)
                    HAVING COUNT(*) > 1
                )
                    THROW 51002, 'Duplicate attendance days exist. Reconcile records before applying the unique employee/day index.', 1;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_Attendances_Employees_EmployeeID",
                table: "Attendances");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_EmployeeID",
                table: "Attendances");

            migrationBuilder.AlterColumn<DateTime>(
                name: "WorkDate",
                table: "Attendances",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<string>(
                name: "ApprovalStatus",
                table: "Attendances",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.AddColumn<string>(
                name: "ReviewNote",
                table: "Attendances",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "Attendances",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReviewedBy",
                table: "Attendances",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Attendances",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_EmployeeID_WorkDate",
                table: "Attendances",
                columns: new[] { "EmployeeID", "WorkDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_ReviewedBy",
                table: "Attendances",
                column: "ReviewedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_Employees_EmployeeID",
                table: "Attendances",
                column: "EmployeeID",
                principalTable: "Employees",
                principalColumn: "EmployeeID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_Employees_ReviewedBy",
                table: "Attendances",
                column: "ReviewedBy",
                principalTable: "Employees",
                principalColumn: "EmployeeID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attendances_Employees_EmployeeID",
                table: "Attendances");

            migrationBuilder.DropForeignKey(
                name: "FK_Attendances_Employees_ReviewedBy",
                table: "Attendances");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_EmployeeID_WorkDate",
                table: "Attendances");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_ReviewedBy",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "ReviewNote",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "ReviewedBy",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Attendances");

            migrationBuilder.AlterColumn<DateTime>(
                name: "WorkDate",
                table: "Attendances",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "date");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_EmployeeID",
                table: "Attendances",
                column: "EmployeeID");

            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_Employees_EmployeeID",
                table: "Attendances",
                column: "EmployeeID",
                principalTable: "Employees",
                principalColumn: "EmployeeID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
