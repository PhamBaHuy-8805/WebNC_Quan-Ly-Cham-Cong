using System.Diagnostics;
using System.Globalization;
using System.Security.Claims;
using BTL_WebNC_Quản_lý_chấm_công.Data;
using BTL_WebNC_Quản_lý_chấm_công.Models;
using BTL_WebNC_Quản_lý_chấm_công.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BTL_WebNC_Quản_lý_chấm_công.Controllers
{
    [AllowAnonymous]
    public class HomeController(TimeProvider clock, ApplicationDbContext context) : Controller
    {
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> Index()
        {
            const string countCookie = "Attendance.VisitCount";
            const string lastVisitCookie = "Attendance.LastVisitUtc";
            var now = clock.GetUtcNow();

            // Cookie đến từ trình duyệt, cần kiểm tra dữ liệu trước khi sử dụng.
            var count = long.TryParse(Request.Cookies[countCookie], NumberStyles.None,
                CultureInfo.InvariantCulture, out var savedCount) && savedCount > 0
                ? savedCount : 0;
            DateTimeOffset? previousVisit = DateTimeOffset.TryParseExact(
                Request.Cookies[lastVisitCookie], "O", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var savedVisit) && savedVisit <= now
                ? savedVisit : null;
            var visitCount = count < long.MaxValue ? count + 1 : long.MaxValue;
            var options = new CookieOptions
            {
                Expires = now.AddDays(30),
                MaxAge = TimeSpan.FromDays(30),
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Path = "/"
            };
            Response.Cookies.Append(countCookie, visitCount.ToString(CultureInfo.InvariantCulture), options);
            Response.Cookies.Append(lastVisitCookie, now.ToString("O", CultureInfo.InvariantCulture), options);

            HomeProfileSummaryViewModel? profile = null;
            if (User.Identity?.IsAuthenticated == true
                && int.TryParse(User.FindFirstValue("EmployeeID"), out var employeeId))
            {
                profile = await context.Employees.AsNoTracking()
                    .Where(e => e.EmployeeID == employeeId)
                    .Select(e => new HomeProfileSummaryViewModel
                    {
                        EmployeeID = e.EmployeeID,
                        EmployeeCode = e.EmployeeCode,
                        FullName = e.FullName,
                        Phone = e.Phone,
                        Email = e.Email,
                        DepartmentName = e.Department != null ? e.Department.DepartmentName : null,
                        PositionName = e.Position != null ? e.Position.PositionName : null,
                        HasPendingProfileRequest = context.ProfileChangeRequests
                            .Any(r => r.EmployeeID == e.EmployeeID && r.Status == "Pending")
                    })
                    .SingleOrDefaultAsync();
            }

            return View(new HomeVisitViewModel
            {
                VisitCount = visitCount,
                PreviousVisit = previousVisit,
                CurrentVisit = now,
                Profile = profile
            });
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
