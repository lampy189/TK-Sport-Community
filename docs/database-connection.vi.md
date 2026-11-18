# Kết nối database từ cấu hình cục bộ

Backend giữ khóa `ConnectionStrings:Sep490`; tên database thực tế nằm trong connection string và có thể khác `Sep490`. Không lưu thông tin xác thực hoặc manifest dữ liệu lab vào Git.

## Cấu hình Development

Mở User Secrets của project `be_da/be_da.csproj` và giữ các khóa Google, SMTP hoặc API đang dùng. Thêm hoặc sửa riêng kết nối SQL:

```json
{
  "ConnectionStrings:Sep490": "Server=tcp:<HOST>,<PORT>;Database=<DATABASE>;User ID=<USER>;Password=<PASSWORD>;Encrypt=True;TrustServerCertificate=True"
}
```

Đây là mẫu cho lab dùng chứng chỉ tự ký; nếu SQL Server có chứng chỉ được tin cậy, dùng `TrustServerCertificate=False`. Với server nội bộ, kết nối VPN đã được cấp trước khi chạy ứng dụng.

Trong Development, backend đọc User Secrets rồi `be_da/appsettings.Local.json`; biến môi trường `ConnectionStrings__Sep490` hoặc tham số dòng lệnh có thể ghi đè. Kiểm tra các nguồn này khi đổi database và khởi động lại backend.

Không dùng địa chỉ `localhost` của một máy Windows khác làm địa chỉ SQL trên máy Mac. Thông tin host, cổng và loại xác thực cần do người quản lý DB cung cấp.

## Khi sử dụng bản SQL hoặc backup mới

Đối chiếu schema với bản code trước khi chuyển kết nối. Kiểm tra policy `SportCenterIsolation`, hai trigger `TR_payments_revenue`/`TR_refunds_revenue`, cùng các predicate ngoại lệ `sc_player_exception`, `sc_profile_exception`, `sc_user_exception`.

Các script lịch sử có thể có guard tên DB. Không sửa guard rồi chạy hàng loạt; cần xem tác động và được người phụ trách DB chấp thuận. Không chạy reset/seed trên DB đã có dữ liệu chỉ để khắc phục lỗi kết nối.

ID buổi, vé, cơ sở, SKU và tài khoản trong dữ liệu mới có thể khác bộ fixture cũ. Test tích hợp phải kiểm tra quyền và tính nhất quán theo dữ liệu thực, tránh giả định ID hoặc tiền tố SKU của một bản seed cụ thể.

## Kiểm thử SQL opt-in

Đặt `TKSC_TEST_SQL_CONNECTION` cục bộ tới DB test được chấp thuận, sau đó chạy:

```sh
dotnet test tests/TKSC.Api.Tests
```

Nếu không có biến này, các test SQL opt-in được bỏ qua. Một số test tạo fixture hoặc đơn hàng trong transaction và rollback; chúng vẫn cần quyền ghi trên DB test và không được trỏ tới production.
