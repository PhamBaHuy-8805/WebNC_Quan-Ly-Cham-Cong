
using BTL_WebNC_Quản_lý_chấm_công.Data;
using BTL_WebNC_Quản_lý_chấm_công.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BTL_WebNC_Quản_lý_chấm_công.Controllers;

[Authorize(Roles = "Admin,HR")]
[Route("Employees")]
public class EmployeeEditController(
    ApplicationDbContext context) : Controller
{
    // GET: /Employees/Edit/5
    [HttpGet("Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var employee = await context.Employees
            .AsNoTracking()
            .SingleOrDefaultAsync(e => e.EmployeeID == id);

        if (employee == null)
            return NotFound();

        await LoadOptionsAsync();

        var model = new EmployeeEditViewModel
        {
            EmployeeID = employee.EmployeeID,
            EmployeeCode = employee.EmployeeCode,
            FullName = employee.FullName,
            DateOfBirth = employee.DateOfBirth,
            Gender = employee.Gender,
            Phone = employee.Phone,
            Email = employee.Email,
            Address = employee.Address,
            DepartmentID = employee.DepartmentID,
            PositionID = employee.PositionID,
            HireDate = employee.HireDate,
            Salary = employee.Salary,
            Status = employee.Status
        };

        return View(model);
    }

    // POST: /Employees/Edit/5
    [HttpPost("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id, EmployeeEditViewModel model)
    {
        if (id != model.EmployeeID)
            return BadRequest();

        var employee = await context.Employees
            .SingleOrDefaultAsync(e => e.EmployeeID == id);

        if (employee == null)
            return NotFound();

        if (ModelState.IsValid)
        {
            var code = model.EmployeeCode.Trim();

            bool codeExists = await context.Employees
                .AnyAsync(e =>
                    e.EmployeeID != id &&
                    e.EmployeeCode == code);

            if (codeExists)
            {
                ModelState.AddModelError(
                    nameof(model.EmployeeCode),
                    "Mã nhân viên đã tồn tại.");
            }

            if (model.DepartmentID.HasValue &&
                !await context.Departments.AnyAsync(d =>
                    d.DepartmentID == model.DepartmentID.Value))
            {
                ModelState.AddModelError(
                    nameof(model.DepartmentID),
                    "Phòng ban không tồn tại.");
            }

            if (model.PositionID.HasValue &&
                !await context.Positions.AnyAsync(p =>
                    p.PositionID == model.PositionID.Value))
            {
                ModelState.AddModelError(
                    nameof(model.PositionID),
                    "Chức vụ không tồn tại.");
            }
        }

        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync();
            return View(model);
        }

        // Chỉ cập nhật những trường được phép
        employee.EmployeeCode = model.EmployeeCode.Trim();
        employee.FullName = model.FullName.Trim();
        employee.DateOfBirth = model.DateOfBirth;
        employee.Gender = model.Gender?.Trim();
        employee.Phone = model.Phone?.Trim();
        employee.Email = model.Email?.Trim();
        employee.Address = model.Address?.Trim();
        employee.DepartmentID = model.DepartmentID;
        employee.PositionID = model.PositionID;
        employee.HireDate = model.HireDate;
        employee.Salary = model.Salary;
        employee.Status = model.Status;

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            ModelState.AddModelError(
                "",
                "Dữ liệu đã thay đổi. Vui lòng tải lại trang.");
            await LoadOptionsAsync();
            return View(model);
        }

        TempData["Success"] =
            "Cập nhật thông tin nhân viên thành công.";

        return RedirectToAction(nameof(Edit), new { id });
    }

    private async Task LoadOptionsAsync()
    {
        var departments = await context.Departments
            .AsNoTracking()
            .OrderBy(d => d.DepartmentName)
            .ToListAsync();

        var positions = await context.Positions
            .AsNoTracking()
            .OrderBy(p => p.PositionName)
            .ToListAsync();

        ViewBag.Departments = new SelectList(
            departments, "DepartmentID", "DepartmentName");

        ViewBag.Positions = new SelectList(
            positions, "PositionID", "PositionName");
    }
}
