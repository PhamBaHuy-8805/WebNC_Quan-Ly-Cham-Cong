using System.ComponentModel.DataAnnotations;

namespace BTL_WebNC_Quản_lý_chấm_công.ViewModels;

public class AccountCreateViewModel
{
    [Required, StringLength(50), Display(Name = "Tên đăng nhập")]
    public string Username { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 6), DataType(DataType.Password), Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    [Required, Range(1, int.MaxValue), Display(Name = "Vai trò")]
    public int RoleID { get; set; }

    [Required, Range(1, int.MaxValue), Display(Name = "Nhân viên")]
    public int EmployeeID { get; set; }

    [Required, RegularExpression("^(Active|Locked)$"), Display(Name = "Trạng thái")]
    public string Status { get; set; } = "Active";
}

public class AccountEditViewModel
{
    public int UserID { get; set; }

    [Required, StringLength(50), Display(Name = "Tên đăng nhập")]
    public string Username { get; set; } = string.Empty;

    [StringLength(100, MinimumLength = 6), DataType(DataType.Password), Display(Name = "Mật khẩu mới")]
    public string? NewPassword { get; set; }

    [Required, Range(1, int.MaxValue), Display(Name = "Vai trò")]
    public int RoleID { get; set; }

    [Display(Name = "Nhân viên")]
    public int? EmployeeID { get; set; }

    [Required, RegularExpression("^(Active|Locked)$"), Display(Name = "Trạng thái")]
    public string Status { get; set; } = "Active";
}

public class ChangePasswordViewModel
{
    [Required, DataType(DataType.Password), Display(Name = "Mật khẩu hiện tại")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 6), DataType(DataType.Password), Display(Name = "Mật khẩu mới")]
    public string NewPassword { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    [Display(Name = "Xác nhận mật khẩu mới")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
