-- Chạy trong đúng database của ứng dụng nếu các tài khoản mẫu còn lưu hash cũ.
-- Chỉ cập nhật tài khoản có sẵn; không thêm mới và không thay đổi vai trò.
UPDATE Accounts
SET PasswordHash = N'123456'
WHERE Username IN (
    N'admin', N'hr', N'employee', N'demo.admin', N'demo.hr',
    N'demo.nv003', N'demo.nv004', N'demo.nv005', N'demo.nv006',
    N'demo.nv007', N'demo.nv008', N'demo.nv009', N'demo.nv010',
    N'demo.nv011', N'demo.nv012'
);

SELECT a.Username, r.RoleName, e.EmployeeCode, a.Status
FROM Accounts a
JOIN Roles r ON r.RoleID = a.RoleID
LEFT JOIN Employees e ON e.EmployeeID = a.EmployeeID;
