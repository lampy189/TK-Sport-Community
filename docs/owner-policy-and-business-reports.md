# OWN-01 / OWN-02 — backend

Nguồn: RP3 SCR-19 / FR-19.1, FR-19.3 (UC-33–35, UC-46 theo bảng màn hình), FR-20.1, FR-22.3–4, đối soát FR-24; yêu cầu OWN-01/OWN-02 ngày 15/11/2026. Backend .NET 8 trong checkout `D:/code_da/TK-Sport-Community`. Không thay giao diện trong đợt này theo phạm vi AGENTS.md.

## Nâng cấp cần duyệt

`db/upgrades/019-owner-policy-versions.sql` chưa được áp dụng. Script thêm:

- `owner_policy_versions`: nội dung, phiên bản theo mã/cơ sở, đối tượng giá, người/lý do gửi và quyết định, khoảng hiệu lực.
- `tickets.price_version_id`, `order_items.price_version_id`: nullable FK; dữ liệu cũ giữ NULL, giữ nguyên toàn bộ số tiền.
- `products.requires_price_approval`: hàng cũ mặc định false; hàng mới do API tạo ở trạng thái ngừng bán, cần giá được duyệt có hiệu lực trước khi Staff mở bán.
- Hai hàm `OwnerEffectivePrice`, `OwnerPriceVersion` để chọn phiên bản theo thời điểm chốt giá.

Không cập nhật số tiền, tồn kho, role hay chính sách RLS. Không tự chạy khi khởi động. Phải triển khai script **trước khi chạy bản backend này** vì truy vấn EF và SQL đã sử dụng cột/hàm mới. Sao lưu theo quy trình vận hành trước khi nâng cấp. Không xóa cột/bảng để rollback khi đã phát sinh đơn dùng phiên bản; rollback ứng dụng phải giữ dữ liệu lịch sử.

## API duyệt

- `POST /api/v1/owner-policies`: Owner/Manager gửi `{code,name,value,reason,effectiveFrom,effectiveTo,venueId,kind,targetId,priceVnd}`. `kind=policy` không có target/giá; `product` trỏ mặt hàng; `session` trỏ buổi chơi. Ngày giờ ISO 8601 có offset. Manager bị giới hạn cơ sở được phân công.
- `POST /api/v1/owner-policies/{id}/decision`: chỉ Owner, `{approve:true|false,reason}`. Không tự duyệt, không quyết định lần hai, không duyệt hồi tố. Hai quyết định đồng thời được khóa trong transaction. Yêu cầu bị từ chối/đã quá ngày phải gửi phiên bản mới.
- `GET /api/v1/owner-policies?code=...&venueId=...`: lịch sử đầy đủ, gồm submitted/approved/rejected. Owner bỏ venueId để xem chuỗi. Manager chỉ thấy cơ sở mình.

Phiên bản mới đóng khoảng hiệu lực của phiên bản cũ cùng đối tượng và ghi audit `policy_superseded`. Không cho khoảng mới đè lên phiên bản đã duyệt có ngày bắt đầu bằng hoặc muộn hơn. Hết khoảng có hiệu lực sẽ dùng giá nền cũ; mặt hàng mới cần duyệt thì bị loại khỏi danh sách mua khi không có phiên bản hiệu lực. Giá nền hàng/buổi cũ được giữ như baseline triển khai, không tự gán người duyệt cho lịch sử.

Mua hàng, đăng ký vé thường/vé giờ lẻ/vé mời, và lựa chọn buổi khi đổi vé dùng giá hiệu lực. Giá và mã phiên bản được chụp tại lần lập đơn/vé; thanh toán vẫn dùng số tiền đã lưu. Vé giờ lẻ tiếp tục áp dụng công thức giảm theo thời gian trên giá nền hiệu lực. Không sửa đơn/vé cũ khi duyệt giá. Sửa thông tin mặt hàng không được trực tiếp đổi giá; hàng mới tạo chưa mở bán.

Chính sách dạng văn bản được lưu và duyệt theo phiên bản; không diễn giải văn bản thành quy tắc phí/hoàn tự động và không tự xuất bản vào chatbot.

## Báo cáo

`GET /api/v1/business-reports?from=2026-11-01&to=2026-11-15&venueId=2`

Owner có thể bỏ venueId để xem từng cơ sở cùng tổng chuỗi. Staff/Manager bị cố định theo cơ sở phía server, không có phân công hoặc yêu cầu cơ sở khác trả 403. Phạm vi ngày tối đa 366 ngày, tính theo Asia/Ho_Chi_Minh, bao gồm ngày cuối.

- `centers` và `totals`: tiền vé, hàng, hoàn đã ghi sổ từ `revenue_entries`, cùng nguồn với báo cáo doanh thu ngày. `net = vé + hàng - hoàn`, không phải lợi nhuận.
- `inventory`: tồn đầu, nhập, xuất, tồn cuối và tồn hiện tại theo đơn vị cơ sở từng sản phẩm. Tồn lịch sử suy ngược từ số dư hiện tại và movement có ngày; `undatedMovements > 0` báo dữ liệu lịch sử thiếu thời điểm, cần kiểm tra trước khi kết luận tồn theo kỳ. Không cộng số lượng các sản phẩm khác đơn vị thành một tổng vô nghĩa.
- `supplies`: theo các buổi có ngày diễn ra trong kỳ; xuất cấp, trả lại, tiêu hao Staff đã đối soát (`reconciledConsumption`), còn chưa chốt (`unresolved`). Đã cấp không đồng nghĩa đã tiêu hao. Đây là trạng thái mới nhất của nhóm buổi trong kỳ, không phải ảnh chụp trạng thái tại cuối kỳ.
- `reconciliations`: chứng từ ca trong kỳ, số dự kiến/thực nhận/chênh lệch và trạng thái; không cộng các chênh lệch vào doanh thu hoặc tự coi chưa có chứng từ là đã khớp.
- `currentOutstanding`: khoản thu Paid chưa ghi sổ và yêu cầu hoàn chưa kết thúc, có ID chứng từ để truy vết. Là tồn hiện tại, không phải tồn lịch sử cuối kỳ vì nguồn cũ không có đủ timestamp. Khoản không xác định được cơ sở không được phân bổ suy đoán.

Các phần được đọc trong một transaction Serializable để không trộn số dư trước và sau một posting đồng thời. Báo cáo không tự sửa dữ liệu đối soát.

## Kiểm chứng và bước tiếp theo

Build backend và bộ `tests/TKSC.Api.Tests`; test mới kiểm tra ngày/giá/loại phiên bản, lý do quyết định, phân quyền trước khi truy cập DB, SQL translation giá hiệu lực, cột giá chốt độc lập, tổng chuỗi và phân biệt xuất/tiêu hao. SQL integration cần migration 019 và cấu hình test DB; chưa tuyên bố đã kiểm chứng transaction duyệt thực tế hoặc đối chiếu số liệu database.

Sau khi được duyệt áp dụng migration: kiểm tra gửi/duyệt/từ chối, tự duyệt, hai Owner duyệt đồng thời, giá tương lai/hết hạn; chốt đơn trước mốc giá mới rồi thanh toán sau mốc; so tổng chuỗi với cộng các cơ sở và sổ thu/hoàn; xác minh Staff không đọc được cơ sở khác.
