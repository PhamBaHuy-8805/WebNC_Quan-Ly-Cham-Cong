# Nhân viên chấm công — HR xác nhận

## Cập nhật và chạy

1. Dừng ứng dụng đang chạy trong Visual Studio (Shift+F5).
2. Chọn project web trong Package Manager Console và chạy `Update-Database`. Migration mới `AddAttendanceReview` thêm thông tin xác nhận, khóa chống cập nhật đồng thời và unique index nhân viên/ngày. Migration giữ dữ liệu cũ ở trạng thái Pending, không tự xác nhận. Nếu có nhiều bản cùng nhân viên/cùng ngày, migration dừng để đối chiếu; không tự xóa bản ghi.
3. Mật khẩu giờ được so sánh trực tiếp theo yêu cầu cho bản thực hành. Tên cột `PasswordHash` được giữ để tương thích database nhưng hiện chứa mật khẩu thường. Tài khoản seed mới dùng `123456`. Tài khoản có hash cũ cần cập nhật bằng `scripts/SetDemoPasswords.sql`; seed không đặt lại mật khẩu đang có.
4. Chạy lại ứng dụng. Đăng nhập `employee`/`123456` và `hr`/`123456` nếu đã thêm ba tài khoản bằng SQL trước đó. Nếu dùng seed, tài khoản tương ứng là `demo.nv003` và `demo.hr`, mật khẩu `123456` sau khi seed mới hoặc chạy script cập nhật.

Thay đổi mật khẩu thường là lựa chọn của bản demo theo yêu cầu mới, khác yêu cầu băm mật khẩu trong SRS ban đầu; không xem đây là thiết kế lưu mật khẩu cho hệ thống triển khai thực tế.

## Quy trình

- Employee → Chấm công → Check-in: máy chủ lấy thời gian Việt Nam và nhân viên từ tài khoản đang đăng nhập. Không nhận EmployeeID hoặc giờ do trình duyệt gửi.
- Employee → Check-out: chỉ thực hiện sau check-in và tối đa một lần/ngày. Máy chủ tính TotalHours, cập nhật tình trạng đi muộn/về sớm, giữ ApprovalStatus=Pending.
- HR/Admin → Chấm công → Chi tiết → Xác nhận hoặc Từ chối. Chỉ xử lý bản đủ giờ vào/ra và còn Pending; không tự xác nhận bản của mình. Từ chối phải nhập lý do, tối đa 500 ký tự.
- Employee xem lại lịch sử, trạng thái HR, người xử lý, thời điểm và ghi chú. Chỉ thấy dữ liệu của mình; HR/Admin thấy toàn bộ.

## Quy ước demo

- Ca làm 08:00–17:00, nghỉ trưa 12:00–13:00, múi giờ Việt Nam.
- Tổng giờ = khoảng check-in/check-out trừ phần giao với giờ nghỉ trưa. Ví dụ 08:00–17:00 = 8 giờ; 08:00–11:00 = 3 giờ; 11:30–12:30 = 0,5 giờ.
- Giờ làm chưa bao gồm quy tắc tính lương hay duyệt tăng ca. Chưa hỗ trợ ca qua đêm, nhiều ca/ngày hoặc sửa công đã xử lý.
- `Status` lưu tình trạng chấm công: Present, Late, EarlyLeave, LateEarly, Absent.
- `ApprovalStatus` lưu quyết định HR: Pending, Approved, Rejected. Không dùng Status để thay thế quyết định của HR.
- Lần xác nhận lưu ReviewedBy, ReviewedAt, ReviewNote. RowVersion kiểm tra dữ liệu kể từ lúc HR mở trang; dữ liệu thay đổi sẽ yêu cầu kiểm tra lại.
- Unique index `(EmployeeID, WorkDate)` chặn hai check-in cùng lúc tạo trùng ngày.

## Kịch bản kiểm tra trên SQL Server

1. Đăng nhập Employee, check-out trước check-in phải bị chặn (cả khi gọi POST trực tiếp).
2. Check-in, thử gửi lần hai/ở hai tab: chỉ có một bản ngày hôm nay.
3. HR mở chi tiết trước check-out: chưa có nút xác nhận.
4. Employee check-out, gửi lần hai: giờ đã ghi không bị thay đổi.
5. HR xác nhận, Employee xem lại quyết định và người xử lý. Không thể xác nhận/từ chối lại.
6. Dùng một bản hoàn tất còn Pending khác để từ chối: lý do trống bị chặn; lý do hợp lệ được lưu.
7. Employee khác không xem được chi tiết người khác và không gọi được Approve/Reject.
8. Hai HR mở cùng bản: người thao tác sau không ghi đè quyết định trước.
9. Thử bộ lọc ngày, mã/tên, quyết định HR và phân trang.

Kiểm tra tự động hiện chạy không cần database: quy tắc tổng giờ, đi muộn/về sớm, chuyển ngày Việt Nam, metadata quan hệ/concurrency/unique index, role/POST/anti-forgery. Không thay thế kiểm tra SQL Server và HTTP thực tế ở trên.
