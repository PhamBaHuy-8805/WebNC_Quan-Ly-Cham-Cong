# Phần cá nhân của Phạm Đức Nam — Module 3

> Cập nhật theo yêu cầu mới: đăng nhập bản demo đã chuyển sang mật khẩu thường `123456`. Nếu dữ liệu còn hash cũ, chạy `scripts/SetDemoPasswords.sql`. Đã bổ sung chấm công và HR xác nhận, xem `CHAM_CONG_HR.md`. Đây là thay đổi so với yêu cầu băm mật khẩu của SRS ban đầu.

## Đối chiếu tài liệu

- Báo cáo chung: FR-05 (nghỉ phép), FR-06 (tăng ca), yêu cầu kiểm tra quyền ở máy chủ, băm mật khẩu và toàn vẹn quan hệ dữ liệu.
- Báo cáo cá nhân LWNC_THN1: Module 3 — Quản lý Đơn từ và Phê duyệt; Employee gửi và theo dõi đơn, HR duyệt/từ chối. EmployeeID lấy từ tài khoản; ApprovedBy lấy từ HR đăng nhập; TotalHours do hệ thống tính; form không cho sửa các trường hệ thống.
- Bài thực hành buổi 6: mục 8 Seed Data thuộc phần nhóm; mục 9 CRUD bằng scaffold thuộc phần cá nhân; mục 10 là Search, Sort, Paging.

Phạm vi lần triển khai này: seed + CRUD cho Leave/Overtime, trạng thái đơn và luồng duyệt để CRUD đúng nghiệp vụ. Có thêm query pipeline mục 10 và đăng nhập cookie tối thiểu để kiểm tra quyền. Chưa triển khai toàn bộ SRS của hệ thống.

## Quy trình thực hiện đúng thứ tự

### 1. Xác định model và quan hệ

Hai entity chính là `Leave` và `Overtime`. Mỗi đơn có một Employee; người xử lý là Employee khác, được liên kết qua ApprovedBy.

Đã sửa ApplicationDbContext:

- Leave.Employee ↔ Employee.Leaves dùng EmployeeID, không sinh EmployeeID1.
- Overtime.Approver dùng ApprovedBy, không sinh ApproverEmployeeID.
- Xóa Employee có đơn/người duyệt bị hạn chế bằng Restrict.
- Status là concurrency token: nếu HR xử lý trong lúc Employee sửa/xóa hoặc hai HR cùng xử lý, thao tác dùng trạng thái cũ sẽ bị từ chối.

Migration mới `FixLeaveOvertimeRelationships` giữ thông tin người duyệt từ cột cũ. Nếu hai cột cũ chỉ tới hai người khác nhau, migration dừng và yêu cầu đối chiếu, không tự đoán.

### 2. Kiểm tra database rồi cập nhật migration

Sau khi restore thành công, sửa ConnectionStrings:DefaultConnection cho đúng server và tên database thực tế. Trong ảnh trước đó server là `DESKTOP-23HD04U\SQLEXPRESS`; tên database phải là tên bạn thực sự restore thành công, không mặc định suy ra từ tên file .bak. Không đổi cấu hình sang server này nếu bạn đang chạy ở máy khác.

Trong SSMS, chọn đúng database, kiểm tra:

```sql
SELECT DB_NAME() AS CurrentDatabase;
SELECT * FROM __EFMigrationsHistory ORDER BY MigrationId;
```

Nếu database đã có `InitialCreate`, trong Visual Studio → Tools → NuGet Package Manager → Package Manager Console, chọn project web rồi chạy:

```powershell
Update-Database
```

Nếu database có các bảng nhưng thiếu lịch sử migration, cần đối chiếu schema trước; không tạo lại migration InitialCreate hoặc xóa database để thử.

### 3. Seed dữ liệu — mục 8

Tại thư mục chứa `.sln`:

```powershell
dotnet run --project ".\BTL_WebNC_Quản lý chấm công\BTL_WebNC_Quản lý chấm công.csproj" --launch-profile https -- --seed
```

