
using System.ComponentModel.DataAnnotations;

namespace BTL_WebNC_Quản_lý_chấm_công.Models
{
    public class Role
    {
        public int RoleID { get; set; }

        [Required, StringLength(30)]
        public string RoleName { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Description { get; set; }

        public ICollection<Account> Accounts { get; set; } = new List<Account>();
    }
}
