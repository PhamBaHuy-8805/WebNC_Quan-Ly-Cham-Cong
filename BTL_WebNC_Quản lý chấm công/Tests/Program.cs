using BTL_WebNC_Quản_lý_chấm_công.Models;
using BTL_WebNC_Quản_lý_chấm_công.Services;
using Microsoft.AspNetCore.Identity;

var checks = 0;
void Equal<T>(T expected, T actual, string name)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"{name}: expected {expected}, got {actual}");
    checks++;
}
var date = new DateTime(2026, 10, 5);
Equal("Present", AttendanceRules.WorkStatus(date.AddHours(8), date.AddHours(17)), "Team shift ends 17:00");
Equal("EarlyLeave", AttendanceRules.WorkStatus(date.AddHours(8), date.AddHours(16)), "16:00 early under team rules");
Equal("Late", AttendanceRules.WorkStatus(date.AddHours(10), date.AddHours(17)), "Two hours late is no longer absent");
Equal("LateEarly", AttendanceRules.WorkStatus(date.AddHours(9), date.AddHours(16)), "Late and early flags");
Equal(8m, AttendanceRules.CalculateHours(date.AddHours(8), date.AddHours(17)), "Eight hour shift after lunch");
Equal(22, PayrollRules.StandardDays(2026, 10), "October calendar");
Equal(700_000m, PayrollRules.DailyRate(15_400_000m, 22), "Daily rate");
Equal(15_400_000m, PayrollRules.BasePay(15_400_000m, 22, 22), "Full month pay");
Equal(14_700_000m, PayrollRules.BasePay(15_400_000m, 22, 21), "One missing day");
Equal(15_400_000m, PayrollRules.BasePay(15_400_000m, 22, 23), "Cap ordinary pay");
Equal(50_000m, PayrollRules.AutoDeduction(1, 0), "Flat late penalty");
Equal(100_000m, PayrollRules.AutoDeduction(1, 1), "Separate late and early penalties");
Equal(262_500m, PayrollRules.OvertimePay(15_400_000m, 22, 2), "Overtime divides daily rate by eight");
Equal(15_612_500m, PayrollRules.Total(15_400_000m, 22, 22, 2, 0, 50_000), "Team total formula");
Equal(0m, PayrollRules.Total(15_400_000m, 22, 0, 0, 0, 50_000), "Nonnegative net pay");
Equal(true, PayrollRules.IsPaidLeave("Annual"), "Annual leave paid");
Equal(true, PayrollRules.IsPaidLeave("Sick"), "Sick leave paid under team policy");
Equal(false, PayrollRules.IsPaidLeave("Unpaid"), "Unpaid leave");
Equal((1, 1), PayrollRules.LateEarly("LateAndEarly"), "Legacy late early label");
var account = new Account();
var hasher = new ExistingAccountPasswordHasher();
Equal(PasswordVerificationResult.SuccessRehashNeeded, hasher.VerifyHashedPassword(account, "123456", "123456"), "Existing practice account login is upgraded");
Equal(PasswordVerificationResult.Failed, hasher.VerifyHashedPassword(account, "123456", "incorrect"), "Wrong practice password");
var hash = hasher.HashPassword(account, "test-password");
Equal(PasswordVerificationResult.Success, hasher.VerifyHashedPassword(account, hash, "test-password"), "Identity hash login");
Equal(PasswordVerificationResult.Failed, hasher.VerifyHashedPassword(account, hash, hash), "Stored hash cannot be used as password");
Console.WriteLine($"PASS: {checks} team payroll, attendance and login checks.");
if (Environment.GetEnvironmentVariable("PAYROLL_TEST_CONNECTION") is { Length: > 0 } connection)
    await IntegrationChecks.Run(connection, Environment.GetEnvironmentVariable("PAYROLL_TEST_URL") ?? "http://127.0.0.1:5286");
