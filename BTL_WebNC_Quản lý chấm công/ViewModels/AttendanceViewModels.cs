using System.ComponentModel.DataAnnotations;
using BTL_WebNC_Quản_lý_chấm_công.Models;

namespace BTL_WebNC_Quản_lý_chấm_công.ViewModels;

public class AttendanceIndexViewModel
{
    public DateTime Today { get; set; }
    public Attendance? TodayAttendance { get; set; }
    public IReadOnlyList<Attendance> Items { get; set; } = Array.Empty<Attendance>();
    public bool IsReviewer { get; set; }
    public string? Search { get; set; }
    public string? ApprovalStatus { get; set; }
    [DataType(DataType.Date)]
    public DateTime? Date { get; set; }
    public int Page { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
}

public class AttendanceReviewViewModel
{
    [Required]
    public string RowVersion { get; set; } = string.Empty;
    [StringLength(500, ErrorMessage = "Ghi chú tối đa 500 ký tự.")]
    public string? ReviewNote { get; set; }
}
