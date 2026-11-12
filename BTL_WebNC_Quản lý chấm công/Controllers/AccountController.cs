using System.Security.Claims;
using BTL_WebNC_Quản_lý_chấm_công.Data;
using BTL_WebNC_Quản_lý_chấm_công.Models;
using BTL_WebNC_Quản_lý_chấm_công.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace BTL_WebNC_Quản_lý_chấm_công.Controllers;

public class AccountController(ApplicationDbContext context, UserManager<Account> userManager) : Controller
{
    [HttpGet, AllowAnonymous]
    public IActionResult Login(string? returnUrl = null) => View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost, ValidateAntiForgeryToken, AllowAnonymous]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var account = await context.Accounts.Include(a => a.Role).Include(a => a.Employee)
            .SingleOrDefaultAsync(a => a.Username == model.Username.Trim());
        // Mật khẩu được kiểm tra bằng hash Identity; dữ liệu cũ dạng thường sẽ tự được nâng cấp.
        var valid = account is { Status: "Active", Role: not null, Employee: { Status: "Active" } }
            && account.Role.RoleName is "Employee" or "HR" or "Admin"
            && await userManager.CheckPasswordAsync(account, model.Password);
        if (!valid)
        {
            ModelState.AddModelError(string.Empty, "Tài khoản hoặc mật khẩu không hợp lệ, hoặc hồ sơ đã ngừng hoạt động.");
            return View(model);
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, account!.UserID.ToString()),
            new Claim(ClaimTypes.Name, account.Username),
            new Claim(ClaimTypes.Role, account.Role!.RoleName),
            new Claim("EmployeeID", account.EmployeeID!.Value.ToString()),
            new Claim("FullName", account.Employee!.FullName)
        };
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
            new AuthenticationProperties
            {
                IsPersistent = false,
                AllowRefresh = false
            });
        // Xóa session cũ trước khi tạo phiên mới để tránh lỗi session fixation.
        HttpContext.Session.Clear();
        HttpContext.Session.SetString("LoginUser", account.Username);
        HttpContext.Session.SetString("LoginRole", account.Role.RoleName);
        HttpContext.Session.SetInt32("EmployeeID", account.EmployeeID.Value);
        return Url.IsLocalUrl(model.ReturnUrl) ? LocalRedirect(model.ReturnUrl!) : RedirectToAction("Index", "Attendance");
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        HttpContext.Session.Clear();
        return RedirectToAction(nameof(Login));
    }

    private static readonly string[] AllowedRoleNames = { "Admin", "HR", "Employee" };

    private int? CurrentUserId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    // Người dùng tự đổi mật khẩu; mật khẩu hiện tại phải khớp và không qua bước duyệt.
    [HttpGet, Authorize]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [HttpPost, ValidateAntiForgeryToken, Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var userId = CurrentUserId;
        if (userId == null) return Challenge();

        var account = await context.Accounts
            .SingleOrDefaultAsync(a => a.UserID == userId.Value);
        if (account == null) return NotFound();

        var verification = userManager.PasswordHasher.VerifyHashedPassword(
            account, account.PasswordHash, model.CurrentPassword);
        if (verification == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(nameof(model.CurrentPassword), "Mật khẩu hiện tại không đúng.");
            return View(model);
        }
        if (model.NewPassword == model.CurrentPassword)
        {
            ModelState.AddModelError(nameof(model.NewPassword), "Mật khẩu mới phải khác mật khẩu hiện tại.");
            return View(model);
        }

        account.PasswordHash = userManager.PasswordHasher.HashPassword(account, model.NewPassword);
        await context.SaveChangesAsync();
        TempData["Success"] = "Đã đổi mật khẩu. Hãy dùng mật khẩu mới cho lần đăng nhập tiếp theo.";
        return RedirectToAction("Index", "Profile");
    }

    // Quản lý tài khoản dành riêng cho Admin.
    [HttpGet, Authorize(Roles = "Admin")]
    public async Task<IActionResult> Index()
    {
        var accounts = await context.Accounts.AsNoTracking()
            .Include(a => a.Role).Include(a => a.Employee)
            .OrderBy(a => a.Username).ToListAsync();
        return View(accounts);
    }

    [HttpGet, Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create()
    {
        await LoadAccountOptionsAsync(onlyWithoutAccount: true);
        return View(new AccountCreateViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(AccountCreateViewModel model)
    {
        model.Username = (model.Username ?? string.Empty).Trim();
        if (await context.Accounts.AnyAsync(a => a.Username == model.Username))
            ModelState.AddModelError(nameof(model.Username), "Tên đăng nhập đã tồn tại.");

        var role = await context.Roles.AsNoTracking()
            .SingleOrDefaultAsync(r => r.RoleID == model.RoleID && AllowedRoleNames.Contains(r.RoleName));
        if (role == null)
            ModelState.AddModelError(nameof(model.RoleID), "Vai trò không hợp lệ.");

        var employee = await context.Employees.AsNoTracking()
            .SingleOrDefaultAsync(e => e.EmployeeID == model.EmployeeID);
        if (employee is not { Status: "Active" })
            ModelState.AddModelError(nameof(model.EmployeeID), "Chỉ có thể cấp tài khoản cho nhân viên đang làm việc.");
        else if (await context.Accounts.AnyAsync(a => a.EmployeeID == model.EmployeeID))
            ModelState.AddModelError(nameof(model.EmployeeID), "Nhân viên này đã có tài khoản.");

        if (!ModelState.IsValid)
        {
            await LoadAccountOptionsAsync(model.RoleID, model.EmployeeID, onlyWithoutAccount: true);
            return View(model);
        }

        var account = new Account
        {
            Username = model.Username,
            RoleID = model.RoleID,
            EmployeeID = model.EmployeeID,
            Status = model.Status
        };
        account.PasswordHash = userManager.PasswordHasher.HashPassword(account, model.Password);
        context.Accounts.Add(account);
        await context.SaveChangesAsync();

        TempData["Success"] = $"Đã tạo tài khoản {account.Username}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet, Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var account = await context.Accounts.AsNoTracking()
            .SingleOrDefaultAsync(a => a.UserID == id);
        if (account == null) return NotFound();

        await LoadAccountOptionsAsync(account.RoleID, account.EmployeeID);
        return View(new AccountEditViewModel
        {
            UserID = account.UserID,
            Username = account.Username,
            RoleID = account.RoleID,
            EmployeeID = account.EmployeeID,
            Status = account.Status
        });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(AccountEditViewModel model)
    {
        var account = await context.Accounts
            .SingleOrDefaultAsync(a => a.UserID == model.UserID);
        if (account == null) return NotFound();

        model.Username = (model.Username ?? string.Empty).Trim();
        var isSelf = CurrentUserId == model.UserID;
        if (isSelf && (!string.Equals(model.Username, account.Username, StringComparison.Ordinal)
            || model.RoleID != account.RoleID
            || model.EmployeeID != account.EmployeeID
            || model.Status != account.Status))
        {
            ModelState.AddModelError(string.Empty,
                "Không thể tự thay đổi tên đăng nhập, vai trò, nhân viên hoặc trạng thái. Chỉ có thể đổi mật khẩu.");
        }

        if (await context.Accounts.AnyAsync(a => a.Username == model.Username && a.UserID != model.UserID))
            ModelState.AddModelError(nameof(model.Username), "Tên đăng nhập đã tồn tại.");

        var role = await context.Roles.AsNoTracking()
            .SingleOrDefaultAsync(r => r.RoleID == model.RoleID && AllowedRoleNames.Contains(r.RoleName));
        if (role == null)
            ModelState.AddModelError(nameof(model.RoleID), "Vai trò không hợp lệ.");

        if (model.EmployeeID.HasValue)
        {
            var employee = await context.Employees.AsNoTracking()
                .SingleOrDefaultAsync(e => e.EmployeeID == model.EmployeeID);
            if (employee == null)
                ModelState.AddModelError(nameof(model.EmployeeID), "Nhân viên không tồn tại.");
            else if (employee.Status != "Active" && employee.EmployeeID != account.EmployeeID)
                ModelState.AddModelError(nameof(model.EmployeeID), "Chỉ có thể gán nhân viên đang làm việc.");
            else if (await context.Accounts.AnyAsync(a => a.EmployeeID == model.EmployeeID && a.UserID != model.UserID))
                ModelState.AddModelError(nameof(model.EmployeeID), "Nhân viên này đã có tài khoản khác.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAccountOptionsAsync(model.RoleID, model.EmployeeID);
            return View(model);
        }

        account.Username = model.Username;
        account.RoleID = model.RoleID;
        account.EmployeeID = model.EmployeeID;
        account.Status = model.Status;
        if (!string.IsNullOrWhiteSpace(model.NewPassword))
            account.PasswordHash = userManager.PasswordHasher.HashPassword(account, model.NewPassword);

        await context.SaveChangesAsync();
        TempData["Success"] = $"Đã cập nhật tài khoản {account.Username}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var account = await context.Accounts
            .Include(a => a.Role).Include(a => a.Employee)
            .SingleOrDefaultAsync(a => a.UserID == id);
        if (account == null) return NotFound();

        if (id == CurrentUserId)
        {
            TempData["Error"] = "Không thể xóa tài khoản đang đăng nhập.";
            return RedirectToAction(nameof(Index));
        }

        if (account.Role?.RoleName == "Admin" && account.Status == "Active"
            && !await context.Accounts.AnyAsync(a => a.UserID != id && a.Status == "Active"
                && a.Role!.RoleName == "Admin" && a.Employee != null && a.Employee.Status == "Active"))
        {
            TempData["Error"] = "Không thể xóa quản trị viên đang hoạt động cuối cùng.";
            return RedirectToAction(nameof(Index));
        }

        context.Accounts.Remove(account);
        await context.SaveChangesAsync();
        TempData["Success"] = $"Đã xóa tài khoản {account.Username}.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadAccountOptionsAsync(int? roleId = null, int? employeeId = null,
        bool onlyWithoutAccount = false)
    {
        var roles = await context.Roles.AsNoTracking()
            .Where(r => AllowedRoleNames.Contains(r.RoleName))
            .OrderBy(r => r.RoleID).ToListAsync();
        ViewBag.Roles = new SelectList(roles, "RoleID", "RoleName", roleId);

        var employeeQuery = context.Employees.AsNoTracking()
            .Where(e => e.Status == "Active" || e.EmployeeID == employeeId);
        if (onlyWithoutAccount)
            employeeQuery = employeeQuery.Where(e => !context.Accounts.Any(a => a.EmployeeID == e.EmployeeID));

        var employees = await employeeQuery
            .OrderBy(e => e.EmployeeCode)
            .Select(e => new { e.EmployeeID, Display = e.EmployeeCode + " · " + e.FullName })
            .ToListAsync();
        ViewBag.Employees = new SelectList(employees, "EmployeeID", "Display", employeeId);
    }

    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }
}
