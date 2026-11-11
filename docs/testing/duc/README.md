# Bộ test thủ công của Đức

Có 33 ca trong [test-cases.csv](test-cases.csv). Mở bằng Excel (CSV UTF-8), điền Kết quả thực tế, Pass/Fail và đường dẫn ảnh evidence. Tất cả ban đầu là Not Run; đây là checklist để bạn tự chạy, không phải báo cáo đã pass.

## Tài khoản

Tên hiển thị đơn giản; email đầy đủ là tên + @demo.tksc.invalid.

| Tên | Role | Mục đích |
| --- | --- | --- |
| ductmtest01 | Manager | Xử lý sự cố, quyết định khiếu nại |
| ductmtest02 | Staff | Issue Hub, reply, đổi/hoàn |
| ductmtest03 | Referee | Duyệt ngoại lệ, review khiếu nại |
| ductmtest04 | Player B2 | Ngoại lệ: Chấp nhận |
| ductmtest05 | Player B2 | Ngoại lệ: Từ chối |
| ductmtest06 | Player B3 | Báo cáo sự cố và nhận reply |
| ductmtest07 | Player B3 | Yêu cầu hoàn vé 100.000đ |
| ductmtest08 | Player B3 | Đổi vé 100.000 → 80.000đ |
| ductmtest09 | Player 50/B3 | Khiếu nại: giữ điểm |
| ductmtest10 | Player 50/B3 | Khiếu nại: sửa thành 70/B4 |
| ductmtest11 | Player B3 | Kiểm tra không xem được vé/hội thoại người khác |

Không có mật khẩu hoặc Google subject giả. Đăng nhập thử nghiệm dùng email đầy đủ, ví dụ ductmtest06@demo.tksc.invalid.
Player dùng /player/login; nhân sự dùng /staff/login → Đăng nhập thử nghiệm (Development).
Dùng cửa sổ/profile trình duyệt riêng cho từng vai trò, hoặc đăng xuất trước khi đổi account.

## Chuẩn bị dữ liệu

Script chỉ thêm dữ liệu riêng; không sửa schema, không xóa hoặc reset dữ liệu đã có.
Chưa áp dụng script vào database trong lượt chuẩn bị này.

1. Dùng DB phát triển/kiểm thử có schema và các upgrade hiện tại của LamNV, gồm Sport Center, hỗ trợ vé, rubric BM-CLB-V1. Không dùng DB vận hành vì có chứng từ paid giả lập và trigger có thể ghi nhận doanh thu test.
2. Cấu hình biến môi trường TKSC_TEST_SQL_CONNECTION trên máy; chuỗi kết nối cần quyền tạo dữ liệu test. Backend cũng phải trỏ vào chính database đó. Không commit thông tin kết nối.
3. Từ gốc repo chạy dotnet run --project scripts/duc-fixtures để xem kế hoạch, không kết nối DB.
4. Tạo dữ liệu bằng dotnet run --project scripts/duc-fixtures -- --apply --database TEN_DB_TEST --output /tmp/ductm-test-manifest.json. Thay TEN_DB_TEST bằng đúng tên trong connection string. Trên Windows có thể thay output bằng C:/Temp/ductm-test-manifest.json.
5. Chỉ sử dụng manifest sau khi terminal báo OK. Manifest có email, user ID, session ID/ngày, ticket ID, payment ID và assessment ID thực tế.
6. Chạy backend Development profile https tại localhost:7207 và frontend localhost:5173. Backend phải đọc được cùng DB; dữ liệu test không thay đổi cấu hình đăng nhập Google.

Ví dụ PowerShell để chỉ định connection (tự điền cấu hình DB của bạn):
$env:TKSC_TEST_SQL_CONNECTION = 'CHUOI_KET_NOI_DB_TEST_CUA_BAN'

Nếu tài khoản ductmtest đã tồn tại, script dừng, không ghi đè quyền/điểm/yêu cầu đã xử lý. Dùng manifest của lần tạo trước hoặc DB kiểm thử mới. Script chưa có reset dữ liệu.

## Dữ liệu và thứ tự chạy

Script tạo 11 account, một venue/sân riêng, 6 buổi, 5 vé paid giả lập và 2 assessment công bố.
Tài khoản nhân sự được gán venue này; Referee được phân công các buổi.

| Key trong manifest | Thời điểm | Dùng cho |
| --- | --- | --- |
| exception | Ngày tạo +2, 18:00 | 04/05 xin vào B3 |
| venue | +3, 18:00 | Vé 06 báo cáo, vé 07 hoàn |
| exchange-source | +4, 18:00 | Vé 08 gốc 100.000đ |
| exchange-target | +5, 18:00 | Buổi thay thế giá 80.000đ |
| appeal-future | +6, 18:00 | Hai vé độc lập của 09/10 |
| assessment | Ngày tạo -1, 18:00 | Hai kết quả 50/B3 của 09/10 |

Thứ tự đề nghị:
- EX-01 → EX-06.
- RP-01 → RP-05 trước IS-03 để chứng minh vé bình thường vẫn báo cáo được.
- IS-01 → IS-03; HB-01 → HB-03; RE-01 → RE-03.
- RF-01 → RF-04; CH-01 → CH-03.
- AP-01 → AP-05.
- IS-04 chạy sau IS-03, có thể để cuối.

Mỗi ca CSV có điều kiện riêng. Các ca quyết định/đổi/hoàn tiêu thụ dữ liệu test; không chạy lại toàn bộ trên cùng bộ dữ liệu rồi mong trạng thái trở về ban đầu.
Nếu đã qua ngày buổi chơi, tạo bộ dữ liệu trên DB test mới; không chỉnh đồng hồ hoặc bỏ kiểm tra thời gian của ứng dụng.

## Kết quả và evidence

Tên ảnh gợi ý: EX-03-ductmtest04.png, RP-02-ductmtest06.png.
Ảnh cần thấy trạng thái/lý do sau xử lý; với đổi/hoàn chụp cả Staff và Player.
Các mã DUCTMTEST-REFUND-01 là bằng chứng giả lập chỉ dùng DB test; không chuyển tiền thật.
Ca kiểm tra API có thể chạy qua Swagger/Postman với đúng cookie của account; không thấy menu không đủ chứng minh API đã chặn.

Muốn chạy 3 test tích hợp SQL Server đã bị bỏ qua, sau khi cấu hình TKSC_TEST_SQL_CONNECTION chạy:
dotnet test tests/TKSC.Api.Tests --filter "FullyQualifiedName~DatabaseReadTests|FullyQualifiedName~TicketAftercareReadTests|FullyQualifiedName~SportCenterDatabaseTests"

Test tích hợp và script seed chưa được chạy trên DB thật ở lượt này. Xem lỗi schema/quyền/kết nối nếu có trước khi đánh dấu Pass.
