# Vé tham gia phần buổi còn lại

Mở rộng theo yêu cầu mới của người dùng: mở bán suất còn trống trong buổi đang diễn ra, mua phần thời gian còn lại đến giờ kết thúc. Liên quan các luồng RP3 UC-08 (đăng ký), UC-11/12 (thanh toán), UC-15 (ngoại lệ kỹ năng), UC-16 (điểm danh), UC-27 (mở/đóng đăng ký) và phần hỗ trợ UC-32. Đây là mở rộng nghiệp vụ mới, chưa coi toàn bộ nội dung bán vé muộn là yêu cầu gốc trong RP3.

## Hành vi

- Buổi đang diễn ra tại cơ sở còn hoạt động sẽ tự rao toàn bộ chỗ trống. Staff/Manager mở mục **Vé giờ lẻ**, hoặc Manager mở màn **Điểm danh** để theo dõi tại Sport Center được phân công; không nhập giá hay bật rao thủ công.
- Giá = làm tròn lên đến 1 VNĐ của `giá gốc × max(50%, 1 − số phút đã trôi / tổng thời lượng buổi)`. Số phút đã trôi lấy phần nguyên; giá mới được tính lại theo phút, tối đa giảm 50%. Ví dụ buổi 18:00–20:00 giá 80.000: lúc 18:15 là 70.000 (87,5%), 18:30 là 60.000 (75%), từ 19:00 đến trước 20:00 là 40.000 (50%). Tổng thời lượng lấy theo từng buổi, không cố định 2 giờ.
- Chỉ rao các buổi có giá gốc từ 2.000 đến 100.000.000 VNĐ, để giá sau giảm vẫn đạt mức thanh toán tối thiểu 1.000. Số suất có thể bán đã trừ vé trả tiền chưa tới, người đang chơi và giữ chỗ còn hạn; hết chỗ/hết giờ/hủy hoặc dừng buổi thì tự ngừng rao.
- Player lọc cơ sở ở **Lịch social**, chọn **Vé giờ lẻ · Phần buổi còn lại**, xem chi tiết và giữ chỗ. Hệ thống kiểm tra lại tài khoản, trình độ/ngoại lệ, trùng lịch và sức chứa ở backend.
- Giữ chỗ tối đa 5 phút, không vượt giờ kết thúc buổi. Thử lại cùng người chơi trả về đơn đang giữ còn hạn. Backend tính lại giá dưới khóa buổi; giá đã thay đổi so với báo giá client thì yêu cầu tải lại và xác nhận. Client không được gửi giá để ghi đè.
- Đơn dùng lại thanh toán payOS, xác minh webhook và review tài chính hiện có. Giá đơn đã tạo không thay đổi khi giá tự động giảm ở phút tiếp theo.
- Mua lúc 18:35 cho buổi kết thúc 20:00 thì vé hết lúc 20:00. Còn dưới một giờ vẫn được mua; không tự cộng 60 phút kể từ khi check-in.
- Khi xác nhận tiền/check-in, vé giờ lẻ được kiểm tra lại theo giờ kết thúc đã lưu, trạng thái buổi, trình độ và sức chứa. Tiền đến muộn hoặc vé không còn đủ điều kiện được đưa vào review; không tự cấp vé hết hiệu lực.
- Doanh thu đi theo đơn social và cơ sở của buổi gốc. Không tạo nguồn thu thứ hai, không dùng đơn hàng hóa.

## Nâng cấp dữ liệu

Script: `db/upgrades/014-late-entry-sales.sql`. Chỉ tạo mới:

1. `late_entry_offers`: lưu báo giá tự động gần nhất khi giữ chỗ thành công, hạn mức sức chứa buổi và người thực hiện giữ chỗ. Danh sách rao tính trực tiếp từ giờ hiện tại, giá gốc và vé đang chiếm chỗ, không phụ thuộc bản ghi này hay cần job nền. GET không ghi database. Không còn API mở/dừng rao thủ công.
2. `late_entry_tickets`: liên kết vé hiện có với đợt rao, phiên bản và giờ hiệu lực đã chốt.

Script chạy trong transaction và có thể chạy lại. Không sửa cột hay xóa bản ghi ở bảng hiện có. Chưa được tự chạy trên database Sep490: phải được người dùng duyệt theo AGENTS.md.

Kiểm tra chỉ đọc ngày 09/11/2026: Sep490 local chưa có `users.venue_id`, bảng `revenue_entries` và hai bảng vé muộn. Vì vậy **014 riêng lẻ chưa đủ**. Phải duyệt và triển khai nền tảng có sẵn `010-sport-center-accounting.sql` (thêm các cột cơ sở nullable ở users/orders/stock_items/shift_reconciliations và bảng doanh thu), `011-sport-center-security.sql` (policy/function phân quyền theo cơ sở), `012-revenue-posting.sql` (trigger ghi nhận thu/hoàn), rồi 014. Không chạy file `010-stock-units-barcode.sql` vì chỉ trùng số, không thuộc gói này. Không tự gán tài khoản hoặc chứng từ lịch sử vào cơ sở. Manager/Staff chưa được gán cơ sở sẽ bị từ chối thao tác. 014 và API kiểm tra điều kiện nền tảng trước khi cho bán vé muộn.

Trước khi áp dụng, xác nhận đúng server/database và có bản sao lưu theo quy trình dự án. Sau khi áp dụng, kiểm tra tồn tại đủ hai bảng và chạy thử bằng tài khoản Player/Staff/Manager ở hai cơ sở khác nhau. Không tự tạo tài khoản, đơn hoặc thanh toán mẫu trên dữ liệu vận hành.

Khi chưa nâng cấp, danh sách công khai trả về rỗng; thao tác vận hành trả 503 với yêu cầu nâng cấp. Luồng vé social thông thường không truy vấn bảng mới. Không có migration tự chạy lúc ứng dụng khởi động.

## API

| Phương thức | Đường dẫn | Quyền |
|---|---|---|
| GET | `/api/late-entry?venueId=…&sessionId=…` | Công khai, chỉ trả đợt còn mở và còn chỗ trong buổi đang chạy |
| GET | `/api/late-entry/manage` | Staff/Manager, cơ sở từ tài khoản đã xác thực |
| POST | `/api/late-entry/{sessionId}/holds` | Player + CSRF; chỉ nhận `expectedRevision`, không nhận giá do client gửi |

## Kiểm thử

- Rules: giảm theo tỷ lệ cho buổi 1/2/3 giờ; không giảm quá 50%; giá không tăng theo thời gian; làm tròn tiền lẻ; ổn định trong cùng phút; giá thay đổi phải xác nhận lại; từ chối đúng giờ kết thúc; hạn giữ không vượt giờ kết thúc.
- API: giới hạn vai trò, cơ sở từ danh tính thay vì query/body, yêu cầu CSRF trước khi ghi dữ liệu.
- Payment: mặc định từ chối vé LATE chưa được repository kiểm tra lại; thay đổi điều kiện trước webhook không được cấp vé; check-in lại sau hết hạn bị từ chối.
- Các bài test dùng SQL thật được opt-in riêng bằng `TKSC_TEST_SQL_CONNECTION`. Chưa chạy các kịch bản SQL/concurrency/payOS thật trước khi được duyệt script và có cấu hình thử nghiệm.

## Bản thiết kế

`?design=hourly` vẫn là bản mẫu tương tác, không phải cửa mua vé thật. Luồng thật nằm trong các màn Lịch social / Chi tiết buổi / Vé giờ lẻ / Thanh toán / Điểm danh.
