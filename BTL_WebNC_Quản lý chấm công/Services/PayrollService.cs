using BTL_WebNC_Quản_lý_chấm_công.Data;
using Microsoft.EntityFrameworkCore;

namespace BTL_WebNC_Quản_lý_chấm_công.Services;

// Tổng hợp công của một nhân viên trong một tháng (FR-08).
public class MonthSummary
{
    public int EmployeeID { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int? DepartmentID { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public decimal BaseSalary { get; set; }
    public int StandardDays { get; set; }
    public int AttendanceDays { get; set; }
    public int PaidLeaveDays { get; set; }
    public int UnpaidLeaveDays { get; set; }
    public decimal WorkingHours { get; set; }
    public decimal OvertimeHours { get; set; }
    public int LateCount { get; set; }
    public int EarlyCount { get; set; }
    public int AbsentDays { get; set; }
    public int PendingDays { get; set; }
    public decimal WorkingDays => AttendanceDays + PaidLeaveDays;
}

public class PayrollService(ApplicationDbContext context, TimeProvider clock)
{
    public async Task<List<MonthSummary>> BuildAsync(int year, int month, int? departmentId = null,
        string? search = null, int? employeeId = null, CancellationToken ct = default)
    {
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);
        var today = AttendanceRules.LocalNow(clock).Date;
        var lastElapsed = today < start ? start.AddDays(-1) : today >= end ? end.AddDays(-1) : today;
        var standardDays = PayrollRules.StandardDays(year, month);

        var query = context.Employees.AsNoTracking().Include(e => e.Department)
            .Where(e => e.Status == "Active" && e.HireDate < end);
        if (departmentId.HasValue) query = query.Where(e => e.DepartmentID == departmentId.Value);
        if (employeeId.HasValue) query = query.Where(e => e.EmployeeID == employeeId.Value);
        search = search?.Trim();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(e => e.EmployeeCode.Contains(search) || e.FullName.Contains(search));
        var employees = await query.OrderBy(e => e.EmployeeCode).ToListAsync(ct);
        if (employees.Count == 0) return new List<MonthSummary>();

        var ids = employees.Select(e => e.EmployeeID).ToList();
        var attendances = (await context.Attendances.AsNoTracking()
            .Where(a => ids.Contains(a.EmployeeID) && a.WorkDate >= start && a.WorkDate < end)
            .ToListAsync(ct)).ToLookup(a => a.EmployeeID);
        var leaves = (await context.Leaves.AsNoTracking()
            .Where(l => ids.Contains(l.EmployeeID) && l.Status == "Approved" && l.StartDate < end && l.EndDate >= start)
            .ToListAsync(ct)).ToLookup(l => l.EmployeeID);
        var overtimes = (await context.OverTimes.AsNoTracking()
            .Where(o => ids.Contains(o.EmployeeID) && o.Status == "Approved" && o.WorkDate >= start && o.WorkDate < end)
            .ToListAsync(ct)).ToLookup(o => o.EmployeeID);

        var result = new List<MonthSummary>(employees.Count);
        foreach (var e in employees)
        {
            var approved = attendances[e.EmployeeID]
                .Where(a => a.ApprovalStatus == "Approved" && a.CheckIn != null && a.CheckOut != null).ToList();
            var attended = approved.Select(a => a.WorkDate.Date).ToHashSet();
            var pending = attendances[e.EmployeeID].Where(a => a.ApprovalStatus == "Pending")
                .Select(a => a.WorkDate.Date).ToHashSet();

            // Ngày nghỉ được duyệt (chỉ tính ngày thường, bỏ ngày đã có công).
            var paidLeave = new HashSet<DateTime>();
            var unpaidLeave = new HashSet<DateTime>();
            foreach (var leave in leaves[e.EmployeeID])
            {
                var from = leave.StartDate.Date > start ? leave.StartDate.Date : start;
                var to = leave.EndDate.Date < end.AddDays(-1) ? leave.EndDate.Date : end.AddDays(-1);
                for (var d = from; d <= to; d = d.AddDays(1))
                {
                    if (!PayrollRules.IsWeekday(d) || attended.Contains(d)) continue;
                    if (PayrollRules.IsPaidLeave(leave.LeaveType)) paidLeave.Add(d);
                    else unpaidLeave.Add(d);
                }
            }
            unpaidLeave.ExceptWith(paidLeave);

            var late = 0;
            var early = 0;
            foreach (var a in approved)
            {
                var (l, ea) = PayrollRules.LateEarly(a.Status);
                late += l;
                early += ea;
            }

            var absent = 0;
            for (var d = start; d <= lastElapsed; d = d.AddDays(1))
            {
                if (!PayrollRules.IsWeekday(d)) continue;
                if (attended.Contains(d) || paidLeave.Contains(d) || unpaidLeave.Contains(d) || pending.Contains(d)) continue;
                absent++;
            }

            result.Add(new MonthSummary
            {
                EmployeeID = e.EmployeeID, EmployeeCode = e.EmployeeCode, FullName = e.FullName,
                DepartmentID = e.DepartmentID, DepartmentName = e.Department?.DepartmentName ?? "Chưa có",
                BaseSalary = e.Salary, StandardDays = standardDays,
                AttendanceDays = attended.Count, PaidLeaveDays = paidLeave.Count, UnpaidLeaveDays = unpaidLeave.Count,
                WorkingHours = approved.Sum(a => a.TotalHours), OvertimeHours = overtimes[e.EmployeeID].Sum(o => o.TotalHours),
                LateCount = late, EarlyCount = early, AbsentDays = absent, PendingDays = pending.Count
            });
        }
        return result;
    }
}
