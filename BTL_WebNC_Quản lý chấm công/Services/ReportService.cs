using System.Globalization;
using BTL_WebNC_Quản_lý_chấm_công.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BTL_WebNC_Quản_lý_chấm_công.Services;

public class ReportKpi
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class ReportResult
{
    public string Title { get; set; } = string.Empty;
    public List<string> Columns { get; set; } = new();
    public List<List<string>> Rows { get; set; } = new();
    public List<ReportKpi> Kpis { get; set; } = new();
    public string ChartTitle { get; set; } = string.Empty;
    public List<string> ChartLabels { get; set; } = new();
    public List<decimal> ChartValues { get; set; } = new();
    public string? Note { get; set; }
}

// FR-11 (báo cáo/thống kê) + FR-13 (Caching). Kết quả được cache ngắn hạn;
// khi bảng lương thay đổi, Invalidate() làm mọi cache cũ mất hiệu lực.
public class ReportService(ApplicationDbContext context, PayrollService payroll, IMemoryCache cache)
{
    public static readonly string[] Types = { "attendance", "overtime", "leave", "payroll" };
    private static readonly CultureInfo Vi = new("vi-VN");
    private static int _version;
    public static void Invalidate() => Interlocked.Increment(ref _version);

    private static string Money(decimal v) => v.ToString("N0", Vi);
    private static string Num(decimal v) => v.ToString("0.##", Vi);

