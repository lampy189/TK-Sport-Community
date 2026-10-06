# Cấu hình đăng nhập Google cho TKSC

Code nằm trong `be_da` trên branch `NhatLT`. Tài khoản đăng ký lần đầu bằng Google nhận role Player. Database localhost/Sep490 đã kết nối được; cột google_subject và unique index đã được thêm theo chấp nhận của bạn. Cần hoàn thành cấu hình dưới đây trước khi đăng nhập bằng Google thật.

## 1. Tạo Google Cloud project

Mở https://console.cloud.google.com/ và tự đăng nhập tài khoản Google dùng quản lý dự án.
Bấm danh sách project ở trên cùng → **New project** → đặt tên `TKSC Development` → **Create**.
Không cần tài khoản Gmail riêng để gửi mail trong luồng này.

## 2. Cấu hình Google Auth Platform

Mở **Google Auth Platform** (một số giao diện hiển thị **APIs & Services → OAuth consent screen**).

- **Branding**: tên ứng dụng `TKSC`; chọn email hỗ trợ và email liên hệ của bạn.
- **Audience**: chọn **External** để dùng tài khoản Google thông thường.
- Khi đang ở trạng thái Testing, thêm email sẽ dùng thử vào **Test users** nếu được yêu cầu.
- Chỉ cần quyền thông tin hồ sơ/email phục vụ đăng nhập; không xin quyền đọc hoặc gửi Gmail.

## 3. Tạo OAuth client

Trong **Clients → Create client**, chọn **Web application** và đặt tên `TKSC Local Backend`.

Tại **Authorized redirect URIs**, thêm chính xác:

```text
https://localhost:7207/signin-google
```

Bấm Create, lấy **Client ID** và **Client Secret**. Đây là thông tin của ứng dụng, không phải mật khẩu Google.

## 4. Lưu cấu hình ở máy

Trong Visual Studio, mở solution `be_da/be_da.slnx`, bấm phải project **be_da → Manage User Secrets**.
Thêm hai khóa dưới đây vào JSON đang mở, thay phần ví dụ bằng giá trị thật:

```json
{
  "Authentication:Google:ClientId": "CLIENT_ID_CUA_BAN.apps.googleusercontent.com",
  "Authentication:Google:ClientSecret": "CLIENT_SECRET_CUA_BAN"
}
```

Giữ các khóa cũ nếu file đã có cấu hình khác. Lưu file và chạy lại backend. Không đặt secret vào appsettings.json hay gửi lên GitHub/chat.

## 5. Chạy backend

Từ thư mục repository, chạy:

```powershell
dotnet dev-certs https --trust
dotnet run --project be_da --launch-profile https
```

Mở `https://localhost:7207/swagger`. Nếu cổng 7207 đang được một phiên backend khác dùng, dừng phiên đó trong Visual Studio trước khi chạy bản mới. Không dùng profile IIS Express vì port/callback khác.

Chưa có frontend nên chưa có nút Google trên trang web. Cách thử luồng bằng form từ trình duyệt, các endpoint và lệnh kiểm tra nằm trong [hướng dẫn backend](../be_da/README.md#run-and-endpoints). Khi cấu hình xong, báo lại để kiểm tra Google thật; không cần gửi Client Secret.

Nếu thấy `redirect_uri_mismatch`, đối chiếu chính xác giao thức HTTPS, port 7207 và `/signin-google` với Google Console. Nếu thấy `google_login_not_configured`, kiểm tra User Secrets của đúng project be_da và chạy ở môi trường Development.
