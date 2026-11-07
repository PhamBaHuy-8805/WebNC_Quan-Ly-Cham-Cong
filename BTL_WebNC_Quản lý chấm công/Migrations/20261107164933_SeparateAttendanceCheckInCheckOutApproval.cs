using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BTL_WebNC_Quản_lý_chấm_công.Migrations
{
    /// <inheritdoc />
    public partial class SeparateAttendanceCheckInCheckOutApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CheckInApprovalStatus",
                table: "Attendances",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.AddColumn<string>(
                name: "CheckInReviewNote",
                table: "Attendances",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CheckInReviewedAt",
                table: "Attendances",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CheckInReviewedBy",
                table: "Attendances",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CheckOutApprovalStatus",
                table: "Attendances",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pending");

            // Giữ kết quả duyệt chung cũ; bản đang chờ phải được duyệt riêng từng bước.
            migrationBuilder.Sql("""
                UPDATE Attendances SET
                    CheckInApprovalStatus = CASE
                        WHEN CheckIn IS NOT NULL AND ApprovalStatus = 'Rejected' THEN 'Rejected'
                        WHEN CheckIn IS NOT NULL AND CAST(CheckIn AS date) = WorkDate AND ApprovalStatus = 'Approved' THEN 'Approved'
                        ELSE 'Pending' END,
                    CheckOutApprovalStatus = CASE
                        WHEN CheckOut IS NOT NULL AND ApprovalStatus = 'Rejected' THEN 'Rejected'
                        WHEN CheckIn IS NOT NULL AND CheckOut > CheckIn
                            AND CAST(CheckIn AS date) = WorkDate AND CAST(CheckOut AS date) = WorkDate
                            AND ApprovalStatus = 'Approved' THEN 'Approved'
                        ELSE 'Pending' END,
                    CheckInReviewedBy = CASE WHEN ApprovalStatus IN ('Approved', 'Rejected') AND CheckIn IS NOT NULL THEN ReviewedBy ELSE NULL END,
                    CheckInReviewedAt = CASE WHEN ApprovalStatus IN ('Approved', 'Rejected') AND CheckIn IS NOT NULL THEN ReviewedAt ELSE NULL END,
                    CheckInReviewNote = CASE WHEN ApprovalStatus IN ('Approved', 'Rejected') AND CheckIn IS NOT NULL
                        THEN LEFT(N'Chuyển từ xác nhận chung trước đây. ' + COALESCE(ReviewNote, N''), 500) ELSE NULL END;

                UPDATE Attendances SET ApprovalStatus = CASE
                    WHEN ApprovalStatus = 'Rejected' THEN 'Rejected'
                    WHEN CheckInApprovalStatus = 'Approved' AND CheckOutApprovalStatus = 'Approved' THEN 'Approved'
                    ELSE 'Pending' END;
                UPDATE Attendances SET TotalHours = 0 WHERE ApprovalStatus <> 'Approved';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_CheckInReviewedBy",
                table: "Attendances",
                column: "CheckInReviewedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_Employees_CheckInReviewedBy",
                table: "Attendances",
                column: "CheckInReviewedBy",
                principalTable: "Employees",
                principalColumn: "EmployeeID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attendances_Employees_CheckInReviewedBy",
                table: "Attendances");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_CheckInReviewedBy",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "CheckInApprovalStatus",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "CheckInReviewNote",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "CheckInReviewedAt",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "CheckInReviewedBy",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "CheckOutApprovalStatus",
                table: "Attendances");
        }
    }
}
