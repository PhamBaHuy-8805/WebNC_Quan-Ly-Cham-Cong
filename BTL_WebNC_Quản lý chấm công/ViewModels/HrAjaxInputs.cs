using System;
using System.ComponentModel.DataAnnotations;

namespace BTL_WebNC_Quản_lý_chấm_công.ViewModels
{
    // Chỉ nhận các thuộc tính người dùng được phép nhập, tránh overposting Entity.
    public class EmployeeAjaxCreateInput
    {
        [Required(ErrorMessage = "Vui lòng nhập mã nhân viên.")]
        [StringLength(20)]
        public string EmployeeCode { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
        [StringLength(100)]
        public string FullName { get; set; } = "";

        public DateTime? DateOfBirth { get; set; }
        [StringLength(10)] public string? Gender { get; set; }
        [StringLength(15)] public string? Phone { get; set; }
        [EmailAddress] [StringLength(254)] public string? Email { get; set; }
        [StringLength(255)] public string? Address { get; set; }
        public int? DepartmentID { get; set; }
        public int? PositionID { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày vào làm.")]
        public DateTime? HireDate { get; set; }

        [Range(typeof(decimal), "0", "9999999999999999", ErrorMessage = "Lương phải lớn hơn hoặc bằng 0.")]
        public decimal Salary { get; set; }

        [Required] [RegularExpression("^(Active|Inactive)$")]
        public string Status { get; set; } = "Active";
    }

    public class DepartmentAjaxCreateInput
    {
        [Required(ErrorMessage = "Vui lòng nhập tên phòng ban.")]
        [StringLength(100)] public string DepartmentName { get; set; } = "";
        [StringLength(255)] public string? Description { get; set; }
        [Required] [RegularExpression("^(Active|Inactive)$")]
        public string Status { get; set; } = "Active";
    }

    public class PositionAjaxCreateInput
    {
        [Required(ErrorMessage = "Vui lòng nhập tên chức vụ.")]
        [StringLength(100)] public string PositionName { get; set; } = "";
        [StringLength(255)] public string? Description { get; set; }
        [Required] [RegularExpression("^(Active|Inactive)$")]
        public string Status { get; set; } = "Active";
    }
}
