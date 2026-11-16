# NOT-01 / NOT-02 — email và Firebase push

Nguồn: yêu cầu NOT-01, NOT-02 của người dùng; RP3 mục 87 “View Notifications”, mục 88 “Deliver Event Notification”, mô tả Firebase Service / Email Service. Email không chứng minh đã chuyển tiền hoàn trả.

## Email và gửi lại

SMTP dùng `Email:Enabled`, `Host`, `Port`, `Username`, `Password`, `From`, `WebUrl` trong User Secrets / appsettings.Local.json (development) hoặc biến môi trường (production). Script `scripts/Configure-TicketEmail.ps1` hỗ trợ cấu hình Gmail App Password. Không đưa bí mật vào Git.

Worker gửi tối đa 5 lần mỗi chu kỳ: `pending`, `retry1`…`retry4`, `sent`/`failed`; tài khoản không Active hoặc đã xóa là `suppressed`. Mỗi chuyển trạng thái lưu audit `email_delivery` kèm thời gian UTC. `sent` chỉ có nghĩa SMTP đã chấp nhận, không chứng minh thư đã vào Inbox.

Mở chuông thông báo → **Email & thông báo thiết bị** để xem 50 thư gần nhất và gửi lại thư `failed`. API chỉ cho chủ thư thực hiện, kiểm tra CSRF, khóa hàng trong transaction và ghi audit `email_retry`. Mỗi thư chỉ có **một lượt gửi lại thủ công**, tối đa 5 lần gửi trong lượt đó. Thư đã gửi/đang chờ/bị ngừng gửi không được gửi lại. Không thay đổi trạng thái vé hoặc trạng thái hoàn tiền khi gửi thư.

API: `GET /api/notifications/email`, `POST /api/notifications/email/{id}/retry`. Link email có `view=tickets&ticketId=...`, mở đúng chi tiết vé trong danh sách thuộc tài khoản. URL môi trường phải là HTTPS, ngoại trừ localhost dùng HTTP; cấu hình URL sai chuyển sang trạng thái lỗi thay vì gửi email thiếu link.

SMTP không đảm bảo exactly-once khi máy dừng sau SMTP accept nhưng trước commit. Message-ID ổn định theo ID thông báo hỗ trợ mail client nhận diện. Không quảng cáo bảo đảm không trùng ở tầng SMTP.

## Firebase push

Đã thay log giả bằng Firebase Admin SDK gửi FCM thực tế. Backend đọc:

- `Firebase:Enabled`: mặc định false nếu không cấu hình.
- `Firebase:ProjectId`: project tương ứng với frontend.
- `Firebase:WebUrl`: URL frontend; nếu thiếu dùng `Email:WebUrl`.
- `Firebase:CredentialsPath`: tùy chọn đường dẫn JSON service account, lưu trong User Secrets cho môi trường local; chỉ chấp nhận loại service-account. Nếu không cấu hình, dùng Application Default Credentials: cấu hình `GOOGLE_APPLICATION_CREDENTIALS` trỏ tới JSON service account **ngoài Git**, hoặc danh tính workload của môi trường triển khai có quyền gửi FCM.

Frontend đọc các key public `VITE_FIREBASE_API_KEY`, `VITE_FIREBASE_PROJECT_ID`, `VITE_FIREBASE_MESSAGING_SENDER_ID`, `VITE_FIREBASE_APP_ID`, `VITE_FIREBASE_VAPID_KEY` trong `.env.local` / môi trường build. Xem `.env.example` và `appsettings.Notifications.example.json`. Firebase JS 13.3 yêu cầu Node >=24.12 theo metadata SDK; máy hiện tại 24.11 vẫn build được nhưng npm có cảnh báo engine.

Bấm **Bật thông báo thiết bị** mới xin quyền. Sau khi người dùng bật, ứng dụng làm mới đăng ký token khi đăng nhập/tải lại; tắt thông báo không bị tự bật lại khi reload. `PUT /api/notifications/device` đăng ký token cho tài khoản đăng nhập, `DELETE` hủy token đúng chủ sở hữu. Đăng xuất hủy đăng ký và xóa token trên thiết bị. Unique index có sẵn và transaction khóa dải tránh tạo trùng token khi nhiều tab đăng ký.

