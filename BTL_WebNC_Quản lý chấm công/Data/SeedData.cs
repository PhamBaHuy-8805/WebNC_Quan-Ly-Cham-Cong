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

    public static async Task InitializeAsync(ApplicationDbContext context)
    {
        // Nếu có lỗi ở một bước, hoàn tác cả lần seed để tránh dữ liệu dở dang.
        await using var transaction = await context.Database.BeginTransactionAsync();

        // 1. Tạo các bảng được tham chiếu trước. Không gán ID thủ công;
        // SQL Server tự sinh ID khi SaveChangesAsync được gọi.
        var roleNames = new[] { "Admin", "HR", "Employee" };
        foreach (var name in roleNames)
        {
            if (!await context.Roles.AnyAsync(r => r.RoleName == name))
            {
                context.Roles.Add(new Role { RoleName = name });
            }
        }

        var departmentNames = new[] { "Nhân sự", "Kỹ thuật", "Kế toán" };
        foreach (var name in departmentNames)
        {
            if (!await context.Departments.AnyAsync(d => d.DepartmentName == name))
            {
                context.Departments.Add(new Department
                {
                    DepartmentName = name,
                    Description = "Dữ liệu mẫu bài thực hành"
                });
            }
        }

        var positionNames = new[] { "Trưởng phòng", "Nhân viên", "Thực tập sinh" };
        foreach (var name in positionNames)
        {
            if (!await context.Positions.AnyAsync(p => p.PositionName == name))
            {
                context.Positions.Add(new Position { PositionName = name });
            }
        }

        await context.SaveChangesAsync();

        // Lấy ID thực tế để liên kết nhân viên, tài khoản với các bảng trên.
        var roles = await context.Roles.Where(r => roleNames.Contains(r.RoleName))
            .ToDictionaryAsync(r => r.RoleName);
        var departments = await context.Departments
            .Where(d => departmentNames.Contains(d.DepartmentName))
            .ToDictionaryAsync(d => d.DepartmentName);
        var positions = await context.Positions
            .Where(p => positionNames.Contains(p.PositionName))
            .ToDictionaryAsync(p => p.PositionName);

        // 2. Có 12 nhân viên để thử phân trang với kích thước trang là 5 hoặc 10.
        var fullNames = new[]
        {
            "Nguyễn Văn An", "Trần Thị Bình", "Lê Minh Châu", "Phạm Quốc Dũng",
            "Hoàng Thu Hà", "Vũ Trung Hiếu", "Đặng Ngọc Lan", "Bùi Đức Minh",
            "Đỗ Hoài Nam", "Hồ Thanh Phương", "Ngô Anh Quân", "Dương Thị Thảo"
        };

        var workDate = new DateTime(2026, 10, 12); // Cố định để chạy lại không sinh ngày mới.

        for (var i = 0; i < fullNames.Length; i++)
        {
            var employeeCode = $"DEMO{i + 1:000}";
            var employee = await context.Employees
                .SingleOrDefaultAsync(e => e.EmployeeCode == employeeCode);

            if (employee == null)
            {
                employee = new Employee
                {
                    EmployeeCode = employeeCode,
                    FullName = fullNames[i],
                    Email = $"demo{i + 1:000}@example.com",
                    HireDate = new DateTime(2026, 9, 1),
                    Salary = 8_000_000m + i * 500_000m,
                    DepartmentID = departments[departmentNames[i % 3]].DepartmentID,
                    PositionID = positions[positionNames[i % 3]].PositionID
                };
                context.Employees.Add(employee);
                await context.SaveChangesAsync();
            }

            // 3. Mỗi nhân viên tối đa một tài khoản. Không đổi mật khẩu hay
            // ghi đè tài khoản đã có khi chạy seed lần tiếp theo.
            var username = i switch
            {
                0 => "demo.admin",
                1 => "demo.hr",
                _ => $"demo.nv{i + 1:000}"
            };

            if (!await context.Accounts.AnyAsync(a =>
                    a.Username == username || a.EmployeeID == employee.EmployeeID))
            {
                var account = new Account
                {
                    Username = username,
                    EmployeeID = employee.EmployeeID,
                    RoleID = roles[roleNames[Math.Min(i, 2)]].RoleID
                };
                // Chỉ dùng cho dữ liệu demo trong Development.
                account.PasswordHash = "123456";
                context.Accounts.Add(account);
            }

            // 4. Một bản chấm công mẫu cho mỗi nhân viên trong ngày cố định.
            // Kiểm tra cả khoảng ngày để không thêm trùng nếu WorkDate có giờ.
            var nextDate = workDate.AddDays(1);
            if (!await context.Attendances.AnyAsync(a =>
                    a.EmployeeID == employee.EmployeeID &&
                    a.WorkDate >= workDate && a.WorkDate < nextDate))
            {
                context.Attendances.Add(new Attendance
                {
                    EmployeeID = employee.EmployeeID,
                    WorkDate = workDate,
                    CheckIn = workDate.AddHours(8),
                    CheckOut = workDate.AddHours(17),
                    TotalHours = 8m,
                    Status = "Present",
                    Note = "Dữ liệu mẫu: 08:00–17:00, trừ 1 giờ nghỉ trưa."
                });
            }

            await context.SaveChangesAsync();
        }

        // 5. Module 3: mẫu nghỉ phép/tăng ca cho Employee, có đủ ba trạng thái.
        var reviewer = await context.Accounts.Include(a => a.Role)
            .SingleOrDefaultAsync(a => a.Username == "demo.hr");
        if (reviewer?.EmployeeID == null || reviewer.Role?.RoleName != "HR")
            throw new InvalidOperationException("Tài khoản demo.hr phải liên kết nhân viên và có vai trò HR để seed Module 3.");

        for (var i = 3; i <= 12; i++)
        {
            var code = $"DEMO{i:000}";
            var employee = await context.Employees.SingleAsync(e => e.EmployeeCode == code);
            var sampleCount = i == 3 ? 6 : 3;
            for (var j = 0; j < sampleCount; j++)
            {
                var date = new DateTime(2026, 11, 2).AddDays(j * 3);
                var status = (j % 3) switch { 0 => "Pending", 1 => "Approved", _ => "Rejected" };
                int? approvedBy = status == "Pending" ? null : reviewer.EmployeeID;
                if (!await context.Leaves.AnyAsync(l => l.EmployeeID == employee.EmployeeID
                    && l.StartDate == date && l.EndDate == date && l.LeaveType == "Annual"))
                {
                    context.Leaves.Add(new Leave
                    {
                        EmployeeID = employee.EmployeeID, LeaveType = "Annual",
                        StartDate = date, EndDate = date, Reason = "Dữ liệu mẫu Module 3: nghỉ việc riêng.",
                        Status = status, ApprovedBy = approvedBy
                    });
                }
                var startTime = TimeSpan.FromHours(18);
                var endTime = TimeSpan.FromHours(20);
                var overtimeDate = date.AddDays(1);
                if (!await context.OverTimes.AnyAsync(o => o.EmployeeID == employee.EmployeeID
                    && o.WorkDate == overtimeDate && o.StartTime == startTime && o.EndTime == endTime))
                {
                    context.OverTimes.Add(new Overtime
                    {
                        EmployeeID = employee.EmployeeID, WorkDate = overtimeDate,
                        StartTime = startTime, EndTime = endTime, TotalHours = 2m,
                        Reason = "Dữ liệu mẫu Module 3: hoàn thành công việc cuối kỳ.",
                        Status = status, ApprovedBy = approvedBy
                    });
                }
                await context.SaveChangesAsync();
            }
        }

        await transaction.CommitAsync();
    }
    }
}
