# Thông báo vé trên web và Gmail

Luồng hiện có: giữ chỗ, thanh toán xác nhận, hủy vé / yêu cầu hoàn, check-in và yêu cầu kiểm tra thanh toán. Người thanh toán và người giữ vé nhận thông báo riêng, không nhận trùng nếu cùng tài khoản. Chỉ thay đổi đã commit mới tạo thông báo. Email không chứng minh đã chuyển tiền hoàn trả.

## Chạy trên máy phát triển

1. Bật xác minh hai bước cho Gmail gửi thư, tạo App Password theo https://support.google.com/mail/answer/185833.
2. Chạy `powershell -ExecutionPolicy Bypass -File scripts/Configure-TicketEmail.ps1` từ repo. Script nhập App Password có che ký tự và lưu vào .NET User Secrets, không ghi vào Git. Script bật gửi thư, gồm cả email đang xếp hàng.
3. Chạy backend bằng profile `https` trong Visual Studio (F5); frontend `npm run dev`.
4. Đặt chỗ: chuông thông báo xuất hiện sau khoảng 2 giây. Email được gửi nền trong khoảng 30 giây nếu SMTP đã cấu hình. Xem cả thư mục spam khi thử lần đầu.

`Email:Enabled` mặc định false; khi thiếu App Password, hàng đợi vẫn giữ nguyên và web vẫn hoạt động. Google sign-in không cấp quyền gửi Gmail, nên cấu hình OAuth đăng nhập không thay thế SMTP.

Các key cấu hình (User Secrets ở development, biến môi trường ở production):
- `Email:Enabled` / `Email__Enabled`: true để bật worker.
- `Email:Username`, `Email:Password`: Gmail gửi và App Password.
- `Email:From`: địa chỉ gửi hợp lệ của tài khoản SMTP.
- `Email:Host`: smtp.gmail.com; `Email:Port`: 587 STARTTLS hoặc 465 TLS.
- `Email:WebUrl`: URL gốc website. Khi triển khai cần thay localhost bằng URL thật để mở từ điện thoại/email.

## Thiết kế và vận hành

Không đổi schema. Dùng bảng notifications có sẵn: mỗi sự kiện một hàng `web` (`unread/read`) và một hàng `email` (`pending/retry1…retry4/sent/failed/suppressed`). `reference_type` là loại sự kiện, `reference_id` trỏ tới vé. Web lấy 50 thông báo gần nhất và tổng chưa đọc. API và SSE chỉ lấy user hiện tại; đánh dấu đọc có CSRF và kiểm tra sở hữu. SSE gửi snapshot khi thay đổi, tự kết nối lại, có polling 30 giây dự phòng. Snapshot tránh bỏ sót ID được cấp trước nhưng commit sau; trình duyệt loại thông báo trùng bằng ID.

Thông báo ghi trong cùng transaction đặt/hủy/thanh toán/check-in. Worker email khóa từng hàng để tránh nhiều worker gửi đồng thời, thử tối đa 5 lần, mỗi vòng 30 giây. `sent` nghĩa là SMTP chấp nhận, không khẳng định đã vào inbox. SMTP không bảo đảm exactly-once: crash sau khi gửi trước khi lưu sent có thể gửi lại; Message-ID ổn định hỗ trợ mail client nhận diện. Lỗi mail không đổi trạng thái vé.

Worker chỉ xử lý tài khoản đang Active, chưa xóa. Không gửi thư cũ cho tài khoản bị khóa. Dữ liệu nội dung email ghi rõ trạng thái hiện tại; nếu hàng đợi chậm, xem Vé của tôi để biết kết quả mới nhất. Hàng `failed` giữ lại để vận hành điều tra; không tự gửi vô hạn. Bảng hiện chưa có thời gian/chi tiết lỗi gửi, nên lịch sử hiển thị theo ID; chưa có giao diện vận hành gửi lại. Không backfill email cho hành động trước khi tính năng được cài.

Kiểm thử: `dotnet test tests/PaymentTests`, `dotnet test tests/TKSC.Api.Tests`, `npm run build` trong fe_da.
