using System.Security.Claims;
using BTL_WebNC_Quản_lý_chấm_công.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace BTL_WebNC_Quản_lý_chấm_công.Services;

// Kiểm tra trước bước phân quyền, áp dụng cho mọi request có cookie đăng nhập.
public class AccountCookieEvents(ApplicationDbContext context) : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext cookieContext)
    {
        var principal = cookieContext.Principal;
        var userId = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var account = int.TryParse(userId, out var id)
            ? await context.Accounts.AsNoTracking().Include(a => a.Role).Include(a => a.Employee)
                .SingleOrDefaultAsync(a => a.UserID == id, cookieContext.HttpContext.RequestAborted)
            : null;

        if (account is not { Status: "Active", Employee: { Status: "Active" }, Role: not null }
            || account.Role.RoleName is not ("Employee" or "HR" or "Admin")
            || principal?.FindFirstValue(ClaimTypes.Role) != account.Role.RoleName
            || principal.FindFirstValue("EmployeeID") != account.EmployeeID?.ToString()
            || principal.Identity?.Name != account.Username)
        {
            cookieContext.RejectPrincipal();
            await cookieContext.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}
