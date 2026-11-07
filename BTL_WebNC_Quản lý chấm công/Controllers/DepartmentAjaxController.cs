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
    [Route("ajax/departments")]
    public class DepartmentAjaxController : Controller
    {
        private readonly ApplicationDbContext _db;
        public DepartmentAjaxController(ApplicationDbContext db) => _db = db;
        [HttpGet("")]
        [HttpGet("/Departments")]
        public IActionResult Index() => View();

        [HttpGet("list")]
        public async Task<IActionResult> List(int page = 1, int pageSize = 10)
            => Json(await BuildPageAsync(null, page, pageSize));

        [HttpGet("search")]
        public async Task<IActionResult> Search(string? keyword, int page = 1, int pageSize = 10)
            => Json(await BuildPageAsync(keyword, page, pageSize));

        private async Task<object> BuildPageAsync(string? keyword, int page, int pageSize)
        {
            int size = Math.Clamp(pageSize, 1, 50);
            var query = _db.Departments.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string search = keyword.Trim();
                query = query.Where(x => x.DepartmentName.Contains(search));
            }
            int totalItems = await query.CountAsync();
            int totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)size));
            int currentPage = Math.Clamp(page, 1, totalPages);
            var items = await query.OrderByDescending(x => x.DepartmentID)
                .Skip((currentPage - 1) * size).Take(size)
                .Select(x => new { id = x.DepartmentID, name = x.DepartmentName,
                    description = x.Description, status = x.Status }).ToListAsync();
            return new { items, page = currentPage, pageSize = size, totalItems, totalPages };
        }

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] DepartmentAjaxCreateInput input)
        {
            if (!ModelState.IsValid) return BadRequest(new { message = "Tên phòng ban hoặc trạng thái không hợp lệ." });
            string name = input.DepartmentName.Trim();
            if (await _db.Departments.AnyAsync(x => x.DepartmentName == name))
                return Conflict(new { message = "Phòng ban đã tồn tại." });
            var entity = new Department { DepartmentName = name, Description = input.Description, Status = input.Status };
            _db.Departments.Add(entity);
            try
            {
                await _db.SaveChangesAsync();
                return Json(new { success = true, message = "Đã thêm phòng ban.", id = entity.DepartmentID });
            }
            catch (DbUpdateException) { return Conflict(new { message = "Không thể lưu phòng ban." }); }
        }

        [HttpPost("delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _db.Departments.FindAsync(id);
            if (entity == null) return NotFound(new { message = "Không tìm thấy phòng ban." });
            if (await _db.Employees.AnyAsync(x => x.DepartmentID == id))
                return Conflict(new { message = "Phòng ban đang có nhân viên; không thể xóa." });
            _db.Departments.Remove(entity);
            try
            {
                await _db.SaveChangesAsync();
                return Json(new { success = true, message = "Đã xóa phòng ban." });
            }
            catch (DbUpdateException) { return Conflict(new { message = "Phòng ban có dữ liệu liên quan; không thể xóa." }); }
        }
    }
}
