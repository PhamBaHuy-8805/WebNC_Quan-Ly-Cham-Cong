using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BTL_WebNC_Quản_lý_chấm_công.Migrations
{
    /// <inheritdoc />
    public partial class FixLeaveOvertimeRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Giữ thông tin người duyệt cũ; dừng nếu hai quan hệ cũ mâu thuẫn.
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM Leaves WHERE EmployeeID1 IS NOT NULL AND EmployeeID1 <> EmployeeID)
                    THROW 51000, 'Leaves has conflicting EmployeeID and EmployeeID1. Reconcile these records before migrating.', 1;
                IF EXISTS (SELECT 1 FROM OverTimes WHERE ApprovedBy IS NOT NULL AND ApproverEmployeeID IS NOT NULL AND ApprovedBy <> ApproverEmployeeID)
                    THROW 51001, 'OverTimes has conflicting approver references. Reconcile these records before migrating.', 1;
                UPDATE OverTimes SET ApprovedBy = ApproverEmployeeID
                WHERE ApprovedBy IS NULL AND ApproverEmployeeID IS NOT NULL;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_Leaves_Employees_EmployeeID1",
                table: "Leaves");

            migrationBuilder.DropForeignKey(
                name: "FK_OverTimes_Employees_ApproverEmployeeID",
                table: "OverTimes");

            migrationBuilder.DropIndex(
                name: "IX_OverTimes_ApproverEmployeeID",
                table: "OverTimes");

            migrationBuilder.DropIndex(
                name: "IX_Leaves_EmployeeID1",
                table: "Leaves");

            migrationBuilder.DropColumn(
                name: "ApproverEmployeeID",
                table: "OverTimes");

            migrationBuilder.DropColumn(
                name: "EmployeeID1",
                table: "Leaves");

            migrationBuilder.CreateIndex(
                name: "IX_OverTimes_ApprovedBy",
                table: "OverTimes",
                column: "ApprovedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_OverTimes_Employees_ApprovedBy",
                table: "OverTimes",
                column: "ApprovedBy",
                principalTable: "Employees",
                principalColumn: "EmployeeID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OverTimes_Employees_ApprovedBy",
                table: "OverTimes");

            migrationBuilder.DropIndex(
                name: "IX_OverTimes_ApprovedBy",
                table: "OverTimes");

            migrationBuilder.AddColumn<int>(
                name: "ApproverEmployeeID",
                table: "OverTimes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EmployeeID1",
                table: "Leaves",
                type: "int",
                nullable: true);

            migrationBuilder.Sql("UPDATE OverTimes SET ApproverEmployeeID = ApprovedBy; UPDATE Leaves SET EmployeeID1 = EmployeeID;");

            migrationBuilder.CreateIndex(
                name: "IX_OverTimes_ApproverEmployeeID",
                table: "OverTimes",
                column: "ApproverEmployeeID");

            migrationBuilder.CreateIndex(
                name: "IX_Leaves_EmployeeID1",
                table: "Leaves",
                column: "EmployeeID1");

            migrationBuilder.AddForeignKey(
                name: "FK_Leaves_Employees_EmployeeID1",
                table: "Leaves",
                column: "EmployeeID1",
                principalTable: "Employees",
                principalColumn: "EmployeeID");

            migrationBuilder.AddForeignKey(
                name: "FK_OverTimes_Employees_ApproverEmployeeID",
                table: "OverTimes",
                column: "ApproverEmployeeID",
                principalTable: "Employees",
                principalColumn: "EmployeeID");
        }
    }
}
