# PAY-03 — Player thanh toán hàng hóa

Nguồn: yêu cầu PAY-03 của người dùng; RP3 UC-37 / API-01, FE-10/FE-15, BR-13/14/34–36/49. Luồng Player tự trả tiền mở rộng API hiện có; xác nhận tiền vẫn qua payOS và tách khỏi đơn vé social.

## API

Base: `/api/v1/commerce/my-goods-orders` (Player đăng nhập).

| Method | Đường dẫn | Kết quả |
| --- | --- | --- |
| GET | `/{id}` | Chi tiết đơn, dòng hàng, trạng thái, tham chiếu, link còn hạn và hóa đơn nếu đã xuất |
| POST | `/{id}/payment-instructions` | Tạo hoặc dùng lại link payOS cho đơn của Player |
| POST | `/{id}/refresh` | Truy vấn payOS, xử lý sự kiện ngân hàng và trả kết quả hiện tại |
| GET | `/{id}/invoice` | Đọc hóa đơn đã xuất; 409 nếu chưa có |

Các POST yêu cầu `X-TKSC-Request: 1` và CSRF như các API commerce hiện có. BuyerId luôn lấy từ phiên đăng nhập. Đơn không tồn tại, đơn của người khác hoặc đơn quầy không gắn BuyerId đều không thể truy cập qua API này (404).

## Hành vi

- Kiểm tra BuyerId trước khi đọc thông tin thanh toán/hóa đơn hoặc gọi payOS; kiểm tra lại trong cả hai transaction tạo link.
- Ghi PayerId bằng BuyerId. Retry kể cả sau timeout giữ nguyên Payment.Id, tham chiếu, giá/dòng hàng đã chốt và thời hạn 15 phút.
- Trình duyệt quay về hoặc trạng thái PAID thiếu giao dịch ngân hàng không tự xác nhận tiền. Dùng dữ liệu payOS đã xác minh, đối chiếu đơn/link/tiền tệ/số tiền/hạn và chống ghi trùng giao dịch.
- payOS xác nhận hủy/hết hạn/thất bại mà chưa nhận tiền: cập nhật PaymentState tương ứng, ẩn link và giữ nguyên tham chiếu cũ để đối soát khoản chuyển đến muộn. Không tự mở lại Cart.
- Sai số tiền hoặc chuyển muộn đi vào NeedsReview. Thanh toán không tự giao hàng hoặc trừ kho.
- Player chỉ đọc hóa đơn Staff đã xuất. Không cấp quyền thu tiền mặt, xuất hóa đơn hoặc truy cập API Staff. Hóa đơn hiện có là bản ghi nghiệp vụ trong hệ thống.

Không đổi schema, không thay frontend. Frontend có thể dùng `goodsOrderId` tại URL trả về payOS để gọi POST refresh rồi GET chi tiết/hóa đơn; không dùng tham số success/cancel từ trình duyệt để quyết định đã thanh toán.

## Kiểm chứng

`PlayerGoodsPaymentTests`: chủ đơn/người khác/đơn Guest, quyền role và CSRF, retry/timeout, kiểm tra lại chủ đơn sau gọi gateway, xác nhận ngân hàng, thiếu sự kiện, sai tiền, trạng thái hủy/hết hạn/thất bại và đọc hóa đơn không xuất lại.

Chạy `dotnet build be_da/be_da.csproj`, `dotnet test tests/TKSC.Api.Tests` và `dotnet test tests/PaymentTests`. Kiểm thử dịch vụ dùng gateway/repository giả; chưa thực hiện thanh toán payOS thật hoặc kiểm chứng transaction SQL Server trong PAY-03.
