using System.ComponentModel.DataAnnotations;
using BTL_WebNC_Quản_lý_chấm_công.Models;

namespace BTL_WebNC_Quản_lý_chấm_công.ViewModels;

public class ProfileIndexViewModel
{
    public Employee Employee { get; set; } = null!;
    public IReadOnlyList<ProfileChangeRequest> Requests { get; set; } = Array.Empty<ProfileChangeRequest>();
    public ProfileChangeRequest? PendingRequest { get; set; }
}

public class ProfileContactRequestViewModel
{
    [StringLength(15), Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
    [Display(Name = "Số điện thoại")]
    public string? Phone { get; set; }

    [StringLength(254), EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [StringLength(255)]
    [Display(Name = "Địa chỉ")]
    public string? Address { get; set; }
}

public class ProfileRequestDetailsViewModel
{
    public ProfileChangeRequest Request { get; set; } = null!;
    public bool HasConflict { get; set; }
    public string? CurrentPhone { get; set; }
    public string? CurrentEmail { get; set; }
    public string? CurrentAddress { get; set; }
}
