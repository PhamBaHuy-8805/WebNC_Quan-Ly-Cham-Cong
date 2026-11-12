using System.ComponentModel.DataAnnotations;
using System.Reflection;
using BTL_WebNC_Quản_lý_chấm_công.Controllers;
using BTL_WebNC_Quản_lý_chấm_công.Data;
using BTL_WebNC_Quản_lý_chấm_công.Models;
using BTL_WebNC_Quản_lý_chấm_công.Services;
using BTL_WebNC_Quản_lý_chấm_công.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var checks = 0;
void Check(bool ok, string name)
{
    if (!ok) throw new Exception("FAIL: " + name);
    Console.WriteLine("PASS: " + name);
    checks++;
}
bool Valid(object model) => Validator.TryValidateObject(model, new ValidationContext(model), new List<ValidationResult>(), true);
var passwordHasher = new ExistingAccountPasswordHasher();
var passwordAccount = new Account();
var passwordHash = passwordHasher.HashPassword(passwordAccount, "MatKhau@123");
Check(passwordHasher.VerifyHashedPassword(passwordAccount, passwordHash, "MatKhau@123") != PasswordVerificationResult.Failed, "Hashed password verifies");
Check(passwordHasher.VerifyHashedPassword(passwordAccount, passwordHash, "wrong") == PasswordVerificationResult.Failed, "Wrong password is rejected");
Check(passwordHasher.VerifyHashedPassword(passwordAccount, "legacy-pass", "legacy-pass") == PasswordVerificationResult.SuccessRehashNeeded, "Legacy plaintext is upgraded after login");
var validLeave = new LeaveCreateViewModel { StartDate = new(2026, 12, 1), EndDate = new(2026, 12, 2), Reason = "Personal work" };
Check(Valid(validLeave), "Accept valid leave range");
Check(!Valid(new LeaveCreateViewModel { StartDate = new(2026, 12, 2), EndDate = new(2026, 12, 1), Reason = "Test" }), "Reject reversed leave dates");
Check(!Valid(new LeaveCreateViewModel { StartDate = new(2026, 12, 1), EndDate = new(2026, 12, 2), Reason = " " }), "Reject blank reason");
Check(!Valid(new LeaveCreateViewModel { LeaveType = "Unknown", StartDate = new(2026, 12, 1), EndDate = new(2026, 12, 2), Reason = "Test" }), "Reject unknown leave type");
Check(!Valid(new LeaveCreateViewModel { Reason = "Test" }), "Require leave dates");
Check(!Valid(new OvertimeCreateViewModel { WorkDate = new(2026, 12, 1), StartTime = TimeSpan.FromHours(20), EndTime = TimeSpan.FromHours(18), Reason = "Test" }), "Reject reversed overtime hours");
Check(!Valid(new OvertimeCreateViewModel { WorkDate = new(2026, 12, 1), StartTime = TimeSpan.FromHours(18), EndTime = TimeSpan.FromHours(18), Reason = "Test" }), "Reject zero duration");
Check(!Valid(new OvertimeCreateViewModel { WorkDate = new(2026, 12, 1), StartTime = TimeSpan.Zero, EndTime = TimeSpan.FromHours(25), Reason = "Test" }), "Reject hours beyond one day");
Check(!Valid(new OvertimeCreateViewModel { Reason = "Test" }), "Require overtime date and times");
var validOt = new OvertimeCreateViewModel { WorkDate = new(2026, 12, 1), StartTime = TimeSpan.FromHours(18), EndTime = TimeSpan.FromHours(20.5), Reason = " Release " };
Check(Valid(validOt), "Accept valid overtime");
var entity = new Overtime();
typeof(OvertimeController).GetMethod("ApplyForm", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, new object[] { entity, validOt });
Check(entity.TotalHours == 2.5m && entity.Reason == "Release", "Server computes fractional overtime duration");
validOt.EndTime = TimeSpan.FromHours(21);
typeof(OvertimeController).GetMethod("ApplyForm", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, new object[] { entity, validOt });
Check(entity.TotalHours == 3m, "Edit recalculates overtime duration");
foreach (var type in new[] { typeof(LeaveCreateViewModel), typeof(OvertimeCreateViewModel) })
    Check(!type.GetProperties().Any(p => new[] { "EmployeeID", "Status", "ApprovedBy", "TotalHours" }.Contains(p.Name)), type.Name + " cannot bind protected fields");
