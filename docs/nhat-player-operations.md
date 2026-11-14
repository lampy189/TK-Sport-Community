# Phần việc Nhật — Player, cơ sở và quyền truy cập (12/11/2026)

Phạm vi được người dùng giao trực tiếp ngày 12/11/2026: ưu tiên hoàn thiện code trên bản refactor mới; môi trường triển khai chung làm sau. Nhánh làm việc: `nhat-refactor-20261112`. Giữ Clean Architecture theo thư mục trong project .NET 8 hiện có. Không chép lại implementation từ nhánh cũ.

## Hành vi đã triển khai

| Yêu cầu / nguồn | Kết quả |
| --- | --- |
| UC-09, UC-10, FR-07/08: mời và trả lời | Lời mời lưu ở server, kiểm tra bạn bè đã kết nối, tài khoản, đúng môn/band hoặc ngoại lệ đã duyệt, lịch trùng, cơ sở/sân và chỗ trống. Gửi lời mời không tạo đơn hoặc giữ chỗ. Chấp nhận kiểm tra lại dưới transaction Serializable và khóa buổi, tạo đúng một vé; retry trả về vé cũ. Từ chối/hết hạn không thu tiền. |
| UC-11: người thanh toán | Chọn mỗi người tự trả hoặc người gửi trả. Mỗi bạn nhận lời có một đơn cố định riêng với `payer_id` đúng người đã chọn, đi qua payment service của Lâm. Màn Đã gửi/Đã nhận hiển thị phản hồi, người trả, hạn giữ chỗ và trạng thái thanh toán từng người. Không thêm đường xác nhận đã trả tiền từ trình duyệt hoặc gộp/chỉnh số tiền của đơn đã phát hành. |
| UC-03–05, UC-25c: Player | Nối điều hướng Lời mời, thanh toán, Vé của tôi, hồ sơ cá nhân/kỹ năng và thông báo. Sửa lỗi tràn thanh tài khoản trên mobile. Mất kết nối khi kiểm tra phiên cho phép thử lại. Ảnh trong form đang sửa chỉ lưu khi người dùng bấm Lưu; kiểm tra kích thước và có trạng thái lỗi/tải. |
| Bạn bè, chat và thông báo | Lưu thông báo web cùng transaction gửi/nhận lời kết bạn và gửi tin nhắn; đọc hội thoại đồng bộ trạng thái thông báo. Trả về tin nhắn gần nhất theo thứ tự đọc. Không để phản hồi tải hội thoại cũ thay thế cuộc hội thoại vừa chọn. Hiển thị lỗi gửi/tải, giữ nội dung để thử lại. Parser thẻ buổi chơi xác thực dữ liệu trước khi render. Thẻ chia sẻ cũ không được coi là chấp nhận lời mời. |
| Cơ sở và sân; yêu cầu trực tiếp + FR-15/16 | Owner thêm/sửa/ngừng cơ sở. Owner và Manager quản lý sân trong phạm vi được phép. Chặn ngừng hoạt động khi còn lịch chưa kết thúc hoặc giao dịch đang xử lý. Không đổi cơ sở của sân; không đổi môn của sân đã có lịch sử buổi chơi. Đổi tên không viết lại giao dịch. |
| Nhân sự cơ sở | Owner gán, gỡ hoặc điều chuyển; lưu cơ sở trước/sau, người thực hiện và thời điểm trong audit. Khi còn lịch chưa kết thúc, yêu cầu người thay thế cùng vai trò tại cơ sở cũ, kiểm tra trùng lịch rồi chuyển phân công trong cùng transaction. Không xóa lịch sử đã qua hoặc sửa tác giả tạo buổi. API phân công cũ cũng đi qua các kiểm tra này. |
| UC-29, UC-34: bổ nhiệm/cấp/thu hồi | Tài khoản đích đã tồn tại và chỉ có một role. Manager duyệt Staff/Referee tại cơ sở mình; Owner duyệt Manager/SystemAdmin và các trường hợp thuộc thẩm quyền. Không tự duyệt hoặc duyệt quyền của bản thân. SystemAdmin chỉ triển khai yêu cầu đã duyệt, kiểm tra lại quyền cũ/phạm vi/thẩm quyền và không triển khai hai lần. Thu hồi chuyển tài khoản sang Inactive, giữ danh tính và lịch sử; chặn khi còn phân công tương lai hoặc là quản trị viên hoạt động cuối cùng. |
| UC-48: bàn giao | Owner phê duyệt người nhận tài khoản kỹ thuật riêng. `approved → provisioned → verified → executed`: chỉ người nhận được xác nhận đã kiểm tra quyền; quyền cũ còn nguyên cho đến bước cuối. Kiểm tra lại cả hai tài khoản trước khi thu hồi. Tài khoản nghiệp vụ không được chuyển thành SystemAdmin. |
| UC-31: tổng quan/phản hồi | Số buổi và vé theo trạng thái trong ngày Việt Nam, giới hạn đúng cơ sở; Owner xem toàn chuỗi. Ghi phản hồi chạy thử kèm actor/cơ sở/thời điểm. SystemAdmin chỉ thấy phần phản hồi kỹ thuật, không được cấp quyền quyết định nghiệp vụ. |

