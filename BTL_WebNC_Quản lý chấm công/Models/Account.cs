
using System.ComponentModel.DataAnnotations;

namespace BTL_WebNC_Quản_lý_chấm_công.Models
{
    public class Account
    {
        [Key]
        public int UserID { get; set; }

        [Required, StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required, StringLength(255)]
        // Giữ tên cột cũ; bản thực hành hiện lưu mật khẩu thường theo yêu cầu.
        public string PasswordHash { get; set; } = string.Empty;

        public int RoleID { get; set; }

        public int? EmployeeID { get; set; }

        [Required, StringLength(20)]
        public string Status { get; set; } = "Active";

        public Role? Role { get; set; }

        public Employee? Employee { get; set; }
    }
}
