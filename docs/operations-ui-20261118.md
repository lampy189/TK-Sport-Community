# Hoàn thiện màn vận hành — 18/11/2026

## Phạm vi đã nối

| Người dùng | Menu | Chức năng |
|---|---|---|
| Manager | Chính sách & giá | Chọn cơ sở được gán, đề xuất chính sách hoặc phiên bản giá hàng/buổi chơi, thời gian hiệu lực, lý do; xem trạng thái và quyết định. |
| Owner | Chính sách & giá | Lọc cơ sở, xem phiên bản, duyệt/từ chối bằng lý do. Giữ kiểm tra độc lập: không tự duyệt đề xuất của mình. |
| Owner, Manager, Staff | Báo cáo tổng hợp | Chọn khoảng ngày, cơ sở theo quyền; tiền vé/hàng/hoàn/thu ròng, biến động tồn, vật tư, đối soát ca và khoản còn chờ xử lý. In bằng trình duyệt. |
| Các vai trò vận hành | Trang chủ | Số buổi, vé điểm danh và chỉ số công việc theo vai trò lấy từ API thật; lỗi tải hiển thị rõ, không thay bằng số mẫu. |
| Owner | Phê duyệt | Tổng hợp số yêu cầu chính sách/giá, quyền tài khoản, ngoại lệ và hoàn vé; mở màn xử lý tương ứng. Không tự thay đổi quyền phê duyệt. |
| SystemAdmin | Cấu hình kỹ thuật | Xem mức sẵn sàng cấu hình máy chủ, số lượng hàng đợi; bật/tắt các worker được phép với lý do và revision. Không đọc/trả khóa bí mật qua API. |
| SystemAdmin | Sự cố | Ghi nhận/cập nhật sự cố Mới/Đang xử lý/Đã xử lý; lưu phiên bản trong audit hiện có. |

Nguồn: yêu cầu 7 hạng mục ngày 18/11 và lựa chọn chỉ làm UI cho các ngoại lệ; SRS sạch nhóm UC-33–35/46 (Owner), UC-43–45 (kho và đối soát), UC-47–48 (kỹ thuật). Số UC trong bản Doc3 41 UC khác bản SRS sạch; không coi số UC hai bản là tương đương.

## Ba trường hợp chỉ có giao diện chờ xử lý

- **Tiền khách đưa lớn hơn tổng đơn:** hiển thị tiền thừa dự kiến; không cho xác nhận số tiền vượt tổng đơn. Số tiền bằng đúng tổng đơn vẫn dùng luồng tiền mặt hiện có. Không thêm hạch toán tiền thừa.
- **Session 0đ:** hiển thị đang chờ quy trình; không mở đăng ký mới từ màn chi tiết, không tạo link payOS từ đơn 0đ đang chờ. Chưa tự phát hành vé miễn phí. Đơn cũ tiếp tục theo hạn giữ chỗ hiện có.
- **Vật tư thiếu/hỏng/chênh lệch:** có dấu chọn thiếu/hỏng và đối chiếu số báo dùng với số thực trả; chặn nút chốt khi chưa khớp, hiển thị chờ Manager xử lý và hướng sang Ngoại lệ vận hành hiện có. Chưa tạo quy tắc điều chỉnh kho hoặc tự tăng tồn. Thông báo chờ là trạng thái giao diện, không tự tạo phiếu nghiệp vụ mới.

## Giữ nguyên quyết định chấm điểm

Công bố hoặc Manager chỉnh điểm không hủy vé tương lai, không tạo hoàn tiền và không sửa phân bổ thanh toán. Các thay đổi này đã có ở working tree trước đợt UI; giữ lại cùng bộ `RatingTicketIsolationTests`.

## Lưu trữ và giới hạn

- Không sửa schema, không chạy migration, không seed dữ liệu trong đợt này. Thiết lập kỹ thuật và sự cố sử dụng `audit_logs` hiện có. Revision theo audit ID; chống ghi đè phiên bản cũ trong transaction.
- Worker đọc override đã lưu, nếu chưa có thì dùng cấu hình máy chủ. Khi không đọc được cấu hình từ DB, tạm dừng lượt xử lý đó. Bật dọn reservation cần bảng/trigger giữ hàng đã triển khai.
- payOS/SMTP/Firebase chỉ hiển thị đã có trường cấu hình cần thiết; đó không phải xác nhận kết nối thực tế. Môi trường kiểm thử hiện báo thiếu cấu hình ba dịch vụ này.
- Báo cáo dựa trên doanh thu đã ghi sổ. Khoản thanh toán đã thu nhưng chưa có revenue entry được tách riêng và hiện cảnh báo; không tự cộng giả hoặc tự áp dụng trigger doanh thu. Các trigger doanh thu trên DB hiện tại cần được kiểm tra/triển khai theo quy trình DB riêng trước khi dùng báo cáo làm số liệu tài chính chính thức.
- Nhánh làm việc NhatLT đã chứa origin/main `0f062fb` lúc đối chiếu; không đổi nhánh hoặc ghi đè phần sửa chưa commit. Các thay đổi được bàn giao trên nhánh NhatLT.

## Kiểm thử và cách xem

1. Owner mở Chính sách & giá, chọn một cơ sở cụ thể, gửi đề xuất; người khác có quyền duyệt xem lý do, hiệu lực và lịch sử.
2. Manager chỉ thấy đối tượng tại cơ sở của mình. Staff không được vào chính sách; SystemAdmin không được vào báo cáo kinh doanh.
3. Báo cáo chọn tháng có giao dịch, kiểm tra các bảng và danh sách khoản chưa ghi sổ; không coi số 0 của ngày không có dữ liệu là lỗi tải.
4. SystemAdmin xem Cấu hình kỹ thuật và Sự cố; bật worker thiếu cấu hình phải bị từ chối. Hai tab cập nhật chung cùng nguồn dữ liệu.
5. Kiểm tra tiền khách đưa thiếu/bằng/thừa; trường hợp thừa phải giữ nút xác nhận bị khóa. Vật tư chưa khớp hoặc đánh dấu hỏng phải giữ nguyên tồn khi chưa xử lý.

Kết quả: BE build không lỗi/cảnh báo; .NET tests 854 pass, 23 skipped (bộ SQL yêu cầu cấu hình riêng). FE production build qua; ESLint các màn mới qua. Kiểm tra Chrome tự động với Owner/Manager/Staff/SystemAdmin đã mở 8 màn, API đọc trả đúng quyền, không lỗi JavaScript; không gửi email, không thanh toán thật và không ghi thêm giao dịch test vào DB.
