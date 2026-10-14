
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BTL_WebNC_Quản_lý_chấm_công.Models
{
    public class Employee
    {
        public int EmployeeID { get; set; }

        [Required, StringLength(20)]
        public string EmployeeCode { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        public DateTime? DateOfBirth { get; set; }

        [StringLength(10)]
        public string? Gender { get; set; }

        [StringLength(15)]
        public string? Phone { get; set; }

        [StringLength(254)]
        public string? Email { get; set; }

        [StringLength(255)]
        public string? Address { get; set; }

        public int? DepartmentID { get; set; }

        public int? PositionID { get; set; }

        public DateTime HireDate { get; set; } = DateTime.Today;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Salary { get; set; }

        [Required, StringLength(20)]
        public string Status { get; set; } = "Active";

        public Department? Department { get; set; }

        public Position? Position { get; set; }

        public Account? Account { get; set; }

        public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

        public ICollection<Leave> Leaves { get; set; } = new List<Leave>();

        public ICollection<Overtime> Overtimes { get; set; } = new List<Overtime>();

        public ICollection<Payroll> Payrolls { get; set; } = new List<Payroll>();
    }
}