Chạy tuần tự hai lần để kiểm tra không thêm trùng. Với database trống ban đầu, có 33 Leave và 33 Overtime; các bảng nền và tài khoản xem trong `SEED_DATA.md`. Nếu đã sửa/xóa dữ liệu mẫu, số lượng có thể khác; seed không ghi đè dữ liệu đã có.

### 4. Scaffold CRUD — mục 9

Hai controller và 10 view CRUD đã được khởi tạo bằng `dotnet-aspnet-codegenerator` (ASP.NET Core scaffolder) rồi chỉnh theo nghiệp vụ. Các view dùng chung _Form/_Details giúp tránh lặp form và thông tin chi tiết.

Quy trình tương đương trong Visual Studio để trình bày bài thực hành:

1. Chuột phải Controllers → Add → New Scaffolded Item.
2. Chọn MVC Controller with views, using Entity Framework.
3. Model: Leave; Data context: ApplicationDbContext; Controller: LeaveController.
4. Chọn sử dụng layout, tạo view và thư viện validation.
5. Lặp lại với Overtime/OvertimeController.

Code đã có sẵn: không scaffold ghi đè lên hai controller hiện tại vì sẽ mất các quy tắc nghiệp vụ đã bổ sung.

| Action | HTTP | Xử lý |
| --- | --- | --- |
| Index | GET | Danh sách đúng quyền; tìm kiếm/lọc/sắp xếp/phân trang |
| Details | GET | Chi tiết trong phạm vi được phép |
| Create | GET/POST | Employee nhập form; máy chủ gán EmployeeID, Pending, ApprovedBy=null |
| Edit | GET/POST | Chỉ chủ đơn còn Pending; chỉ sửa các trường trên form |
| Delete | GET/POST | GET xác nhận; POST xóa đơn Pending của chính mình |
| Approve | POST | HR/Admin xử lý đơn Pending, gán Approved và ApprovedBy |
| Reject | POST | HR/Admin xử lý đơn Pending, gán Rejected và ApprovedBy |

POST có anti-forgery token. Chặn truy cập bằng URL/POST trực tiếp ở phía máy chủ, không chỉ ẩn nút trên giao diện.

### 5. Điều chỉnh scaffold theo báo cáo

Scaffold mặc định cho chọn EmployeeID, ApprovedBy và sửa Status. Điều này không đúng thiết kế của báo cáo cá nhân, nên đã thay form bằng `LeaveCreateViewModel` và `OvertimeCreateViewModel`. Một form được dùng cho cả Create/Edit vì các trường được nhập giống nhau.

Các quyết định triển khai để bổ sung CRUD mà báo cáo chưa mô tả chi tiết:

- Employee chỉ được sửa/xóa đơn của mình khi Pending; đơn Approved/Rejected giữ nguyên để tra cứu.
- HR/Admin được xem toàn bộ và xử lý đơn; Admin kế thừa quyền quản trị cao nhất từ báo cáo chung. Không tự duyệt đơn của chính mình.
- Loại nghỉ demo: Annual, Sick, Unpaid.
- Tăng ca trong cùng ngày; EndTime phải lớn hơn StartTime. Ca qua đêm cần thiết kế thêm ngày kết thúc trước khi hỗ trợ.
- Ngày nghỉ kết thúc không trước ngày bắt đầu; lý do bắt buộc, tối đa 500 ký tự.

Phần UC3.6 (hạn mức phép, ngày còn lại) và UC3.7 (báo cáo tổng hợp OT) chưa nằm trong CRUD lần này. Hai báo cáo chưa nêu hạn mức phép, cách tính cuối tuần/ngày lễ, phép nửa ngày hoặc chính sách chuyển năm; chưa tự đặt công thức trừ phép. Trạng thái Approved được lưu làm đầu vào cho bước tổng hợp sau.

### 6. Query pipeline — mục 10

Index thực hiện: `AsNoTracking → Where (quyền/tìm kiếm/trạng thái) → CountAsync → OrderBy + ThenBy → Skip → Take → ToListAsync`.

- Tìm theo mã nhân viên, tên hoặc lý do.
- Lọc Pending/Approved/Rejected.
- Sắp xếp ngày tăng/giảm, tên hoặc trạng thái; dùng ID để ổn định thứ tự.
- 5 bản ghi/trang; giữ điều kiện lọc khi chuyển trang; chặn chỉ số trang ngoài phạm vi.
- `Include(Employee)` và `Include(Approver)` để hiển thị thông tin liên quan.

