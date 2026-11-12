using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BTL_WebNC_Quản_lý_chấm_công.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileChangeRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProfileChangeRequests",
                columns: table => new
                {
                    ProfileChangeRequestID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeID = table.Column<int>(type: "int", nullable: false),
                    OriginalPhone = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    OriginalEmail = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    OriginalAddress = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    RequestedPhone = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    RequestedEmail = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    RequestedAddress = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReviewNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReviewedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfileChangeRequests", x => x.ProfileChangeRequestID);
                    table.ForeignKey(
                        name: "FK_ProfileChangeRequests_Employees_EmployeeID",
                        column: x => x.EmployeeID,
                        principalTable: "Employees",
                        principalColumn: "EmployeeID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProfileChangeRequests_Employees_ReviewedBy",
                        column: x => x.ReviewedBy,
                        principalTable: "Employees",
                        principalColumn: "EmployeeID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProfileChangeRequests_EmployeeID",
                table: "ProfileChangeRequests",
                column: "EmployeeID",
                unique: true,
                filter: "[Status] = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileChangeRequests_ReviewedBy",
                table: "ProfileChangeRequests",
                column: "ReviewedBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProfileChangeRequests");
        }
    }
}
