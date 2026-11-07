using System.Net;
using System.Text.RegularExpressions;
using BTL_WebNC_Quản_lý_chấm_công.Data;
using BTL_WebNC_Quản_lý_chấm_công.Models;
using BTL_WebNC_Quản_lý_chấm_công.Services;
using Microsoft.EntityFrameworkCore;

static class AttendanceChecks
{
    public static async Task<int> Run(ApplicationDbContext db, HttpClient hr, HttpClient employee, int employeeId, int hrId, DateTime historicalDate)
    {
        var checks = 0;
        void Check(bool condition, string name) { if (!condition) throw new Exception(name); checks++; }
        static string Field(string html, string name) => WebUtility.HtmlDecode(Regex.Match(html, "name=\"" + name + "\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
        async Task<HttpResponseMessage> Send(HttpClient client, string action, string html, int? id = null, string? stage = null, string? note = null, string? version = null) =>
            await client.PostAsync($"/Attendance/{action}" + (id.HasValue ? $"/{id}" : ""), new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = Field(html, "__RequestVerificationToken"),
                ["Stage"] = stage ?? Field(html, "Stage"), ["RowVersion"] = version ?? Field(html, "RowVersion"), ["ReviewNote"] = note ?? ""
            }));
        var today = AttendanceRules.LocalNow(TimeProvider.System).Date;
        async Task<Attendance> Current() => await db.Attendances.AsNoTracking().SingleAsync(a => a.EmployeeID == employeeId && a.WorkDate == today);
        var page = await employee.GetStringAsync("/Attendance");
        await Send(employee, "CheckIn", page);
        var record = await Current();
        var requestedIn = record.CheckIn;
        Check(record.CheckInApprovalStatus == "Pending" && record.TotalHours == 0 && record.ApprovalStatus == "Pending", "Check-in should wait for HR");
        await Send(employee, "CheckIn", page);
        Check(await db.Attendances.CountAsync(a => a.EmployeeID == employeeId && a.WorkDate == today) == 1, "Duplicate check-in allowed");
        await Send(employee, "CheckOut", page);
        Check((await Current()).CheckOut == null, "Checkout allowed before check-in approval");
        var reviewPage = await hr.GetStringAsync($"/Attendance/Details/{record.AttendanceID}");
        var staleVersion = Field(reviewPage, "RowVersion");
        Check(Field(reviewPage, "Stage") == "CheckIn", "HR should review check-in before checkout exists");
        await Send(hr, "Approve", reviewPage, record.AttendanceID, "CheckOut");
        Check((await Current()).CheckInApprovalStatus == "Pending", "Skipped check-in approval");
        var employeeDetails = await employee.GetStringAsync($"/Attendance/Details/{record.AttendanceID}");
        Check((await Send(employee, "Approve", employeeDetails, record.AttendanceID, "CheckIn", version: staleVersion)).StatusCode == HttpStatusCode.Forbidden, "Employee can approve attendance");
        await Send(hr, "Reject", reviewPage, record.AttendanceID, "CheckIn");
        Check((await Current()).CheckInApprovalStatus == "Pending", "Rejection accepted without reason");
        await Send(hr, "Approve", reviewPage, record.AttendanceID, "CheckIn");
        record = await Current();
        Check(record.CheckInApprovalStatus == "Approved" && record.CheckInReviewedBy == hrId && record.CheckInReviewedAt.HasValue, "Check-in approval metadata missing");
        Check(record.CheckIn == requestedIn && record.CheckOut == null && record.TotalHours == 0 && record.ApprovalStatus == "Pending", "Check-in changed timestamp or prematurely earned daily pay");
        page = await employee.GetStringAsync("/Attendance");
        Check(page.Contains("/Attendance/CheckOut"), "Approved check-in should expose checkout button");
        await Send(employee, "CheckOut", page);
        record = await Current();
        var requestedOut = record.CheckOut;
        Check(requestedOut.HasValue && record.CheckOutApprovalStatus == "Pending" && record.TotalHours == 0, "Checkout should wait for HR and earn no hours yet");
        db.OverTimes.Add(new Overtime { EmployeeID = employeeId, WorkDate = today, StartTime = TimeSpan.FromHours(17), EndTime = TimeSpan.FromHours(19), TotalHours = 2, Status = "Approved", ApprovedBy = hrId });
        await db.SaveChangesAsync();
        var service = new PayrollService(db, TimeProvider.System);
        var summary = (await service.BuildAsync(today.Year, today.Month, employeeId: employeeId)).Single();
        Check(summary.AttendanceDays == 0 && summary.WorkingHours == 0 && summary.OvertimeHours == 0 && summary.PendingDays == 1, "Payroll counted unconfirmed checkout");
        reviewPage = await hr.GetStringAsync($"/Attendance/Details/{record.AttendanceID}");
        Check(Field(reviewPage, "Stage") == "CheckOut", "HR checkout form missing");
        await Send(hr, "Approve", reviewPage, record.AttendanceID, "CheckOut", version: staleVersion);
        Check((await Current()).CheckOutApprovalStatus == "Pending", "Stale form approved checkout");
        await Send(hr, "Approve", reviewPage, record.AttendanceID, "CheckOut");
        record = await Current();
        Check(record.ApprovalStatus == "Approved" && record.CheckOutApprovalStatus == "Approved" && record.ReviewedBy == hrId, "Checkout not confirmed independently");
        Check(record.CheckIn == requestedIn && record.CheckOut == requestedOut, "HR approval changed employee request times");
        Check(record.TotalHours == AttendanceRules.CalculateHours(requestedIn!.Value, requestedOut!.Value), "Confirmed hours incorrect");
        summary = (await service.BuildAsync(today.Year, today.Month, employeeId: employeeId)).Single();
        Check(summary.AttendanceDays == 1 && summary.OvertimeHours == 2 && summary.PendingDays == 0, "Completed confirmations not counted in payroll");
        await Send(employee, "CheckOut", page);
        Check((await Current()).CheckOut == requestedOut, "Duplicate checkout changed request time");

        // A legacy in+out request still pending must go through both decisions.
        var rejectedDay = historicalDate.AddDays(-1);
        var candidate = new Attendance { EmployeeID = employeeId, WorkDate = rejectedDay, CheckIn = rejectedDay.AddHours(8) };
        db.Attendances.Add(candidate);
        await db.SaveChangesAsync();
        var rejectPage = await hr.GetStringAsync($"/Attendance/Details/{candidate.AttendanceID}");
        await Send(hr, "Reject", rejectPage, candidate.AttendanceID, "CheckIn", "Không xác nhận có mặt.");
        var rejected = await db.Attendances.AsNoTracking().SingleAsync(a => a.AttendanceID == candidate.AttendanceID);
        Check(rejected.CheckInApprovalStatus == "Rejected" && rejected.ApprovalStatus == "Rejected" && rejected.TotalHours == 0 && rejected.CheckInReviewNote == "Không xác nhận có mặt.", "Check-in rejection not recorded");
        var prior = await db.Attendances.SingleAsync(a => a.EmployeeID == employeeId && a.WorkDate == historicalDate);
        prior.CheckOutApprovalStatus = "Pending"; prior.ApprovalStatus = "Pending"; prior.TotalHours = 0;
        await db.SaveChangesAsync();
        rejectPage = await hr.GetStringAsync($"/Attendance/Details/{prior.AttendanceID}");
        await Send(hr, "Reject", rejectPage, prior.AttendanceID, "CheckOut", "Giờ ra cần đối soát.");
        rejected = await db.Attendances.AsNoTracking().SingleAsync(a => a.AttendanceID == prior.AttendanceID);
        Check(rejected.CheckInApprovalStatus == "Approved" && rejected.CheckOutApprovalStatus == "Rejected" && rejected.ApprovalStatus == "Rejected" && rejected.TotalHours == 0, "Checkout rejection altered check-in or earned hours");
        return checks;
    }
}