foreach (var type in new[] { typeof(LeaveController), typeof(OvertimeController) })
{
    Check(type.GetCustomAttributes<AuthorizeAttribute>(true).Any(), type.Name + " requires authentication");
    foreach (var name in new[] { "Approve", "Reject" })
    {
        var method = type.GetMethod(name)!;
        Check(method.GetCustomAttributes<AuthorizeAttribute>(true).Any(a => a.Roles == "HR,Admin")
            && method.IsDefined(typeof(HttpPostAttribute), true) && method.IsDefined(typeof(ValidateAntiForgeryTokenAttribute), true), type.Name + "." + name + " has role/POST/CSRF guards");
    }
    foreach (var method in type.GetMethods().Where(m => new[] { "Create", "Edit", "Delete", "DeleteConfirmed" }.Contains(m.Name)))
    {
        Check(method.GetCustomAttributes<AuthorizeAttribute>(true).Any(a => a.Roles == "Employee"), type.Name + "." + method.Name + " is employee-only");
        if (method.IsDefined(typeof(HttpPostAttribute), true))
            Check(method.IsDefined(typeof(ValidateAntiForgeryTokenAttribute), true), type.Name + "." + method.Name + " validates CSRF token");
    }
}
var validProfile = new ProfileContactRequestViewModel
{
    Phone = "0901234567", Email = "nhanvien@example.com", Address = "Hà Nội"
};
Check(Valid(validProfile), "Accept valid profile contact request");
Check(!Valid(new ProfileContactRequestViewModel { Email = "not-an-email" }), "Reject invalid profile email");
Check(!Valid(new ProfileContactRequestViewModel { Phone = "1234567890123456" }), "Reject overly long profile phone");
Check(!typeof(ProfileContactRequestViewModel).GetProperties().Any(p =>
        new[] { "EmployeeID", "Status", "ReviewedBy", "OriginalPhone", "OriginalEmail", "OriginalAddress" }.Contains(p.Name)),
    "Profile contact request cannot bind protected fields");
Check(typeof(ProfileController).GetCustomAttributes<AuthorizeAttribute>(true)
        .Any(a => a.Roles == "Employee,HR,Admin"), "ProfileController requires signed-in employee roles");
foreach (var name in new[] { "Approve", "Reject" })
{
    var method = typeof(ProfileController).GetMethod(name)!;
    Check(method.GetCustomAttributes<AuthorizeAttribute>(true).Any(a => a.Roles == "HR,Admin")
        && method.IsDefined(typeof(HttpPostAttribute), true)
        && method.IsDefined(typeof(ValidateAntiForgeryTokenAttribute), true),
        "Profile." + name + " has role/POST/CSRF guards");
}
foreach (var name in new[] { "EditRequest", "CancelRequest" })
{
    var methods = typeof(ProfileController).GetMethods()
        .Where(m => m.Name == name && m.IsDefined(typeof(HttpPostAttribute), true)).ToArray();
    Check(methods.Length > 0
        && methods.All(m => m.IsDefined(typeof(ValidateAntiForgeryTokenAttribute), true)),
        "Profile." + name + " POST validates CSRF token");
}
var validPasswordChange = new ChangePasswordViewModel
{
    CurrentPassword = "old-pass", NewPassword = "new-pass-123", ConfirmPassword = "new-pass-123"
};
Check(Valid(validPasswordChange), "Accept valid self-service password change");
Check(!Valid(new ChangePasswordViewModel
{
    CurrentPassword = "old-pass", NewPassword = "new-pass-123", ConfirmPassword = "different"
}), "Reject mismatched password confirmation");
Check(!Valid(new ChangePasswordViewModel
{
    CurrentPassword = "old-pass", NewPassword = "12345", ConfirmPassword = "12345"
}), "Reject short new password");
var passwordActions = typeof(AccountController).GetMethods()
    .Where(m => m.Name == "ChangePassword").ToArray();
Check(passwordActions.Any(m => m.IsDefined(typeof(HttpGetAttribute), true))
    && passwordActions.Any(m => m.IsDefined(typeof(HttpPostAttribute), true)
        && m.IsDefined(typeof(ValidateAntiForgeryTokenAttribute), true))
    && passwordActions.All(m => m.GetCustomAttributes<AuthorizeAttribute>(true).Any()),
    "Self-service password change requires authentication and CSRF on POST");
// Chỉ dựng EF model và dịch LINQ thành SQL; không mở kết nối database.
using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseSqlServer("Server=localhost;Database=ModelOnly;Integrated Security=True;TrustServerCertificate=True").Options);
Check(db.Model.FindEntityType(typeof(Leave))!.FindProperty("EmployeeID1") == null
    && db.Model.FindEntityType(typeof(Overtime))!.FindProperty("ApproverEmployeeID") == null, "No unintended shadow foreign keys");
foreach (var type in new[] { typeof(Leave), typeof(Overtime) })
{
    var model = db.Model.FindEntityType(type)!;
    Check(model.FindProperty("Status")!.IsConcurrencyToken, type.Name + " protects status against concurrent updates");
    Check(model.GetForeignKeys().Any(f => f.Properties.Single().Name == "ApprovedBy" && f.DeleteBehavior == DeleteBehavior.Restrict), type.Name + " approver references Employee");
}
var profileModel = db.Model.FindEntityType(typeof(ProfileChangeRequest))!;
Check(profileModel.FindProperty("Status")!.IsConcurrencyToken, "Profile request protects status against concurrent updates");
Check(profileModel.GetForeignKeys().Count() == 2
    && profileModel.GetForeignKeys().All(f => f.DeleteBehavior == DeleteBehavior.Restrict),
    "Profile request references employee/reviewer without cascade deletion");
