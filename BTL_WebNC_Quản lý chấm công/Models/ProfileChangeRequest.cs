using System.ComponentModel.DataAnnotations;

namespace BTL_WebNC_Quản_lý_chấm_công.Models
{
    // Nhân viên chỉ được đề nghị đổi thông tin liên hệ; HR/Admin duyệt mới ghi vào Employee.
    public class ProfileChangeRequest
    {
        public int ProfileChangeRequestID { get; set; }

        public int EmployeeID { get; set; }

        // Ảnh chụp dữ liệu tại thời điểm gửi yêu cầu, dùng để phát hiện HR đã sửa trong lúc chờ.
        [StringLength(15)]
        public string? OriginalPhone { get; set; }

        [StringLength(254)]
        public string? OriginalEmail { get; set; }

        [StringLength(255)]
        public string? OriginalAddress { get; set; }

        [StringLength(15)]
        public string? RequestedPhone { get; set; }

        [StringLength(254)]
        public string? RequestedEmail { get; set; }

        [StringLength(255)]
        public string? RequestedAddress { get; set; }

        [Required, StringLength(20)]
        public string Status { get; set; } = "Pending";

        [StringLength(500)]
        public string? ReviewNote { get; set; }

        public int? ReviewedBy { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? ReviewedAt { get; set; }

        public Employee? Employee { get; set; }

        public Employee? Reviewer { get; set; }
    }
}
