using System.ComponentModel.DataAnnotations;

namespace BTL_WebNC_Quản_lý_chấm_công.ViewModels;

public class LeaveCreateViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Vui lòng nhập loại nghỉ."), StringLength(30)]
    [Display(Name = "Loại nghỉ")]
    public string LeaveType { get; set; } = "Annual";

    [Required, DataType(DataType.Date), Display(Name = "Từ ngày")]
    public DateTime? StartDate { get; set; }

    [Required, DataType(DataType.Date), Display(Name = "Đến ngày")]
    public DateTime? EndDate { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập lý do."), StringLength(500)]
    [Display(Name = "Lý do")]
    public string Reason { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartDate.HasValue && EndDate.HasValue && EndDate.Value.Date < StartDate.Value.Date)
            yield return new ValidationResult("Ngày kết thúc phải từ ngày bắt đầu trở đi.", new[] { nameof(EndDate) });
        if (StartDate == DateTime.MinValue || EndDate == DateTime.MinValue)
            yield return new ValidationResult("Vui lòng chọn ngày hợp lệ.", new[] { nameof(StartDate), nameof(EndDate) });
        if (LeaveType is not ("Annual" or "Sick" or "Unpaid"))
            yield return new ValidationResult("Loại nghỉ không hợp lệ.", new[] { nameof(LeaveType) });
    }
}

public class OvertimeCreateViewModel : IValidatableObject
{
    [Required, DataType(DataType.Date), Display(Name = "Ngày tăng ca")]
    public DateTime? WorkDate { get; set; }

    [Required, DataType(DataType.Time), Display(Name = "Giờ bắt đầu")]
    public TimeSpan? StartTime { get; set; }

    [Required, DataType(DataType.Time), Display(Name = "Giờ kết thúc")]
    public TimeSpan? EndTime { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập lý do."), StringLength(500)]
    [Display(Name = "Lý do")]
    public string Reason { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (WorkDate == DateTime.MinValue)
            yield return new ValidationResult("Vui lòng chọn ngày hợp lệ.", new[] { nameof(WorkDate) });
        if (StartTime.HasValue && (StartTime < TimeSpan.Zero || StartTime >= TimeSpan.FromDays(1)))
            yield return new ValidationResult("Giờ bắt đầu không hợp lệ.", new[] { nameof(StartTime) });
        if (EndTime.HasValue && (EndTime < TimeSpan.Zero || EndTime >= TimeSpan.FromDays(1)))
            yield return new ValidationResult("Giờ kết thúc không hợp lệ.", new[] { nameof(EndTime) });
        if (StartTime.HasValue && EndTime.HasValue && EndTime <= StartTime)
            yield return new ValidationResult("Giờ kết thúc phải sau giờ bắt đầu trong cùng ngày.", new[] { nameof(EndTime) });
    }
}

public class LoginViewModel
{
    [Required, StringLength(50), Display(Name = "Tên đăng nhập")]
    public string Username { get; set; } = string.Empty;
    [Required, DataType(DataType.Password), Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;
    public string? ReturnUrl { get; set; }
}

public class RequestListViewModel<T>
{
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
    public string? Search { get; set; }
    public string? Status { get; set; }
    public string Sort { get; set; } = "date_desc";
    public int Page { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
}
