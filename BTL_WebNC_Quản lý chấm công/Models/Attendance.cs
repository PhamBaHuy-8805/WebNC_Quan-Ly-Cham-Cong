
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BTL_WebNC_Quản_lý_chấm_công.Models
{
    public class Attendance
    {
        public int AttendanceID { get; set; }

        public int EmployeeID { get; set; }

        public DateTime WorkDate { get; set; }

        public DateTime? CheckIn { get; set; }

        public DateTime? CheckOut { get; set; }

        [Column(TypeName = "decimal(6,2)")]
        public decimal TotalHours { get; set; }

        [Required, StringLength(20)]
        public string Status { get; set; } = "Absent";

        [StringLength(500)]
        public string? Note { get; set; }

        public Employee? Employee { get; set; }
    }
}
