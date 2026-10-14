
using System.ComponentModel.DataAnnotations;

namespace BTL_WebNC_Quản_lý_chấm_công.Models
{
    public class Leave
    {
        public int LeaveID { get; set; }

        public int EmployeeID { get; set; }

        [Required, StringLength(30)]
        public string LeaveType { get; set; } = string.Empty;

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        [StringLength(500)]
        public string? Reason { get; set; }

        [Required, StringLength(20)]
        public string Status { get; set; } = "Pending";

        public int? ApprovedBy { get; set; }

        public Employee? Employee { get; set; }

        public Employee? Approver { get; set; }
    }
}
