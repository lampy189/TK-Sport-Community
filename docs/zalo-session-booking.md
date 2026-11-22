# Đăng ký vé hiện có qua Zalo OA

## Phạm vi đã chốt ngày 19/11/2026

Người dùng đã có OA. Chỉ bán vé/suất buổi chơi hiện có; không xây dựng thuê nguyên sân.
Khách khai báo trình độ ngay trong cuộc trò chuyện, không bắt liên kết tài khoản website.
Bot hiểu tin nhắn tự do, hỏi lại thông tin thiếu, tra SQL Server, báo vé và giá,
xin xác nhận trước giữ chỗ, gửi ảnh QR thanh toán payOS và thông báo kết quả trên Zalo.

Áp dụng UC-08 (giữ chỗ, kiểm tra trình độ, sức chứa, trùng lịch), UC-11 (giá và mã đơn cố định),
UC-12 (chỉ xác nhận từ giao dịch đã kiểm chứng). Khai báo qua Zalo là yêu cầu mới thay bước
khảo sát ban đầu cho khách kênh này; không phải đánh giá đã được trọng tài xác nhận.

## Đã chuẩn bị, chưa phải tính năng chạy hoàn chỉnh

- `ZaloWebhookVerifier`: xác thực chữ ký trên body gốc, kiểm tra đúng app/OA,
  chỉ nhận tin nhắn khách; từ chối dữ liệu bị sửa, JSON trùng thuộc tính, payload vượt giới hạn.
- `ZaloOaClient`: gửi văn bản/ảnh đã upload, token chỉ đặt trong header,
  yêu cầu Zalo trả mã tin nhắn thành công; không coi HTTP 200 có lỗi nghiệp vụ là thành công.
- Các bài kiểm thử dùng HTTP giả, không gửi tin cho khách thật.
- `appsettings.Zalo.example.json`: mẫu cấu hình tắt mặc định, không chứa token thật.
- Migration `db/upgrades/021-zalo-session-booking.sql`: đã soạn, CHƯA áp dụng.

Chưa đăng ký webhook endpoint/worker với runtime để tránh nhận rồi làm mất tin khi chưa có
kho inbox bền vững. Chưa tạo khách, vé, QR hoặc gọi Zalo/payOS thật trong công việc này.

## Dữ liệu đề xuất để duyệt

Migration chỉ tạo sáu bảng mới nếu chưa tồn tại:

| Bảng | Mục đích |
| --- | --- |
| zalo_customers | Định danh bằng cặp OA ID + Zalo sender ID; tham chiếu hồ sơ nội bộ khi tạo |
| zalo_skill_declarations | Lịch sử trình độ khách tự khai theo môn và nguồn tin nhắn |
| zalo_conversations | Trạng thái hội thoại, hạn dùng, revision chống ghi đè |
| zalo_inbox | Tin đã xác thực, khóa duy nhất chống Zalo gửi webhook lặp |
| zalo_booking_orders | Liên kết khách với đơn vé và báo giá họ đã xác nhận |
| zalo_outbox | Tin/QR cần gửi và kết quả gửi, chống phát sinh thông báo lặp |

Không sửa/xóa bảng hoặc bản ghi cũ; không seed vé hay giá; không đổi chính sách RLS.
Script chạy trong transaction và có khóa migration. Cần backup đã xác minh trước áp dụng.
Không có lệnh tự động DROP khi rollback: bảng có giao dịch thật phải được giữ để đối soát.

## Phần cần triển khai tiếp sau khi duyệt dữ liệu

1. Webhook HTTPS kiểm tra chữ ký rồi ghi inbox, chỉ phản hồi thành công sau khi lưu bền vững.
   Worker tuần tự theo khách, khử trùng lặp, phục hồi khi restart; giới hạn kích thước và tần suất.
2. Nhận dạng AI có ngữ cảnh với hành động cho phép: tìm vé, cung cấp tên/trình độ,
   chọn vé, xác nhận, xem thanh toán. AI không được tự chọn người nhận, tài khoản ngân hàng,
   giá, user ID hay SQL. Khi AI chưa cấu hình, thông báo rõ và chuyển nhân viên.
