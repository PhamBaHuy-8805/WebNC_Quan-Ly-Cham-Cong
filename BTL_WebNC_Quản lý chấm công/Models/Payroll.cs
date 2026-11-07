
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BTL_WebNC_Quản_lý_chấm_công.Models
{
    public class Payroll
    {
        public int PayrollID { get; set; }

        public int EmployeeID { get; set; }

        [Range(1, 12)]
        public byte Month { get; set; }

        [Range(2000, 2100)]
        public short Year { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal BaseSalary { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal WorkingDays { get; set; }

        [Column(TypeName = "decimal(7,2)")]
        public decimal WorkingHours { get; set; }

        [Column(TypeName = "decimal(7,2)")]
        public decimal OvertimeHours { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Allowance { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Deduction { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalSalary { get; set; }

        [Required, StringLength(20)]
        public string Status { get; set; } = "Draft";

        public int StandardDays { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal OvertimePay { get; set; }
        [Column(TypeName = "decimal(5,2)")]
        public decimal OvertimeMultiplier { get; set; }
        public string CalculationDetails { get; set; } = "[]";
        public DateTime? CalculatedAt { get; set; }
        public int? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public Employee? Employee { get; set; }
    }
}