## Dữ liệu và giới hạn hiện tại

- DB làm việc là `localhost / Sep490_Nhat_20261112`. Ngày 14/11/2026, người dùng đã duyệt và đã áp dụng upgrade 017, trigger 012 và policy cách ly cơ sở cùng compatibility 014. Xem `db-update-20261114.md`.
- `db/upgrades/017-player-invitations.sql` bổ sung bốn cột nullable vào `invitations`: `payer_user_id` (FK users), `expires_at`, `created_at`, `responded_at`. Không tạo vé, thay đổi tiền hoặc sửa dữ liệu lịch sử. Lời mời lịch sử thiếu thông tin đồng ý/người trả không tự tạo giữ chỗ.
- Hạn trả lời mặc định là thời điểm sớm hơn giữa 24 giờ sau khi mời và giờ bắt đầu buổi. Giữ chỗ thanh toán tối đa 15 phút và không vượt giờ bắt đầu. Thời hạn của vé giờ lẻ tiếp tục do luồng hiện có xử lý.
- Bản dump ban đầu thiếu `TR_payments_revenue`/`TR_refunds_revenue` và có `SportCenterIsolation` ở trạng thái OFF. Đã khôi phục hai trigger, bật policy và áp dụng visibility 014 theo phê duyệt ngày 14/11/2026. Không chạy lại scripts seed/rollout 015/016: ID cơ sở của dump mới khác baseline cũ.
- Account access dùng bảng `approvals` và audit JSON bất biến của đề xuất; không thêm bảng quyền hoặc một account có nhiều role. Truy cập dữ liệu phục vụ bổ nhiệm dùng identity DB `service` đã có sẵn cho SystemAdmin và các thao tác account được kiểm soát của Manager. Phân quyền HTTP, kiểm tra cơ sở và phê duyệt độc lập vẫn dùng scope/actor thật; đường quản lý sân/cơ sở không được mở quyền theo identity kỹ thuật này.
- Hệ thống hiện có unique email/Google subject, chưa có khóa liên kết một người thật giữa nhiều tài khoản Google khác nhau. Kiểm tra tự duyệt dùng ID/email/Google subject hiện có; không khẳng định đã phát hiện được cùng một người dùng nhiều danh tính Google độc lập.
- Đăng nhập Google thật, payOS thật, gửi push Firebase thật, SMTP, HTTPS production, lưu ảnh dùng chung và backup/restore triển khai chưa được kiểm chứng trong phần code này. Firebase adapter hiện có vẫn là adapter ghi log; thông báo web trong DB hoạt động độc lập với nó.

## Kiểm chứng

- Backend build: thành công, không warning/error.
- API: 644 đạt, không bỏ qua, trên DB kiểm thử riêng có đủ schema/chính sách/trigger nền. Trong đó 7 test mới kiểm tra lời mời, tranh chỗ cuối, điểm thay đổi, hết hạn/trùng lịch, bảo vệ lịch/điều chuyển, quyền theo cơ sở, bàn giao và chat.
- PaymentTests: 71 đạt trên source đã tích hợp phần Quân ngày 14/11/2026.
- Frontend: build thành công; 20 test Node đạt; ESLint không có error trên các file JavaScript/JSX đã sửa/thêm. Vite còn cảnh báo kích thước bundle vốn có.
- Kiểm thử Chrome: người gửi theo dõi lời mời; người nhận nhận lời và mở đúng đơn thanh toán; Vé của tôi/hồ sơ/điểm; mobile không tràn ngang; Owner thêm cơ sở; SystemAdmin đề xuất → Owner độc lập duyệt → SystemAdmin triển khai. Không có lỗi JavaScript trình duyệt.
- Test catalog đa cơ sở đã bỏ giả định ID `1/2` và SKU `HN-DEMO-*`, kiểm tra bằng quan hệ cơ sở thực tế của dữ liệu.

Các test ghi dữ liệu mới chỉ cho phép database có tên bắt đầu bằng `TKSC_Nhat_WorkflowTests_`. Fixture dùng tài khoản tổng hợp `example.invalid`, không ghi vào DB làm việc.

```powershell
# Các biến dưới đây phải trỏ tới DB kiểm thử riêng, không dùng DB làm việc.
$env:TKSC_NHAT_WORKFLOW_TEST_SQL = 'Server=localhost;Database=TKSC_Nhat_WorkflowTests_20261112;Integrated Security=True;TrustServerCertificate=True'
$env:TKSC_TEST_SQL_CONNECTION = $env:TKSC_NHAT_WORKFLOW_TEST_SQL
dotnet build be_da/be_da.csproj
dotnet test tests/TKSC.Api.Tests
dotnet test tests/PaymentTests
```

DB kiểm thử được nhập từ cùng dump, chạy 017, khôi phục hai trigger theo 012, bật policy đã có và kiểm tra visibility theo 014 (guard tên DB được đổi riêng cho test). Ngày 14/11/2026 các cập nhật tương ứng đã được áp dụng vào DB làm việc sau backup và phê duyệt; xem biên bản cập nhật. Không tự áp dụng chúng khi ứng dụng khởi động. Backend do Codex mở để kiểm thử được dừng trước khi bàn giao Visual Studio.
