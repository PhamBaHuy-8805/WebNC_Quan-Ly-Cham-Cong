using System.Net;
using System.Text.RegularExpressions;
using BTL_WebNC_Quản_lý_chấm_công.Data;
using BTL_WebNC_Quản_lý_chấm_công.Models;
using BTL_WebNC_Quản_lý_chấm_công.Services;
using Microsoft.EntityFrameworkCore;

static class IntegrationChecks
{
    public static async Task Run(string connection, string url)
    {
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options);
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var password = Guid.NewGuid().ToString("N");
        var roles = await db.Roles.Where(r => r.RoleName == "HR" || r.RoleName == "Employee").ToDictionaryAsync(r => r.RoleName);
        var period = AttendanceRules.LocalNow(TimeProvider.System).AddMonths(-1);
        var from = new DateTime(period.Year, period.Month, 1);
        var workday = Enumerable.Range(0, 7).Select(i => from.AddDays(i)).First(d => d.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday));
        var hr = new Employee { EmployeeCode = "TESTHR" + suffix, FullName = "Payroll test HR", Salary = 15_400_000, HireDate = from.AddYears(-1) };
        var employee = new Employee { EmployeeCode = "TESTNV" + suffix, FullName = "Payroll test employee", Salary = 15_400_000, HireDate = from.AddYears(-1) };
        var fixtures = new[] { hr, employee };
        db.Employees.AddRange(fixtures);
        await db.SaveChangesAsync();
        try
        {
            db.Accounts.AddRange(new Account { Username = "payroll.hr." + suffix, PasswordHash = password, EmployeeID = hr.EmployeeID, RoleID = roles["HR"].RoleID },
                new Account { Username = "payroll.nv." + suffix, PasswordHash = password, EmployeeID = employee.EmployeeID, RoleID = roles["Employee"].RoleID });
            db.Attendances.Add(new Attendance { EmployeeID = employee.EmployeeID, WorkDate = workday, CheckIn = workday.AddHours(8), CheckOut = workday.AddHours(17), TotalHours = 8, ApprovalStatus = "Approved", CheckInApprovalStatus = "Approved", CheckOutApprovalStatus = "Approved", Status = "Present" });
            await db.SaveChangesAsync();
            var payroll = new Payroll { EmployeeID = employee.EmployeeID, Month = (byte)period.Month, Year = (short)period.Year };
            var service = new PayrollService(db, TimeProvider.System);
            var summary = (await service.BuildAsync(period.Year, period.Month, employeeId: employee.EmployeeID)).Single();
            payroll.BaseSalary = summary.BaseSalary;
            payroll.WorkingDays = summary.WorkingDays;
            payroll.WorkingHours = summary.WorkingHours;
            payroll.OvertimeHours = summary.OvertimeHours;
            payroll.TotalSalary = PayrollRules.Total(summary.BaseSalary, summary.StandardDays, summary.WorkingDays, summary.OvertimeHours, 0, 0);
            db.Payrolls.Add(payroll);
            var ownPayroll = new Payroll { EmployeeID = hr.EmployeeID, Month = (byte)period.Month, Year = (short)period.Year };
            db.Payrolls.Add(ownPayroll);
            await db.SaveChangesAsync();
            var checks = 0;
            void Check(bool value, string name) { if (!value) throw new Exception(name); checks++; }
            async Task<Payroll> Reload() => await db.Payrolls.AsNoTracking().SingleAsync(p => p.PayrollID == payroll.PayrollID);
            static string Token(string html, string name) => WebUtility.HtmlDecode(Regex.Match(html, "name=\"" + name + "\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
            async Task<HttpClient> Login(string username)
            {
                var client = new HttpClient(new HttpClientHandler { CookieContainer = new CookieContainer() }) { BaseAddress = new Uri(url) };
                var login = await client.GetStringAsync("/Account/Login");
                var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
                { ["Username"] = username, ["Password"] = password, ["__RequestVerificationToken"] = Token(login, "__RequestVerificationToken") }));
                Check(response.RequestMessage!.RequestUri!.AbsolutePath == "/Attendance", "Fixture login failed");
                return client;
            }
            using var hrClient = await Login("payroll.hr." + suffix);
            using var employeeClient = await Login("payroll.nv." + suffix);
            var hrPage = await hrClient.GetStringAsync($"/Payroll?month={period.Month}&year={period.Year}");
            Check(hrPage.Contains("/Payroll/Generate"), "HR generation controls missing");
            Check((await employeeClient.GetAsync($"/Payroll/Details/{payroll.PayrollID}")).StatusCode == HttpStatusCode.NotFound, "Draft visible to employee");
            var details = await hrClient.GetStringAsync($"/Payroll/Details/{payroll.PayrollID}");
            var csrf = Token(details, "__RequestVerificationToken");
            var rowVersion = Token(details, "RowVersion");
            Check(csrf.Length > 0, "Payroll forms not rendered");
            async Task<HttpResponseMessage> Post(HttpClient client, string action, int id, string version, bool confirm = true, string? token = null) =>
                await client.PostAsync($"/Payroll/{action}/{id}", new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["__RequestVerificationToken"] = token ?? csrf, ["RowVersion"] = version,
                    ["ConfirmAbsences"] = confirm ? "true" : "false"
                }));
            var legacyDetails = await hrClient.GetStringAsync($"/Payroll/Details/{ownPayroll.PayrollID}");
            Check((await Post(hrClient, "Approve", ownPayroll.PayrollID, Token(legacyDetails, "RowVersion"), token: Token(legacyDetails, "__RequestVerificationToken"))).StatusCode == HttpStatusCode.Forbidden, "Self approval allowed");
            await Post(hrClient, "Approve", payroll.PayrollID, rowVersion);
            Check((await Reload()).Status == "Approved", "Approval failed");
            Check((await employeeClient.GetAsync($"/Payroll/Details/{payroll.PayrollID}")).StatusCode == HttpStatusCode.OK, "Own approved payslip hidden");
            Check((await employeeClient.GetAsync($"/Payroll/Details/{ownPayroll.PayrollID}")).StatusCode == HttpStatusCode.NotFound, "Other employee payslip exposed");
            var employeePage = await employeeClient.GetStringAsync("/Attendance");
            var denied = await employeeClient.PostAsync("/Payroll/Generate", new FormUrlEncodedContent(new Dictionary<string, string>
            { ["Month"] = period.Month.ToString(), ["Year"] = period.Year.ToString(), ["__RequestVerificationToken"] = Token(employeePage, "__RequestVerificationToken") }));
            Check(denied.StatusCode == HttpStatusCode.Forbidden, "Employee generation allowed");
            details = await hrClient.GetStringAsync($"/Payroll/Details/{payroll.PayrollID}");
            await Post(hrClient, "MarkPaid", payroll.PayrollID, Token(details, "RowVersion"), token: Token(details, "__RequestVerificationToken"));
            Check((await Reload()).Status == "Paid", "Payment recording failed");
            Check((await hrClient.GetAsync("/Payroll/Summary")).StatusCode == HttpStatusCode.OK, "Summary page failed");
            Check((await hrClient.GetAsync($"/Payroll/SummaryData?month={period.Month}&year={period.Year}")).StatusCode == HttpStatusCode.OK, "Summary data failed");
            Check((await hrClient.GetAsync("/Report")).StatusCode == HttpStatusCode.OK, "Report page failed");
            foreach (var type in ReportService.Types)
                Check((await hrClient.GetAsync($"/Report/Data?type={type}&month={period.Month}&year={period.Year}")).StatusCode == HttpStatusCode.OK, $"Report {type} failed");
            Check((await employeeClient.GetAsync("/Report")).StatusCode == HttpStatusCode.Forbidden, "Employee can access reports");
            foreach (var path in new[] { "/Employees", "/Departments", "/Positions", "/ajax/employees/list", "/ajax/departments/list", "/ajax/positions/list" })
                Check((await hrClient.GetAsync(path)).StatusCode == HttpStatusCode.OK, $"Team HR route failed: {path}");
            Check((await employeeClient.GetAsync("/Employees")).StatusCode == HttpStatusCode.Forbidden, "Employee can access HR module");
            var frozen = await Reload();
            var paidSnapshot = frozen.TotalSalary;
            var recalc = await Post(hrClient, "Adjust", payroll.PayrollID, Convert.ToBase64String(frozen.RowVersion));
            Check((await Reload()).TotalSalary == paidSnapshot && (await Reload()).Status == "Paid", "Paid payroll was recalculated");
            checks += await AttendanceChecks.Run(db, hrClient, employeeClient, employee.EmployeeID, hr.EmployeeID, workday);
            Console.WriteLine($"PASS: {checks} SQL/HTTP integration checks (fixtures removed afterwards).");
        }
        finally
        {
            db.ChangeTracker.Clear();
            var ids = fixtures.Select(e => e.EmployeeID).ToArray();
            await db.Accounts.Where(a => a.EmployeeID.HasValue && ids.Contains(a.EmployeeID.Value)).ExecuteDeleteAsync();
            await db.Payrolls.Where(p => ids.Contains(p.EmployeeID)).ExecuteDeleteAsync();
            await db.OverTimes.Where(o => ids.Contains(o.EmployeeID)).ExecuteDeleteAsync();
            await db.Attendances.Where(a => ids.Contains(a.EmployeeID)).ExecuteDeleteAsync();
            await db.Employees.Where(e => ids.Contains(e.EmployeeID)).ExecuteDeleteAsync();
        }
    }

}
