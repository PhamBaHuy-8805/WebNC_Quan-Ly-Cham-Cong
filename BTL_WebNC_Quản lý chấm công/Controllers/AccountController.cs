using System.Security.Claims;
using BTL_WebNC_Quản_lý_chấm_công.Data;
using BTL_WebNC_Quản_lý_chấm_công.Models;
using BTL_WebNC_Quản_lý_chấm_công.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BTL_WebNC_Quản_lý_chấm_công.Controllers;

public class AccountController(ApplicationDbContext context) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null) => View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var account = await context.Accounts.Include(a => a.Role).Include(a => a.Employee)
            .SingleOrDefaultAsync(a => a.Username == model.Username.Trim());
        // Bản thực hành dùng mật khẩu thường theo yêu cầu; giữ tên cột cũ để tương thích DB.
        var valid = account is { Status: "Active", Role: not null, Employee: { Status: "Active" } }
            && string.Equals(account.PasswordHash, model.Password, StringComparison.Ordinal);
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
            new Claim("EmployeeID", account.EmployeeID!.Value.ToString())
        };
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
        return Url.IsLocalUrl(model.ReturnUrl) ? LocalRedirect(model.ReturnUrl!) : RedirectToAction("Index", "/ajax/employees");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }
}
