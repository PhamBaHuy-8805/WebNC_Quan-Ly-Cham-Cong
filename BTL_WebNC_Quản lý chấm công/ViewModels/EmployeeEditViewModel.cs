
using System.ComponentModel.DataAnnotations;

namespace BTL_WebNC_Quản_lý_chấm_công.ViewModels;

public class EmployeeEditViewModel
{
    public int EmployeeID { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mã nhân viên.")]
    [StringLength(20)]
    public string EmployeeCode { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    [StringLength(100)]
    public string FullName { get; set; } = "";

    [DataType(DataType.Date)]
    public DateTime? DateOfBirth { get; set; }

    public string? Gender { get; set; }

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    public string? Email { get; set; }

    public string? Address { get; set; }

    public int? DepartmentID { get; set; }

    public int? PositionID { get; set; }

    [DataType(DataType.Date)]
    public DateTime HireDate { get; set; }

    [Range(typeof(decimal), "0", "999999999999")]
    public decimal Salary { get; set; }

    [Required]
    [RegularExpression("^(Active|Inactive|Resigned)$",
        ErrorMessage = "Trạng thái không hợp lệ.")]
    public string Status { get; set; } = "Active";
}