3. Lưu khai báo theo môn. Hỏi lại nếu chưa ánh xạ được về band hệ thống. Không tự cho điểm
   chính thức hay sửa hồ sơ đã được trọng tài đánh giá. Giữ nguồn `ZaloSelfDeclared` để vận hành
   phân biệt với khảo sát và đánh giá sân. Cần tạo hồ sơ khách nội bộ phù hợp FK hiện hữu;
   không tự gộp với người khác dựa trên tên hoặc số điện thoại khách gõ.
4. Đọc buổi chơi và giá hiệu lực từ SQL. Trước giữ chỗ, hiện cơ sở, sân, giờ, band, tiền và
   thời hạn; xác nhận chỉ có hiệu lực với báo giá còn hạn. Giá đổi phải xin xác nhận lại.
5. Gọi nghiệp vụ đăng ký sẵn có và payOS, giữ kiểm tra sức chứa/trùng lịch. Phục hồi sau lỗi
   theo cùng mã giao dịch; webhook/tin xác nhận lặp không tạo thêm đơn.
6. Lấy QR chuyển khoản từ giao dịch payOS thật, dựng ảnh trên server, upload Zalo rồi gửi
   attachment. Không tự ghép tài khoản ngân hàng từ nội dung chat hoặc dùng QR chỉ mở website
   để thay cho QR chuyển khoản. Gửi kèm số tiền, nội dung và hạn thanh toán.
7. Chỉ phát thông báo thành công sau webhook payOS đã xác minh và transaction DB commit.
   Sai tiền/hết hạn/chưa đủ điều kiện vào đối soát; ảnh biên lai khách gửi không chứng minh đã trả.
8. Token OA hết hạn cần refresh có lưu an toàn và khóa đồng thời; không lưu token ở Git.
   Kiểm tra quyền/gói OA và điều kiện gửi tin. Timeout gửi tin là trạng thái chưa biết,
   không tự gửi lặp vô hạn. Ngoài cửa sổ tư vấn, dùng mẫu phù hợp đã duyệt hoặc chuyển nhân viên.

## Kiểm tra môi trường hiện tại

Đã đọc metadata `localhost / Sep490`: chưa có bảng Zalo. Bảng `payments` có link payOS,
nhưng hiện không lưu payload QR. Kiểm tra trực tiếp cũng xác nhận `tickets.price_version_id`,
`OwnerEffectivePrice` và `OwnerPriceVersion` đã tồn tại; ghi chú thiếu upgrade 019 ngày 16/11
không còn phản ánh đủ trạng thái này. Không áp dụng lại upgrade 019 trong thay đổi Zalo.

Build backend thành công, không warning/error. Bộ API test: 878 passed, 26 skipped,
0 failed (bao gồm 13 bài kiểm thử transport Zalo). Các bài SQL bị skip không phải bằng chứng
đã chạy migration hoặc đã đặt vé qua Zalo.

## Cấu hình

Merge phần `Zalo` từ file mẫu vào cấu hình server. Dev dùng `be_da/appsettings.Local.json`
(đã ignore); triển khai dùng `Zalo__Enabled`, `Zalo__AppId`, `Zalo__OaId`,
`Zalo__OaSecretKey`, `Zalo__AccessToken` từ secret store. File example không tự được load.
OpenAI và payOS vẫn cần cấu hình hợp lệ riêng. Chưa có endpoint công khai để đăng ký với OA.
Không gửi token/secret vào cuộc trò chuyện hoặc commit vào Git.

Nguồn chính thức đã đối chiếu:
- https://docs.zaloplatforms.com/docs/OA/webhook/tin-nhan/su-kien-nguoi-dung-gui-tin-nhan
- https://docs.zaloplatforms.com/docs/OA/tin-nhan/tin-tu-van/gui-tin-tu-van-dinh-kem-anh
