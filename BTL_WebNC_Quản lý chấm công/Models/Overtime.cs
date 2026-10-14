
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BTL_WebNC_Quản_lý_chấm_công.Models
{
    public class Overtime
    {
        public int OvertimeID { get; set; }

        public int EmployeeID { get; set; }

        public DateTime WorkDate { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        [Column(TypeName = "decimal(6,2)")]
        public decimal TotalHours { get; set; }

        [StringLength(500)]
        public string? Reason { get; set; }

        [Required, StringLength(20)]
        public string Status { get; set; } = "Pending";

        public int? ApprovedBy { get; set; }

        public Employee? Employee { get; set; }

        public Employee? Approver { get; set; }
    }
}
