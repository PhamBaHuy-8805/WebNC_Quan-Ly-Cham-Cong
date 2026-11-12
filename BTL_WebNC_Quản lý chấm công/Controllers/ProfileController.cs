using BTL_WebNC_Quản_lý_chấm_công.Data;
using BTL_WebNC_Quản_lý_chấm_công.Models;
using BTL_WebNC_Quản_lý_chấm_công.Services;
using BTL_WebNC_Quản_lý_chấm_công.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BTL_WebNC_Quản_lý_chấm_công.Controllers;

// Hồ sơ cá nhân và quy trình HR duyệt thay đổi thông tin liên hệ.
public class ProfileController(ApplicationDbContext context, TimeProvider clock) : RequestControllerBase(context)
{
    private static readonly string[] ValidStatuses = { "Pending", "Approved", "Rejected", "Cancelled" };

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var employee = await Context.Employees.AsNoTracking()
            .Include(x => x.Department).Include(x => x.Position)
            .SingleOrDefaultAsync(x => x.EmployeeID == CurrentEmployeeId);
        if (employee == null) return NotFound();

        var requests = await Context.ProfileChangeRequests.AsNoTracking()
            .Include(x => x.Reviewer)
            .Where(x => x.EmployeeID == CurrentEmployeeId)
            .OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.ProfileChangeRequestID)
            .ToListAsync();

        return View(new ProfileIndexViewModel
        {
            Employee = employee,
            Requests = requests,
            PendingRequest = requests.FirstOrDefault(x => x.Status == "Pending")
        });
    }

    [HttpGet]
    public async Task<IActionResult> EditRequest()
    {
        var employee = await Context.Employees.AsNoTracking()
            .Include(x => x.Department).Include(x => x.Position)
            .SingleOrDefaultAsync(x => x.EmployeeID == CurrentEmployeeId);
        if (employee == null) return NotFound();

        if (await HasPendingRequestAsync())
        {
            TempData["Error"] = "Bạn đã có một yêu cầu đang chờ duyệt. Hãy hủy yêu cầu cũ trước khi gửi yêu cầu mới.";
            return RedirectToAction(nameof(Index));
        }

        ViewData["Employee"] = employee;
        return View(new ProfileContactRequestViewModel
        {
            Phone = employee.Phone,
            Email = employee.Email,
            Address = employee.Address
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditRequest(ProfileContactRequestViewModel model)
    {
        var employee = await Context.Employees
            .Include(x => x.Department).Include(x => x.Position)
            .SingleOrDefaultAsync(x => x.EmployeeID == CurrentEmployeeId);
        if (employee == null) return NotFound();

        ViewData["Employee"] = employee;

        var phone = Normalize(model.Phone);
        var email = Normalize(model.Email);
        var address = Normalize(model.Address);

        if (phone is { Length: > 15 })
            ModelState.AddModelError(nameof(model.Phone), "Số điện thoại tối đa 15 ký tự.");
        if (email is { Length: > 254 })
            ModelState.AddModelError(nameof(model.Email), "Email tối đa 254 ký tự.");
        if (address is { Length: > 255 })
            ModelState.AddModelError(nameof(model.Address), "Địa chỉ tối đa 255 ký tự.");

        if (Same(phone, employee.Phone) && Same(email, employee.Email) && Same(address, employee.Address))
            ModelState.AddModelError(string.Empty, "Thông tin liên hệ không thay đổi. Vui lòng nhập ít nhất một giá trị mới.");

        if (!ModelState.IsValid)
            return View(model);

        if (await HasPendingRequestAsync())
        {
            TempData["Error"] = "Bạn đã có một yêu cầu đang chờ duyệt. Hãy hủy yêu cầu cũ trước khi gửi yêu cầu mới.";
            return RedirectToAction(nameof(Index));
        }

        var request = new ProfileChangeRequest
        {
            EmployeeID = CurrentEmployeeId,
            OriginalPhone = Normalize(employee.Phone),
            OriginalEmail = Normalize(employee.Email),
            OriginalAddress = Normalize(employee.Address),
            RequestedPhone = phone,
            RequestedEmail = email,
            RequestedAddress = address,
            Status = "Pending",
            CreatedAt = AttendanceRules.LocalNow(clock)
        };
        Context.ProfileChangeRequests.Add(request);

        try
        {
            await Context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Chỉ mục duy nhất có điều kiện chặn trường hợp hai yêu cầu Pending được gửi đồng thời.
            TempData["Error"] = "Không thể gửi yêu cầu. Có thể bạn vừa có một yêu cầu đang chờ duyệt.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "Đã gửi yêu cầu cập nhật. Thông tin sẽ được áp dụng sau khi HR phê duyệt.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelRequest(int id)
    {
        var request = await Context.ProfileChangeRequests
            .SingleOrDefaultAsync(x => x.ProfileChangeRequestID == id && x.EmployeeID == CurrentEmployeeId);
        if (request == null) return NotFound();
        if (request.Status != "Pending") return Conflict("Chỉ có thể hủy yêu cầu đang chờ duyệt.");

        request.Status = "Cancelled";
        try
        {
            await Context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("Yêu cầu vừa được xử lý. Vui lòng tải lại trang.");
        }

        TempData["Success"] = "Đã hủy yêu cầu. Bạn có thể gửi yêu cầu mới.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet, Authorize(Roles = "HR,Admin")]
    public async Task<IActionResult> Requests(string? search, string? status = "Pending", int page = 1)
    {
        var query = Context.ProfileChangeRequests.AsNoTracking();
        search = search?.Trim();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.Employee!.FullName.Contains(search)
                || x.Employee.EmployeeCode.Contains(search));

        if (status == "All" || string.IsNullOrWhiteSpace(status))
            status = "All";
        else if (!ValidStatuses.Contains(status))
            status = "Pending";
        if (status != "All")
            query = query.Where(x => x.Status == status);

        var count = await query.CountAsync();
        const int pageSize = 10;
        var totalPages = Math.Max(1, (int)Math.Ceiling(count / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);

        var items = await query
            .Include(x => x.Employee).Include(x => x.Reviewer)
            .OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.ProfileChangeRequestID)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

        return View(new RequestListViewModel<ProfileChangeRequest>
        {
            Items = items,
            Search = search,
            Status = status,
            Page = page,
            TotalPages = totalPages,
            TotalCount = count
        });
    }

    [HttpGet, Authorize(Roles = "HR,Admin")]
    public async Task<IActionResult> RequestDetails(int id)
    {
        var request = await Context.ProfileChangeRequests.AsNoTracking()
            .Include(x => x.Employee).Include(x => x.Reviewer)
            .SingleOrDefaultAsync(x => x.ProfileChangeRequestID == id);
        if (request == null) return NotFound();

        var employee = request.Employee;
        var hasConflict = request.Status == "Pending" && employee != null
            && (!Same(request.OriginalPhone, employee.Phone)
                || !Same(request.OriginalEmail, employee.Email)
                || !Same(request.OriginalAddress, employee.Address));

        return View(new ProfileRequestDetailsViewModel
        {
            Request = request,
            HasConflict = hasConflict,
            CurrentPhone = employee?.Phone,
            CurrentEmail = employee?.Email,
            CurrentAddress = employee?.Address
        });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "HR,Admin")]
    public Task<IActionResult> Approve(int id, string? reviewNote) => Review(id, true, reviewNote);

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "HR,Admin")]
    public Task<IActionResult> Reject(int id, string? reviewNote) => Review(id, false, reviewNote);

    private async Task<IActionResult> Review(int id, bool approve, string? reviewNote)
    {
        var request = await Context.ProfileChangeRequests
            .Include(x => x.Employee)
            .SingleOrDefaultAsync(x => x.ProfileChangeRequestID == id);
        if (request == null) return NotFound();
        if (request.EmployeeID == CurrentEmployeeId) return Forbid();
        if (request.Status != "Pending") return Conflict("Yêu cầu đã được xử lý, không thể duyệt hoặc từ chối lần nữa.");

        reviewNote = Normalize(reviewNote);
        if (reviewNote is { Length: > 500 })
        {
            TempData["Error"] = "Ghi chú duyệt tối đa 500 ký tự.";
            return RedirectToAction(nameof(RequestDetails), new { id });
        }
        if (!approve && reviewNote == null)
        {
            TempData["Error"] = "Vui lòng nhập lý do từ chối.";
            return RedirectToAction(nameof(RequestDetails), new { id });
        }

        if (approve)
        {
            var employee = request.Employee;
            if (employee == null) return NotFound();
            if (!Same(request.OriginalPhone, employee.Phone)
                || !Same(request.OriginalEmail, employee.Email)
                || !Same(request.OriginalAddress, employee.Address))
            {
                TempData["Error"] = "Thông tin liên hệ của nhân viên đã thay đổi sau khi yêu cầu được gửi. "
                    + "Không thể phê duyệt để tránh ghi đè dữ liệu mới; hãy từ chối và đề nghị nhân viên gửi lại.";
                return RedirectToAction(nameof(RequestDetails), new { id });
            }

            employee.Phone = request.RequestedPhone;
            employee.Email = request.RequestedEmail;
            employee.Address = request.RequestedAddress;
        }

        request.Status = approve ? "Approved" : "Rejected";
        request.ReviewNote = reviewNote;
        request.ReviewedBy = CurrentEmployeeId;
        request.ReviewedAt = AttendanceRules.LocalNow(clock);

        try
        {
            // Một lần lưu duy nhất: yêu cầu và Employee cùng thành công hoặc cùng không đổi.
            await Context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("Một người khác vừa xử lý yêu cầu. Vui lòng tải lại trang.");
        }

        TempData["Success"] = approve
            ? "Đã phê duyệt và cập nhật thông tin liên hệ của nhân viên."
            : "Đã từ chối yêu cầu cập nhật.";
        return RedirectToAction(nameof(RequestDetails), new { id });
    }

    private Task<bool> HasPendingRequestAsync() =>
        Context.ProfileChangeRequests.AnyAsync(x => x.EmployeeID == CurrentEmployeeId && x.Status == "Pending");

    private static string? Normalize(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    private static bool Same(string? left, string? right) =>
        string.Equals(Normalize(left), Normalize(right), StringComparison.Ordinal);
}
