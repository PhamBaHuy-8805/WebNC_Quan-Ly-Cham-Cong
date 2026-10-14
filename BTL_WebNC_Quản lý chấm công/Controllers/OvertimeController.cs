using BTL_WebNC_Quản_lý_chấm_công.Data;
using BTL_WebNC_Quản_lý_chấm_công.Models;
using BTL_WebNC_Quản_lý_chấm_công.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BTL_WebNC_Quản_lý_chấm_công.Controllers;

// Khởi tạo bằng ASP.NET Core scaffolder, bổ sung quy tắc Module 3 theo SRS.
public class OvertimeController(ApplicationDbContext context) : RequestControllerBase(context)
{
    private IQueryable<Overtime> VisibleRequests() =>
        IsReviewer ? Context.OverTimes : Context.OverTimes.Where(x => x.EmployeeID == CurrentEmployeeId);

    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? status, string sort = "date_desc", int page = 1)
    {
        var query = VisibleRequests().AsNoTracking();
        search = search?.Trim();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.Employee!.FullName.Contains(search)
                || x.Employee.EmployeeCode.Contains(search) || (x.Reason != null && x.Reason.Contains(search)));
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(x => x.Status == status);
        var count = await query.CountAsync();
        const int pageSize = 5;
        var totalPages = Math.Max(1, (int)Math.Ceiling(count / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);
        sort = sort is "date" or "name" or "status" ? sort : "date_desc";
        var ordered = sort switch
        {
            "date" => query.OrderBy(x => x.WorkDate).ThenBy(x => x.OvertimeID),
            "name" => query.OrderBy(x => x.Employee!.FullName).ThenBy(x => x.OvertimeID),
            "status" => query.OrderBy(x => x.Status).ThenBy(x => x.OvertimeID),
            _ => query.OrderByDescending(x => x.WorkDate).ThenByDescending(x => x.OvertimeID)
        };
        var items = await ordered.Include(x => x.Employee).Include(x => x.Approver)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return View(new RequestListViewModel<Overtime>
        {
            Items = items, Search = search, Status = status, Sort = sort,
            Page = page, TotalPages = totalPages, TotalCount = count
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var entity = await VisibleRequests().AsNoTracking().Include(x => x.Employee).Include(x => x.Approver)
            .SingleOrDefaultAsync(x => x.OvertimeID == id);
        return entity == null ? NotFound() : View(entity);
    }

    [HttpGet, Authorize(Roles = "Employee")]
    public IActionResult Create() => View(new OvertimeCreateViewModel());

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Employee")]
    public async Task<IActionResult> Create(OvertimeCreateViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var entity = new Overtime { EmployeeID = CurrentEmployeeId, Status = "Pending", ApprovedBy = null };
        ApplyForm(entity, model);
        Context.OverTimes.Add(entity);
        await Context.SaveChangesAsync();
        TempData["Success"] = "Đã gửi yêu cầu. Đơn đang chờ HR xử lý.";
        return RedirectToAction(nameof(Details), new { id = entity.OvertimeID });
    }

    [HttpGet, Authorize(Roles = "Employee")]
    public async Task<IActionResult> Edit(int id)
    {
        var entity = await Context.OverTimes.AsNoTracking()
            .SingleOrDefaultAsync(x => x.OvertimeID == id && x.EmployeeID == CurrentEmployeeId);
        if (entity == null) return NotFound();
        if (entity.Status != "Pending") return Conflict("Chỉ được sửa đơn đang chờ duyệt.");
        ViewData["RequestId"] = id;
        return View(new OvertimeCreateViewModel
        {
            WorkDate = entity.WorkDate,
            StartTime = entity.StartTime,
            EndTime = entity.EndTime,
            Reason = entity.Reason ?? string.Empty
        });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Employee")]
    public async Task<IActionResult> Edit(int id, OvertimeCreateViewModel model)
    {
        var entity = await Context.OverTimes
            .SingleOrDefaultAsync(x => x.OvertimeID == id && x.EmployeeID == CurrentEmployeeId);
        if (entity == null) return NotFound();
        if (entity.Status != "Pending") return Conflict("Đơn đã xử lý, không thể sửa.");
        ViewData["RequestId"] = id;
        if (!ModelState.IsValid) return View(model);
        ApplyForm(entity, model);
        try { await Context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return Conflict("Đơn vừa được xử lý hoặc xóa. Vui lòng tải lại danh sách."); }
        TempData["Success"] = "Đã cập nhật yêu cầu.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet, Authorize(Roles = "Employee")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await Context.OverTimes.AsNoTracking().Include(x => x.Employee).Include(x => x.Approver)
            .SingleOrDefaultAsync(x => x.OvertimeID == id && x.EmployeeID == CurrentEmployeeId);
        if (entity == null) return NotFound();
        if (entity.Status != "Pending") return Conflict("Chỉ được xóa đơn đang chờ duyệt.");
        return View(entity);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken, Authorize(Roles = "Employee")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var entity = await Context.OverTimes
            .SingleOrDefaultAsync(x => x.OvertimeID == id && x.EmployeeID == CurrentEmployeeId);
        if (entity == null) return NotFound();
        if (entity.Status != "Pending") return Conflict("Đơn đã xử lý, không thể xóa.");
        Context.OverTimes.Remove(entity);
        try { await Context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return Conflict("Đơn vừa được xử lý hoặc xóa. Vui lòng tải lại danh sách."); }
        TempData["Success"] = "Đã xóa yêu cầu đang chờ duyệt.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "HR,Admin")]
    public Task<IActionResult> Approve(int id) => Review(id, "Approved");

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "HR,Admin")]
    public Task<IActionResult> Reject(int id) => Review(id, "Rejected");

    private async Task<IActionResult> Review(int id, string decision)
    {
        var entity = await Context.OverTimes.SingleOrDefaultAsync(x => x.OvertimeID == id);
        if (entity == null) return NotFound();
        if (entity.EmployeeID == CurrentEmployeeId) return Forbid();
        if (entity.Status != "Pending") return Conflict("Đơn đã được xử lý, không thể duyệt hoặc từ chối lần nữa.");
        entity.Status = decision;
        entity.ApprovedBy = CurrentEmployeeId;
        try { await Context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return Conflict("Một người khác vừa xử lý đơn. Vui lòng tải lại danh sách."); }
        TempData["Success"] = decision == "Approved" ? "Đã phê duyệt yêu cầu." : "Đã từ chối yêu cầu.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private static void ApplyForm(Overtime entity, OvertimeCreateViewModel model)
    {
        entity.WorkDate = model.WorkDate!.Value.Date;
        entity.StartTime = model.StartTime!.Value;
        entity.EndTime = model.EndTime!.Value;
        entity.Reason = model.Reason.Trim();
        entity.TotalHours = decimal.Round((decimal)(entity.EndTime - entity.StartTime).TotalHours, 2);
    }
}
