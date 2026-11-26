# Triển khai Zalo OA của TKSC

Webhook: `https://tksport-136-85-19-220.sslip.io/api/integrations/zalo/webhook`

Source đã có endpoint, worker và file xác minh tên miền. Chỉ pull/build code chưa đủ:
server phải truyền cấu hình Zalo vào tiến trình backend. Không đưa khóa/token vào Git.

## Cấu hình server

1. Nếu đã có `deploy/runtime.env`, giữ các giá trị SQL, Google, OpenAI và payOS đang chạy;
   bổ sung khối `Zalo__...` từ `runtime.env.example`. Không ghi đè file hiện có bằng mẫu.
   Nếu triển khai lần đầu, sao chép mẫu sang `runtime.env` và điền cấu hình thực tế.
2. Mẫu đã bật `Zalo__Enabled=true`, `Zalo__WorkerEnabled=true`, điền App ID và OA ID.
   Điền `Zalo__OaSecretKey` đúng khóa xác thực webhook và `Zalo__AccessToken` còn hiệu lực.
   App Secret là khóa ứng dụng, không tự coi là khóa ký webhook.
   Token đã thử trước đây bị nhà cung cấp từ chối; cần cấp lại token đúng ứng dụng/OA.
3. Nếu dùng Docker CLI, thêm `--env-file deploy/runtime.env` vào lệnh tạo container hiện có.
   Nếu dùng Compose, thêm vào service backend (đường dẫn tương đối với file compose):

   ```yaml
   env_file:
     - ./deploy/runtime.env
   ```

   File `.env` của Compose chỉ dùng thay thế biến, không tự truyền toàn bộ vào container.
   Với systemd/dịch vụ khác, khai báo các biến môi trường tương ứng cho tiến trình .NET.
4. Build lại image từ Dockerfile của repo và tạo lại container bằng cấu hình hiện có.
   Chỉ restart container cũ không nạp các biến môi trường mới của Docker.
   `appsettings.Local.json` không được publish và không được đọc trong Production.
5. Kiểm tra DB server có sáu bảng của `db/upgrades/021-zalo-session-booking.sql`.
   Nếu thiếu, người quản trị cần kiểm tra, sao lưu và áp dụng migration được duyệt trước khi bật bot.
   Không dùng dữ liệu hay connection string localhost của máy phát triển cho server.

## Token tự gia hạn

Để thử ban đầu có thể chỉ cấu hình AccessToken. Khi token hết hạn phải cấp lại.
Để vận hành lâu dài, thêm AppSecret, RefreshToken hợp lệ và TokenFile tuyệt đối ngoài wwwroot.
Giữ bền vững cả thư mục chứa token lẫn key ring ASP.NET Data Protection qua mỗi lần deploy;
tài khoản chạy backend cần quyền đọc/ghi các thư mục này. Không bật refresh với storage tạm.
Xem thêm `docs/zalo-session-booking.md`. Chỉ một worker Zalo cần được bật.

## Kiểm tra sau triển khai

- Mở `/zalo_verifierVzg-2gJ4CrrKkfDMgPKIJqcqgNEbmd8bDpSv.html` trên domain:
  phải trả đúng file xác minh. Dockerfile đã đưa file từ frontend public vào wwwroot.
- Đăng nhập SystemAdmin và gọi `GET /api/integrations/zalo/status`:
  cần thấy `enabled: true`, `workerEnabled: true` và truy vấn SQL thành công.
- Đăng ký đúng URL webhook phía trên ở Zalo Developers, chọn sự kiện `user_send_text`.
- 503 từ endpoint: kiểm tra biến `Zalo__Enabled` thực tế của container/tiến trình đang chạy.
  401: kiểm tra chữ ký, đúng khóa webhook, App/OA và payload; không bỏ xác thực để ép trả 200.
  POST rỗng hoặc tự gửi bằng curl không phải sự kiện Zalo hợp lệ và không được kỳ vọng trả 200.
- Nhắn thử vào OA để kiểm tra tìm buổi chơi, khai trình độ, xác nhận vé và QR.
  AI cần API key có hạn mức; thanh toán cần cấu hình payOS đang hoạt động.
  Chỉ webhook thanh toán đã xác minh mới đánh dấu đã trả tiền.

Tài liệu này chuẩn bị cấu hình trong source; không xác nhận server đã được triển khai
hay Zalo đã chấp nhận webhook. Không tạo đơn/thanh toán thật chỉ để kiểm tra deploy.
