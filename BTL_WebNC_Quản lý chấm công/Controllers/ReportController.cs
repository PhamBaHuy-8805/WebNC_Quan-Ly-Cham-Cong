using BTL_WebNC_Quản_lý_chấm_công.Data;
using BTL_WebNC_Quản_lý_chấm_công.Helpers;
using BTL_WebNC_Quản_lý_chấm_công.Services;
using BTL_WebNC_Quản_lý_chấm_công.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BTL_WebNC_Quản_lý_chấm_công.Controllers;

// Module 4: báo cáo và thống kê (FR-11). Chỉ HR/Admin được xem.
[Authorize(Roles = "HR,Admin")]
public class ReportController(ApplicationDbContext context, ReportService reports, TimeProvider clock)
    : RequestControllerBase(context)
{
    private (string Type, int Month, int Year) Normalize(string? type, int? month, int? year)
    {
        var now = AttendanceRules.LocalNow(clock);
        return (ReportService.Types.Contains(type) ? type! : "attendance",
            Math.Clamp(month ?? now.Month, 1, 12), Math.Clamp(year ?? now.Year, 2000, 2100));
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? type, int? month, int? year, int? departmentId)
    {
        var (t, m, y) = Normalize(type, month, year);
        return View(new ReportPageViewModel
        {
            Type = t, Month = m, Year = y, DepartmentId = departmentId,
            Departments = await reports.GetDepartmentsAsync()
        });
    }

    // Trả JSON cho trang báo cáo (Fetch API).
    [HttpGet]
    public async Task<IActionResult> Data(string? type, int? month, int? year, int? departmentId)
    {
        var (t, m, y) = Normalize(type, month, year);
        return Json(await reports.GetAsync(t, y, m, departmentId));
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv(string? type, int? month, int? year, int? departmentId)
    {
        var (t, m, y) = Normalize(type, month, year);
        var result = await reports.GetAsync(t, y, m, departmentId);
        return File(CsvHelper.Build(result.Columns, result.Rows), "text/csv; charset=utf-8", $"bao-cao-{t}-{y}-{m:00}.csv");
    }
}
