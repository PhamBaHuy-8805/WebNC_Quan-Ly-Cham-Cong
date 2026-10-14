# Seed data — Bài thực hành Data Layer

Mục 8 của tài liệu yêu cầu tạo `SeedData.cs` để khởi tạo dữ liệu mẫu. Đề không quy định số lượng bản ghi hay yêu cầu seed tất cả các bảng.

## Dữ liệu được thêm

Khi database chưa có dữ liệu, lệnh seed thêm:

| Bảng | Số lượng | Dữ liệu |
| --- | --- | --- |
| Roles | 3 | Admin, HR, Employee |
| Departments | 3 | Nhân sự, Kỹ thuật, Kế toán |
| Positions | 3 | Trưởng phòng, Nhân viên, Thực tập sinh |
| Employees | 12 | Mã DEMO001 đến DEMO012 |
| Accounts | 12 | demo.admin, demo.hr, demo.nv003 đến demo.nv012 |
| Attendances | 12 | Một bản ghi/nhân viên ngày 12/10/2026 |
| Leaves | 33 | Đơn nghỉ của DEMO003–DEMO012, ba trạng thái |
| OverTimes | 33 | Đơn tăng ca của DEMO003–DEMO012, mỗi đơn 2 giờ |

Đây là dữ liệu giả phục vụ bài thực hành. Module cá nhân của Nam là nghỉ phép và tăng ca: DEMO003 có 6 đơn mỗi loại để thử phân trang 5 bản ghi/trang; DEMO004–DEMO012 có 3 đơn mỗi loại. Các đơn dùng ngày cố định trong tháng 11/2026, gồm Pending, Approved, Rejected. Người xử lý mẫu là nhân viên liên kết tài khoản demo.hr. Payrolls chưa được seed.

Theo yêu cầu mới của bản thực hành, mật khẩu mẫu của tài khoản mới là `123456`, được lưu và so sánh trực tiếp. Tên cột `PasswordHash` được giữ nguyên để tương thích database. Tài khoản đã có không bị reset bởi seed; nếu còn hash cũ, chạy `scripts/SetDemoPasswords.sql`. Dùng demo.nv003 để chấm công/gửi đơn, demo.hr để xác nhận công/duyệt đơn.

## Cách chạy

1. Đảm bảo SQL Server đang chạy và `ConnectionStrings:DefaultConnection` trong `appsettings.json` trỏ đúng instance/database của bạn.
2. Trong Visual Studio, mở **Tools → NuGet Package Manager → Package Manager Console**, chọn project web làm Default project, chạy:

   ```powershell
   Update-Database
   ```

   Project có các migration `InitialCreate`, `FixLeaveOvertimeRelationships` và `AddAttendanceReview`, không cần tạo lại. Cập nhật đủ migration trước khi seed. Seed không tự tạo hoặc sửa schema. Với database restore, cần kiểm tra bảng `__EFMigrationsHistory` khớp lịch sử project trước; không chạy InitialCreate lên database đã có bảng nhưng thiếu lịch sử migration.

3. Mở Terminal tại thư mục chứa file `.sln` rồi chạy:

   ```powershell
   dotnet run --project ".\BTL_WebNC_Quản lý chấm công\BTL_WebNC_Quản lý chấm công.csproj" --launch-profile https -- --seed
   ```

   Profile `https` hiện cấu hình môi trường Development. Phần `-- --seed` chuyển cờ `--seed` tới chương trình. Chương trình thêm dữ liệu rồi thoát, không mở web server. Thành công sẽ có thông báo `Seed dữ liệu mẫu hoàn tất`.

4. Chạy lại chính lệnh ở bước 3 để kiểm tra không thêm trùng. Khi muốn mở website, chạy bình thường bằng Visual Studio hoặc bỏ `-- --seed` khỏi lệnh trên.

## Code hoạt động như thế nào?

Luồng gọi: `Program.cs` → tạo DI scope → lấy `ApplicationDbContext` → gọi `SeedData.InitializeAsync(context)` → EF Core gửi SQL tới database.

- `AnyAsync(...)`: kiểm tra bản ghi có tồn tại hay chưa, ví dụ vai trò Admin.
- `Add(...)`: đánh dấu đối tượng mới cần thêm trong DbContext; chưa ghi xuống database ngay.
- `SaveChangesAsync()`: thực thi việc ghi dữ liệu và nhận ID do SQL Server tự sinh.
- Bảng Roles, Departments, Positions được lưu trước để lấy ID cho Employee và Account.
- Nhân viên được nhận diện bằng EmployeeCode; tài khoản được kiểm tra theo Username hoặc EmployeeID; chấm công được kiểm tra theo nhân viên và ngày.
- Nếu bản ghi đã có, seed bỏ qua, không đặt lại mật khẩu hoặc ghi đè nội dung đã chỉnh sửa. Nếu danh mục có tên trùng nhiều lần, cần xử lý dữ liệu trùng trước vì seed không đoán bản ghi để liên kết.
- Transaction đảm bảo nếu một bước thất bại thì toàn bộ lần seed được hoàn tác.
- Chỉ chạy một tiến trình seed tại một thời điểm. Cơ chế kiểm tra trước khi thêm phục vụ việc chạy lại tuần tự; schema hiện chưa có đầy đủ unique index để bảo vệ trường hợp nhiều tiến trình cùng seed.

## Kiểm tra và lấy minh chứng

Trong SSMS, chọn đúng database đã cấu hình rồi chạy:

```sql
SELECT * FROM Roles;
SELECT * FROM Departments;
SELECT * FROM Positions;
SELECT EmployeeID, EmployeeCode, FullName, DepartmentID, PositionID
FROM Employees WHERE EmployeeCode LIKE 'DEMO%'
ORDER BY EmployeeCode;
SELECT UserID, Username, RoleID, EmployeeID, Status
FROM Accounts WHERE Username LIKE 'demo.%';
SELECT a.EmployeeID, e.EmployeeCode, a.WorkDate, a.CheckIn, a.CheckOut, a.TotalHours
FROM Attendances a
JOIN Employees e ON e.EmployeeID = a.EmployeeID
WHERE e.EmployeeCode LIKE 'DEMO%'
  AND a.WorkDate >= '20261012' AND a.WorkDate < '20261013';
```

Chụp kết quả các bảng sau khi seed. Chạy seed lần hai và đối chiếu số bản ghi: với database ban đầu trống và chưa sửa dữ liệu, kết quả vẫn là 3/3/3/12/12/12/33/33. Mở `/Leave` và `/Overtime` sau khi đăng nhập để xem dữ liệu Module 3. Xem `MODULE3_THUC_HANH.md` để kiểm tra luồng CRUD/phê duyệt.
