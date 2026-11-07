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
    [Route("ajax/positions")]
    public class PositionAjaxController : Controller
    {
        private readonly ApplicationDbContext _db;
        public PositionAjaxController(ApplicationDbContext db) => _db = db;
        [HttpGet("")]
        [HttpGet("/Positions")]
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
            var query = _db.Positions.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string search = keyword.Trim();
                query = query.Where(x => x.PositionName.Contains(search));
            }
            int totalItems = await query.CountAsync();
            int totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)size));
            int currentPage = Math.Clamp(page, 1, totalPages);
            var items = await query.OrderByDescending(x => x.PositionID)
                .Skip((currentPage - 1) * size).Take(size)
                .Select(x => new { id = x.PositionID, name = x.PositionName,
                    description = x.Description, status = x.Status }).ToListAsync();
            return new { items, page = currentPage, pageSize = size, totalItems, totalPages };
        }

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] PositionAjaxCreateInput input)
        {
            if (!ModelState.IsValid) return BadRequest(new { message = "Tên chức vụ hoặc trạng thái không hợp lệ." });
            string name = input.PositionName.Trim();
            if (await _db.Positions.AnyAsync(x => x.PositionName == name))
                return Conflict(new { message = "Chức vụ đã tồn tại." });
            var entity = new Position { PositionName = name, Description = input.Description, Status = input.Status };
            _db.Positions.Add(entity);
            try
            {
                await _db.SaveChangesAsync();
                return Json(new { success = true, message = "Đã thêm chức vụ.", id = entity.PositionID });
            }
            catch (DbUpdateException) { return Conflict(new { message = "Không thể lưu chức vụ." }); }
        }

        [HttpPost("delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _db.Positions.FindAsync(id);
            if (entity == null) return NotFound(new { message = "Không tìm thấy chức vụ." });
            if (await _db.Employees.AnyAsync(x => x.PositionID == id))
                return Conflict(new { message = "Chức vụ đang có nhân viên; không thể xóa." });
            _db.Positions.Remove(entity);
            try
            {
                await _db.SaveChangesAsync();
                return Json(new { success = true, message = "Đã xóa chức vụ." });
            }
            catch (DbUpdateException) { return Conflict(new { message = "Chức vụ có dữ liệu liên quan; không thể xóa." }); }
        }
    }
}
