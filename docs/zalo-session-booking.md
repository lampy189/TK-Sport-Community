# Đăng ký vé hiện có qua Zalo OA

Hướng dẫn triển khai domain/OA hiện tại và xử lý HTTP 503:
[deploy/README-zalo.md](../deploy/README-zalo.md). Mẫu biến môi trường production
`deploy/runtime.env.example` đã có App ID/OA ID và bật Zalo; điền secrets trên server trước khi dùng.

## Trạng thái bàn giao 25/11/2026

Đã nối backend: nhận webhook có chữ ký → inbox SQL → hiểu tin nhắn bằng OpenAI → tìm vé →
khai tên/điểm → xác nhận báo giá → giữ chỗ → payOS → ảnh QR Zalo → thông báo kết quả.
Chưa bật trên OA thật: thiếu cấu hình Zalo và tên miền HTTPS. Gọi thử OpenAI với khóa hiện tại
trả HTTP 429; cần xử lý hạn mức/credit trước khi xác nhận chất lượng hiểu ngôn ngữ thật.
Không gửi tin cho khách thật hoặc thực hiện chuyển tiền trong quá trình triển khai.

Phạm vi người dùng chốt: chỉ mua một vé cho chính người nhắn của buổi chơi có sẵn, không thêm
thuê nguyên sân. Không yêu cầu đăng nhập website. UC-08, UC-11, UC-12 vẫn kiểm tra sức chứa,
trùng lịch, giá, hạn giữ chỗ và giao dịch ngân hàng đã xác minh.

## Cách chạy

1. Nhập cấu hình Zalo vào `be_da/appsettings.Local.json` khi chạy Development. Mục này đã được
   thêm ở trạng thái `Enabled=false`, giữ nguyên cấu hình AI/payOS/SQL đang có. File bị Git ignore.
   Người pull code có mẫu trong `appsettings.json` và `appsettings.Zalo.example.json`.
2. Cần `AppId`, `OaId`, `OaSecretKey` (khóa ký webhook), `AppSecret` (khóa ứng dụng),
   `RefreshToken`; có thể nhập `AccessToken` để thử ngắn hạn. Hai secret không phải cùng một khóa.
   Lấy token từ ứng dụng đã được quản trị viên OA cấp quyền. Không gửi key/token vào chat/Git.
3. Khi có refresh token, đặt `TokenFile` là đường dẫn tuyệt đối ngoài wwwroot/source, ví dụ trên
   máy phát triển là `%LOCALAPPDATA%/TKSC/zalo-tokens.bin`. Token quay vòng được mã hóa bằng
   ASP.NET Data Protection rồi lưu nguyên tử; không sửa/xóa file này mỗi lần restart.
   Nếu chỉ dùng access token, phải thay token khi hết hạn.
4. Server phải giữ bền vững cả TokenFile và key ring ASP.NET Data Protection, giới hạn quyền đọc
   cho tài khoản chạy backend. Dùng một worker Zalo; các instance web khác đặt WorkerEnabled=false.
   Không sao chép token đã quay vòng về cấu hình ban đầu. Token refresh có kết quả không rõ sau
   lỗi mạng/crash có thể cần quản trị viên cấp token mới, không thử tạo token vô hạn.
5. Backend phải truy cập được SQL Server thật. Chỉ bật `Zalo:Enabled=true` sau khi đã cấu hình
   đủ. `VenueId` giới hạn một cơ sở; null cho phép bot tìm các cơ sở đang hoạt động.
6. Đăng ký webhook Zalo: `https://<backend>/api/integrations/zalo/webhook`, sự kiện `user_send_text`.
   Cấp quyền nhận tin, gửi tin và upload ảnh. Cấu hình xác minh tên miền theo Zalo Developer Portal.
   Endpoint không dùng cookie/CSRF: xác thực bằng chữ ký body gốc và đúng App/OA.
7. Webhook payOS hiện có: `https://<backend>/api/v1/commerce/payment/webhook/payos`.
   Giữ cấu hình `Payments:PayOS` hiện có; không thay bằng endpoint Zalo. ReturnUrl/CancelUrl
   không được dùng làm bằng chứng khách đã trả tiền.
8. Trên server dùng biến môi trường `Zalo__...`, `Chatbot__ApiKey`, `Payments__PayOS__...`;
   appsettings.Local.json chỉ được load trong Development và không được publish.
9. Khởi động lại backend. Kiểm tra `GET /api/integrations/zalo/status` bằng tài khoản SystemAdmin
   để xem trạng thái bật/tắt và số inbox/outbox theo trạng thái; endpoint không trả secret.

## Luồng khách

- “Còn slot cầu lông tối mai không?” → danh sách tối đa 5 vé, kèm cơ sở/sân/giờ/band/giá/chỗ.
- “Vé thứ hai” → hỏi tên và điểm tự khai 0–100 cho môn của vé. Điểm mơ hồ như “trung bình”
  hoặc thang 3.5 không tự biến thành điểm chính thức: bot hỏi lại thang 0–100.
- Khi đủ thông tin, bot hiển thị báo giá có hạn 5 phút. Khách đồng ý rõ ràng mới tạo giữ chỗ.
  Giá/lịch/sân/band đổi, hết chỗ, trùng lịch hoặc hết báo giá đều không tạo vé theo báo giá cũ.
- Khách có hồ sơ nội bộ được định danh bằng OA + sender ID để dùng nghiệp vụ vé hiện có;
  không cấp phiên đăng nhập website. Alias @zalo.invalid không phải email nhận thư và bị chặn
  tại dev-login; email thông báo cho alias này được suppress.
- Điểm chỉ được khởi tạo một lần theo môn với trạng thái `ZaloSelfDeclared`; lưu khai báo riêng
  để phân biệt với khảo sát/đánh giá trọng tài. Tự khai mới không ghi đè hồ sơ hiện có hoặc đã đánh giá.
