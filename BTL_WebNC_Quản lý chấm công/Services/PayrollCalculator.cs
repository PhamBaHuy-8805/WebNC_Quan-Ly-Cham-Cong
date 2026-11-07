using System.Text.Json;
using BTL_WebNC_Quản_lý_chấm_công.Data;
using BTL_WebNC_Quản_lý_chấm_công.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BTL_WebNC_Quản_lý_chấm_công.Services;

public class PayrollOptions
{
    public decimal OvertimeMultiplier { get; set; } = 1.5m;
    public DayOfWeek[] WorkingDays { get; set; } = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday];
    public DateTime[] Holidays { get; set; } = [];
    public string[] PaidLeaveTypes { get; set; } = ["Annual"];
}

public record PayrollDay(DateTime Date, string Description, decimal PaidHours, decimal OvertimeHours, bool NeedsReview);

public class PayrollCalculator(ApplicationDbContext context, IOptions<PayrollOptions> options, TimeProvider clock)
{
    public async Task CalculateAsync(Payroll payroll, Employee employee)
    {
        var start = new DateTime(payroll.Year, payroll.Month, 1);
        var end = start.AddMonths(1);
        var attendance = await context.Attendances.AsNoTracking().Where(a => a.EmployeeID == employee.EmployeeID && a.WorkDate >= start && a.WorkDate < end).ToListAsync();
        var leaves = await context.Leaves.AsNoTracking().Where(l => l.EmployeeID == employee.EmployeeID && l.StartDate < end && l.EndDate >= start && l.Status != "Rejected").ToListAsync();
        var overtime = await context.OverTimes.AsNoTracking().Where(o => o.EmployeeID == employee.EmployeeID && o.WorkDate >= start && o.WorkDate < end && o.Status != "Rejected").ToListAsync();
        Calculate(payroll, employee, attendance, leaves, overtime);
    }

