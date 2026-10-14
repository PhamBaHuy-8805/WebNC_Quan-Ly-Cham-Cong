using Microsoft.EntityFrameworkCore;
using BTL_WebNC_Quản_lý_chấm_công.Models;

namespace BTL_WebNC_Quản_lý_chấm_công.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Role> Roles { get; set; }

        public DbSet<Department> Departments { get; set; }

        public DbSet<Position> Positions { get; set; }

        public DbSet<Employee> Employees { get; set; }

        public DbSet<Account> Accounts { get; set; }

        public DbSet<Attendance> Attendances { get; set; }

        public DbSet<Leave> Leaves { get; set; }

        public DbSet<Overtime> OverTimes { get; set; }

        public DbSet<Payroll> Payrolls { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cấu hình quan hệ cho bảng Leave với Employee (Người xin nghỉ)
            modelBuilder.Entity<Leave>()
                .HasOne(l => l.Employee)
                .WithMany()
                .HasForeignKey(l => l.EmployeeID)
                .OnDelete(DeleteBehavior.Restrict);

            // Cấu hình quan hệ cho bảng Leave với Approver (Người phê duyệt)
            modelBuilder.Entity<Leave>()
                .HasOne(l => l.Approver)
                .WithMany()
                .HasForeignKey(l => l.ApprovedBy)
                .OnDelete(DeleteBehavior.Restrict);
                modelBuilder.Entity<Overtime>()
        .HasOne(o => o.Employee) // Giả định trong Overtime.cs có thuộc tính Employee và EmployeeID
        .WithMany(e => e.Overtimes)
        .HasForeignKey(o => o.EmployeeID)
        .OnDelete(DeleteBehavior.Restrict);
        }
    }
}