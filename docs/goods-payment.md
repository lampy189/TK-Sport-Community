# UC-30 (UC-37 SRS) — Thanh toán hàng hóa và xuất hóa đơn

Triển khai theo yêu cầu SWL 3 ngày 02/11/2026: chuyển khoản qua payOS; Staff xác nhận thủ công khi thu tiền mặt. Đối chiếu UC-36/37 và BR-33–35 trong `docs/requirements/RP3-Software-Requirements.txt`. Không dùng đơn social cho hàng hóa.

## Hợp đồng bàn giao Cart từ Quân

- Cart phải được lưu trong `orders` và `order_items`: `status = 'Cart'`, `payment_state = 'Unpaid'` (chấp nhận null), các dòng có số lượng dương, đơn giá chốt, subtotal và total khớp. Không nhận total/giá từ trình duyệt ở bước thanh toán.
- Bên tạo Cart chịu trách nhiệm kiểm tra giá được duyệt và tồn kho/giữ hàng. Baseline hiện chưa có dữ liệu reservation/expiry cho hàng hóa; phần này không tạo cơ chế giữ kho hay JOB-04. Cần nối hợp đồng giữ kho của UC-36 trước khi dùng luồng bán hàng đầy đủ trong vận hành.
- Gọi API với `orders.id`, không phải `social_booking_orders.id`. Có thể điều hướng Staff về trang chủ với `?goodsOrderId={id}` để mở đúng đơn.
- Sau khi Cart chuyển `PendingPayment`, không cho phía bán hàng sửa dòng hàng. Backend lưu snapshot để chặn xác nhận nếu dữ liệu thay đổi trong lúc chuyển khoản.
- Giao diện **Staff → Bán hàng** đọc danh sách đơn đã có; không thay phần tạo giỏ hàng của Quân.

## API

Chỉ role Staff; cookie đăng nhập. POST cần `X-TKSC-Request: 1` và `X-CSRF-TOKEN` từ `/api/auth/csrf`.

| Method | Đường dẫn dưới `/api/v1/commerce/goods-orders` | Kết quả |
| --- | --- | --- |
| GET | `/` | 100 đơn gần nhất |
| GET | `/{id}` | Dòng hàng, trạng thái tiền, hướng dẫn và hóa đơn đã xuất |
| POST | `/{id}/payment-instructions` | Tạo/lấy lại link payOS cho cùng payment; `PendingPayment` / `Pending` |
| POST | `/{id}/confirm-transfer` | Đọc giao dịch từ payOS, chỉ xác nhận khi khớp đầy đủ |
| POST | `/{id}/confirm-cash` | Body `{ "receivedAmount": 50000 }`; Staff xác nhận đúng số tiền của đơn |
| POST | `/{id}/invoice` | Sau khi trả đủ tiền: sinh mã `INV-G-{id:D10}`, `status = 'Invoiced'` |

QR trong màn hàng hóa mở trang payOS; trang payOS cung cấp QR ngân hàng/thông tin chuyển khoản. Nút kiểm tra chuyển khoản không tin trạng thái trên URL hoặc thông tin do trình duyệt khai báo. Dùng cấu hình payOS đang có; URL quay về hàng hóa mang `goodsOrderId` thay cho `orderId`.

`payments.goods_order_id` trỏ đến đơn hàng hóa, `order_id` luôn null; mã reference bắt đầu `GOODS-`. Thanh toán thành công: `payments.status = 'paid'`, `orders.payment_state = 'Paid'`, `orders.status = 'Paid'`. `handover_state` không đổi. Khách đưa tiền mặt thừa: Staff trả tiền thừa trước, nhập số tiền thực thu bằng tổng hóa đơn.

Webhook payOS hiện có tự phân luồng theo payment đã lưu. Dùng cùng external identity `payos:{bank-reference}`, cùng transaction lock và bảng `bank_transactions` với thanh toán social để chống dùng lại một giao dịch. Số tiền/currency/link/order phải khớp; tổng tiền từ payOS phải đủ, trạng thái `PAID`. Sai tiền, thanh toán muộn, thay đổi dòng hàng chuyển `NeedsReview`; không tự phát hành hóa đơn hay giao hàng. Thanh toán nhiều lần với số tiền lẻ cần đối soát, không tự cộng gộp.

Mỗi link có hạn 15 phút lưu trong snapshot audit. Không tự mở link mới, đổi từ chuyển khoản sang tiền mặt hoặc bỏ qua giao dịch chưa rõ kết quả; cần đối soát đơn hết hạn/đang chờ. Luồng xử lý đối soát hàng hóa và giải phóng giữ kho thuộc phần tiếp nối, chưa triển khai ở đây.

## Hóa đơn và chống lặp

Không thêm/sửa schema hay chạy seed. Snapshot checkout, dòng hàng đã thu tiền và hóa đơn được lưu trong `audit_logs` với `entity_type = 'GoodsOrder'`. Hóa đơn lưu tên khách, dòng hàng, số tiền, phương thức, mã thanh toán, người xuất và thời gian. Đọc lại/xuất lại trả cùng bản ghi; không sinh mã hoặc thu tiền lần nữa. Giao diện hiển thị hóa đơn và tải bản `.txt`. Đây là hóa đơn bán hàng nội bộ, chưa tích hợp hóa đơn điện tử thuế.

Ghi nhận tiền mặt và xuất hóa đơn dùng transaction Serializable cùng application lock. Không ghi ticket, refund, tồn kho hoặc trạng thái bàn giao trong các thao tác này.

## Kiểm thử và giới hạn xác minh

`dotnet build be_da/be_da.csproj`, `dotnet test tests/TKSC.Api.Tests`, `dotnet test tests/PaymentTests`, `npm run build` tại `fe_da`.

Tests kiểm tra tiền VND, dòng hàng/tổng tiền, chống tràn số, giao dịch sai link/order/currency, thanh toán muộn/trùng trạng thái, CSRF, role và URL quay về riêng cho hàng hóa. Chưa chạy giao dịch thật payOS hoặc kiểm thử ghi SQL Server. Không áp dụng thay đổi database trong phiên này.