- Đơn giữ chỗ tối đa 15 phút và không quá giờ bắt đầu. QR lấy từ phản hồi payOS thật,
  dựng PNG trên server và upload Zalo; không dùng AI để tạo tài khoản ngân hàng hoặc số tiền.
- Nếu payOS timeout trước khi payload QR được lưu, bot cung cấp link payOS của cùng giao dịch
  khi khôi phục được; không tạo giao dịch mới chỉ để lấy ảnh. Khi cổng chưa phản hồi, bot báo rõ
  đã giữ chỗ nhưng chưa có thông tin thanh toán. Khách có thể nhắn “gửi lại QR”.
- Webhook payOS đã xác minh cập nhật DB, worker gửi thông báo sau commit. Ảnh biên lai hoặc
  tin “đã trả” không xác nhận thanh toán. Sai tiền/hết hạn vào luồng đối soát hiện có.
- Đặt hộ/nhiều vé, thuê nguyên sân, hủy/hoàn tiền và nội dung không rõ cần nhân viên hỗ trợ.
  Không có thao tác tự chuyển tiền/hoàn tiền qua bot.

## Chống trùng lặp và phục hồi

Inbox có khóa OA/message ID. Mỗi khách được xử lý đúng thứ tự; lỗi tạm thời thử lại tối đa
5 lần rồi gửi hướng dẫn nhờ nhân viên. SQL application lock chỉ cho một runner của OA hoạt động.
Đơn vé và liên kết tin xác nhận được commit cùng transaction; retry sau crash lấy lại đơn đó.
Báo giá được kiểm tra trong transaction giữ chỗ. Không tự cập nhật giá đã xác nhận.

Outbox lưu nội dung trước khi gửi. Gửi thành công cần provider message ID. Nếu gửi xong nhưng
mất phản hồi/crash, trạng thái là unknown, không tự gửi lặp. Upload thất bại là failed. Nhân viên
kiểm tra OA Manager và số liệu status; khách có thể chủ động yêu cầu lại thông tin đơn.
Tin quá cửa sổ 47 giờ kể từ tương tác nhận vào được đánh dấu expired, cần nhân viên hoặc giải pháp
mẫu tin Zalo đã được cấp quyền; không tự gửi mẫu chưa duyệt. QR của đơn đã đóng/hết hạn không gửi.

Model chỉ nhận tin khách vừa gửi, trạng thái hỏi tên/điểm/xác nhận và danh sách buổi chơi công khai.
Không gửi lại tên lưu trong DB, mã vé, mã đơn hoặc link thanh toán từ các câu trả lời trước cho model.
Giới hạn webhook 64 KB, tin 1.200 ký tự, 12 tin/phút/người, tối đa 1.000 tin chờ; chống replay
quá 24 giờ hoặc timestamp tương lai hơn 5 phút. Không ghi secret hoặc response lỗi chứa key vào log.

## Dữ liệu và kiểm thử

Migration `021-zalo-session-booking.sql` đã được người dùng duyệt và áp dụng vào localhost/Sep490
ngày 24/11/2026. Tạo sáu bảng, không sửa/xóa dữ liệu cũ. Backup COPY_ONLY + CHECKSUM đã
VERIFYONLY thành công tại `C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\Backup\Sep490_before_zalo021_20261124.bak` (set 1).

Database kiểm thử riêng `TKSC_ZaloTest_20261124_01` được restore từ bản backup này rồi áp dụng 021.
Toàn bộ khách/vé/giao dịch giả trong test nằm ở DB kiểm thử, không ở Sep490. Bài integration từ
chối chạy nếu tên database không bắt đầu bằng TKSC_ZaloTest_. DB kiểm thử được giữ để đối chiếu.
Không cần migration thêm cho luồng đã triển khai.

- Build backend: thành công, không lỗi/warning nguồn.
- Bộ API: 891 passed, 27 skipped, 0 failed; có bật integration SQL Zalo trên DB riêng.
- Kiểm thử token bổ sung: 1 passed (quay vòng một lần, lưu mã hóa, đọc lại sau tạo provider mới).
- Bộ PaymentTests: 73 passed.
- Integration Zalo: hội thoại → giữ chỗ → PNG QR → xác nhận trả tiền; duplicate tin/transaction,
  hết chỗ, sai định danh, báo giá đổi/hết hạn, không gửi lại khi kết quả gửi unknown.
- Live AI opt-in: chưa đạt do HTTP 429. API suite mặc định bỏ qua test gọi AI trả phí.
- Chưa kiểm thử OAuth OA thật, gửi tin qua OA thật hoặc giao dịch ngân hàng thật.

Chạy lại test Zalo với `TKSC_ZALO_TEST_SQL_CONNECTION` trỏ tới DB kiểm thử và
`dotnet test tests/TKSC.Api.Tests --filter FullyQualifiedName~Zalo`. Chỉ bật
`TKSC_ZALO_LIVE_AI=1` khi chủ động kiểm tra API thật bằng các câu giả, không gửi dữ liệu SQL.

Nguồn chính thức:
- https://docs.zaloplatforms.com/docs/OA/webhook/tin-nhan/su-kien-nguoi-dung-gui-tin-nhan
- https://docs.zaloplatforms.com/docs/OA/tin-nhan/tin-tu-van/gui-tin-tu-van-dinh-kem-anh
- https://docs.zaloplatforms.com/docs/OA/tin-nhan/quan-ly-tin-nhan/upload-hinh-anh
- https://docs.zaloplatforms.com/docs/OA/bat-dau/xac-thuc-va-uy-quyen-cho-ung-dung-new
- https://payos.vn/docs/api/