    public async Task<IReadOnlyList<(int Id, string Name)>> GetDepartmentsAsync()
    {
        // Danh mục ít thay đổi nên cache 5 phút.
        return (await cache.GetOrCreateAsync("report:departments", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            return (IReadOnlyList<(int, string)>)(await context.Departments.AsNoTracking()
                .OrderBy(d => d.DepartmentName).Select(d => new { d.DepartmentID, d.DepartmentName }).ToListAsync())
                .Select(d => (d.DepartmentID, d.DepartmentName)).ToList();
        }))!;
    }

    public async Task<ReportResult> GetAsync(string type, int year, int month, int? departmentId)
    {
        var key = $"report:{_version}:{type}:{year}:{month}:{departmentId}";
        return (await cache.GetOrCreateAsync(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60);
            return BuildAsync(type, year, month, departmentId);
        }))!;
    }

    private static void FillChart(ReportResult r, string title, IEnumerable<(string Dept, decimal Value)> data)
    {
        r.ChartTitle = title;
        foreach (var g in data.GroupBy(x => x.Dept).OrderBy(g => g.Key))
        {
            r.ChartLabels.Add(g.Key);
            r.ChartValues.Add(g.Sum(x => x.Value));
        }
    }

    private async Task<ReportResult> BuildAsync(string type, int year, int month, int? departmentId)
    {
        var r = new ReportResult();
        var period = $"{month:00}/{year}";
        if (type == "payroll")
        {
            r.Title = $"Báo cáo công lương tháng {period}";
            var q = context.Payrolls.AsNoTracking().Include(p => p.Employee).ThenInclude(e => e!.Department)
                .Where(p => p.Month == (byte)month && p.Year == (short)year);
            if (departmentId.HasValue) q = q.Where(p => p.Employee!.DepartmentID == departmentId.Value);
            var items = await q.OrderBy(p => p.Employee!.EmployeeCode).ToListAsync();
            r.Columns = new() { "Mã NV", "Họ tên", "Phòng ban", "Lương cơ bản", "Ngày công", "Giờ tăng ca", "Phụ cấp", "Khấu trừ", "Thực nhận", "Trạng thái" };
            r.Rows = items.Select(p => new List<string>
            {
                p.Employee!.EmployeeCode, p.Employee.FullName, p.Employee.Department?.DepartmentName ?? "Chưa có",
                Money(p.BaseSalary), Num(p.WorkingDays), Num(p.OvertimeHours), Money(p.Allowance), Money(p.Deduction),
                Money(p.TotalSalary), StatusLabel(p.Status)
            }).ToList();
            r.Kpis = new()
            {
                new() { Label = "Số bảng lương", Value = items.Count.ToString() },
                new() { Label = "Tổng thực nhận (VNĐ)", Value = Money(items.Sum(p => p.TotalSalary)) },
                new() { Label = "Tổng khấu trừ (VNĐ)", Value = Money(items.Sum(p => p.Deduction)) },
                new() { Label = "Tổng giờ tăng ca", Value = Num(items.Sum(p => p.OvertimeHours)) }
            };
            FillChart(r, "Tổng thực nhận theo phòng ban (VNĐ)",
                items.Select(p => (p.Employee!.Department?.DepartmentName ?? "Chưa có", p.TotalSalary)));
            if (items.Count == 0) r.Note = "Chưa lập bảng lương cho tháng này. Vào mục Bảng lương để tính lương.";
            return r;
        }

        var rows = await payroll.BuildAsync(year, month, departmentId);
        switch (type)
        {
            case "attendance":
                r.Title = $"Báo cáo chấm công tháng {period}";
                r.Columns = new() { "Mã NV", "Họ tên", "Phòng ban", "Ngày công", "Giờ làm", "Đi muộn", "Về sớm", "Vắng", "Chờ xác nhận" };
                r.Rows = rows.Select(s => new List<string>
                {
                    s.EmployeeCode, s.FullName, s.DepartmentName, Num(s.WorkingDays), Num(s.WorkingHours),
                    s.LateCount.ToString(), s.EarlyCount.ToString(), s.AbsentDays.ToString(), s.PendingDays.ToString()
                }).ToList();
                r.Kpis = new()
                {
                    new() { Label = "Nhân viên", Value = rows.Count.ToString() },
                    new() { Label = "Tổng ngày công", Value = Num(rows.Sum(s => s.WorkingDays)) },
                    new() { Label = "Tổng giờ làm", Value = Num(rows.Sum(s => s.WorkingHours)) },
                    new() { Label = "Lần đi muộn/về sớm", Value = rows.Sum(s => s.LateCount + s.EarlyCount).ToString() }
                };
                FillChart(r, "Số lần đi muộn/về sớm theo phòng ban", rows.Select(s => (s.DepartmentName, (decimal)(s.LateCount + s.EarlyCount))));
                r.Note = "Chỉ tính các bản chấm công đã được HR xác nhận. Cột \"Chờ xác nhận\" là số ngày chưa được duyệt.";
                break;
            case "overtime":
                r.Title = $"Báo cáo tăng ca tháng {period}";
                r.Columns = new() { "Mã NV", "Họ tên", "Phòng ban", "Giờ tăng ca được duyệt" };
                r.Rows = rows.Where(s => s.OvertimeHours > 0).Select(s => new List<string>
                    { s.EmployeeCode, s.FullName, s.DepartmentName, Num(s.OvertimeHours) }).ToList();
                r.Kpis = new()
                {
                    new() { Label = "Nhân viên có tăng ca", Value = rows.Count(s => s.OvertimeHours > 0).ToString() },
                    new() { Label = "Tổng giờ tăng ca", Value = Num(rows.Sum(s => s.OvertimeHours)) }
                };
                FillChart(r, "Giờ tăng ca theo phòng ban", rows.Select(s => (s.DepartmentName, s.OvertimeHours)));
                break;
            case "leave":
                r.Title = $"Báo cáo nghỉ phép tháng {period}";
                r.Columns = new() { "Mã NV", "Họ tên", "Phòng ban", "Nghỉ có lương (ngày)", "Nghỉ không lương (ngày)" };
                r.Rows = rows.Where(s => s.PaidLeaveDays + s.UnpaidLeaveDays > 0).Select(s => new List<string>
                    { s.EmployeeCode, s.FullName, s.DepartmentName, s.PaidLeaveDays.ToString(), s.UnpaidLeaveDays.ToString() }).ToList();
                r.Kpis = new()
                {
                    new() { Label = "Nhân viên có nghỉ", Value = rows.Count(s => s.PaidLeaveDays + s.UnpaidLeaveDays > 0).ToString() },
                    new() { Label = "Ngày nghỉ có lương", Value = rows.Sum(s => s.PaidLeaveDays).ToString() },
                    new() { Label = "Ngày nghỉ không lương", Value = rows.Sum(s => s.UnpaidLeaveDays).ToString() }
                };
                FillChart(r, "Số ngày nghỉ theo phòng ban", rows.Select(s => (s.DepartmentName, (decimal)(s.PaidLeaveDays + s.UnpaidLeaveDays))));
                r.Note = "Chỉ tính đơn nghỉ đã duyệt, bỏ cuối tuần và ngày đã có công.";
                break;
            default:
                throw new ArgumentException("Loại báo cáo không hợp lệ.", nameof(type));
        }
        return r;
    }

    public static string StatusLabel(string status) => status switch
    {
        "Draft" => "Nháp", "Approved" => "Đã xác nhận", "Paid" => "Đã chi trả", _ => status
    };
}