var pendingProfileIndex = profileModel.GetIndexes()
    .Single(i => i.Properties.Single().Name == "EmployeeID");
Check(pendingProfileIndex.IsUnique && pendingProfileIndex.GetFilter() == "[Status] = 'Pending'",
    "At most one pending profile request per employee");
Check(new ProfileChangeRequest().Status == "Pending", "New profile request starts pending");
var sql = db.Leaves.AsNoTracking().Where(x => x.EmployeeID == 3 && x.Status == "Pending")
    .OrderByDescending(x => x.StartDate).ThenByDescending(x => x.LeaveID)
    .Include(x => x.Employee).Skip(5).Take(5).ToQueryString();
Check(sql.Contains("OFFSET") && sql.Contains("FETCH NEXT") && sql.Contains("WHERE"), "SQL Server translates filter, sort and paging");
var day = new DateTime(2026, 10, 14);
Check(AttendanceRules.CalculateHours(day.AddHours(8), day.AddHours(17)) == 8m, "Full shift subtracts lunch once");
Check(AttendanceRules.CalculateHours(day.AddHours(8), day.AddHours(11)) == 3m, "Morning shift does not subtract lunch");
Check(AttendanceRules.CalculateHours(day.AddHours(13), day.AddHours(17)) == 4m, "Afternoon shift does not subtract lunch");
Check(AttendanceRules.CalculateHours(day.AddHours(11.5), day.AddHours(12.5)) == 0.5m, "Subtract only overlapping lunch minutes");
Check(AttendanceRules.CalculateHours(day.AddHours(12.25), day.AddHours(12.75)) == 0m, "Lunch-only interval has zero work hours");
Check(AttendanceRules.CalculateHours(day.AddHours(18), day.AddHours(20.5)) == 2.5m, "Fractional work hours remain accurate");
var invalidTimes = false;
try { AttendanceRules.CalculateHours(day.AddHours(17), day.AddHours(8)); } catch (ArgumentException) { invalidTimes = true; }
Check(invalidTimes, "Reject checkout before check-in");
var overnight = false;
try { AttendanceRules.CalculateHours(day.AddHours(22), day.AddDays(1).AddHours(2)); } catch (ArgumentException) { overnight = true; }
Check(overnight, "Reject unsupported overnight shifts");
Check(AttendanceRules.WorkStatus(day.AddHours(8), day.AddHours(17)) == "Present", "On-time attendance");
Check(AttendanceRules.WorkStatus(day.AddHours(9), day.AddHours(17)) == "Late", "Late attendance");
Check(AttendanceRules.WorkStatus(day.AddHours(8), day.AddHours(16)) == "EarlyLeave", "Early checkout");
Check(AttendanceRules.WorkStatus(day.AddHours(9), day.AddHours(16)) == "LateEarly", "Late and early checkout");
Check(AttendanceRules.LocalNow(new FixedClock(new DateTimeOffset(2026, 10, 13, 18, 0, 0, TimeSpan.Zero))) == day.AddHours(1), "Vietnam date crosses midnight correctly");
var attendanceModel = db.Model.FindEntityType(typeof(Attendance))!;
Check(attendanceModel.GetIndexes().Any(i => i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual(new[] { "EmployeeID", "WorkDate" })), "One attendance record per employee/day");
Check(attendanceModel.FindProperty("RowVersion") == null, "Attendance uses review stages instead of removed RowVersion");
Check(attendanceModel.GetForeignKeys().Any(f => f.Properties.Single().Name == "ReviewedBy" && f.DeleteBehavior == DeleteBehavior.Restrict), "Reviewer references employee without cascade deletion");
Check(new Attendance().ApprovalStatus == "Pending", "New attendance requires review");
foreach (var name in new[] { "CheckIn", "CheckOut", "Approve", "Reject" })
{
    var method = typeof(AttendanceController).GetMethod(name)!;
    var roles = name is "CheckIn" or "CheckOut" ? "Employee" : "HR,Admin";
    Check(method.GetCustomAttributes<AuthorizeAttribute>().Any(a => a.Roles == roles)
        && method.IsDefined(typeof(HttpPostAttribute)) && method.IsDefined(typeof(ValidateAntiForgeryTokenAttribute)), "Attendance." + name + " checks role/POST/CSRF");
}
Console.WriteLine($"Completed {checks} checks. Database CRUD, seed idempotency and browser flows still require SQL Server verification.");

sealed class FixedClock(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
