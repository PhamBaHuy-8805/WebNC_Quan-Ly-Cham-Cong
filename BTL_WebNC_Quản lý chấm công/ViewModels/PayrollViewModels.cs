using System.ComponentModel.DataAnnotations;
using BTL_WebNC_Quản_lý_chấm_công.Models;
using BTL_WebNC_Quản_lý_chấm_công.Services;

namespace BTL_WebNC_Quản_lý_chấm_công.ViewModels;

public class PayrollIndexViewModel
{
    public bool IsReviewer { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public int? DepartmentId { get; set; }
    public string? Search { get; set; }
    public string? Status { get; set; }
    public IReadOnlyList<Payroll> Items { get; set; } = Array.Empty<Payroll>();
    public IReadOnlyList<(int Id, string Name)> Departments { get; set; } = Array.Empty<(int, string)>();
    public int Page { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    public decimal TotalSalary { get; set; }
    public int DraftCount { get; set; }
}

public class PayrollSummaryViewModel
{
    public int Month { get; set; }
    public int Year { get; set; }
    public int? DepartmentId { get; set; }
    public string? Search { get; set; }
    public IReadOnlyList<(int Id, string Name)> Departments { get; set; } = Array.Empty<(int, string)>();
}

public class PayrollDetailsViewModel
{
    public Payroll Payroll { get; set; } = null!;
    public int StandardDays { get; set; }
    public decimal DailyRate { get; set; }
    public decimal BasePay { get; set; }
    public decimal OvertimePay { get; set; }
    public bool CanReview { get; set; }
    public bool CanApprove { get; set; }
}

public class PayrollAdjustViewModel
{
    public int PayrollID { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;

    [Range(0, 1_000_000_000, ErrorMessage = "Phụ cấp phải từ 0 đến 1.000.000.000.")]
    [Display(Name = "Phụ cấp / thưởng (VNĐ)")]
    public decimal Allowance { get; set; }

    [Range(0, 1_000_000_000, ErrorMessage = "Khấu trừ phải từ 0 đến 1.000.000.000.")]
    [Display(Name = "Khấu trừ (VNĐ)")]
    public decimal Deduction { get; set; }
}

public class ReportPageViewModel
{
    public string Type { get; set; } = "attendance";
    public int Month { get; set; }
    public int Year { get; set; }
    public int? DepartmentId { get; set; }
    public IReadOnlyList<(int Id, string Name)> Departments { get; set; } = Array.Empty<(int, string)>();
}
