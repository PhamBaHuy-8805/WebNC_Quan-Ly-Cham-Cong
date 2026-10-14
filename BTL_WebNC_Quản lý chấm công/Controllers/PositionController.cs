
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BTL_WebNC_Quản_lý_chấm_công.Data;
using BTL_WebNC_Quản_lý_chấm_công.Models;

namespace BTL_WebNC_Quản_lý_chấm_công.Controllers
{
    public class PositionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PositionController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Position
        public async Task<IActionResult> Index()
        {
            var positions = await _context.Positions
                .AsNoTracking()
                .OrderBy(p => p.PositionID)
                .ToListAsync();

            return View(positions);
        }

        // GET: Position/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var position = await _context.Positions
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PositionID == id);

            if (position == null)
                return NotFound();

            return View(position);
        }

        // GET: Position/Create
        public IActionResult Create()
        {
            return View(new Position());
        }

        // POST: Position/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("PositionName,Description")] Position input)
        {
            if (await _context.Positions.AnyAsync(
                p => p.PositionName == input.PositionName))
            {
                ModelState.AddModelError(
                    nameof(Position.PositionName),
                    "Tên chức vụ đã tồn tại");
            }

            if (!ModelState.IsValid)
                return View(input);

            input.Status = "Active";

            _context.Positions.Add(input);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Thêm chức vụ thành công";
            return RedirectToAction(nameof(Index));
        }

        // GET: Position/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var position = await _context.Positions.FindAsync(id);

            if (position == null)
                return NotFound();

            return View(position);
        }

        // POST: Position/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("PositionID,PositionName,Description")] Position input)
        {
            if (id != input.PositionID)
                return BadRequest();

            if (await _context.Positions.AnyAsync(
                p => p.PositionName == input.PositionName
                     && p.PositionID != id))
            {
                ModelState.AddModelError(
                    nameof(Position.PositionName),
                    "Tên chức vụ đã tồn tại");
            }

            if (!ModelState.IsValid)
                return View(input);

            var position = await _context.Positions.FindAsync(id);

            if (position == null)
                return NotFound();

            position.PositionName = input.PositionName;
            position.Description = input.Description;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật chức vụ thành công";
            return RedirectToAction(nameof(Index));
        }

        // GET: Position/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var position = await _context.Positions
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PositionID == id);

            if (position == null)
                return NotFound();

            return View(position);
        }

        // POST: Position/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var position = await _context.Positions.FindAsync(id);

            if (position == null)
                return NotFound();

            bool hasActiveEmployees = await _context.Employees
                .AnyAsync(e => e.PositionID == id
                            && e.Status == "Active");

            if (hasActiveEmployees)
            {
                TempData["Error"] =
                    "Không thể ngừng hoạt động chức vụ vì vẫn còn nhân viên đang làm việc.";

                return RedirectToAction(nameof(Index));
            }

            position.Status = "Inactive";
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã ngừng hoạt động chức vụ";
            return RedirectToAction(nameof(Index));
        }
    }
}
