using System.Data;
using BTL_WebNC_Quản_lý_chấm_công.Data;
using BTL_WebNC_Quản_lý_chấm_công.Models;
using BTL_WebNC_Quản_lý_chấm_công.Services;
using BTL_WebNC_Quản_lý_chấm_công.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BTL_WebNC_Quản_lý_chấm_công.Controllers;

public class PayrollController(ApplicationDbContext context, PayrollCalculator calculator, TimeProvider clock)
    : RequestControllerBase(context)
{
    private IQueryable<Payroll> VisiblePayrolls() => IsReviewer ? Context.Payrolls
        : Context.Payrolls.Where(p => p.EmployeeID == CurrentEmployeeId && (p.Status == "Approved" || p.Status == "Paid"));

    [HttpGet]
    public async Task<IActionResult> Index(int? month, int? year)
    {
        var previous = AttendanceRules.LocalNow(clock).AddMonths(-1);
        var selectedMonth = month ?? previous.Month;
        var selectedYear = year ?? previous.Year;
        if (selectedMonth is < 1 or > 12 || selectedYear is < 2000 or > 2100) return BadRequest("Tháng/năm không hợp lệ.");
        return View(new PayrollIndexViewModel
        {
            Month = selectedMonth, Year = selectedYear, IsReviewer = IsReviewer,
            Items = await VisiblePayrolls().AsNoTracking().Include(p => p.Employee)
                .Where(p => p.Month == selectedMonth && p.Year == selectedYear).OrderBy(p => p.Employee!.EmployeeCode).ToListAsync()
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var payroll = await VisiblePayrolls().AsNoTracking().Include(p => p.Employee).SingleOrDefaultAsync(p => p.PayrollID == id);
        return payroll == null ? NotFound() : View(payroll);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "HR,Admin")]
    public async Task<IActionResult> Generate(PayrollGenerateViewModel model)
    {
        if (!ModelState.IsValid) return BadRequest("Tháng/năm không hợp lệ.");
        var end = new DateTime(model.Year, model.Month, 1).AddMonths(1);
        if (end > AttendanceRules.LocalNow(clock).Date)
            return ListError(model.Month, model.Year, "Chỉ lập lương cho tháng đã kết thúc.");
        await using var transaction = await Context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var employees = await Context.Employees.Where(e => e.Status == "Active" && e.HireDate < end).ToListAsync();
        var existing = await Context.Payrolls.Where(p => p.Year == model.Year && p.Month == model.Month).ToListAsync();
        var count = 0;
        try
        {
            foreach (var employee in employees)
            {
                var payroll = existing.SingleOrDefault(p => p.EmployeeID == employee.EmployeeID);
                if (payroll != null && payroll.Status != "Draft") continue;
                if (payroll == null)
                {
                    payroll = new Payroll { EmployeeID = employee.EmployeeID, Month = (byte)model.Month, Year = (short)model.Year };
                    Context.Payrolls.Add(payroll);
                }
                await calculator.CalculateAsync(payroll, employee);
                count++;
            }
            await Context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (InvalidOperationException ex) { return ListError(model.Month, model.Year, ex.Message); }
        catch (DbUpdateException) { return ListError(model.Month, model.Year, "Dữ liệu vừa thay đổi hoặc kỳ lương đã tồn tại. Vui lòng tải lại."); }
        TempData["Success"] = $"Đã lập/tính lại {count} bảng lương nháp. Bảng đã duyệt hoặc thanh toán được giữ nguyên.";
        return RedirectToAction(nameof(Index), new { month = model.Month, year = model.Year });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "HR,Admin")]
    public async Task<IActionResult> Recalculate(int id, PayrollActionViewModel model)
    {
        var payroll = await Context.Payrolls.Include(p => p.Employee).SingleOrDefaultAsync(p => p.PayrollID == id);
        if (payroll == null) return NotFound();
        if (payroll.Status != "Draft") return Error(id, "Chỉ được tính lại bảng lương nháp.");
        if (!SetVersion(payroll, model)) return BadRequest("Biểu mẫu hoặc phiên bản không hợp lệ.");
        payroll.Allowance = model.Allowance;
        try
        {
            await calculator.CalculateAsync(payroll, payroll.Employee!);
            await Context.SaveChangesAsync();
        }
        catch (InvalidOperationException ex) { return Error(id, ex.Message); }
        catch (DbUpdateConcurrencyException) { return Error(id, "Bảng lương vừa thay đổi. Vui lòng tải lại."); }
        TempData["Success"] = "Đã tính lại bảng lương và lưu phụ cấp.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "HR,Admin")]
    public async Task<IActionResult> Approve(int id, PayrollActionViewModel model)
    {
        await using var transaction = await Context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var payroll = await Context.Payrolls.Include(p => p.Employee).SingleOrDefaultAsync(p => p.PayrollID == id);
        if (payroll == null) return NotFound();
        if (payroll.EmployeeID == CurrentEmployeeId) return Forbid();
        if (payroll.Status != "Draft") return Error(id, "Bảng lương đã được xử lý.");
        if (!SetVersion(payroll, model)) return BadRequest("Biểu mẫu hoặc phiên bản không hợp lệ.");
        if (!model.ConfirmAbsences) return Error(id, "Cần xác nhận đã đối soát ngày vắng và các khoản lương.");
        var oldDetails = payroll.CalculationDetails;
        var oldTotal = payroll.TotalSalary;
        var oldBase = payroll.BaseSalary;
        var oldMultiplier = payroll.OvertimeMultiplier;
        try
        {
            await calculator.CalculateAsync(payroll, payroll.Employee!);
            if (oldDetails != payroll.CalculationDetails || oldTotal != payroll.TotalSalary || oldBase != payroll.BaseSalary || oldMultiplier != payroll.OvertimeMultiplier)
            {
                await Context.SaveChangesAsync();
                await transaction.CommitAsync();
                return Error(id, "Dữ liệu nguồn hoặc chính sách đã thay đổi. Đã tính lại bản nháp; hãy kiểm tra trước khi duyệt.");
            }
            if (PayrollCalculator.Details(payroll).Any(d => d.NeedsReview))
                return Error(id, "Còn dữ liệu cần xử lý. Hãy sửa/duyệt chấm công, nghỉ phép hoặc tăng ca rồi tính lại.");
            payroll.Status = "Approved";
            payroll.ApprovedBy = CurrentEmployeeId;
            payroll.ApprovedAt = AttendanceRules.LocalNow(clock);
            await Context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (InvalidOperationException ex) { return Error(id, ex.Message); }
        catch (DbUpdateConcurrencyException) { return Error(id, "Bảng lương vừa thay đổi. Vui lòng tải lại."); }
        TempData["Success"] = "Đã duyệt và khóa dữ liệu tính lương của kỳ này.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "HR,Admin")]
    public async Task<IActionResult> MarkPaid(int id, PayrollActionViewModel model)
    {
        var payroll = await Context.Payrolls.SingleOrDefaultAsync(p => p.PayrollID == id);
        if (payroll == null) return NotFound();
        if (payroll.EmployeeID == CurrentEmployeeId) return Forbid();
        if (payroll.Status != "Approved") return Error(id, "Chỉ xác nhận thanh toán bảng lương đã duyệt.");
        if (payroll.CalculatedAt == null) return Error(id, "Bảng lương cũ chưa có chi tiết tính. Cần đối soát dữ liệu cũ trước khi ghi nhận thanh toán.");
        if (!SetVersion(payroll, model)) return BadRequest("Biểu mẫu hoặc phiên bản không hợp lệ.");
        payroll.Status = "Paid";
        payroll.PaidAt = AttendanceRules.LocalNow(clock);
        try { await Context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return Error(id, "Bảng lương vừa thay đổi. Vui lòng tải lại."); }
        TempData["Success"] = "Đã ghi nhận thanh toán. Thao tác này không thực hiện chuyển tiền.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private bool SetVersion(Payroll payroll, PayrollActionViewModel model)
    {
        if (!ModelState.IsValid) return false;
        try
        {
            var version = Convert.FromBase64String(model.RowVersion);
            if (version.Length != 8) return false;
            Context.Entry(payroll).Property(p => p.RowVersion).OriginalValue = version;
            return true;
        }
        catch (FormatException) { return false; }
    }

    private IActionResult Error(int id, string message)
    {
        TempData["Error"] = message;
        return RedirectToAction(nameof(Details), new { id });
    }

    private IActionResult ListError(int month, int year, string message)
    {
        TempData["Error"] = message;
        return RedirectToAction(nameof(Index), new { month, year });
    }
}