## Chạy và kiểm tra quy trình

```powershell
dotnet run --project ".\BTL_WebNC_Quản lý chấm công\BTL_WebNC_Quản lý chấm công.csproj" --launch-profile https
```

Mở địa chỉ HTTPS do ứng dụng in ra, chọn Đăng nhập.

Tài khoản demo mới có mật khẩu `123456`:

| Tài khoản | Mục đích |
| --- | --- |
| demo.nv003 | Employee: 6 đơn mỗi loại để thử phân trang |
| demo.nv004 | Employee khác để kiểm tra giới hạn truy cập |
| demo.hr | HR: xem, duyệt, từ chối |
| demo.admin | Admin: xem và xử lý theo quyền quản trị |

Nếu tài khoản này đã tồn tại với mật khẩu khác, seed không đặt lại mật khẩu.

Kịch bản minh chứng:

1. Đăng nhập demo.nv003 → Nghỉ phép → Tạo yêu cầu → chọn ngày, lý do → gửi. Kết quả Pending, không có ô chọn nhân viên/người duyệt.
2. Mở Details, Edit và lưu lý do mới; tạo một đơn khác rồi thử Delete, xác nhận xóa.
3. Thử ngày kết thúc trước ngày bắt đầu hoặc lý do trống: phải có lỗi validation và không lưu.
4. Tăng ca: chọn 18:00–20:30 → tổng 2,5 giờ; sửa thành 21:00 → tổng 3 giờ. Giờ kết thúc trước/bằng giờ bắt đầu bị chặn.
5. Đăng nhập demo.nv004: danh sách không có đơn của demo.nv003; thử URL chi tiết/sửa/xóa đơn của người khác phải nhận 404; gọi Approve/Reject phải bị từ chối quyền.
6. Đăng nhập demo.hr: mở chi tiết đơn Pending → Phê duyệt hoặc Từ chối. Người xử lý hiển thị HR; thao tác lần hai bị chặn.
7. Đăng nhập lại demo.nv003: trạng thái mới được hiển thị, không còn sửa/xóa đơn đã xử lý.
8. Thử tìm kiếm, lọc, sắp xếp, trang trước/sau; tài khoản Employee vẫn chỉ thấy đơn của mình.
9. Mở hai phiên HR cùng một đơn Pending, xử lý cả hai: lần xử lý sau không được ghi đè quyết định trước.

## Data flow để giải thích khi nộp bài

Ví dụ Create Leave:

`Employee → Create.cshtml → POST Leave/Create → LeaveCreateViewModel → ModelState → gán EmployeeID/Status → DbContext.Leaves.Add → SaveChangesAsync → SQL INSERT → Database → redirect Details`.

Ví dụ HR duyệt:

`HR → Details → POST Approve → kiểm tra quyền → đọc đơn → kiểm tra Pending → gán Status/ApprovedBy → SaveChangesAsync (kiểm tra concurrency) → SQL UPDATE → Database → Details`.

`async` tránh chặn luồng trong lúc chờ database. `AsNoTracking` dùng ở danh sách/chi tiết chỉ đọc. Khi sửa/duyệt/xóa, lấy entity có tracking để EF lưu thay đổi và kiểm tra trạng thái gốc.

## Kiểm tra tự động và giới hạn

```powershell
dotnet build ".\BTL_WebNC_Quản lý chấm công.sln" --no-restore
dotnet run --project ".\tests\Module3.SmokeTests\Module3.SmokeTests.csproj" --no-restore
```

Bộ kiểm tra không cần database: validation ngày/giờ, tính lại TotalHours, form không nhận trường hệ thống, metadata quyền/POST/anti-forgery, quan hệ EF, concurrency token và dịch query sang SQL Server.

Các kiểm tra này không thay thế kiểm tra đọc/ghi và phân quyền qua HTTP. Chưa xác minh migration, seed chạy hai lần, CRUD, luồng cookie hay xử lý đồng thời trên SQL Server thực tế. Thực hiện kịch bản ở trên sau khi restore và cập nhật database để lấy minh chứng cuối cùng.
