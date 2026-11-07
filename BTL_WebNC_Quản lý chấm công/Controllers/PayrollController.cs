using BTL_WebNC_Quản_lý_chấm_công.Data;
using BTL_WebNC_Quản_lý_chấm_công.Helpers;
using BTL_WebNC_Quản_lý_chấm_công.Models;
using BTL_WebNC_Quản_lý_chấm_công.Services;
using BTL_WebNC_Quản_lý_chấm_công.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BTL_WebNC_Quản_lý_chấm_công.Controllers;

// Module 4: tổng hợp công (FR-08) và tính lương, lập bảng lương (FR-09).
// Nhân viên chỉ xem được phiếu lương của chính mình khi HR đã xác nhận.
public class PayrollController(ApplicationDbContext context, PayrollService payroll, ReportService reports,
    TimeProvider clock) : RequestControllerBase(context)
{
    private (int Month, int Year) Period(int? month, int? year)
    {
        var now = AttendanceRules.LocalNow(clock);
        return (Math.Clamp(month ?? now.Month, 1, 12), Math.Clamp(year ?? now.Year, 2000, 2100));
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? month, int? year, int? departmentId, string? search, string? status, int page = 1)
    {
        var (m, y) = Period(month, year);
        IQueryable<Payroll> query = Context.Payrolls.AsNoTracking();
        search = search?.Trim();
        if (IsReviewer)
        {
            query = query.Where(p => p.Month == (byte)m && p.Year == (short)y);
            if (departmentId.HasValue) query = query.Where(p => p.Employee!.DepartmentID == departmentId.Value);
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p => p.Employee!.EmployeeCode.Contains(search) || p.Employee.FullName.Contains(search));
            if (status is "Draft" or "Approved" or "Paid") query = query.Where(p => p.Status == status);
            else status = null;
        }
        else
        {
            query = query.Where(p => p.EmployeeID == CurrentEmployeeId && (p.Status == "Approved" || p.Status == "Paid"));
            departmentId = null; search = null; status = null;
        }

        var count = await query.CountAsync();
        const int pageSize = 10;
        var totalPages = Math.Max(1, (int)Math.Ceiling(count / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);
        var items = await query.Include(p => p.Employee).ThenInclude(e => e!.Department)
            .OrderByDescending(p => p.Year).ThenByDescending(p => p.Month).ThenBy(p => p.Employee!.EmployeeCode)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return View(new PayrollIndexViewModel
        {
            IsReviewer = IsReviewer, Month = m, Year = y, DepartmentId = departmentId, Search = search, Status = status,
            Items = items, Departments = IsReviewer ? await reports.GetDepartmentsAsync() : Array.Empty<(int, string)>(),
            Page = page, TotalPages = totalPages, TotalCount = count,
            TotalSalary = await query.SumAsync(p => (decimal?)p.TotalSalary) ?? 0m,
            DraftCount = IsReviewer ? await query.CountAsync(p => p.Status == "Draft") : 0
        });
    }

    // ---- Tổng hợp công (FR-08) ----
    [HttpGet, Authorize(Roles = "HR,Admin")]
    public async Task<IActionResult> Summary(int? month, int? year, int? departmentId, string? search)
    {
        var (m, y) = Period(month, year);
        return View(new PayrollSummaryViewModel
        {
            Month = m, Year = y, DepartmentId = departmentId, Search = search,
            Departments = await reports.GetDepartmentsAsync()
        });
    }

    // Trả JSON cho trang Summary (Fetch API, không tải lại trang).
    [HttpGet, Authorize(Roles = "HR,Admin")]
    public async Task<IActionResult> SummaryData(int month, int year, int? departmentId, string? search)
    {
        (month, year) = Period(month, year);
        var rows = await payroll.BuildAsync(year, month, departmentId, search);
        return Json(new
        {
            standardDays = PayrollRules.StandardDays(year, month),
            rows = rows.Select(s => new
            {
                s.EmployeeCode, s.FullName, s.DepartmentName, workingDays = s.WorkingDays, s.AttendanceDays,
                s.PaidLeaveDays, s.UnpaidLeaveDays, s.WorkingHours, s.OvertimeHours, s.LateCount, s.EarlyCount,
                s.AbsentDays, s.PendingDays
            })
        });
    }

    // ---- Tính lương ----
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "HR,Admin")]
    public async Task<IActionResult> Generate(int month, int year, bool recalculate = false)
    {
        if (month is < 1 or > 12 || year is < 2000 or > 2100) return BadRequest("Kỳ lương không hợp lệ.");
        var start = new DateTime(year, month, 1);
        if (start > AttendanceRules.LocalNow(clock).Date)
        {
            TempData["Error"] = "Chưa thể tính lương cho tháng chưa bắt đầu.";
            return RedirectToAction(nameof(Index), new { month, year });
        }

        var summaries = await payroll.BuildAsync(year, month);
        var existing = await Context.Payrolls.Where(p => p.Month == (byte)month && p.Year == (short)year).ToListAsync();
        int created = 0, updated = 0, skipped = 0, pending = summaries.Sum(s => s.PendingDays);
        foreach (var s in summaries)
        {
            var row = existing.FirstOrDefault(p => p.EmployeeID == s.EmployeeID);
            if (row != null && (row.Status != "Draft" || !recalculate)) { skipped++; continue; }
            if (row == null)
            {
                row = new Payroll { EmployeeID = s.EmployeeID, Month = (byte)month, Year = (short)year, Status = "Draft" };
                Context.Payrolls.Add(row);
                created++;
            }
            else updated++;
            row.BaseSalary = s.BaseSalary;
            row.WorkingDays = s.WorkingDays;
            row.WorkingHours = s.WorkingHours;
            row.OvertimeHours = s.OvertimeHours;
            row.Deduction = PayrollRules.AutoDeduction(s.LateCount, s.EarlyCount);
            row.TotalSalary = PayrollRules.Total(s.BaseSalary, s.StandardDays, s.WorkingDays, s.OvertimeHours, row.Allowance, row.Deduction);
        }
        await Context.SaveChangesAsync();
        ReportService.Invalidate();
        TempData["Success"] = $"Đã tạo {created}, tính lại {updated}, bỏ qua {skipped} bảng lương tháng {month:00}/{year}."
            + (pending > 0 ? $" Còn {pending} ngày chấm công chưa được xác nhận nên chưa tính vào công." : "");
        return RedirectToAction(nameof(Index), new { month, year });
    }

    [HttpGet, Authorize(Roles = "HR,Admin")]
    public async Task<IActionResult> Adjust(int id)
    {
        var p = await Context.Payrolls.AsNoTracking().Include(x => x.Employee).SingleOrDefaultAsync(x => x.PayrollID == id);
        if (p == null) return NotFound();
        if (p.Status != "Draft") return BackWithError("Chỉ điều chỉnh được bảng lương ở trạng thái Nháp.");
        return View(new PayrollAdjustViewModel
        {
            PayrollID = p.PayrollID, EmployeeName = $"{p.Employee!.EmployeeCode} - {p.Employee.FullName}",
            Period = $"{p.Month:00}/{p.Year}", Allowance = p.Allowance, Deduction = p.Deduction
        });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "HR,Admin")]
    public async Task<IActionResult> Adjust(int id, PayrollAdjustViewModel model)
    {
        var p = await Context.Payrolls.Include(x => x.Employee).SingleOrDefaultAsync(x => x.PayrollID == id);
        if (p == null) return NotFound();
        if (p.Status != "Draft") return BackWithError("Chỉ điều chỉnh được bảng lương ở trạng thái Nháp.");
        if (!ModelState.IsValid)
        {
            model.PayrollID = id;
            model.EmployeeName = $"{p.Employee!.EmployeeCode} - {p.Employee.FullName}";
            model.Period = $"{p.Month:00}/{p.Year}";
            return View(model);
        }
        p.Allowance = model.Allowance;
        p.Deduction = model.Deduction;
        p.TotalSalary = PayrollRules.Total(p.BaseSalary, PayrollRules.StandardDays(p.Year, p.Month),
            p.WorkingDays, p.OvertimeHours, p.Allowance, p.Deduction);
        await Context.SaveChangesAsync();
        ReportService.Invalidate();
        TempData["Success"] = "Đã cập nhật phụ cấp/khấu trừ và tính lại thực nhận.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // ---- Duyệt bảng lương: Draft -> Approved -> Paid ----
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "HR,Admin")]
    public async Task<IActionResult> Approve(int id)
    {
        var p = await Context.Payrolls.SingleOrDefaultAsync(x => x.PayrollID == id);
        if (p == null) return NotFound();
        if (p.EmployeeID == CurrentEmployeeId) return Forbid();
        if (p.Status != "Draft") return BackWithError("Bảng lương này không còn ở trạng thái Nháp.");
        p.Status = "Approved";
        await Context.SaveChangesAsync();
        ReportService.Invalidate();
        TempData["Success"] = "Đã xác nhận bảng lương.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "HR,Admin")]
    public async Task<IActionResult> MarkPaid(int id)
    {
        var p = await Context.Payrolls.SingleOrDefaultAsync(x => x.PayrollID == id);
        if (p == null) return NotFound();
        if (p.Status != "Approved") return BackWithError("Chỉ đánh dấu chi trả cho bảng lương đã xác nhận.");
        p.Status = "Paid";
        await Context.SaveChangesAsync();
        ReportService.Invalidate();
        TempData["Success"] = "Đã đánh dấu chi trả.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "HR,Admin")]
    public async Task<IActionResult> DeleteDraft(int id)
    {
        var p = await Context.Payrolls.SingleOrDefaultAsync(x => x.PayrollID == id);
        if (p == null) return NotFound();
        if (p.Status != "Draft") return BackWithError("Chỉ xóa được bảng lương ở trạng thái Nháp.");
        Context.Payrolls.Remove(p);
        await Context.SaveChangesAsync();
        ReportService.Invalidate();
        TempData["Success"] = "Đã xóa bảng lương nháp.";
        return RedirectToAction(nameof(Index), new { month = p.Month, year = p.Year });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var p = await Context.Payrolls.AsNoTracking().Include(x => x.Employee).ThenInclude(e => e!.Department)
            .Include(x => x.Employee).ThenInclude(e => e!.Position)
            .SingleOrDefaultAsync(x => x.PayrollID == id);
        if (p == null) return NotFound();
        if (!IsReviewer && (p.EmployeeID != CurrentEmployeeId || p.Status == "Draft")) return NotFound();
        var std = PayrollRules.StandardDays(p.Year, p.Month);
        return View(new PayrollDetailsViewModel
        {
            Payroll = p, StandardDays = std, DailyRate = PayrollRules.DailyRate(p.BaseSalary, std),
            BasePay = PayrollRules.BasePay(p.BaseSalary, std, p.WorkingDays),
            OvertimePay = PayrollRules.OvertimePay(p.BaseSalary, std, p.OvertimeHours),
            CanReview = IsReviewer, CanApprove = IsReviewer && p.EmployeeID != CurrentEmployeeId
        });
    }

    [HttpGet, Authorize(Roles = "HR,Admin")]
    public async Task<IActionResult> ExportCsv(int month, int year)
    {
        (month, year) = Period(month, year);
        var items = await Context.Payrolls.AsNoTracking().Include(p => p.Employee).ThenInclude(e => e!.Department)
            .Where(p => p.Month == (byte)month && p.Year == (short)year)
            .OrderBy(p => p.Employee!.EmployeeCode).ToListAsync();
        var bytes = CsvHelper.Build(
            new[] { "Mã NV", "Họ tên", "Phòng ban", "Lương cơ bản", "Ngày công", "Giờ làm", "Giờ tăng ca", "Phụ cấp", "Khấu trừ", "Thực nhận", "Trạng thái" },
            items.Select(p => new[]
            {
                p.Employee!.EmployeeCode, p.Employee.FullName, p.Employee.Department?.DepartmentName ?? "",
                p.BaseSalary.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture),
                p.WorkingDays.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture),
                p.WorkingHours.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture),
                p.OvertimeHours.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture),
                p.Allowance.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture),
                p.Deduction.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture),
                p.TotalSalary.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture),
                ReportService.StatusLabel(p.Status)
            }));
        return File(bytes, "text/csv; charset=utf-8", $"bang-luong-{year}-{month:00}.csv");
    }

    private IActionResult BackWithError(string message)
    {
        TempData["Error"] = message;
        return RedirectToAction(nameof(Index));
    }
}
