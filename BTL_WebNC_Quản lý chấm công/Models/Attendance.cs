
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BTL_WebNC_Quản_lý_chấm_công.Models
{
    public class Attendance
    {
        public int AttendanceID { get; set; }

        public int EmployeeID { get; set; }

        [Column(TypeName = "date")]
        public DateTime WorkDate { get; set; }

        public DateTime? CheckIn { get; set; }

        public DateTime? CheckOut { get; set; }

        [Column(TypeName = "decimal(6,2)")]
        public decimal TotalHours { get; set; }

        [Required, StringLength(20)]
        public string Status { get; set; } = "Absent";

        [StringLength(500)]
        public string? Note { get; set; }

        // Liên kết với nhân viên chấm công
        public Employee? Employee { get; set; }

        // Trạng thái phê duyệt
        [Required, StringLength(20)]
        public string ApprovalStatus { get; set; } = "Pending";

        [Required, StringLength(20)]
        public string CheckInApprovalStatus { get; set; } = "Pending";

        [Required, StringLength(20)]
        public string CheckOutApprovalStatus { get; set; } = "Pending";

        public int? CheckInReviewedBy { get; set; }
        public Employee? CheckInReviewer { get; set; }
        public DateTime? CheckInReviewedAt { get; set; }
        [StringLength(500)]
        public string? CheckInReviewNote { get; set; }

        // ID nhân viên phê duyệt
        public int? ReviewedBy { get; set; }

        // Liên kết với nhân viên phê duyệt
        public Employee? Reviewer { get; set; }

        // Thời gian phê duyệt
        public DateTime? ReviewedAt { get; set; }

        // Ghi chú phê duyệt
        [StringLength(500)]
        public string? ReviewNote { get; set; }

    }
}
