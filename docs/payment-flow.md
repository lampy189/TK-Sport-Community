# Thanh toán payOS và vé social

Phạm vi nghiệp vụ: Report 3, phần I, mục 1–4; BF-01 và UC-09, UC-10, UC-11,
UC-24. Nhà cung cấp thanh toán là payOS theo yêu cầu của người dùng.

## Cấu trúc 4 layer trong một project

Backend chỉ có một project ứng dụng: `be_da/be_da.csproj`. Các layer là folder:

```text
be_da/
  Controllers/
    PaymentsController.cs  HTTP, xác thực và phân quyền
    Middlewares/           Chuyển lỗi nghiệp vụ thành HTTP response
    Program.cs             Đăng ký dependency, cấu hình host
    appsettings.json       Cấu hình API
  Application/
    Interfaces/            Interface use case, repository và gateway
    Services/              Điều phối nghiệp vụ thanh toán
    DTOs/                  Dữ liệu request/response
    Validators/            Kiểm tra đầu vào và quyền trên đơn
    Mappings/              Chuyển entity sang DTO
  Domain/
    Entities/              Entity hiện có của hệ thống
    Payments/              Quy tắc xác nhận thanh toán
  Infrastructure/
    Interfaces/            Interface tạo client payOS
    Repositories/          Truy cập dữ liệu SQL Server
    DbContext/             Sep490Context
    Configurations/        Mapping EF Core và cấu hình client
    Migrations/            Script cập nhật schema
    Services/              Adapter SDK payOS
    DependencyInjection.cs
  be_da.csproj
```

Controller chỉ gọi Application. Application sử dụng interface và Domain;
Infrastructure triển khai interface, truy cập SQL Server và payOS. Domain
không tham chiếu EF Core, SDK payOS hay ASP.NET. Một project không cưỡng chế
ranh giới dependency như nhiều assembly; cần giữ quy ước này khi bổ sung code.

## Chạy giao diện minh họa

Mặc định giao diện gọi API thật và chuyển đến checkoutUrl của payOS khi tạo
link thành công. Người trả tiền không đăng nhập tài khoản quản trị payOS.

Để thử minh họa, tạo `fe_da/.env.local` với `VITE_PAYMENT_MODE=demo`.
Trong `fe_da`, chạy `npm install` rồi `npm run dev`. Chọn Người chơi → Thanh toán.
Đơn thử có hạn 15 phút. Cổng minh họa hỗ trợ thành công, thất bại/hủy, sai số tiền.
Không phát sinh giao dịch ngân hàng; dữ liệu localStorage không có giá trị vào sân.

- Thành công: đơn `paid`, mỗi vé của người chơi/bạn bè là `confirmed` và có mã riêng.
- Thất bại/hủy: giao dịch `failed`; được thử lại nếu đơn còn hạn.
- Thiếu/thừa tiền, đến muộn hoặc không khớp: `review_required`, chưa kích hoạt vé.
- Đã trả tiền nhưng chưa có kết quả: nhập nội dung và Gửi yêu cầu kiểm tra.
- Staff/Manager → Thanh toán: xem giao dịch cần kiểm tra, gồm sự kiện không khớp đơn.
- Tạo lại dữ liệu minh họa: khởi tạo đơn thử mới.

## Kết nối dữ liệu thật

1. Dùng database SQL Server hiện có, chạy `be_da/Infrastructure/Migrations/20261020-payos.sql` để thêm
   cột mã/link payOS và raw payload cho Bank Transaction. Script không xóa dữ liệu.
2. Cấu hình qua biến môi trường hoặc user-secrets:
   `ConnectionStrings__Sep490`, `Payments__PayOS__ClientId`,
   `Payments__PayOS__ApiKey`, `Payments__PayOS__ChecksumKey`,
   `Payments__PayOS__ReturnUrl`, `Payments__PayOS__CancelUrl`.
   Xem `be_da/Controllers/appsettings.Payment.example.json`; file mẫu không tự được nạp.
3. Đăng ký webhook HTTPS công khai `/api/v1/commerce/payment/webhook/payos`
   trong kênh payOS. ReturnUrl/CancelUrl trỏ về frontend; backend thêm orderId.
4. Đặt frontend `VITE_PAYMENT_MODE=api`; cấu hình `API_PROXY_TARGET` theo URL backend.
   Chạy `dotnet run --project be_da/be_da.csproj`. Dùng reverse proxy HTTPS cùng
   origin cho frontend/API và cookie đăng nhập.

Client ID, API Key và Checksum Key chỉ nằm ở backend. SDK payOS 2.1.0 ký yêu cầu,
xác minh chữ ký phản hồi và webhook HMAC-SHA256. Giá trị tiền là VND nguyên,
không nhân 100. `payments.id` làm orderCode payOS ổn định cho từng lần thanh toán;
kênh payOS phải dành riêng cho ứng dụng/database này để tránh trùng mã.
Không reset identity payments trên một kênh đã có giao dịch.

Repository chưa có đăng nhập và chức năng tạo đơn/giữ chỗ. Module đăng nhập cần
phát cookie scheme `Cookies`, claim `NameIdentifier` = users.id; claim Role
`staff`/`manager` cho người được cấp quyền. Bộ chọn vai trò UI chỉ là preview,
không cấp quyền API. Cookie có Secure; API trả 401/403 khi chưa có quyền.

