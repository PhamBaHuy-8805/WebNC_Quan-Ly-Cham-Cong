# Tính lương

Menu **Bảng lương**: HR/Admin chọn tháng đã kết thúc, lập nháp, kiểm tra chi tiết theo ngày, lưu phụ cấp, duyệt và ghi nhận thanh toán. Employee chỉ xem phiếu lương cá nhân đã duyệt. Không tự duyệt hoặc xác nhận thanh toán cho mình.

## Quy tắc

- Ca mặc định 08:00–16:00, nghỉ trưa 12:00–13:00: 7 giờ/ngày. Múi giờ Việt Nam.
- Lịch chuẩn mặc định thứ Hai–thứ Sáu; cấu hình ngày nghỉ lễ trong `Payroll:Holidays` (chuỗi ngày ISO `yyyy-MM-dd`). Ngày lễ được loại khỏi mẫu số ngày chuẩn.
- Lương giờ = lương cơ bản / ngày chuẩn của tháng / 7. Lương cơ bản được chụp lại khi tính, không đọc lại khi xem bảng đã duyệt.
- Công hưởng lương chỉ lấy phần trong ca, trừ phần giao giờ nghỉ trưa. Đi muộn dưới 120 phút/về sớm làm giảm công theo thời gian thiếu. Từ 10:00 trở đi: vắng cả ngày, không trừ thêm khoản đi muộn/về sớm.
- Ngày làm việc không có bản ghi chấm công: nháp tính vắng một ngày. HR bắt buộc xác nhận đã đối soát trước khi duyệt. Ngày trước ngày vào làm không hưởng lương.
- Chỉ chấm công và đơn đã duyệt được tính. Chấm công thiếu/sai giờ hoặc bị từ chối, đơn chờ duyệt, nghỉ trùng ngày và tăng ca trùng giờ sẽ chặn duyệt lương.
- `Payroll:PaidLeaveTypes` mặc định chỉ có `Annual`: nghỉ phép năm đã duyệt hưởng đủ công. `Sick` và `Unpaid` mặc định không hưởng lương trong phép tính này. Cần cấu hình chính sách nghỉ ốm phù hợp trước khi sử dụng thực tế. Chưa có quản lý quỹ phép hoặc giới hạn số ngày phép năm.
- Tăng ca: lấy giao của khoảng đăng ký được duyệt với chấm công thực tế đã duyệt. Ngày làm việc chỉ tính phần ngoài 08:00–16:00; ngày nghỉ tính thời gian thực tế, vẫn loại giờ nghỉ trưa. `Payroll:OvertimeMultiplier` mặc định 1.5 là chính sách cấu hình của ứng dụng, chưa phân loại ngày thường/ngày nghỉ/ngày lễ. Đi muộn bị tính vắng vẫn được hưởng tăng ca riêng nếu đủ điều kiện.
- Tổng lương = lương cơ bản − tiền thiếu công + tiền tăng ca + phụ cấp. Chưa tính thuế, bảo hiểm hoặc các khoản khấu trừ khác.
- Chỉ sinh nháp tự động cho nhân viên `Active`; chưa có ngày kết thúc làm việc để quyết toán tự động cho nhân viên đã nghỉ việc.

## Khóa kỳ và dữ liệu cũ

Mỗi nhân viên chỉ có một bảng lương/tháng. Bản nháp có thể tính lại; bản đã duyệt/thanh toán được giữ nguyên. Khi duyệt, hệ thống đọc lại nguồn trong giao dịch và yêu cầu xem lại nếu kết quả thay đổi. `RowVersion` chặn ghi đè đồng thời. Sau khi chốt, việc sửa dữ liệu nguồn không tự sửa lương đã chốt.

Migration bổ sung chi tiết tính và ràng buộc chống trùng, không xóa bảng lương cũ. Bảng cũ chưa tính lại có thể thiếu chi tiết; bản đã duyệt cũ không được tự chuyển sang đã thanh toán bằng luồng mới. Nhãn đã thanh toán chỉ ghi nhận, không chuyển tiền.

## Chạy và kiểm tra

```powershell
dotnet ef database update
dotnet run --project Tests/PayrollChecks.csproj
```

Kiểm thử bao gồm mốc muộn 120 phút, ca mới, nghỉ trưa, tháng đủ công, ngày vắng, nghỉ có/không lương, dữ liệu chưa duyệt, tăng ca thực tế, trùng giờ và tháng chưa kết thúc.