    public void Calculate(Payroll payroll, Employee employee, IReadOnlyList<Attendance> attendance,
        IReadOnlyList<Leave> leaves, IReadOnlyList<Overtime> overtime)
    {
        var policy = options.Value;
        if (policy.OvertimeMultiplier < 0 || policy.OvertimeMultiplier > 10 || policy.WorkingDays.Length == 0)
            throw new InvalidOperationException("Cấu hình tính lương không hợp lệ.");
        if (employee.Salary < 0) throw new InvalidOperationException("Lương cơ bản không được âm.");
        var start = new DateTime(payroll.Year, payroll.Month, 1);
        var end = start.AddMonths(1);
        if (end > AttendanceRules.LocalNow(clock).Date)
            throw new InvalidOperationException("Chỉ lập bảng lương cho tháng đã kết thúc.");

        bool IsWorkingDay(DateTime date) => policy.WorkingDays.Contains(date.DayOfWeek)
            && !policy.Holidays.Any(h => h.Date == date);
        var dates = Enumerable.Range(0, (end - start).Days).Select(day => start.AddDays(day)).ToArray();
        var standardDays = dates.Count(IsWorkingDay);
        if (standardDays == 0) throw new InvalidOperationException("Tháng không có ngày làm việc chuẩn.");
        var details = new List<PayrollDay>();
        foreach (var date in dates)
        {
            if (date < employee.HireDate.Date)
            {
                if (IsWorkingDay(date)) details.Add(new(date, "Chưa vào làm — không hưởng lương", 0, 0, false));
                continue;
            }
            var record = attendance.SingleOrDefault(a => a.WorkDate.Date == date);
            var approvedLeaves = leaves.Where(l => l.Status == "Approved" && l.StartDate.Date <= date && l.EndDate.Date >= date).ToList();
            var pendingLeave = leaves.Any(l => l.Status == "Pending" && l.StartDate.Date <= date && l.EndDate.Date >= date);
            var issues = new List<string>();
            decimal paidHours = 0, overtimeHours = 0;
            var description = IsWorkingDay(date) ? "Không chấm công — vắng 1 ngày; HR cần xác nhận khi duyệt lương" : "Ngày nghỉ";
            var complete = record?.CheckIn != null && record.CheckOut != null
                && record.CheckIn.Value.Date == date && record.CheckOut.Value.Date == date && record.CheckOut > record.CheckIn;
            if (IsWorkingDay(date))
            {
                if (pendingLeave) issues.Add("Đơn nghỉ còn chờ duyệt");
                if (approvedLeaves.Count > 1) issues.Add("Đơn nghỉ được duyệt trùng ngày");
                if (approvedLeaves.Count > 0)
                {
                    var leave = approvedLeaves[0];
                    paidHours = policy.PaidLeaveTypes.Contains(leave.LeaveType) ? AttendanceRules.StandardHours : 0;
                    description = paidHours > 0 ? "Nghỉ có lương đã duyệt" : "Nghỉ không hưởng lương đã duyệt";
                    if (record?.CheckIn != null) issues.Add("Chấm công trùng ngày nghỉ được duyệt");
                }
                else if (record != null)
                {
                    if (!complete) issues.Add("Thiếu hoặc sai giờ vào/ra");
                    if (record.ApprovalStatus != "Approved") issues.Add("Chấm công chưa được xác nhận hoặc đã bị từ chối");
                    if (complete && record.ApprovalStatus == "Approved")
                    {
                        paidHours = AttendanceRules.PayableHours(record.CheckIn!.Value, record.CheckOut!.Value);
                        description = AttendanceRules.WorkStatus(record.CheckIn.Value, record.CheckOut.Value) == "Absent"
                            ? "Muộn từ 120 phút — vắng cả ngày" : paidHours < AttendanceRules.StandardHours
                                ? "Đi làm — thiếu thời gian trong ca" : "Đi làm đủ ca";
                    }
                }
            }
            var registrations = overtime.Where(o => o.WorkDate.Date == date).OrderBy(o => o.StartTime).ToList();
            TimeSpan? previousEnd = null;
            foreach (var ot in registrations)
            {
                if (ot.Status == "Pending") { issues.Add("Tăng ca còn chờ duyệt"); continue; }
                if (ot.Status != "Approved") continue;
                if (ot.StartTime < TimeSpan.Zero || ot.EndTime >= TimeSpan.FromDays(1) || ot.EndTime <= ot.StartTime)
                { issues.Add("Giờ tăng ca không hợp lệ"); continue; }
                if (previousEnd.HasValue && ot.StartTime < previousEnd.Value) issues.Add("Đơn tăng ca trùng giờ");
                previousEnd = previousEnd.HasValue && previousEnd.Value > ot.EndTime ? previousEnd : ot.EndTime;
                if (!complete || record!.ApprovalStatus != "Approved")
                { issues.Add("Tăng ca cần chấm công đầy đủ đã xác nhận"); continue; }
                var from = date.Add(ot.StartTime) > record.CheckIn!.Value ? date.Add(ot.StartTime) : record.CheckIn.Value;
                var to = date.Add(ot.EndTime) < record.CheckOut!.Value ? date.Add(ot.EndTime) : record.CheckOut.Value;
                if (to <= from) continue;
                if (!IsWorkingDay(date)) overtimeHours += AttendanceRules.CalculateHours(from, to);
                else
                {
                    // Chỉ trả tăng ca ngoài ca chuẩn, không trả hai lần giờ làm thường.
                    var beforeEnd = to < date.AddHours(AttendanceRules.StartHour) ? to : date.AddHours(AttendanceRules.StartHour);
                    if (beforeEnd > from) overtimeHours += AttendanceRules.CalculateHours(from, beforeEnd);
                    var afterStart = from > date.AddHours(AttendanceRules.EndHour) ? from : date.AddHours(AttendanceRules.EndHour);
                    if (to > afterStart) overtimeHours += AttendanceRules.CalculateHours(afterStart, to);
                }
            }
            if (issues.Count > 0) description += "; " + string.Join("; ", issues.Distinct());
            details.Add(new(date, description, paidHours, overtimeHours, issues.Count > 0));
        }
        payroll.BaseSalary = employee.Salary;
        payroll.StandardDays = standardDays;
        payroll.WorkingHours = details.Sum(d => d.PaidHours);
        payroll.WorkingDays = decimal.Round(payroll.WorkingHours / AttendanceRules.StandardHours, 2);
        payroll.OvertimeHours = details.Sum(d => d.OvertimeHours);
        payroll.OvertimeMultiplier = policy.OvertimeMultiplier;
        var hourlyRate = payroll.BaseSalary / standardDays / AttendanceRules.StandardHours;
        payroll.Deduction = decimal.Round(payroll.BaseSalary - payroll.WorkingHours * hourlyRate, 2);
        payroll.OvertimePay = decimal.Round(payroll.OvertimeHours * hourlyRate * policy.OvertimeMultiplier, 2);
        payroll.TotalSalary = payroll.BaseSalary - payroll.Deduction + payroll.OvertimePay + payroll.Allowance;
        payroll.CalculationDetails = JsonSerializer.Serialize(details);
        payroll.CalculatedAt = AttendanceRules.LocalNow(clock);
    }

    public static List<PayrollDay> Details(Payroll payroll) => string.IsNullOrWhiteSpace(payroll.CalculationDetails)
        ? [] : JsonSerializer.Deserialize<List<PayrollDay>>(payroll.CalculationDetails) ?? [];
}
