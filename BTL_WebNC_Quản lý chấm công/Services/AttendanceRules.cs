namespace BTL_WebNC_Quản_lý_chấm_công.Services;

// Quy ước demo: ca 08:00–17:00, nghỉ trưa 12:00–13:00, giờ Việt Nam.
// Chỉ trừ phần nghỉ trưa thực sự giao với khoảng check-in/check-out.
public static class AttendanceRules
{
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
        var late = checkIn.TimeOfDay > TimeSpan.FromHours(8);
        var early = checkOut.HasValue && checkOut.Value.TimeOfDay < TimeSpan.FromHours(17);
        return (late, early) switch
        {
            (true, true) => "LateEarly",
            (true, false) => "Late",
            (false, true) => "EarlyLeave",
            _ => "Present"
        };
    }
}
