# Tính lương theo bản nhóm

Tích hợp `origin/master` đến commit `6a845f1`: nhân sự AJAX, tổng hợp công, bảng lương, báo cáo và CSV. Code cục bộ trước khi tích hợp được lưu ở commit `386824b`.

## Chính sách đang dùng

- Ca 08:00–17:00, nghỉ trưa 12:00–13:00; ngày lương 8 giờ, lịch chuẩn thứ Hai–thứ Sáu.
- Chấm công đầy đủ được HR xác nhận tính một ngày công. Đi muộn/về sớm phạt cố định 50.000đ mỗi lần; vừa muộn vừa về sớm là hai lần.
- Bỏ quy tắc muộn từ 120 phút tính vắng cả ngày.
- Nghỉ Annual/Sick đã duyệt hưởng công; Unpaid không hưởng công. Ngày nghỉ trùng ngày đã chấm công không cộng hai lần.
- Tăng ca đã duyệt: số giờ đăng ký × (lương cơ bản / ngày chuẩn / 8) × 1,5.
- Thực nhận = lương theo ngày công + tiền tăng ca + phụ cấp − khấu trừ; tối thiểu bằng 0.
- Điều chỉnh phụ cấp/khấu trừ ở trạng thái Nháp. HR/Admin lập, sửa, duyệt lương và xem báo cáo; nhân viên chỉ xem phiếu lương cá nhân đã duyệt.

Công thức: `Services/PayrollRules.cs`; tổng hợp: `Services/PayrollService.cs`. Bản nháp đã tính bằng cách cũ cần dùng **Tính lại bảng Nháp** khi muốn áp dụng chính sách mới. Ghép code không tự tính lại lương hoặc thêm dữ liệu lịch sử.

## Tương thích

Giữ database `Attendance_Restored`. Không chạy seed trong quá trình tích hợp. Giữ migration và cột lương hiện có để tương thích database. Giữ màn hình CRUD cũ bên cạnh giao diện AJAX mới.

Identity dùng kho tài khoản hiện có, hỗ trợ mật khẩu thực hành và hash Identity; không đổi mật khẩu hoặc vai trò tài khoản.

## Kiểm tra

```powershell
dotnet build
dotnet run --project Tests/PayrollChecks.csproj
```

Kiểm thử SQL/HTTP tùy chọn dùng `PAYROLL_TEST_CONNECTION`, `PAYROLL_TEST_URL` và ứng dụng cục bộ đang chạy. Kiểm thử tạo dữ liệu riêng rồi xóa đúng các bản ghi đó; không lập lương hàng loạt cho nhân viên thật.
