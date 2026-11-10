# Sport Center và doanh số ngày — triển khai thật

Đã triển khai trên SQL Server localhost, database Sep490 theo yêu cầu ngày 05/11/2026. Dữ liệu DEMO-SC là chứng từ mẫu trong database thật; màn chính gọi API, không sử dụng báo cáo frontend giả lập.

## Chạy và xem

1. Mở be_da/be_da.slnx, chạy profile https trong Visual Studio (cổng 7207).
2. Chạy npm run dev trong fe_da, mở http://localhost:5173/staff/login.
3. Mở Đăng nhập thử nghiệm (Development), nhập một tài khoản dưới đây. Chỉ có trên môi trường development; đăng nhập Google bình thường không thay đổi.
4. Owner: Kinh doanh hoặc Phân quyền Sport Center. Staff/Manager: Thống kê doanh số.
5. Chọn ngày 30/10/2026–05/11/2026 để xem toàn bộ bộ dữ liệu mẫu.

| Tài khoản | Quyền |
| --- | --- |
| demo-owner-sc01@example.invalid | Owner, toàn chuỗi |
| demo-manager-sc01@example.invalid | Manager, Thủ Đức |
| demo-staff-sc01@example.invalid | Staff, Thủ Đức |
| demo-staff-sc02@example.invalid | Staff, Bình Thạnh |
| demo-manager-sc03@example.invalid | Manager, Quận 7 |

Mỗi cơ sở còn có Staff, Manager, Referee và Player mẫu theo dạng demo-{role}-sc01/sc02/sc03@example.invalid. Không có mật khẩu hoặc Google subject giả. Tài khoản nhân sự cũ chưa gán cơ sở cần Owner phân công trong màn Phân quyền Sport Center; hệ thống không tự đoán cơ sở của họ.

## Nâng cấp và seed

Đã áp dụng các file sau trên máy hiện tại. Với máy mới, cài schema nền và các upgrade cũ của ứng dụng trước, sau đó chạy theo thứ tự 010, 011, 012, 013 và seed. Luôn dùng sqlcmd -b -f 65001 để dừng khi có lỗi và giữ tiếng Việt UTF-8:

- db/upgrades/010-sport-center-accounting.sql: thêm venue_id trên users, orders, stock_items, shift_reconciliations; tạo revenue_entries.
- db/upgrades/011-sport-center-security.sql: chính sách SQL Server Row-Level Security, kiểm tra đọc và ghi trên dữ liệu nghiệp vụ và các bảng con.
- db/upgrades/012-revenue-posting.sql: trigger ghi nhận lần đầu chuyển payment sang paid và refund sang completed trong cùng transaction với chứng từ.
- db/upgrades/013-sport-center-catalog.sql: thêm venue_id cho products để ngăn sửa danh mục cơ sở khác; giới hạn thông tin nhân sự/khách liên quan.
- db/seed-sport-center-demo.sql: thêm 3 cơ sở [DEMO-SC], nhân sự, sân, sản phẩm/kho riêng và các chứng từ mẫu. Chạy lặp không nhân đôi chứng từ; không gọi gateway hoặc gửi thông báo.

Ví dụ: sqlcmd -S localhost -d Sep490 -E -C -b -f 65001 -i db/seed-sport-center-demo.sql

Không xóa dữ liệu cũ: hai venues, 5 đơn hàng, 21 payments và 3 dòng tồn kho trước triển khai được giữ lại. Không suy ngày thu từ ngày diễn ra buổi chơi. Các khoản cũ chưa có ngày/cơ sở hạch toán được báo riêng, không cộng nhầm vào số liệu. Hoàn tiền cũ chưa có bút toán thu gốc cần đối soát trước khi hoàn tất.

## Phân quyền và hạch toán

- Cookie được đối chiếu tài khoản hiện tại trên mỗi request; venue_id cũng được làm mới. Chọn cơ sở trên trình duyệt không cấp thêm quyền.
- Mỗi kết nối EF Core/SqlClient đều thiết lập lại role và venue trong SESSION_CONTEXT, tránh giữ phạm vi của kết nối dùng trước đó.
- Staff/Manager/Referee chỉ đọc và ghi trong cơ sở được gán. Người chưa được gán bị chặn. Owner có phạm vi toàn chuỗi, vẫn giữ các giới hạn hành động theo role hiện có. SystemAdmin không có quyền xem doanh thu.
- Player/Guest giữ quyền của các API công khai/cá nhân hiện có. Worker/gateway dùng các luồng xử lý đã có; RLS không thay thế xác thực chữ ký webhook và kiểm tra sở hữu của API.
- Quản trị SQL trực tiếp cần thiết lập SESSION_CONTEXT tksc_role=service cho tác vụ bảo trì được ủy quyền; không lấy role hoặc venue trực tiếp từ header người dùng.
- Ngày ghi nhận là ngày xác nhận thu/hoàn thành hoàn tiền theo UTC+7. Số tiền lưu dạng số nguyên VND. Unique index và transaction chống ghi lặp.
- Mỗi đơn social hạch toán tại đúng một cơ sở. Giải liên cơ sở tạo đơn riêng cho từng cơ sở; cùng mã giải chỉ dùng để liên hệ các khoản, không nhân đôi khoản thu.
- Danh mục/kho hiện theo từng cơ sở và SKU riêng, chưa bổ sung nghiệp vụ chuyển kho hoặc chia một SKU chung cho nhiều cơ sở.

## Số liệu mẫu và kiểm thử

Thu social/giải 24.160.000 VND; hàng hóa 5.145.000; hoàn đã hoàn thành 240.000; thu ròng 29.065.000. Thu ròng không phải lợi nhuận. Thủ Đức: 9.670.000; Bình Thạnh: 8.275.000; Quận 7: 11.120.000.

- dotnet build be_da/be_da.csproj
- dotnet test tests/TKSC.Api.Tests (đặt TKSC_TEST_SQL_CONNECTION để chạy kiểm thử SQL; kiểm thử ghi dữ liệu tự rollback).
- sqlcmd -S localhost -d Sep490 -E -C -b -f 65001 -i db/test-sport-center-isolation.sql
- pwsh -NoProfile -File scripts/test-sport-center-api.ps1 (backend Development đang chạy).
- npm run build trong fe_da; ESLint các file chức năng mới.

192 kiểm thử backend đã đạt, gồm kiểm tra EF ghi bút toán cùng transaction. Kiểm tra API thật đạt: Owner toàn chuỗi, Staff đúng cơ sở và trả 403 khi yêu cầu doanh số cơ sở khác. SQL kiểm tra chặn đọc/sửa/thêm chéo cơ sở và chống ghi lặp; các bản ghi kiểm thử được rollback.

Nguồn nghiệp vụ: yêu cầu UC-34/35 hiện tại, FR-19.3 và BR-44. Mã UC trong SRS snapshot có phần đánh số không nhất quán; dùng mô tả chức năng người dùng đã chốt.
