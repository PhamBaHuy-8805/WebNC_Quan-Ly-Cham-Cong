
using System.ComponentModel.DataAnnotations;

namespace BTL_WebNC_Quản_lý_chấm_công.Models
{
    public class Position
    {
        public int PositionID { get; set; }

        [Required, StringLength(100)]
        public string PositionName { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Description { get; set; }

        [Required, StringLength(20)]
        public string Status { get; set; } = "Active";

        public ICollection<Employee> Employees { get; set; } = new List<Employee>();
    }
}
