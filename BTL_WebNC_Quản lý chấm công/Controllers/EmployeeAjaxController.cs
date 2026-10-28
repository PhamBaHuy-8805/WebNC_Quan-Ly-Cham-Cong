using System;
using System.Linq;
using System.Threading.Tasks;
using BTL_WebNC_Quản_lý_chấm_công.Data;
using BTL_WebNC_Quản_lý_chấm_công.Models;
using BTL_WebNC_Quản_lý_chấm_công.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BTL_WebNC_Quản_lý_chấm_công.Controllers
{
    [Authorize(Roles = "Admin,HR")]
    [Route("ajax/employees")]
    public class EmployeeAjaxController : Controller
    {
        private readonly ApplicationDbContext _db;
        public EmployeeAjaxController(ApplicationDbContext db) => _db = db;

        [HttpGet("")]
        [HttpGet("/Employees")]
        public IActionResult Index() => View();

        // 1. XMLHttpRequest lần đầu hiển thị danh sách.
        [HttpGet("list")]
        public async Task<IActionResult> List(int page = 1, int pageSize = 10)
            => Json(await BuildPageAsync(null, null, null, null, page, pageSize));

        // 4. fetch() tìm kiếm/lọc; cùng cơ chế phân trang Skip/Take.
        [HttpGet("search")]
        public async Task<IActionResult> Search(
            string? keyword, int? departmentId, int? positionId,
            string? status, int page = 1, int pageSize = 10)
            => Json(await BuildPageAsync(keyword, departmentId, positionId, status, page, pageSize));

        private async Task<object> BuildPageAsync(
            string? keyword, int? departmentId, int? positionId,
            string? status, int page, int pageSize)
        {
            var size = Math.Clamp(pageSize, 1, 50);
            IQueryable<Employee> query = _db.Employees.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string search = keyword.Trim();
                query = query.Where(e => e.EmployeeCode.Contains(search) || e.FullName.Contains(search));
            }
            if (departmentId.HasValue && departmentId.Value > 0)
                query = query.Where(e => e.DepartmentID == departmentId.Value);
            if (positionId.HasValue && positionId.Value > 0)
                query = query.Where(e => e.PositionID == positionId.Value);
            if (status == "Active" || status == "Inactive")
                query = query.Where(e => e.Status == status);

            int totalItems = await query.CountAsync();
            int totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)size));
            int currentPage = Math.Clamp(page, 1, totalPages);

            var items = await query.OrderByDescending(e => e.EmployeeID)
                .Skip((currentPage - 1) * size).Take(size)
                .Select(e => new
                {
                    employeeID = e.EmployeeID, employeeCode = e.EmployeeCode,
                    fullName = e.FullName, email = e.Email,
                    phone = e.Phone, hireDate = e.HireDate,
                    salary = e.Salary, status = e.Status,
                    departmentName = e.Department != null ? e.Department.DepartmentName : "",
                    positionName = e.Position != null ? e.Position.PositionName : ""
                })
                .ToListAsync();
            return new { items, page = currentPage, pageSize = size, totalItems, totalPages };
        }

        [HttpGet("lookups")]
        public async Task<IActionResult> Lookups()
        {
            var departments = await _db.Departments.AsNoTracking()
                .OrderBy(x => x.DepartmentName)
                .Select(x => new { id = x.DepartmentID, name = x.DepartmentName }).ToListAsync();
            var positions = await _db.Positions.AsNoTracking()
                .OrderBy(x => x.PositionName)
                .Select(x => new { id = x.PositionID, name = x.PositionName }).ToListAsync();
            return Json(new { departments, positions });
        }

        // 2. jQuery AJAX POST tạo mới; dùng Anti-forgery.
        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] EmployeeAjaxCreateInput input)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = "Thông tin chưa hợp lệ.", errors =
                    ModelState.Where(x => x.Value?.Errors.Count > 0)
                        .ToDictionary(x => x.Key, x => x.Value!.Errors.Select(e => e.ErrorMessage).ToArray()) });

            string code = input.EmployeeCode.Trim();
            if (await _db.Employees.AnyAsync(e => e.EmployeeCode == code))
                return Conflict(new { message = "Mã nhân viên đã tồn tại." });
            if (input.DepartmentID.HasValue &&
                !await _db.Departments.AnyAsync(d => d.DepartmentID == input.DepartmentID.Value))
                return BadRequest(new { message = "Phòng ban không tồn tại." });
            if (input.PositionID.HasValue &&
                !await _db.Positions.AnyAsync(p => p.PositionID == input.PositionID.Value))
                return BadRequest(new { message = "Chức vụ không tồn tại." });

            var employee = new Employee
            {
                EmployeeCode = code, FullName = input.FullName.Trim(),
                DateOfBirth = input.DateOfBirth, Gender = input.Gender,
                Phone = input.Phone, Email = input.Email, Address = input.Address,
                DepartmentID = input.DepartmentID, PositionID = input.PositionID,
                HireDate = input.HireDate!.Value, Salary = input.Salary,
                Status = input.Status
            };
            _db.Employees.Add(employee);
            try
            {
                await _db.SaveChangesAsync();
                return Json(new { success = true, message = "Đã thêm nhân viên.", id = employee.EmployeeID });
            }
            catch (DbUpdateException)
            {
                return Conflict(new { message = "Không thể lưu. Kiểm tra mã nhân viên hoặc dữ liệu liên kết." });
            }
        }

        // 2. jQuery AJAX POST xóa; bản ghi đã được sử dụng nơi khác sẽ bị FK chặn.
        [HttpPost("delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var employee = await _db.Employees.FindAsync(id);
            if (employee == null) return NotFound(new { message = "Không tìm thấy nhân viên." });
            // Bảo vệ lịch sử nghiệp vụ ngay cả khi cơ sở dữ liệu cũ có FK ON DELETE CASCADE.
            // Nên chuyển trạng thái Inactive nếu nhân viên đã phát sinh nghiệp vụ.
            bool hasRelations =
                await _db.Accounts.AnyAsync(x => x.EmployeeID == id) ||
                await _db.Attendances.AnyAsync(x => x.EmployeeID == id) ||
                await _db.Leaves.AnyAsync(x => x.EmployeeID == id || x.ApprovedBy == id) ||
                await _db.OverTimes.AnyAsync(x => x.EmployeeID == id || x.ApprovedBy == id) ||
                await _db.Payrolls.AnyAsync(x => x.EmployeeID == id);
            if (hasRelations)
                return Conflict(new { message = "Nhân viên có tài khoản, chấm công, nghỉ phép, tăng ca hoặc bảng lương; không thể xóa. Hãy chuyển trạng thái sang Inactive." });

            _db.Employees.Remove(employee);
            try
            {
                await _db.SaveChangesAsync();
                return Json(new { success = true, message = "Đã xóa nhân viên." });
            }
            catch (DbUpdateException)
            {
                return Conflict(new { message = "Nhân viên đã có chấm công, tài khoản hoặc dữ liệu liên quan; không thể xóa." });
            }
        }
    }
}
