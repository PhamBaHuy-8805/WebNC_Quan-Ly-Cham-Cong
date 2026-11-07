using System.Security.Claims;
using BTL_WebNC_Quản_lý_chấm_công.Data;
using BTL_WebNC_Quản_lý_chấm_công.Models;
using BTL_WebNC_Quản_lý_chấm_công.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace BTL_WebNC_Quản_lý_chấm_công.Controllers;

public class AccountController(ApplicationDbContext context, UserManager<Account> userManager) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null) => View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var account = await context.Accounts.Include(a => a.Role).Include(a => a.Employee)
            .SingleOrDefaultAsync(a => a.Username == model.Username.Trim());
        var valid = account is { Status: "Active", Role: not null, Employee: { Status: "Active" } }
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
            new Claim("EmployeeID", account.EmployeeID!.Value.ToString())
        };
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
        // Lưu thông tin người dùng trong Session
        HttpContext.Session.SetString("LoginUser", account.Username);
        HttpContext.Session.SetString("LoginRole", account.Role!.RoleName);
        HttpContext.Session.SetInt32("EmployeeID", account.EmployeeID!.Value);
        return Url.IsLocalUrl(model.ReturnUrl)
            ? LocalRedirect(model.ReturnUrl!)
            : Redirect("/Employees");   
         }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        HttpContext.Session.Clear();
        return RedirectToAction(nameof(Login));
    }

    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }
}
