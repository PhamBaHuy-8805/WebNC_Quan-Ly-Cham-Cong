using Microsoft.EntityFrameworkCore;
using BTL_WebNC_Quản_lý_chấm_công.Models;
using Microsoft.AspNetCore.Identity;

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

        public DbSet<ProfileChangeRequest> ProfileChangeRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cấu hình quan hệ cho bảng Leave với Employee (Người xin nghỉ)
            modelBuilder.Entity<Leave>()
                .HasOne(l => l.Employee)
                .WithMany(e => e.Leaves)
                .HasForeignKey(l => l.EmployeeID)
                .OnDelete(DeleteBehavior.Restrict);

            // Cấu hình quan hệ cho bảng Leave với Approver (Người phê duyệt)
            modelBuilder.Entity<Leave>()
                .HasOne(l => l.Approver)
                .WithMany()
                .HasForeignKey(l => l.ApprovedBy)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Overtime>()
                .HasOne(o => o.Employee)
                .WithMany(e => e.Overtimes)
                .HasForeignKey(o => o.EmployeeID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Overtime>()
                .HasOne(o => o.Approver)
                .WithMany()
                .HasForeignKey(o => o.ApprovedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Không cho hai thao tác đồng thời ghi đè trạng thái đã được xử lý.
            modelBuilder.Entity<Leave>().Property(l => l.Status).IsConcurrencyToken();
            modelBuilder.Entity<Overtime>().Property(o => o.Status).IsConcurrencyToken();

            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.Employee).WithMany(e => e.Attendances)
                .HasForeignKey(a => a.EmployeeID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.Reviewer).WithMany()
                .HasForeignKey(a => a.ReviewedBy).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Attendance>()
                .HasIndex(a => new { a.EmployeeID, a.WorkDate }).IsUnique();
            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.CheckInReviewer).WithMany()
                .HasForeignKey(a => a.CheckInReviewedBy).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Payroll>()
                .HasIndex(p => new { p.EmployeeID, p.Year, p.Month }).IsUnique();

            modelBuilder.Entity<ProfileChangeRequest>()
                .HasOne(r => r.Employee).WithMany(e => e.ProfileChangeRequests)
                .HasForeignKey(r => r.EmployeeID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ProfileChangeRequest>()
                .HasOne(r => r.Reviewer).WithMany()
                .HasForeignKey(r => r.ReviewedBy).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ProfileChangeRequest>()
                .Property(r => r.Status).IsConcurrencyToken();
            // Mỗi nhân viên chỉ có tối đa một yêu cầu đang chờ duyệt.
            modelBuilder.Entity<ProfileChangeRequest>()
                .HasIndex(r => r.EmployeeID).IsUnique().HasFilter("[Status] = 'Pending'");
        }
    }
}
