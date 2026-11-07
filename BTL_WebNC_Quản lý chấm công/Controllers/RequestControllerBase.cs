using System.Security.Claims;
using BTL_WebNC_Quản_lý_chấm_công.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BTL_WebNC_Quản_lý_chấm_công.Controllers;

[Authorize(Roles = "Employee,HR,Admin")]
public abstract class RequestControllerBase(ApplicationDbContext context) : Controller
{
    protected readonly ApplicationDbContext Context = context;
    protected int CurrentEmployeeId { get; private set; }
    protected bool IsReviewer => User.IsInRole("HR") || User.IsInRole("Admin");

    public override async Task OnActionExecutionAsync(ActionExecutingContext actionContext, ActionExecutionDelegate next)
    {
        // AccountCookieEvents đã đối chiếu các claim với tài khoản trước bước phân quyền.
        if (!int.TryParse(User.FindFirstValue("EmployeeID"), out var employeeId))
        {
            actionContext.Result = Challenge();
            return;
        }
        CurrentEmployeeId = employeeId;
        ViewData["CurrentEmployeeID"] = CurrentEmployeeId;
        await next();
    }
}
