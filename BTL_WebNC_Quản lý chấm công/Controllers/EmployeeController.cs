using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BTL_WebNC_Quản_lý_chấm_công.Data;
using BTL_WebNC_Quản_lý_chấm_công.Models;

namespace BTL_WebNC_Quản_lý_chấm_công.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EmployeeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Employee
        
public async Task<IActionResult> Index(
    string? search,
    int? departmentId,
    int? positionId,
    string? sortOrder,
    int page = 1)
{
    int pageSize = 5;

    // 1. Khởi tạo Query Pipeline
    var query = _context.Employees
        .Include(e => e.Department)
        .Include(e => e.Position)
        .AsNoTracking()
        .AsQueryable();

    // 2. SEARCH - Where()
    if (!string.IsNullOrWhiteSpace(search))
    {
        query = query.Where(e =>
            e.FullName.Contains(search) ||
            e.EmployeeCode.Contains(search));
    }

    // Lọc theo phòng ban
    if (departmentId.HasValue)
    {
        query = query.Where(e =>
            e.DepartmentID == departmentId.Value);
    }

    // Lọc theo chức vụ
    if (positionId.HasValue)
    {
        query = query.Where(e =>
            e.PositionID == positionId.Value);
    }

    // 3. SORT - OrderBy / OrderByDescending
    query = sortOrder switch
    {
        "name_asc" => query.OrderBy(e => e.FullName)
                           .ThenBy(e => e.EmployeeID),

        "name_desc" => query.OrderByDescending(e => e.FullName)
                            .ThenBy(e => e.EmployeeID),

        "date_asc" => query.OrderBy(e => e.HireDate)
                           .ThenBy(e => e.EmployeeID),

        "date_desc" => query.OrderByDescending(e => e.HireDate)
                            .ThenBy(e => e.EmployeeID),

        _ => query.OrderBy(e => e.EmployeeID)
    };

    // 4. PAGING - Skip / Take
    int totalItems = await query.CountAsync();

    int totalPages = Math.Max(
        1, (int)Math.Ceiling((double)totalItems / pageSize));

    page = Math.Clamp(page, 1, totalPages);

    var employees = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    // 5. Truyền dữ liệu sang View
    ViewBag.Search = search;
    ViewBag.DepartmentId = departmentId;
    ViewBag.PositionId = positionId;
    ViewBag.SortOrder = sortOrder;
    ViewBag.CurrentPage = page;
    ViewBag.TotalPages = totalPages;
    ViewBag.PageSize = pageSize;

    ViewBag.Departments = await _context.Departments
        .AsNoTracking()
        .OrderBy(d => d.DepartmentName)
        .ToListAsync();

    ViewBag.Positions = await _context.Positions
        .AsNoTracking()
        .OrderBy(p => p.PositionName)
        .ToListAsync();

    return View(employees);
}


        // GET: Employee/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var employee = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Position)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployeeID == id);

            if (employee == null)
                return NotFound();

            return View(employee);
        }

        // GET: Employee/Create
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();
            return View(new Employee());
        }

        // POST: Employee/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Employee employee)
        {
            ModelState.Remove(nameof(Employee.Department));
            ModelState.Remove(nameof(Employee.Position));

            if (await _context.Employees.AnyAsync(
                e => e.EmployeeCode == employee.EmployeeCode))
            {
                ModelState.AddModelError(
                    nameof(Employee.EmployeeCode),
                    "Mã nhân viên đã tồn tại");
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdowns(
                    employee.DepartmentID,
                    employee.PositionID);

                return View(employee);
            }

            employee.EmployeeID = 0;
            employee.Status = "Active";

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Thêm nhân viên thành công";

            return RedirectToAction(nameof(Index));
        }

        // GET: Employee/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var employee = await _context.Employees.FindAsync(id);

            if (employee == null)
                return NotFound();

            await LoadDropdowns(
                employee.DepartmentID,
                employee.PositionID);

            return View(employee);
        }

        // POST: Employee/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Employee input)
        {
            if (id != input.EmployeeID)
                return BadRequest();

            ModelState.Remove(nameof(Employee.Department));
            ModelState.Remove(nameof(Employee.Position));

            if (await _context.Employees.AnyAsync(
                e => e.EmployeeCode == input.EmployeeCode
                     && e.EmployeeID != id))
            {
                ModelState.AddModelError(
                    nameof(Employee.EmployeeCode),
                    "Mã nhân viên đã tồn tại");
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdowns(
                    input.DepartmentID,
                    input.PositionID);

                return View(input);
            }

            var employee = await _context.Employees.FindAsync(id);

            if (employee == null)
                return NotFound();

            employee.EmployeeCode = input.EmployeeCode;
            employee.FullName = input.FullName;
            employee.DateOfBirth = input.DateOfBirth;
            employee.Gender = input.Gender;
            employee.Phone = input.Phone;
            employee.Email = input.Email;
            employee.Address = input.Address;
            employee.DepartmentID = input.DepartmentID;
            employee.PositionID = input.PositionID;
            employee.HireDate = input.HireDate;
            employee.Salary = input.Salary;

            // Giữ nguyên trạng thái tại đây.
            // Thay đổi trạng thái thông qua chức năng Delete.
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật nhân viên thành công";

            return RedirectToAction(nameof(Index));
        }

        // GET: Employee/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var employee = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Position)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployeeID == id);

            if (employee == null)
                return NotFound();

            return View(employee);
        }

        // POST: Employee/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var employee = await _context.Employees.FindAsync(id);

            if (employee == null)
                return NotFound();

            // Xóa mềm
            employee.Status = "Inactive";

            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã ngừng hoạt động nhân viên";

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadDropdowns(
            int? departmentId = null,
            int? positionId = null)
        {
            var departments = await _context.Departments
                .AsNoTracking()
                .ToListAsync();

            var positions = await _context.Positions
                .AsNoTracking()
                .ToListAsync();

            ViewBag.Departments = new SelectList(
                departments,
                "DepartmentID",
                "DepartmentName",
                departmentId);

            ViewBag.Positions = new SelectList(
                positions,
                "PositionID",
                "PositionName",
                positionId);
        }
    }
}