using BTL_WebNC_Quản_lý_chấm_công.Helpers;
using BTL_WebNC_Quản_lý_chấm_công.Models;
using Microsoft.EntityFrameworkCore;
namespace BTL_WebNC_Quản_lý_chấm_công.Data
{
    public static class SeedData
    {
        private static string Hash(string password) => PasswordHelper.Hash(password);
        public static void Initialize(ApplicationDbContext context)
        {
            // Chỉ seed khi database chưa có dữ liệu ở các bảng liên quan.
            // Tránh thêm trùng khi ứng dụng khởi động nhiều lần.
            if (context.Roles.Any() || context.Departments.Any() ||
            context.Positions.Any() || context.Employees.Any() ||
            context.Accounts.Any() || context.Attendances.Any() ||
            context.Leaves.Any() || context.OverTimes.Any() ||
            context.Payrolls.Any())
            {
                return;
            }
            var rAdmin = new Role
            {
                RoleName = "Admin", Description = "Quản trị hệ thống"
            };
            var rHr = new Role
            {
                RoleName = "HR", Description = "Quản lý nhân sự, chấm công, lương"
            };
            var rEmp = new Role
            {
                RoleName = "Employee", Description = "Nhân viên"
            };
            context.Roles.AddRange(rAdmin, rHr, rEmp);
            var dTech = new Department
            {
                DepartmentName = "Phòng Kỹ thuật", Description = "Phát triển phần mềm", Status = "Active"
            };
            var dHr = new Department
            {
                DepartmentName = "Phòng Nhân sự", Description = "Quản lý nhân sự", Status = "Active"
            };
            var dAcc = new Department
            {
                DepartmentName = "Phòng Kế toán", Description = "Kế toán, tiền lương", Status = "Active"
            };
            var dOld = new Department
            {
                DepartmentName = "Phòng Marketing", Description = "Tạm ngưng hoạt động", Status = "Inactive"
            };
            context.Departments.AddRange(dTech, dHr, dAcc, dOld);
            var pDirector = new Position
            {
                PositionName = "Giám đốc", Description = "Điều hành công ty", Status = "Active"
            };
            var pManager = new Position
            {
                PositionName = "Trưởng phòng", Description = "Quản lý phòng ban", Status = "Active"
            };
            var pStaff = new Position
            {
                PositionName = "Nhân viên", Description = "Nhân viên chính thức", Status = "Active"
            };
            var pIntern = new Position
            {
                PositionName = "Thực tập sinh", Description = "Đang thực tập", Status = "Active"
            };
            context.Positions.AddRange(pDirector, pManager, pStaff, pIntern);
            var e1 = new Employee
            {
                EmployeeCode = "NV001", FullName = "Nguyễn Văn An", DateOfBirth = new DateTime(1990, 3, 15), Gender = "Nam", Phone = "0901000001", Email = "an@congty.vn", Address = "Hà Nội", Department = dHr, Position = pDirector, HireDate = new DateTime(2018, 1, 10), Salary = 30000000m, Status = "Active"
            };
            var e2 = new Employee
            {
                EmployeeCode = "NV002", FullName = "Trần Thị Bình", DateOfBirth = new DateTime(1994, 7, 20), Gender = "Nữ", Phone = "0901000002", Email = "binh@congty.vn", Address = "Bắc Ninh", Department = dHr, Position = pManager, HireDate = new DateTime(2020, 5, 1), Salary = 20000000m, Status = "Active"
            };
            var e3 = new Employee
            {
                EmployeeCode = "NV003", FullName = "Lê Văn Cường", DateOfBirth = new DateTime(1998, 11, 2), Gender = "Nam", Phone = "0901000003", Email = "cuong@congty.vn", Address = "Hải Phòng", Department = dTech, Position = pStaff, HireDate = new DateTime(2022, 2, 15), Salary = 13000000m, Status = "Active"
            };
            var e4 = new Employee
            {
                EmployeeCode = "NV004", FullName = "Phạm Thị Dung", DateOfBirth = new DateTime(1996, 9, 9), Gender = "Nữ", Phone = "0901000004", Email = "dung@congty.vn", Address = "Hà Nội", Department = dAcc, Position = pStaff, HireDate = new DateTime(2021, 8, 20), Salary = 12000000m, Status = "Active"
            };
            var e5 = new Employee
            {
                EmployeeCode = "NV005", FullName = "Hoàng Văn Em", DateOfBirth = new DateTime(2002, 1, 25), Gender = "Nam", Phone = "0901000005", Email = "em@congty.vn", Address = "Bắc Giang", Department = dTech, Position = pIntern, HireDate = new DateTime(2024, 6, 1), Salary = 5000000m, Status = "Active"
            };
            var e6 = new Employee
            {
                EmployeeCode = "NV006", FullName = "Vũ Thị Phương", DateOfBirth = new DateTime(1995, 4, 12), Gender = "Nữ", Phone = "0901000006", Email = "phuong@congty.vn", Address = "Hưng Yên", Department = dAcc, Position = pStaff, HireDate = new DateTime(2019, 3, 3), Salary = 12500000m, Status = "Resigned"
            };
            context.Employees.AddRange(e1, e2, e3, e4, e5, e6);
            context.Accounts.AddRange( new Account
            {
                Username = "admin", PasswordHash = Hash("123456"), Role = rAdmin, Employee = e1, Status = "Active"
            }
            , new Account
            {
                Username = "hr.binh", PasswordHash = Hash("123456"), Role = rHr, Employee = e2, Status = "Active"
            }
            , new Account
            {
                Username = "cuong", PasswordHash = Hash("123456"), Role = rEmp, Employee = e3, Status = "Active"
            }
            , new Account
            {
                Username = "dung", PasswordHash = Hash("123456"), Role = rEmp, Employee = e4, Status = "Active"
            }
            , new Account
            {
                Username = "em", PasswordHash = Hash("123456"), Role = rEmp, Employee = e5, Status = "Locked"
            }
            , new Account
            {
                Username = "system", PasswordHash = Hash("123456"), Role = rAdmin, Employee = null, Status = "Active"
            }
            );
 var today = DateTime.Today;
            var yesterday = today.AddDays(-1);
            context.Attendances.AddRange( new Attendance
            {
                Employee = e1, WorkDate = today, CheckIn = today.AddHours(7).AddMinutes(55), CheckOut = today.AddHours(17).AddMinutes(5), TotalHours = 8.17m, Status = "Present"
            }
            , new Attendance
            {
                Employee = e2, WorkDate = today, CheckIn = today.AddHours(8).AddMinutes(25), CheckOut = today.AddHours(17).AddMinutes(30), TotalHours = 8.08m, Status = "Late", Note = "Kẹt xe"
            }
            , new Attendance
            {
                Employee = e3, WorkDate = today, CheckIn = today.AddHours(8), CheckOut = today.AddHours(16).AddMinutes(30), TotalHours = 7.5m, Status = "EarlyLeave", Note = "Về sớm có việc riêng"
            }
            ,
            new Attendance
            {
                Employee = e4, WorkDate = today, CheckIn = today.AddHours(8).AddMinutes(20), CheckOut = today.AddHours(16).AddMinutes(40), TotalHours = 7.33m, Status = "LateAndEarly"
            }
            , new Attendance
            {
                Employee = e5, WorkDate = today, Status = "Leave", Note = "Nghỉ ốm có phép"
            }
            , new Attendance
            {
                Employee = e1, WorkDate = yesterday, CheckIn = yesterday.AddHours(8), CheckOut = yesterday.AddHours(17), TotalHours = 8m, Status = "Present"
            }
            , new Attendance
            {
                Employee = e3, WorkDate = yesterday, Status = "Absent", Note = "Vắng không phép"
            }
            );
context.Leaves.AddRange( new Leave
            {
                Employee = e5, LeaveType = "Sick", StartDate = today, EndDate = today.AddDays(1), Reason = "Sốt, đã có giấy khám", Status = "Approved", Approver = e2
            }
            , new Leave
            {
                Employee = e3, LeaveType = "Annual", StartDate = today.AddDays(10), EndDate = today.AddDays(12), Reason = "Du lịch gia đình", Status = "Pending"
            }
            , new Leave
            {
                Employee = e4, LeaveType = "Unpaid", StartDate = today.AddDays(-5), EndDate = today.AddDays(-4), Reason = "Việc riêng", Status = "Rejected", Approver = e2
            }
            , new Leave
            {
                Employee = e2, LeaveType = "Other", StartDate = today.AddDays(20), EndDate = today.AddDays(20), Reason = "Đi đám cưới", Status = "Cancelled"
            }
            );
 context.OverTimes.AddRange( new Overtime
            {
                Employee = e3, WorkDate = today.AddDays(-2), StartTime = new TimeSpan(18, 0, 0), EndTime = new TimeSpan(21, 0, 0), TotalHours = 3m, Reason = "Chạy deadline dự án", Status = "Approved", Approver = e2
            }
            , new Overtime
            {
                Employee = e4, WorkDate = today.AddDays(-3), StartTime = new TimeSpan(17, 30, 0), EndTime = new TimeSpan(19, 30, 0), TotalHours = 2m, Reason = "Chốt sổ cuối tháng", Status = "Approved", Approver = e2
            }
            , new Overtime
            {
                Employee = e5, WorkDate = today.AddDays(-1), StartTime = new TimeSpan(18, 0, 0), EndTime = new TimeSpan(20, 0, 0), TotalHours = 2m, Reason = "Hỗ trợ release", Status = "Pending"
            }
            );
var last = today.AddMonths(-1);
            context.Payrolls.AddRange( new Payroll
            {
                Employee = e3, Month = (byte)last.Month, Year = (short)last.Year, BaseSalary = 13000000m, WorkingDays = 22m, WorkingHours = 176m, OvertimeHours = 3m, Allowance = 500000m, Deduction = 200000m, TotalSalary = 13550000m, Status = "Paid"
            }
            , new Payroll
            {
                Employee = e4, Month = (byte)last.Month, Year = (short)last.Year, BaseSalary = 12000000m, WorkingDays = 21.5m, WorkingHours = 172m, OvertimeHours = 2m, Allowance = 500000m, Deduction = 0m, TotalSalary = 12600000m, Status = "Approved"
            }
            ,
            new Payroll
            {
                Employee = e5, Month = (byte)last.Month, Year = (short)last.Year, BaseSalary = 5000000m, WorkingDays = 20m, WorkingHours = 160m, OvertimeHours = 0m, Allowance = 0m, Deduction = 0m, TotalSalary = 5000000m, Status = "Draft"
            }
            );
            context.SaveChanges();
        }
    }
}
