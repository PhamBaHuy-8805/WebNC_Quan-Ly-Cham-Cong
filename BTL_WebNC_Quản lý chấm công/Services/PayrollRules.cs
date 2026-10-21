namespace BTL_WebNC_Quản_lý_chấm_công.Services;

// Quy ước tính lương (Module 4). Mọi hằng số nằm ở đây để dễ giải thích và chỉnh sửa.
//  - Ngày công chuẩn của tháng = số ngày từ thứ Hai đến thứ Sáu.
//  - Lương ngày = lương cơ bản / ngày công chuẩn; lương giờ = lương ngày / 8.
//  - 1 bản chấm công đã được HR xác nhận (Approved) và có đủ giờ vào/ra = 1 ngày công.
//  - Nghỉ phép được duyệt loại Annual/Sick được tính là ngày công; Unpaid thì không.
//  - Tăng ca được duyệt tính hệ số 1,5 (chỉ tiền lương giờ, không cộng vào ngày công).
//  - Mỗi lần đi muộn hoặc về sớm bị trừ một khoản cố định.
public static class PayrollRules
{
    public const decimal HoursPerDay = 8m;
    public const decimal OvertimeMultiplier = 1.5m;
    public const decimal PenaltyPerLateOrEarly = 50_000m;

    public static bool IsPaidLeave(string leaveType) => leaveType is "Annual" or "Sick";

    public static bool IsWeekday(DateTime date) => date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);

    public static int CountWeekdays(DateTime from, DateTime to)
    {
        var count = 0;
        for (var d = from.Date; d <= to.Date; d = d.AddDays(1))
            if (IsWeekday(d)) count++;
        return count;
    }

    public static int StandardDays(int year, int month)
    {
        var start = new DateTime(year, month, 1);
        return CountWeekdays(start, start.AddMonths(1).AddDays(-1));
    }

    // Trả về (số lần đi muộn, số lần về sớm) từ trạng thái chấm công.
    // Chấp nhận cả "LateEarly" (AttendanceRules) lẫn "LateAndEarly" (dữ liệu seed cũ).
    public static (int Late, int Early) LateEarly(string status) => status switch
    {
        "Late" => (1, 0),
        "EarlyLeave" => (0, 1),
        "LateEarly" or "LateAndEarly" => (1, 1),
        _ => (0, 0)
    };

    private static decimal Round0(decimal value) => decimal.Round(value, 0, MidpointRounding.AwayFromZero);

    public static decimal DailyRate(decimal baseSalary, int standardDays)
        => standardDays <= 0 ? 0m : baseSalary / standardDays;

    public static decimal BasePay(decimal baseSalary, int standardDays, decimal workingDays)
        => Round0(DailyRate(baseSalary, standardDays) * Math.Min(Math.Max(workingDays, 0m), standardDays));

    public static decimal OvertimePay(decimal baseSalary, int standardDays, decimal overtimeHours)
        => Round0(DailyRate(baseSalary, standardDays) / HoursPerDay * OvertimeMultiplier * Math.Max(overtimeHours, 0m));

    public static decimal AutoDeduction(int lateCount, int earlyCount)
        => (lateCount + earlyCount) * PenaltyPerLateOrEarly;

    public static decimal Total(decimal baseSalary, int standardDays, decimal workingDays,
        decimal overtimeHours, decimal allowance, decimal deduction)
        => Math.Max(0m, BasePay(baseSalary, standardDays, workingDays)
            + OvertimePay(baseSalary, standardDays, overtimeHours) + allowance - deduction);
}
