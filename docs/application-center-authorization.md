# Phân quyền cơ sở trong backend — 11/11/2026

Theo yêu cầu của người dùng, `SportCenterIsolation` ở SQL Server được tắt. SSMS xem đầy đủ dữ liệu mà không cần đặt `tksc_role`. Không xóa hoặc nhập lại dữ liệu.

Nguồn nghiệp vụ: cập nhật nhiều cơ sở đã duyệt trong `PROJECT_CONTEXT.md`; RP3 UC-16–18 (điểm danh/chấm điểm), UC-37 (thanh toán hàng hóa), UC-39–42 (kho/vật tư). Staff, Manager, Referee chỉ thao tác cơ sở được phân công; Owner giữ quyền toàn hệ thống. Player chọn cơ sở để xem/đăng ký/mua mới, còn hồ sơ và lịch sử cá nhân vẫn dùng chung và được kiểm tra quyền sở hữu.

## Cách thực thi

- Cookie được đối chiếu tài khoản hiện tại ở mỗi request. Middleware xác định cơ sở từ phiên đăng nhập, không tin cơ sở do client tự gửi.
- `Sep490Context.CenterAccess.cs` lọc các entity nghiệp vụ theo cơ sở nhân sự ngay khi tạo truy vấn EF. `SaveChanges` kiểm tra cả bản ghi cũ và khóa ngoại để ngăn cập nhật/xóa bằng entity gắn trực tiếp hoặc thêm dữ liệu liên kết sang cơ sở khác. Nhân sự thiếu cơ sở bị từ chối.
- `CenterAccess` và các repository SQL trực tiếp kiểm tra cơ sở cho kho, barcode, nhập hàng, cấp vật tư, lịch chơi, doanh thu và vé giờ lẻ. Danh sách cơ sở phục vụ đăng nhập vẫn có sẵn để `CenterSelection` kiểm tra tài khoản trước khi middleware thiết lập scope.
- Vé giờ lẻ kiểm tra schema và hạch toán, không kiểm tra policy SQL phải bật.
- Ngữ cảnh kết nối `tksc_venue` vẫn phục vụ các giá trị mặc định hạch toán hiện có. Nó không còn quyết định quyền truy cập SQL. Không bật lại policy để sửa lỗi ứng dụng.

## Triển khai và kiểm tra

Triển khai backend mới cùng `db/upgrades/017-application-center-authorization.sql` cho các môi trường còn bật policy. Không chạy lại script nhập/reset dữ liệu. Database local đã tắt policy theo yêu cầu.

Bật `SportCenters:Enabled=true` và `SportCenters:RequireAssignment=true` trong cấu hình môi trường để yêu cầu chọn cơ sở cho Player và phân công cơ sở cho nhân sự. Cấu hình local đã được cập nhật (file chứa bí mật, không đưa vào Git). Kiểm tra profile `https` thực tế: API báo chế độ nhiều cơ sở bật; Staff Vĩnh Phúc và Hà Nội mỗi người thấy 4 sản phẩm, 2 đơn hàng thuộc cơ sở mình. Backend kiểm thử đã dừng để nhường Visual Studio.

Kết quả: build thành công, 636 test thông thường passed (13 test tùy chọn skipped ở lượt này); chạy riêng 4 test SQL/API mới đều passed. Các kiểm tra SQL chạy trong khi `SportCenterIsolation` OFF; SSMS vẫn đọc được 43 tài khoản khi không đặt ngữ cảnh phiên.

Chạy `dotnet build be_da/be_da.csproj` và `dotnet test tests/TKSC.Api.Tests`. Các test `ApplicationCenterAccessTests` luôn kiểm tra bộ lọc theo vai trò và chặn ghi dữ liệu cơ sở khác. Đặt `TKSC_TEST_SQL_CONNECTION` rồi chạy riêng `ApplicationCenterDatabaseTests` để kiểm tra trên SQL Server với policy tắt: mọi entity có bộ lọc, truy cập ID khác cơ sở, ghi bị từ chối, ghi hợp lệ trong transaction rollback, đăng nhập và API thực tế cho nhân sự hai cơ sở. Factory kiểm thử tắt background workers; không gửi email hay gọi thanh toán.

Các kiểm thử database cũ có thể gắn với dữ liệu demo/ID cũ hoặc policy RLS; không coi chúng là hướng dẫn bật lại RLS. Các test mới chọn cơ sở/ID từ dữ liệu thực tế thay vì hard-code ID Hà Nội/Vĩnh Phúc.
