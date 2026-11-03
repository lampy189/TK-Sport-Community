# Xử lý vé sau thanh toán

Phạm vi UC-14 đến UC-17 theo yêu cầu ngày 30/10/2026 (số UC theo yêu cầu mới, khác bản SRS cũ).

## Staff / Manager

- Thanh toán: xem thanh toán bị gắn cờ, chọn giao dịch ngân hàng đã liên kết, nhập bằng chứng đối chiếu để duyệt. Số tiền phải khớp và buổi chơi vẫn hợp lệ, còn chỗ.
- Sự cố sân: nhập lý do, tạm ngưng hoặc huỷ buổi chơi. Vé hiển thị cảnh báo, không cho check-in hoặc thanh toán tiếp.
- Đổi & hoàn vé: xử lý yêu cầu đổi và xác nhận từng khoản hoàn sau khi đã chuyển tiền thực tế. Có kiểm tra quyền, CSRF và nhật ký xử lý.

## Player

Vé đã thanh toán của buổi gặp sự cố cho phép yêu cầu đổi hoặc hoàn. Người giữ vé và người trả tiền có thể quản lý; tiền hoàn trả về người trả tiền gốc.

Buổi thay thế phải cùng môn, phù hợp trình độ/lịch chơi, còn chỗ và tối đa 8 người. Yêu cầu đổi chưa giữ chỗ; hệ thống kiểm tra lại khi Staff duyệt. Player có thể rút yêu cầu đang chờ.

- Giá bằng nhau: Staff xác nhận đổi.
- Giá cao hơn: Staff xác minh đã thu phần chênh lệch, nhập mã giao dịch ngân hàng và bằng chứng.
- Giá thấp hơn: hệ thống tạo yêu cầu hoàn phần dư; Staff xác nhận sau khi hoàn thực tế.

Đổi thành công cấp vé và QR mới, vô hiệu vé cũ. Nguồn tiền được lưu qua nhiều lần đổi để thu/hoàn đúng từng khoản. Không tự chuyển tiền ngân hàng; các thao tác Staff ghi nhận giao dịch đã thực hiện. Các hành động tạo thông báo web và email qua hàng đợi hiện có; gửi email phụ thuộc cấu hình SMTP và backend đang chạy.

## Database

Upgrade 006 chỉ thêm ticket_changes và ticket_funding. Đã áp dụng vào Sep490 sau khi người dùng đồng ý. Với database khác, cần được chủ database chấp thuận trước khi chạy:

```powershell
sqlcmd -S localhost -E -d Sep490 -b -i db/upgrades/006-ticket-aftercare.sql
```

## Kiểm tra

Build backend/frontend; kiểm thử PaymentTests, TKSC.Api.Tests, ticketState.test.js và aftercare.test.js. Kiểm thử SQL trong TicketAftercareReadTests chỉ đọc và bật bằng TKSC_TEST_SQL_CONNECTION. Chưa xác minh giao dịch ngân hàng hay gửi email thực tế trong đợt kiểm thử này.

## Trao đổi và thông tin nhận hoàn (31/10/2026)

Upgrade 007 đã được người dùng chấp thuận và áp dụng: thêm ticket_support_messages. Người giữ vé/người thanh toán và Staff/Manager được xem tin nhắn và ảnh theo vé. PNG/JPG tối đa 2 MB, lưu trong database, ảnh chỉ trả qua API xác thực và không cache. Không gửi nội dung tài khoản/ảnh qua email.

Player mở nút Gửi QR / số tài khoản & trao đổi với Staff trên vé bị sự cố, đang đổi, có khoản hoàn hoặc đã đổi. Staff mở yêu cầu hoặc tab Trao đổi với người chơi để trả lời, kể cả sau khi xử lý. Hộp thoại tải lại mỗi 10 giây khi mở. Phản hồi Staff tạo thông báo web/email; tin Player tạo thông báo web cho Staff/Manager. Tài khoản nhận hoàn phải thuộc người thanh toán gốc; Staff đối chiếu trước khi chuyển. Xác nhận đổi/hoàn vẫn dùng thao tác nghiệp vụ riêng, không thay trạng thái chỉ bằng tin nhắn.
