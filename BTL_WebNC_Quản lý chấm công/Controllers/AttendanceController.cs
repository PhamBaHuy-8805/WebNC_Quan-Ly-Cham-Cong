using System.Globalization;
using System.Security.Claims;
using BTL_WebNC_Quản_lý_chấm_công.Data;
using BTL_WebNC_Quản_lý_chấm_công.Models;
using BTL_WebNC_Quản_lý_chấm_công.Services;
using BTL_WebNC_Quản_lý_chấm_công.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BTL_WebNC_Quản_lý_chấm_công.Controllers;

// Bảo vệ cả Index/Details; các action POST vẫn giữ phân quyền cụ thể.
[Authorize]
public class AttendanceController(ApplicationDbContext context, TimeProvider clock) : RequestControllerBase(context)
{
    private IQueryable<Attendance> VisibleRecords() => IsReviewer
        ? Context.Attendances
        : Context.Attendances.Where(a => a.EmployeeID == CurrentEmployeeId);

    // Bài thực hành buổi 7: Cookies lưu ở trình duyệt; Session lưu ở server.
    private async Task RecordVisitAsync()
    {
        var nowUtc = clock.GetUtcNow();

        // 1. Persistent Cookie: số lượt truy cập trang (theo trình duyệt).
        var oldCount = 0;
        int.TryParse(Request.Cookies["Attendance.VisitCount"], out oldCount);
        var visits = oldCount < 0 ? 1 : oldCount == int.MaxValue ? int.MaxValue : oldCount + 1;

        var oldVisit = Request.Cookies["Attendance.LastVisitUtc"];
        var previousVisit = DateTimeOffset.TryParse(
            oldVisit,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out var previousTime)
            ? previousTime.ToOffset(TimeSpan.FromHours(7)).ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture)
            : "Lần đầu truy cập";

        var cookieOptions = new CookieOptions
        {
            Expires = nowUtc.AddDays(30),
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Path = "/"
        };
        Response.Cookies.Append("Attendance.VisitCount", visits.ToString(CultureInfo.InvariantCulture), cookieOptions);
        Response.Cookies.Append("Attendance.LastVisitUtc", nowUtc.ToString("O", CultureInfo.InvariantCulture), cookieOptions);

        // 2. Server Session: lượt truy cập trong phiên và dữ liệu hiển thị.
        await HttpContext.Session.LoadAsync();
        var oldSessionViews = HttpContext.Session.GetInt32("Attendance.SessionViews") ?? 0;
        var sessionViews = oldSessionViews == int.MaxValue ? int.MaxValue : oldSessionViews + 1;
        HttpContext.Session.SetInt32("Attendance.SessionViews", sessionViews);
        HttpContext.Session.SetString("Attendance.LastFeature", "Xem danh sách chấm công");
        HttpContext.Session.SetString("Attendance.UserName", User.Identity?.Name ?? "");
        HttpContext.Session.SetString("Attendance.Role", string.Join(", ", User.FindAll(ClaimTypes.Role).Select(r => r.Value)));

        // Chỉ dùng ViewBag/Session để hiển thị, không dùng để quyết định phân quyền.
        ViewBag.CookieVisitCount = visits;
        ViewBag.CookiePreviousVisit = previousVisit;
        ViewBag.SessionViews = sessionViews;
        ViewBag.SessionUserName = HttpContext.Session.GetString("Attendance.UserName");
        ViewBag.SessionRole = HttpContext.Session.GetString("Attendance.Role");
        ViewBag.SessionLastFeature = HttpContext.Session.GetString("Attendance.LastFeature");
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? approvalStatus, DateTime? date, int page = 1)
    {
        await RecordVisitAsync(); // Chỉ bổ sung dòng này; nghiệp vụ cũ giữ nguyên.

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
        var record = await VisibleRecords().AsNoTracking().Include(a => a.Employee).Include(a => a.Reviewer).Include(a => a.CheckInReviewer)
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
        TempData["Success"] = $"Đã gửi yêu cầu check-in lúc {now:HH:mm:ss}. Chỉ được công nhận check-in sau khi HR xác nhận.";
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
        if (record.CheckInApprovalStatus != "Approved") return BackWithError("Check-in cần được HR xác nhận trước khi gửi yêu cầu check-out.");
        if (now <= record.CheckIn.Value) return BackWithError("Giờ ra phải sau giờ vào.");

        record.CheckOut = now;
        record.TotalHours = 0;
        record.CheckOutApprovalStatus = "Pending";
        record.ApprovalStatus = "Pending";
        record.Status = AttendanceRules.WorkStatus(record.CheckIn.Value, now);
        try { await Context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException)
        {
            return BackWithError("Bản chấm công vừa được cập nhật. Vui lòng tải lại trang.");
        }
        TempData["Success"] = "Đã gửi yêu cầu check-out. Chỉ được công nhận giờ ra và tính công sau khi HR xác nhận.";
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
            TempData["Error"] = "Kiểm tra bước xác nhận và biểu mẫu. Lý do từ chối là bắt buộc; ghi chú tối đa 500 ký tự.";
            return RedirectToAction(nameof(Details), new { id });
        }
        var record = await Context.Attendances.SingleOrDefaultAsync(a => a.AttendanceID == id);
        if (record == null) return NotFound();
        if (record.EmployeeID == CurrentEmployeeId) return Forbid();
        if (record.CheckIn == null || record.CheckIn.Value.Date != record.WorkDate.Date)
            return BackWithError("Yêu cầu check-in không hợp lệ.");
        if (model.Stage == "CheckIn")
        {
            if (record.CheckInApprovalStatus != "Pending") return BackWithError("Yêu cầu check-in đã được xử lý.");
            record.CheckInApprovalStatus = decision;
            record.CheckInReviewedBy = CurrentEmployeeId;
            record.CheckInReviewedAt = AttendanceRules.LocalNow(clock);
            record.CheckInReviewNote = model.ReviewNote?.Trim();
            record.ApprovalStatus = decision == "Rejected" ? "Rejected" : "Pending";
            record.TotalHours = 0;
        }
        else
        {
            if (record.CheckInApprovalStatus != "Approved") return BackWithError("Cần xác nhận check-in trước khi xử lý check-out.");
            if (record.CheckOut == null || record.CheckOut <= record.CheckIn || record.CheckOut.Value.Date != record.WorkDate.Date)
                return BackWithError("Yêu cầu check-out phải có giờ ra hợp lệ trong cùng ngày.");
            if (record.CheckOutApprovalStatus != "Pending") return BackWithError("Yêu cầu check-out đã được xử lý.");
            record.CheckOutApprovalStatus = decision;
            record.ReviewedBy = CurrentEmployeeId;
            record.ReviewedAt = AttendanceRules.LocalNow(clock);
            record.ReviewNote = model.ReviewNote?.Trim();
            record.ApprovalStatus = decision;
            record.TotalHours = decision == "Approved" ? AttendanceRules.CalculateHours(record.CheckIn.Value, record.CheckOut.Value) : 0;
        }
        try { await Context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException)
        {
            return BackWithError("Dữ liệu đã thay đổi sau khi bạn mở trang. Vui lòng kiểm tra lại trước khi xác nhận.");
        }
        ReportService.Invalidate();
        var stageName = model.Stage == "CheckIn" ? "check-in" : "check-out";
        TempData["Success"] = decision == "Approved" ? $"Đã xác nhận {stageName}." : $"Đã từ chối {stageName}.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private IActionResult BackWithError(string message)
    {
        TempData["Error"] = message;
        return RedirectToAction(nameof(Index));
    }
}
