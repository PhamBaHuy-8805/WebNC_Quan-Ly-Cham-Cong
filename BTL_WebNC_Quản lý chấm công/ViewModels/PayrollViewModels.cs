using System.ComponentModel.DataAnnotations;
using BTL_WebNC_Quản_lý_chấm_công.Models;

namespace BTL_WebNC_Quản_lý_chấm_công.ViewModels;

public class PayrollIndexViewModel
{
    public int Month { get; set; }
    public int Year { get; set; }
    public bool IsReviewer { get; set; }
    public List<Payroll> Items { get; set; } = [];
}

public class PayrollGenerateViewModel
{
    [Range(1, 12)] public int Month { get; set; }
    [Range(2000, 2100)] public int Year { get; set; }
}

public class PayrollActionViewModel
{
    [Required] public string RowVersion { get; set; } = "";
    [Range(typeof(decimal), "0", "1000000000")] public decimal Allowance { get; set; }
    public bool ConfirmAbsences { get; set; }
}
