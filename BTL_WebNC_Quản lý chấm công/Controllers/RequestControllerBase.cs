using System.Security.Claims;
using BTL_WebNC_Quản_lý_chấm_công.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace BTL_WebNC_Quản_lý_chấm_công.Controllers;

[Authorize(Roles = "Employee,HR,Admin")]
public abstract class RequestControllerBase(ApplicationDbContext context) : Controller
{
    protected readonly ApplicationDbContext Context = context;
    protected int CurrentEmployeeId { get; private set; }
    protected bool IsReviewer => User.IsInRole("HR") || User.IsInRole("Admin");

    public override async Task OnActionExecutionAsync(ActionExecutingContext actionContext, ActionExecutionDelegate next)
    {
        // Kiểm tra lại tài khoản mỗi request để cookie cũ không giữ quyền khi bị khóa/đổi vai trò.
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var account = int.TryParse(userId, out var id)
            ? await Context.Accounts.AsNoTracking().Include(a => a.Role).Include(a => a.Employee)
                .SingleOrDefaultAsync(a => a.UserID == id)
            : null;
        if (account is not { Status: "Active", Employee: { Status: "Active" }, Role: not null }
            || !User.IsInRole(account.Role.RoleName))
        {
            await HttpContext.SignOutAsync();
            actionContext.Result = Challenge();
            return;
        }
        CurrentEmployeeId = account.EmployeeID!.Value;
        ViewData["CurrentEmployeeID"] = CurrentEmployeeId;
        await next();
    }
}
