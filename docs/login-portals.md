# Cổng đăng nhập theo vai trò

Nguồn: UC-01/02/49, FR-01.3 và BR-47; tiếp tục dùng Google theo quyết định trong PROJECT_CONTEXT.md.

- `/`: Player, giao diện hiện có.
- `/admin/login`: SystemAdmin.
- `/staff/login`: Referee, Staff, Manager, Owner.

Role lấy từ database, không phải lựa chọn trên giao diện. Cổng nội bộ chỉ nhận Google subject đã liên kết với tài khoản có sẵn; không tự đăng ký hoặc tự liên kết qua email. Không thay đổi schema hay cấp thêm quyền.

OAuth giữ cổng trong state được bảo vệ, kiểm tra role trước khi cấp cookie, trả về đường dẫn cố định. Các API nghiệp vụ tiếp tục kiểm tra quyền độc lập. API không truyền portal vẫn giữ tương thích với client/Swagger hiện có.

Trong Development, mở mục đăng nhập thử nghiệm để nhập email tài khoản có sẵn. Backend kiểm tra role cho cả luồng này. Production không cung cấp dev-login. Các cổng mới có trang chào theo vai trò; Staff/Manager mở được chức năng thanh toán đã có, chức năng khác chưa triển khai không được giả lập.

Khởi động lại backend sau khi cập nhật. Kiểm thử OAuth dùng Google stub; cần tài khoản Google đã liên kết để kiểm thử đăng nhập thật.
