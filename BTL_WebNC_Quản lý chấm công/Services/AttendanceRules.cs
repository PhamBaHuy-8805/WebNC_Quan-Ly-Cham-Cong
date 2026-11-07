namespace BTL_WebNC_Quản_lý_chấm_công.Services;

// Ca mặc định 08:00–16:00, nghỉ trưa 12:00–13:00, giờ Việt Nam.
// Chỉ trừ phần nghỉ trưa thực sự giao với khoảng check-in/check-out.
public static class AttendanceRules
{
    public const int StartHour = 8;
    public const int EndHour = 16;
    public const int AbsentAfterMinutes = 120;
    public const decimal StandardHours = 7m;
    private static readonly TimeZoneInfo Vietnam = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
    public static DateTime LocalNow(TimeProvider clock) => TimeZoneInfo.ConvertTime(clock.GetUtcNow(), Vietnam).DateTime;

    public static decimal CalculateHours(DateTime checkIn, DateTime checkOut)
    {
        if (checkOut < checkIn || checkOut.Date != checkIn.Date)
            throw new ArgumentException("Giờ ra phải từ giờ vào trở đi và trong cùng ngày.");
        var lunchStart = checkIn.Date.AddHours(12);
        var lunchEnd = checkIn.Date.AddHours(13);
        var overlapStart = checkIn > lunchStart ? checkIn : lunchStart;
        var overlapEnd = checkOut < lunchEnd ? checkOut : lunchEnd;
        var breakTime = overlapEnd > overlapStart ? overlapEnd - overlapStart : TimeSpan.Zero;
        return decimal.Round((decimal)(checkOut - checkIn - breakTime).TotalHours, 2);
    }

    public static string WorkStatus(DateTime checkIn, DateTime? checkOut)
    {
        if (checkIn.TimeOfDay >= TimeSpan.FromHours(StartHour).Add(TimeSpan.FromMinutes(AbsentAfterMinutes)))
            return "Absent";
        var late = checkIn.TimeOfDay > TimeSpan.FromHours(8);
        var early = checkOut.HasValue && checkOut.Value.TimeOfDay < TimeSpan.FromHours(EndHour);
        return (late, early) switch
        {
            (true, true) => "LateEarly",
            (true, false) => "Late",
            (false, true) => "EarlyLeave",
            _ => "Present"
        };
    }

    public static decimal PayableHours(DateTime checkIn, DateTime checkOut)
    {
        if (checkOut.Date != checkIn.Date || checkOut <= checkIn || WorkStatus(checkIn, checkOut) == "Absent") return 0;
        var start = checkIn > checkIn.Date.AddHours(StartHour) ? checkIn : checkIn.Date.AddHours(StartHour);
        var end = checkOut < checkIn.Date.AddHours(EndHour) ? checkOut : checkIn.Date.AddHours(EndHour);
        return end > start ? CalculateHours(start, end) : 0;
    }
}
