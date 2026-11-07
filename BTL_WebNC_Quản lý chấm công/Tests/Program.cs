using BTL_WebNC_Quản_lý_chấm_công.Data;
using BTL_WebNC_Quản_lý_chấm_công.Models;
using BTL_WebNC_Quản_lý_chấm_công.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var checks = 0;
void Equal<T>(T expected, T actual, string name)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"{name}: expected {expected}, got {actual}");
    checks++;
}
var date = new DateTime(2026, 10, 5); // Monday
Equal("Late", AttendanceRules.WorkStatus(date.AddHours(9).AddMinutes(59), date.AddHours(16)), "09:59 below absence threshold");
Equal("Absent", AttendanceRules.WorkStatus(date.AddHours(10), date.AddHours(16)), "10:00 inclusive absence threshold");
Equal("Absent", AttendanceRules.WorkStatus(date.AddHours(11), date.AddHours(19)), "Staying late does not cancel absence");
Equal("Present", AttendanceRules.WorkStatus(date.AddHours(8), date.AddHours(16)), "16:00 is normal checkout");
Equal("EarlyLeave", AttendanceRules.WorkStatus(date.AddHours(8), date.AddHours(15).AddMinutes(59)), "15:59 is early checkout");
Equal(7m, AttendanceRules.PayableHours(date.AddHours(8), date.AddHours(16)), "Seven paid hours after lunch");
Equal(7m, AttendanceRules.PayableHours(date.AddHours(7), date.AddHours(19)), "Clamp ordinary pay to shift");
Equal(6.5m, AttendanceRules.PayableHours(date.AddHours(8).AddMinutes(30), date.AddHours(16)), "Thirty minutes late");
Equal(0m, AttendanceRules.PayableHours(date.AddHours(10), date.AddHours(19)), "Absent gets zero ordinary pay");

using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().Options);
var clock = new FixedClock();
var employee = new Employee { EmployeeID = 1, Salary = 15_400_000m, HireDate = new DateTime(2020, 1, 1) };
var calculator = new PayrollCalculator(context, Options.Create(new PayrollOptions()), clock);
Attendance Record(DateTime day, double from = 8, double to = 16, string status = "Approved") => new()
{
    EmployeeID = 1, WorkDate = day, CheckIn = day.AddHours(from), CheckOut = day.AddHours(to), ApprovalStatus = status
};
Payroll Run(List<Attendance> attendance, List<Leave>? leaves = null, List<Overtime>? overtime = null)
{
    var payroll = new Payroll { EmployeeID = 1, Year = 2026, Month = 10 };
    calculator.Calculate(payroll, employee, attendance, leaves ?? [], overtime ?? []);
    return payroll;
}
var weekdays = Enumerable.Range(0, 31).Select(i => new DateTime(2026, 10, 1).AddDays(i))
    .Where(d => d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday).ToList();
var full = weekdays.Select(d => Record(d)).ToList();
var normal = Run(full);
Equal(22, normal.StandardDays, "October calendar");
Equal(15_400_000m, normal.TotalSalary, "Full month salary");
Equal(154m, normal.WorkingHours, "Full month hours");
Equal(0m, normal.Deduction, "No full month deduction");

var absent = Run(full.Where(a => a.WorkDate != date).ToList());
Equal(700_000m, absent.Deduction, "Missing day deducts exactly one day");
Equal(14_700_000m, absent.TotalSalary, "Missing day net");
var lateRecords = full.Select(a => a.WorkDate == date ? Record(date, 10, 19) : a).ToList();
Equal(700_000m, Run(lateRecords).Deduction, "Two hours late deducts one day only");
Equal(50_000m, Run(full.Select(a => a.WorkDate == date ? Record(date, 8.5) : a).ToList()).Deduction, "Thirty minute deduction");

var leave = new Leave { EmployeeID = 1, StartDate = date, EndDate = date, Status = "Approved", LeaveType = "Annual" };
Equal(15_400_000m, Run(full.Where(a => a.WorkDate != date).ToList(), [leave]).TotalSalary, "Annual leave paid");
leave.LeaveType = "Unpaid";
Equal(700_000m, Run(full.Where(a => a.WorkDate != date).ToList(), [leave]).Deduction, "Unpaid leave");
leave.LeaveType = "Annual";
Equal(true, PayrollCalculator.Details(Run(full, [leave])).Single(d => d.Date == date).NeedsReview, "Leave attendance conflict blocks approval");
var pending = Run(full.Select(a => a.WorkDate == date ? Record(date, status: "Pending") : a).ToList());
Equal(true, PayrollCalculator.Details(pending).Single(d => d.Date == date).NeedsReview, "Pending attendance needs review");
var incomplete = Record(date); incomplete.CheckOut = null;
Equal(true, PayrollCalculator.Details(Run(full.Where(a => a.WorkDate != date).Append(incomplete).ToList())).Single(d => d.Date == date).NeedsReview, "Missing checkout needs review");

var ot = new Overtime { EmployeeID = 1, WorkDate = date, StartTime = TimeSpan.FromHours(16), EndTime = TimeSpan.FromHours(18), Status = "Approved", TotalHours = 99 };
var withOt = full.Select(a => a.WorkDate == date ? Record(date, 8, 19) : a).ToList();
var earned = Run(withOt, overtime: [ot]);
Equal(2m, earned.OvertimeHours, "Approved overtime uses interval rather than stored total");
Equal(300_000m, earned.OvertimePay, "Overtime at configured 1.5 multiplier");
Equal(15_700_000m, earned.TotalSalary, "Overtime net");
ot.StartTime = TimeSpan.FromHours(15);
Equal(2m, Run(withOt, overtime: [ot]).OvertimeHours, "No double payment of ordinary hour");
ot.EndTime = TimeSpan.FromHours(20);
Equal(3m, Run(withOt, overtime: [ot]).OvertimeHours, "Overtime limited by actual checkout");
Equal(true, PayrollCalculator.Details(Run(withOt, overtime: [ot, ot])).Single(d => d.Date == date).NeedsReview, "Overlapping overtime blocks approval");
ot.Status = "Pending";
Equal(true, PayrollCalculator.Details(Run(withOt, overtime: [ot])).Single(d => d.Date == date).NeedsReview, "Pending overtime blocks approval");
ot.Status = "Rejected";
Equal(0m, Run(withOt, overtime: [ot]).OvertimeHours, "Rejected overtime not paid");

var futureRejected = false;
try { calculator.Calculate(new Payroll { Month = 11, Year = 2026 }, employee, [], [], []); }
catch (InvalidOperationException) { futureRejected = true; }
Equal(true, futureRejected, "Unfinished month cannot be calculated");
Console.WriteLine($"PASS: {checks} payroll and attendance checks.");
if (Environment.GetEnvironmentVariable("PAYROLL_TEST_CONNECTION") is { Length: > 0 } connection)
    await IntegrationChecks.Run(connection, Environment.GetEnvironmentVariable("PAYROLL_TEST_URL") ?? "http://127.0.0.1:5286");

sealed class FixedClock : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => new(2026, 11, 7, 0, 0, 0, TimeSpan.Zero);
}