Sự kiện vé ghi `web`, `email`, `push` trong transaction nghiệp vụ, loại trùng payer/holder. Thay đổi thanh toán/giao hàng hóa trong GoodsPaymentRepository ghi `web`, `push` cho buyer trong cùng lần SaveChanges. Link riêng `view=goods&goodsOrderId=...` mở đơn tương ứng. Các API nghiệp vụ vẫn kiểm tra quyền sở hữu; link không cấp quyền xem dữ liệu.

Worker push claim nguyên tử `pending` → `sending` trước gọi mạng. Kết quả `sent`, `failed`, `suppressed`, `no_device`; `sent` là FCM accept, chưa phải xác nhận người dùng đọc. Trạng thái `sending` còn lại sau crash là kết quả chưa xác định: **không tự gửi lại** để tránh gửi trùng tới các thiết bị đã nhận. Trường hợp một số thiết bị lỗi được ghi `failed`, không gửi lại toàn bộ nhóm. Đây là đánh đổi tránh gửi trùng, không phải bảo đảm mọi push sẽ đến. Web/SSE vẫn giữ thông báo để người dùng xem.

Token bị FCM trả `UNREGISTERED` bị xóa; lỗi cấu hình/transport không làm xóa token hợp lệ. Token của người nhận được distinct và kiểm tra lại chủ sở hữu trước gửi. Service worker nhận data-only payload, hiển thị một lần theo event ID (cache 500 sự kiện), chỉ mở link cùng origin và đưa về đúng vé/đơn. Không dùng đồng thời notification payload tự hiển thị để tránh hai popup.

## Bằng chứng nghiệm thu

- SMTP probe tới chính mailbox gửi đang cấu hình: `SMTP_ACCEPTED`, event `1794718599787`.
- Đọc Inbox Gmail bằng IMAP read-only, tìm đúng Message-ID: `INBOX_VERIFIED body_link=True refund_wording=True`.
- Link thực tế trong thư: `http://localhost:5173/player/home?view=tickets`. Đây là nghiệm thu môi trường local, chưa chứng minh website production hay mở từ điện thoại khác máy.
- Thư probe ghi rõ kiểm tra kết nối, không tạo giao dịch/hoàn tiền giả. Template email nghiệp vụ và link ticket ID được kiểm tra tự động riêng.
- SQL integration test dùng transaction rollback: kiểm tra chủ sở hữu, chỉ một lượt gửi lại, audit, xóa token hết hiệu lực. Không thay schema, không giữ dữ liệu kiểm thử.
- Chưa nghiệm thu FCM thật trên thiết bị: đã nhận web config project `tksc-adbc2` từ người dùng và lưu trong `fe_da/.env.local` (Git ignored); ProjectId/WebUrl của backend lưu trong User Secrets. Service-account JSON đã được lưu ngoài repo tại `%USERPROFILE%/.config/tksc/firebase-admin.json`; `Firebase:CredentialsPath` lưu trong User Secrets. Xác thực OAuth với scope firebase.messaging thành công (`FIREBASE_OAUTH_VERIFIED=True`). Đã lưu VAPID public key vào `.env.local`, build frontend thành công và bật `Firebase:Enabled` trong User Secrets. Probe FCM HTTP v1 `validate_only` đã qua bước xác thực và trả `INVALID_ARGUMENT` cho token giả có chủ đích; đây không phải bằng chứng giao tới thiết bị. Còn nghiệm thu token thật, nhận popup và bấm mở đúng vé/đơn trên trình duyệt.

Kiểm tra: `dotnet build be_da/be_da.csproj`, `dotnet test tests/TKSC.Api.Tests`, `dotnet test tests/PaymentTests`, `npm run build` tại fe_da, `node --test tests/notifications/push-worker.test.mjs` tại fe_da. Test SQL cần biến `TKSC_TEST_SQL_CONNECTION`; chỉ test mới về notification được chạy với SQL thật trong lượt này.

Để chốt NOT-02 sau khi cấu hình: mở frontend HTTPS/localhost, đăng nhập, bật thông báo; tạo một sự kiện vé với payer=holder, xác nhận đúng một popup/thiết bị và đúng ticket ID; thử payer khác holder; cập nhật đơn hàng và bấm mở đúng đơn; kiểm tra logout/đổi tài khoản không nhận thông báo của tài khoản cũ. Không đánh dấu bước này pass bằng mock.

Tham khảo chính thức: [Web setup](https://firebase.google.com/docs/cloud-messaging/web/get-started), [FCM HTTP v1](https://firebase.google.com/docs/cloud-messaging/send/v1-api), [token lifecycle](https://firebase.google.com/docs/cloud-messaging/manage-tokens).
