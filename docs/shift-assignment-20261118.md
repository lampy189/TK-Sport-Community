# Chia ca và nhận ca — 18/11/2026

Phạm vi: ca gắn với buổi chơi hiện có, theo RP3 FR-25 (Assign Referee), FR-26 (Assign Staff), quy tắc không trùng lịch; bước xác nhận nhận ca theo yêu cầu người dùng ngày 18/11/2026. Không tạo mô hình ca độc lập với buổi chơi.

- Manager vào **Chia ca**, chọn buổi, chọn trọng tài/staff rồi lưu; xem từng người chờ nhận hoặc đã nhận.
- Referee/Staff vào **Ca của tôi**, lọc ngày/ca sắp tới/chờ nhận/tất cả và bấm **Nhận ca**. API lấy người nhận từ tài khoản đăng nhập.
- Kiểm tra nhân sự đang hoạt động, đúng vai trò/cơ sở, phân công còn tồn tại và không trùng giờ. Các ca tiếp giáp được phép. Không giao/nhận ca đã bắt đầu hoặc buổi đã hủy/kết thúc. Buổi đang mở phải giữ ít nhất một trọng tài.
- Lưu lại cùng danh sách giữ nguyên ID phân công và xác nhận. Gỡ rồi giao lại tạo ID mới; đổi ngày, sân hoặc khung giờ làm mất hiệu lực xác nhận cũ và yêu cầu nhận lại.
- Giữ quyền vận hành hiện có dựa trên phân công; xác nhận nhận ca không thay đổi điều kiện mở đăng ký hoặc quyền điểm danh/đánh giá.

## Lưu trữ và API

Đã kiểm tra chỉ đọc schema localhost/Sep490: session_referees, session_staff và audit_logs có đủ cột. Không thay đổi schema/data ứng dụng trong quá trình triển khai. Xác nhận lưu trong audit_logs dưới action shift_accepted, kèm ID phân công, người nhận, ngày, khung giờ, sân và thời điểm UTC. Action shift_schedule_changed vô hiệu hóa xác nhận trước khi đổi lịch, kể cả khi đổi về lịch cũ. Hai thao tác chạy trong transaction Serializable.

- GET /api/shifts/mine — Referee/Staff xem ca của mình.
- POST /api/shifts/{sessionId}/accept — Referee/Staff xác nhận, có CSRF và kiểm tra cơ sở; gọi lại không tạo thêm sự kiện.
- API manager /api/sessions/manage và /assignments được tái sử dụng. SessionAssignee bổ sung accepted, tương thích các trường hiện có.

## Kiểm thử

- dotnet build be_da/be_da.csproj: thành công, 0 warnings/errors.
- dotnet test tests/TKSC.Api.Tests: 813 passed, 25 skipped, 0 failed; có chạy kiểm thử SQL mới bằng TKSC_SHIFT_TEST_SERVER trỏ localhost với Windows Authentication.
- Kiểm thử SQL tạo database TKSC_ShiftTest_<GUID> riêng rồi dọn trong finally; bao phủ nhận ca hai vai trò, sai người/cơ sở, trùng ca, gọi lại, lưu không đổi, gỡ/giao lại, đổi lịch, ca đã qua/hủy và bỏ trọng tài cuối cùng khỏi buổi mở. Không áp dụng migration cho Sep490.
- npm run build: thành công, còn cảnh báo kích thước bundle có sẵn.

Các SQL integration test cũ cần biến môi trường riêng vẫn được skip; kết quả trên không thay thế nghiệm thu trên tài khoản thật trong trình duyệt.
