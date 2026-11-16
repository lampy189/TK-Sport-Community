# PAY-04–PAY-07: bán hàng, giữ tồn và đối soát

Trạng thái ngày 16/11/2026: sau khi người dùng phê duyệt, migration 018 đã được kiểm thử trên bản sao và áp dụng vào Sep490. Kiểm thử đồng thời SQL Server đạt; worker giữ tồn đã bật trong Development user-secrets, có hiệu lực khi backend khởi động tiếp theo. Chưa thực hiện thanh toán payOS thật.

Nguồn: yêu cầu PAY-04/05/06/07; RP3 UC-36–UC-38 (giỏ, xác nhận tiền, bàn giao), UC-12/API-01 (giao dịch xác minh), BR-13/14/34–36/49. Giữ riêng đơn vé và đơn hàng hóa; duyệt nghiệp vụ theo Staff/Manager, giới hạn cơ sở từ backend.

## Kết quả

- PAY-04: giỏ mới chuyển trực tiếp vào phần thanh toán Staff. Thu tiền và xuất hóa đơn trong transaction; retry giữ nguyên Payment/hóa đơn. GET hóa đơn chỉ đọc bản đã xuất. Tải bản TXT và in lại cùng mã, thời điểm, dòng hàng, số tiền, mã thanh toán. Không gọi xuất hóa đơn lại khi tải/in. Bàn giao ghi SaleOut một lần; xác nhận nhận hàng không trừ kho lần nữa.
- PAY-05: giữ hàng ngay khi tạo giỏ, mặc định 15 phút. Khóa dòng stock_items bằng UPDLOCK/HOLDLOCK trước khi đếm giữ tồn hoặc cập nhật; hàng khả dụng = tồn thực trừ Held còn hạn và Committed. Link thanh toán dùng hạn giữ hàng đã có, retry không gia hạn. Hủy/hết hạn trả phần giữ về hàng khả dụng. Trả tiền thành công chuyển Held thành Committed; bàn giao chuyển Committed thành Consumed và ghi stock_movements trong cùng transaction. Trigger bảo vệ phần đã giữ khỏi cả luồng xuất vật tư/xuất thủ công. Không đủ tồn trả 409.
- PAY-06: giữ Payment.Id trước khi gọi payOS; timeout/retry dùng lại mã. Bank external_id dùng chung giữa vé/hàng hóa và khóa transaction, chống dùng một khoản tiền hai lần. Giao dịch muộn/sai tiền/đơn hủy/giữ tồn hết hạn vào review, không cấp vé hoặc giao hàng. Chuyển dư sau khi đã trả tiền giữ nguyên khoản thu hợp lệ, ghi giao dịch dư cần đối soát riêng. Webhook sai thứ tự không tự nâng giao dịch review thành thành công.
- PAY-07: tab “Thanh toán cần kiểm tra” có danh sách chung vé/hàng hóa, chứng từ ngân hàng, nguyên nhân, lịch sử người xử lý/thời gian/bằng chứng. Staff/Manager được kiểm tra lại payOS, ghi chú, duyệt khoản đủ tiền sau xác minh, hoặc đóng khi payOS xác nhận đã kết thúc và không nhận tiền. Không tự duyệt giao dịch của mình; không tự duyệt thiếu/thừa, đơn hàng hóa đã hủy hoặc vượt tồn. Duyệt vé dùng lại kiểm tra điều kiện/chỗ; duyệt hàng có thể giữ lại hàng hết hạn nếu hiện còn hàng. Audit lần yêu cầu được lưu trước network; lỗi được lưu riêng, không vô tình SaveChanges phần nghiệp vụ đã rollback.

Khoản thiếu/thừa vẫn cần quyết định đối soát/hoàn tiền theo quy trình hiện có. Không có nút bỏ qua bằng chứng để cưỡng bức ghi Paid. Hóa đơn ở đây là chứng từ bán hàng nội bộ hiện có, không tích hợp nhà cung cấp hóa đơn thuế.

## Database cần phê duyệt

`db/upgrades/018-goods-stock-reservations.sql`:

1. Thêm bảng `goods_stock_reservations`: order_id, stock_item_id, quantity, expires_at, state, updated_at; PK theo đơn/mặt hàng; FK đơn/kho; index tìm hàng đang giữ.
2. Thêm trigger `TR_stock_items_preserve_goods_holds` trên stock_items, chặn giảm tồn xuống thấp hơn phần đã giữ. EF dùng cách update tương thích trigger, không dùng OUTPUT trực tiếp.
3. Tạo Committed cho đơn đã Paid, chưa Received, chưa có SaleOut để không bỏ quên hàng đã bán trước nâng cấp. Không trừ kho hoặc tạo thêm khoản thu khi backfill. Nếu thiếu stock_item hoặc tổng giữ vượt tồn, rollback toàn migration. Cài bảng, backfill và trigger trong cùng transaction; khóa tồn khi nâng cấp.
4. Không sửa/xóa vé, khoản thu hoặc hóa đơn cũ. Đơn chuyển khoản cũ chưa có giữ tồn phải đối soát nếu nhận tiền; không tự phát hàng.

