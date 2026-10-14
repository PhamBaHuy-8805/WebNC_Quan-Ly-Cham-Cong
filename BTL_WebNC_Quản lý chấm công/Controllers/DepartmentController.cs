
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BTL_WebNC_Quản_lý_chấm_công.Data;
using BTL_WebNC_Quản_lý_chấm_công.Models;

namespace BTL_WebNC_Quản_lý_chấm_công.Controllers
{
    public class DepartmentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DepartmentController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Department
        public async Task<IActionResult> Index()
        {
            var departments = await _context.Departments
                .AsNoTracking()
                .OrderBy(d => d.DepartmentID)
                .ToListAsync();

            return View(departments);
        }

        // GET: Department/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var department = await _context.Departments
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.DepartmentID == id);

            if (department == null)
                return NotFound();

            return View(department);
        }

        // GET: Department/Create
        public IActionResult Create()
        {
            return View(new Department());
        }

        // POST: Department/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("DepartmentName,Description")] Department input)
        {
            if (await _context.Departments.AnyAsync(
                d => d.DepartmentName == input.DepartmentName))
            {
                ModelState.AddModelError(
                    nameof(Department.DepartmentName),
                    "Tên phòng ban đã tồn tại");
            }

            if (!ModelState.IsValid)
                return View(input);

            input.Status = "Active";

            _context.Departments.Add(input);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Thêm phòng ban thành công";
            return RedirectToAction(nameof(Index));
        }

        // GET: Department/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var department = await _context.Departments
                .FindAsync(id);

            if (department == null)
                return NotFound();

            return View(department);
        }

        // POST: Department/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("DepartmentID,DepartmentName,Description")] Department input)
        {
            if (id != input.DepartmentID)
                return BadRequest();

            if (await _context.Departments.AnyAsync(
                d => d.DepartmentName == input.DepartmentName
                     && d.DepartmentID != id))
            {
                ModelState.AddModelError(
                    nameof(Department.DepartmentName),
                    "Tên phòng ban đã tồn tại");
            }

            if (!ModelState.IsValid)
                return View(input);

            var department = await _context.Departments.FindAsync(id);

            if (department == null)
                return NotFound();

            department.DepartmentName = input.DepartmentName;
            department.Description = input.Description;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật phòng ban thành công";
            return RedirectToAction(nameof(Index));
        }

        // GET: Department/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var department = await _context.Departments
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.DepartmentID == id);

            if (department == null)
                return NotFound();

            return View(department);
        }

        // POST: Department/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var department = await _context.Departments.FindAsync(id);

            if (department == null)
                return NotFound();

            bool hasActiveEmployees = await _context.Employees
                .AnyAsync(e => e.DepartmentID == id
                            && e.Status == "Active");

            if (hasActiveEmployees)
            {
                TempData["Error"] =
                    "Không thể ngừng hoạt động phòng ban vì vẫn còn nhân viên đang làm việc.";

                return RedirectToAction(nameof(Index));
            }

            department.Status = "Inactive";
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã ngừng hoạt động phòng ban";
            return RedirectToAction(nameof(Index));
        }
    }
}
