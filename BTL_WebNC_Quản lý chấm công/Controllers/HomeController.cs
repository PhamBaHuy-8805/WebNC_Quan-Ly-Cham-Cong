using System.Diagnostics;
using System.Globalization;
using BTL_WebNC_Quản_lý_chấm_công.Models;
using BTL_WebNC_Quản_lý_chấm_công.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace BTL_WebNC_Quản_lý_chấm_công.Controllers
{
    public class HomeController(TimeProvider clock) : Controller
    {
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Index()
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
            return View(new HomeVisitViewModel
            {
                VisitCount = visitCount,
                PreviousVisit = previousVisit,
                CurrentVisit = now
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

