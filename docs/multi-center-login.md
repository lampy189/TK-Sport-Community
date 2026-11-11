# Nhiều cơ sở — Hà Nội và Vĩnh Phúc

Đã triển khai trên database Sep490 local ngày 10/11/2026 theo yêu cầu được duyệt. Yêu cầu cuối cùng: toàn bộ dữ liệu cũ thuộc Vĩnh Phúc; Hà Nội là cơ sở mới, tạo bộ mẫu riêng. Player được đổi cơ sở; kỹ năng, bạn bè và lịch sử vé/đơn giữ chung theo tài khoản.

## Dữ liệu hiện tại

| Cơ sở | Sân | Ca chơi | Sản phẩm | Nhân sự vận hành |
|---|---:|---:|---:|---:|
| Hà Nội (id 1) | 2 mới | 6 mẫu | 4 mới | 3 tài khoản demo mới |
| Vĩnh Phúc (id 2) | 9 cũ | 129 cũ | 4 cũ | 6 tài khoản cũ |

Hàng, tồn kho, đơn hàng hóa, phân công và lịch sử nghiệp vụ cũ được giữ lại ở Vĩnh Phúc. Không đổi số lượng kho cũ, không tạo thanh toán hoặc doanh thu giả. Trong 129 ca cũ, có 3 ca suspended nên lịch công khai mặc định chỉ trả 126 ca. Owner có quyền toàn chuỗi; Player không bị khóa vĩnh viễn vào cơ sở.

Hàng mẫu Hà Nội: nước suối 120 chai, cầu lông 96 quả, bóng Pickleball 48 quả, khăn 40 cái. Có OpeningBalance và đơn vị tính/barcode riêng. Ca mẫu có buổi đang diễn ra và buổi sắp tới cho hai môn; vé giờ lẻ tự rao khi ca còn chỗ và chưa hết giờ.

## Đăng nhập kiểm tra

Mở http://localhost:5173/player/login để chọn Hà Nội/Vĩnh Phúc trước đăng nhập. Theo yêu cầu giao diện cuối ngày 10/11, không còn thanh đổi cơ sở trong trang lịch hoặc hồ sơ; Player đăng xuất rồi chọn cơ sở khác khi đăng nhập lại. Tại http://localhost:5173/staff/login chọn cơ sở rồi đăng nhập đúng nhân sự được gán.

`db/seed-week-20261110.sql` bổ sung lịch cho cả hai cơ sở từ 10–15/11/2026, giữ các ca đã có và tránh trùng phân công mới. Script còn tạo buổi test #350 tại Vĩnh Phúc cho Hyune: vé 0đ đã điểm danh, không tạo payment, doanh thu hoặc điểm kỹ năng. Các số lượng trong bảng trên là bộ dữ liệu trước khi bổ sung lịch tuần.

Tài khoản Development riêng cho Hà Nội (không có mật khẩu hoặc Google subject giả):
- demo-manager-hn@example.invalid — Manager.
- demo-staff-hn@example.invalid — Staff.
- demo-referee-hn@example.invalid — Referee.

Dùng mục Đăng nhập thử nghiệm (Development), không dùng Google cho các tài khoản mẫu này. Nhân sự Vĩnh Phúc dùng tài khoản cũ. Owner dùng tài khoản cũ để xem toàn chuỗi hoặc từng cơ sở.

## Hành vi và kiểm soát

- Cơ sở đang chọn nằm trong cookie phiên có chữ ký, tách khỏi users.venue_id dùng cho phân công nhân sự.
- Player chỉ xem lịch và mua hàng tại cơ sở đang chọn. Backend chặn sửa venueId, sessionId hoặc productId để truy cập cơ sở khác.
- Khi đăng nhập lại với cơ sở khác, giao diện dùng phạm vi mới. Đơn đã tạo giữ nguyên cơ sở gốc; lịch sử vé/đơn vẫn theo chủ tài khoản.
- Staff/Manager/Referee không tự đổi ngoài phân công. Quyền được đọc lại từ database mỗi request; SQL Row-Level Security bảo vệ dữ liệu nghiệp vụ.
- Owner xem toàn chuỗi hoặc chọn cơ sở. System Admin không được cấp quyền doanh thu.
- Chấm 20 tiêu chí và so sánh điểm cũ/mới được giữ lại; chatbot mới từ LamNV cũng lọc lịch theo cơ sở.

## Migration và sao lưu

Đã backup và RESTORE VERIFYONLY trước thay đổi. Đã áp dụng 010-accounting, 011-security, 012-revenue, 013-catalog; 014 vé giờ lẻ đã có. Áp dụng thêm 010-stock-units-barcode để khớp code kho từ nhánh đã merge. Script 015 đã được cập nhật theo quyết định cuối cùng; 016 sửa dữ liệu của lần rollout đầu sang toàn bộ Vĩnh Phúc. db/seed-hanoi-demo.sql tạo dữ liệu mẫu Hà Nội có kiểm tra chống nhân đôi.

Config local (không commit): SportCenters:Enabled=true; SportCenters:RequireAssignment=true; LateEntry:Standalone=false.

## Kiểm thử đã thực hiện

- Build backend và frontend thành công.
- Toàn bộ 475 test đạt, không có test bị bỏ qua khi bật kiểm tra SQL thật.
- Test database tạo đơn trong transaction rồi rollback: danh mục đúng cơ sở, chặn sản phẩm khác cơ sở, đơn vẫn giữ cơ sở gốc sau khi đổi lựa chọn.
- Test SQL xác nhận thanh toán và bút toán thu cùng transaction, thử lặp không nhân đôi, sau đó rollback.
- API thật: Hà Nội 6 ca/4 sản phẩm, Vĩnh Phúc 126 ca Published/4 sản phẩm; sửa cơ sở bị 403, mua sản phẩm cơ sở khác bị 404; kho Staff mỗi bên chỉ có 4 mặt hàng; Staff đổi cơ sở trái phân công bị 403; Owner thấy cả 2 cơ sở.
- Vé giờ lẻ API đang có 2 ca Hà Nội trong thời gian thử. Đây là số theo thời gian thực, tự hết khi qua giờ kết thúc.

## Phạm vi tiếp theo

Màn quản lý cơ sở/sân/lịch vận hành đầy đủ, chuyển kho giữa cơ sở, và đối soát các chứng từ lịch sử chưa có bút toán thu vẫn cần công việc riêng. Không suy diễn ngày hạch toán hoặc tạo doanh thu cho 21 payment cũ. Không coi ca mẫu hay OpeningBalance là giao dịch thu tiền.
