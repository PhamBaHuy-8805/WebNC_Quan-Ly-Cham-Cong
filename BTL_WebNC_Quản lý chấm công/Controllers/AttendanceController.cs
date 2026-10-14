using BTL_WebNC_Quản_lý_chấm_công.Data;
using BTL_WebNC_Quản_lý_chấm_công.Models;
using BTL_WebNC_Quản_lý_chấm_công.Services;
using BTL_WebNC_Quản_lý_chấm_công.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BTL_WebNC_Quản_lý_chấm_công.Controllers;

public class AttendanceController(ApplicationDbContext context, TimeProvider clock) : RequestControllerBase(context)
{
    private IQueryable<Attendance> VisibleRecords() => IsReviewer
        ? Context.Attendances
        : Context.Attendances.Where(a => a.EmployeeID == CurrentEmployeeId);

    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? approvalStatus, DateTime? date, int page = 1)
    {
        var today = AttendanceRules.LocalNow(clock).Date;
        var query = VisibleRecords().AsNoTracking();
        search = search?.Trim();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(a => a.Employee!.EmployeeCode.Contains(search) || a.Employee.FullName.Contains(search));
        if (approvalStatus is "Pending" or "Approved" or "Rejected")
            query = query.Where(a => a.ApprovalStatus == approvalStatus);
        else approvalStatus = null;
        if (date.HasValue) query = query.Where(a => a.WorkDate == date.Value.Date);
        var count = await query.CountAsync();
        const int pageSize = 10;
        var totalPages = Math.Max(1, (int)Math.Ceiling(count / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);
        var items = await query.Include(a => a.Employee).Include(a => a.Reviewer)
            .OrderByDescending(a => a.WorkDate).ThenByDescending(a => a.AttendanceID)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return View(new AttendanceIndexViewModel
        {
            Today = today, IsReviewer = IsReviewer, Items = items,
            TodayAttendance = await Context.Attendances.AsNoTracking()
                .SingleOrDefaultAsync(a => a.EmployeeID == CurrentEmployeeId && a.WorkDate == today),
            Search = search, ApprovalStatus = approvalStatus, Date = date,
            Page = page, TotalPages = totalPages, TotalCount = count
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var record = await VisibleRecords().AsNoTracking().Include(a => a.Employee).Include(a => a.Reviewer)
            .SingleOrDefaultAsync(a => a.AttendanceID == id);
        return record == null ? NotFound() : View(record);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Employee")]
    public async Task<IActionResult> CheckIn()
    {
        var now = AttendanceRules.LocalNow(clock);
        if (await Context.Attendances.AnyAsync(a => a.EmployeeID == CurrentEmployeeId && a.WorkDate == now.Date))
            return BackWithError("Bạn đã có bản chấm công hôm nay, không thể check-in thêm.");

        Context.Attendances.Add(new Attendance
        {
            EmployeeID = CurrentEmployeeId, WorkDate = now.Date, CheckIn = now,
            Status = AttendanceRules.WorkStatus(now, null), ApprovalStatus = "Pending"
        });
        try { await Context.SaveChangesAsync(); }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return BackWithError("Check-in đã được ghi nhận ở một yêu cầu khác. Vui lòng tải lại trang.");
        }
        TempData["Success"] = $"Đã check-in lúc {now:HH:mm:ss}. Hãy check-out khi kết thúc làm việc để gửi HR phê duyệt.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Employee")]
    public async Task<IActionResult> CheckOut()
    {
        var now = AttendanceRules.LocalNow(clock);
        var record = await Context.Attendances
            .SingleOrDefaultAsync(a => a.EmployeeID == CurrentEmployeeId && a.WorkDate == now.Date);
        if (record?.CheckIn == null) return BackWithError("Bạn cần check-in trước khi check-out.");
        if (record.CheckOut != null) return BackWithError("Bạn đã check-out hôm nay.");
        if (record.ApprovalStatus != "Pending") return BackWithError("Bản chấm công đã được HR xử lý.");
        if (now <= record.CheckIn.Value) return BackWithError("Giờ ra phải sau giờ vào.");

        record.CheckOut = now;
        record.TotalHours = AttendanceRules.CalculateHours(record.CheckIn.Value, now);
        record.Status = AttendanceRules.WorkStatus(record.CheckIn.Value, now);
        try { await Context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException)
        {
            return BackWithError("Bản chấm công vừa được cập nhật. Vui lòng tải lại trang.");
        }
        TempData["Success"] = "Đã check-out. Bản chấm công đang chờ HR xác nhận.";
        return RedirectToAction(nameof(Details), new { id = record.AttendanceID });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "HR,Admin")]
    public Task<IActionResult> Approve(int id, AttendanceReviewViewModel model) => Review(id, model, "Approved");

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "HR,Admin")]
    public Task<IActionResult> Reject(int id, AttendanceReviewViewModel model) => Review(id, model, "Rejected");

    private async Task<IActionResult> Review(int id, AttendanceReviewViewModel model, string decision)
    {
        if (decision == "Rejected" && string.IsNullOrWhiteSpace(model.ReviewNote))
            ModelState.AddModelError(nameof(model.ReviewNote), "Vui lòng nhập lý do từ chối.");
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Kiểm tra lại biểu mẫu. Lý do từ chối là bắt buộc; ghi chú tối đa 500 ký tự.";
            return RedirectToAction(nameof(Details), new { id });
        }
        byte[] originalVersion;
        try { originalVersion = Convert.FromBase64String(model.RowVersion); }
        catch (FormatException) { return BadRequest("Phiên bản bản ghi không hợp lệ."); }
        if (originalVersion.Length != 8) return BadRequest("Phiên bản bản ghi không hợp lệ.");

        var record = await Context.Attendances.SingleOrDefaultAsync(a => a.AttendanceID == id);
        if (record == null) return NotFound();
        if (record.EmployeeID == CurrentEmployeeId) return Forbid();
        if (record.CheckIn == null || record.CheckOut == null || record.CheckOut <= record.CheckIn)
            return BackWithError("Chỉ xử lý bản chấm công có đầy đủ giờ vào và giờ ra hợp lệ.");
        if (record.ApprovalStatus != "Pending") return BackWithError("Bản chấm công này đã được xử lý.");

        Context.Entry(record).Property(a => a.RowVersion).OriginalValue = originalVersion;
        record.ApprovalStatus = decision;
        record.ReviewedBy = CurrentEmployeeId;
        record.ReviewedAt = AttendanceRules.LocalNow(clock);
        record.ReviewNote = model.ReviewNote?.Trim();
        try { await Context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException)
        {
            return BackWithError("Dữ liệu đã thay đổi sau khi bạn mở trang. Vui lòng kiểm tra lại trước khi xác nhận.");
        }
        TempData["Success"] = decision == "Approved" ? "Đã xác nhận chấm công." : "Đã từ chối chấm công.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private IActionResult BackWithError(string message)
    {
        TempData["Error"] = message;
        return RedirectToAction(nameof(Index));
    }
}