Module đặt chỗ tạo SocialBookingOrder `pending`, tổng tiền dương, hạn thanh toán
và các Ticket `reserved` đã qua điều kiện tham gia/giữ chỗ; tổng vé bằng tổng đơn.
Mở `/?orderId=<id>` để vào thanh toán. Khi giữ chỗ bị thu hồi, module đặt chỗ phải
đổi trạng thái vé/đơn theo cùng cơ chế khóa dòng đơn. Thống nhất chuỗi trạng thái
với CHECK constraint của database. Thanh toán không thay thế kiểm tra đăng ký.

## API

### HTTPS khi chạy Vite trên Windows

`npm run dev` khởi động Node với `--use-system-ca` để tin cậy chứng chỉ .NET
đã có trong kho chứng chỉ Windows. Cần phiên bản Node hỗ trợ cờ này
(máy phát triển hiện dùng Node 24.11). Kiểm tra bằng
`dotnet dev-certs https --check --trust`. Sau khi thay đổi script, dừng Vite
và chạy lại `npm run dev`; HMR không thay đổi tùy chọn TLS của tiến trình Node.

Các route dưới `/api/v1/commerce/payment`:

| Method | Route | Quyền |
| --- | --- | --- |
| GET | `/orders/{id}` | Người trả tiền |
| POST | `/orders/{id}/process` | Người trả tiền |
| POST | `/orders/{id}/refresh` | Người trả tiền |
| POST | `/orders/{id}/review`, JSON `{"reason":"..."}` | Người trả tiền |
| GET | `/tickets` | Vé bản thân hoặc vé trong đơn mình trả tiền |
| GET | `/reviews` | Staff/Manager |
| POST | `/webhook/payos`, JSON payOS | Chữ ký hợp lệ |

POST từ UI gửi `X-TKSC-Request: 1`; không mở credentialed CORS tùy ý.
Trình duyệt quay lại không thể tự xác nhận thanh toán. Kiểm tra trạng thái gọi
server để cập nhật hủy/hết hạn; vé chỉ được xác nhận khi có bank event hợp lệ.

Lần tạo giao dịch được lưu trước khi gọi payOS. Nếu timeout, retry dùng cùng
orderCode; adapter khôi phục link đã tạo qua API lấy thông tin. Không gọi mạng
trong transaction SQL. Khi nhận webhook, hệ thống xác minh chữ ký và mã link,
lưu Bank Transaction với raw payload, match status, cùng audit. Sự kiện ngân hàng
không khớp đơn vẫn được lưu để Staff kiểm tra và được trả HTTP 200.

Khóa dòng đơn UPDLOCK/HOLDLOCK tuần tự hóa các cập nhật; sp_getapplock theo mã
ngân hàng và unique index external_id chống xử lý lặp. Cập nhật payment, order,
từng ticket và event nằm trong một transaction. Giao dịch bổ sung sau khi đã trả
đủ được gắn cờ kiểm tra; không tạo thêm vé hoặc tự thu hồi vé đã xác nhận.
Raw payload chỉ lưu ở database, không trả qua danh sách review thông thường.

Module cung cấp gắn cờ và danh sách kiểm tra; quyết định đối soát, chứng từ hoàn
trả và phê duyệt điều chỉnh tiền thuộc quy trình Staff/Manager, không tự phê duyệt.
Xác nhận tiền vào không đồng nghĩa hoàn tiền đã hoàn tất.

## Kiểm thử và tài liệu kỹ thuật

- `dotnet build be_da/be_da.csproj`
- `dotnet test tests/PaymentTests/PaymentTests.csproj`
- Trong fe_da: `npm run build`, `npm run lint`

Tests là project kiểm thử riêng ngoài backend, không phải một layer ứng dụng.
Unit test dùng repository/gateway giả để kiểm tra nghiệp vụ; chưa thay thế kiểm
thử transaction SQL Server, đăng nhập thực và tài khoản payOS thật. Chưa áp dụng
SQL vào database của người dùng hoặc thực hiện chuyển tiền trong phiên triển khai.

Nguồn tích hợp: [payOS .NET SDK](https://payos.vn/docs/sdks/back-end/net/),
[payOS API](https://payos.vn/docs/api/),
[chữ ký webhook](https://payos.vn/docs/tich-hop-webhook/kiem-tra-du-lieu-voi-signature/).

### Tích hợp sau merge Google/session

- Giao diện chính có mục Thanh toán và Vé & lịch chơi; sau khi đăng ký thành công, chọn Tiếp tục thanh toán qua payOS.
- Vé Pending từ đăng ký mới được chấp nhận để thanh toán; mã vé tạm chỉ có hiệu lực sau webhook đã xác thực.
- API dùng cookie đăng nhập Google TKSC và CSRF cho process/refresh/review.
- Staff/Manager có mục Kiểm tra thanh toán.
- Vite chạy HTTP localhost:5173; ReturnUrl/CancelUrl phải dùng đúng địa chỉ frontend đang chạy.
- Kiểm thử thêm: dotnet test tests/TKSC.Api.Tests.

### Đồng bộ khi quay về từ payOS
Frontend gọi refresh khi mở đơn. Backend lấy chi tiết giao dịch từ API payOS có xác thực, đối chiếu mã link, số tiền và điều kiện vé; dữ liệu URL không phải bằng chứng thanh toán. Giao dịch lấy từ API và webhook dùng chung khóa payos:reference để tránh ghi nhận trùng. Trạng thái PAID thiếu chi tiết giao dịch không tự phát hành vé. Vẫn cần webhook HTTPS công khai để cập nhật khi người dùng không quay về website.