Kiểm tra **chỉ đọc** trên Sep490 ngày 16/11/2026: chưa có bảng giữ tồn; 2 đơn Paid chưa ghi SaleOut, 4 sản phẩm; không có sản phẩm thiếu stock_item hoặc thiếu tồn cho backfill tại thời điểm kiểm tra. Phải kiểm tra lại khi áp dụng vì dữ liệu có thể đổi.

### Triển khai sau phê duyệt

- Sao lưu và tạm dừng ghi đơn/kho trong thời gian nâng cấp.
- Chạy script 018 với chế độ dừng khi lỗi (`sqlcmd -b` hoặc runner tương đương). Script có thể chạy lại; không thêm trùng các giữ tồn đã tồn tại.
- Bật `GoodsReservations:ExpiryWorkerEnabled=true` trong cấu hình triển khai (không bật mặc định). Worker 30 giây/lần đánh dấu Released. Dù worker tạm ngừng, truy vấn khả dụng vẫn loại Held hết hạn theo thời gian SQL Server.
- Các đường thu tiền/tạo giỏ/giao hàng mới từ chối an toàn với 503 khi chưa có schema/trigger hoạt động. Không triển khai code giữ tồn vào môi trường phục vụ bán hàng trước khi có migration.
- Với đơn Delivered cũ chưa xuất kho, xác nhận nhận hàng dùng Committed được backfill để ghi SaleOut một lần.
- Nếu cần quay lui, dừng bán hàng và đối soát giữ tồn đang hoạt động trước; không xóa bảng giữ tồn trong khi vẫn còn đơn chưa giao.

## API bổ sung

- Staff: `GET /api/v1/commerce/goods-orders/{id}/invoice`, `POST .../{id}/cancel`.
- Player: `POST /api/v1/commerce/my-goods-orders/{id}/cancel`, kiểm tra BuyerId.
- Staff/Manager: `GET /api/v1/commerce/payment-recovery`; `POST /{paymentId}/{recheck|approve|close-unpaid|note}` với `{ evidence, bankTransactionId }`. Evidence 10–1000 ký tự, bankTransactionId bắt buộc khi duyệt. POST yêu cầu CSRF và X-TKSC-Request: 1. Danh sách tối đa 150 giao dịch, ưu tiên review trước.
- API duyệt vé cũ cũng đi qua bước xác minh payOS của recovery service.

## Kiểm thử

- Backend: `dotnet build be_da/be_da.csproj`, `dotnet test tests/TKSC.Api.Tests`, `dotnet test tests/PaymentTests`.
- Frontend: Vite build; ESLint các file thay đổi; `npx playwright test --config tests/payments/playwright.config.js` (API giả, không giao dịch thật).
- Test dịch vụ bao phủ retry thu tiền/invoice/giao/nhận, sai tiền, hết giữ tồn, hủy và chuyển muộn, webhook đảo thứ tự, quyền truy cập và CSRF.
- `GoodsStockConcurrencyTests`: hai transaction tranh mặt hàng cuối, trigger chặn xuất phần đã giữ, hết hạn cho khách khác giữ, xuất kho lặp chỉ ghi một movement. **Đã chạy đạt trên TKSC_PAY018_Test_20261116**; chỉ chạy khi cung cấp `TKSC_PAYMENTS_TEST_SQL` trỏ database kiểm thử có chữ `test` trong tên và đã cài migration. Test tạo/xóa đúng fixture của nó, tuyệt đối không trỏ vào Sep490 đang dùng.
- Ảnh giao diện desktop/mobile và bản in: `docs/evidence/pay-04-07/`. Kiểm thử in/tải xác nhận không POST/xuất chứng từ mới.

## Biên bản áp dụng 16/11/2026

- Người dùng phê duyệt kiểm thử và áp dụng migration trong chat.
- COPY_ONLY backup có CHECKSUM và RESTORE VERIFYONLY đạt: `C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\Backup\Sep490_PAY018_20261116.bak`.
- Khôi phục thành `TKSC_PAY018_Test_20261116`; kiểm thử tranh mua món cuối, hết hạn, bảo vệ hàng giữ và xuất kho một lần đạt. Migration chạy lại trên bản thử: 0 dòng backfill thêm. Lỗi dấu ngoặc trigger phát hiện trong thử nghiệm đã sửa trước khi áp dụng thật; transaction thử lỗi rollback.
- Áp dụng Sep490 thành công: 4 dòng Committed cho 2 đơn; trigger bật; 0 trường hợp tổng giữ vượt tồn. Không ghi thêm khoản thu hoặc SaleOut khi migration.
- Bật `GoodsReservations:ExpiryWorkerEnabled=true` bằng dotnet user-secrets của be_da; không lưu cấu hình bí mật vào Git. Backend chưa được khởi động nền; worker hoạt động khi người dùng chạy F5 tiếp theo.
- Chạy lại kiểm thử đồng thời trên bản thử và dotnet build sau áp dụng: đạt. Giữ bản sao kiểm thử và backup để đối chiếu/khôi phục; chưa xóa.
