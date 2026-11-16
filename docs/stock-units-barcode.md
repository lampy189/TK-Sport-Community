# UC-26, 27, 39 — Đơn vị tính, mã vạch, nhập kho hàng loạt

Phần của Dũng theo file chia việc 05/11/2026. Đối chiếu RP3 SCR-22 FR-22.1, 22.2, 22.6 và UC-39 (một mã hàng dùng một đơn vị khi cộng/trừ kho; nhập trùng phiếu không cộng hai lần). Quy đổi đơn vị và mã vạch là yêu cầu mới của nhóm, RP3 chưa có.

## Chạy trên máy mới

1. Database, chọn một trong hai (đều đã gồm upgrade 010, không cần chạy thêm upgrade nào):
   - Có sẵn dữ liệu mẫu (người chơi, buổi chơi, rubric; kho trống):

```bash
sqlcmd -S localhost -E -C -b -f 65001 -i db/Sep490_Full_Setup.sql
```

   - Database trống, rồi nạp dữ liệu demo:

```bash
sqlcmd -S localhost -E -C -b -f 65001 -i db/schema.sql
```

```bash
sqlcmd -S localhost -d Sep490 -E -C -b -f 65001 -i db/seed-demo.sql
```

2. Tạo `be_da/appsettings.Development.json` (file này không có trong Git) với `ConnectionStrings:Sep490` = `Server=localhost;Database=Sep490;Integrated Security=True;Encrypt=True;TrustServerCertificate=True`. Thiếu file này thì mọi API báo lỗi 500.
3. Tin chứng chỉ dev một lần: `dotnet dev-certs https --trust`.
4. Chạy backend profile `https`; `npm ci` rồi `npm run dev` trong `fe_da` (cổng 5173).

Máy đã có `Sep490` thì chạy `db/upgrades/010-stock-units-barcode.sql`; thiếu upgrade này thì mọi truy vấn đơn hàng hóa báo lỗi 500.

## Quy tắc

- Mỗi mặt hàng có đúng một đơn vị chuẩn (`factor_to_base = 1`) và là đơn vị nhỏ nhất. Tồn kho và `stock_movements.quantity` luôn tính theo đơn vị chuẩn. Ví dụ: Chai = 1, Ca = 8, Thùng = 24.
- Đã có phiếu kho hoặc còn tồn thì không đổi tên, hệ số hay xóa đơn vị cũ; vẫn thêm đơn vị mới và đổi mã vạch được.
- Mã vạch gắn theo từng đơn vị và không trùng toàn hệ thống. Quét mã thùng thì đơn vị là thùng.

## API (Staff, Manager; ghi cần `X-CSRF-TOKEN`)

| Method | Đường dẫn | Ghi chú |
| --- | --- | --- |
| GET | `/api/stock/lookup?code=` | Tra mã vạch hoặc SKU; trả `productId, sku, name, isActive, unitPriceVnd, quantityOnHand, baseUnit, unitName, factorToBase, barcode` |
| POST | `/api/stock/products` | Thêm `units: [{ unitName, factorToBase, barcode? }]`; bỏ trống thì tạo đơn vị `Cái` |
| PUT | `/api/stock/products/{id}` | `units` bỏ trống thì giữ nguyên đơn vị; có `units` thì thay toàn bộ theo quy tắc trên |
| POST | `/api/stock/products/{id}/movements` | Thêm `unitName`; bỏ trống là đơn vị chuẩn |
| POST | `/api/stock/receipts/batch` | `{ sourceRef, lines: [{ code, unitName?, quantity }] }`, tối đa 500 dòng; được cả hoặc không dòng nào |
| POST | `/api/stock/session-supplies/{id}/issue` | Thêm `unitName` |

Player: `GET /api/v1/commerce/my-goods-orders`, `POST /api/v1/commerce/my-goods-orders/{id}/receive` (chỉ đơn của chính mình, sau khi Staff bấm Giao hàng).

Lỗi có dạng `{ code, message }`, `message` bằng tiếng Việt. `400 INVALID_REQUEST`: thiếu trường, sai kiểu, body rỗng. `400 VALIDATION_FAILED`: sai quy tắc. `INVALID_LINES` kèm `lineErrors: [{ lineNo, message }]`. `409`: `SKU_EXISTS`, `BARCODE_EXISTS`, `UNITS_LOCKED`, `INSUFFICIENT_STOCK`, `PRODUCT_INACTIVE`, `SOURCE_REF_USED`, `SESSION_ENDED`. Gửi lại đúng phiếu đã nhập trả `200` với `alreadyPosted = true`, không cộng kho.

## Hợp đồng với Quân (bán hàng, quét mã)

- Quét mã: gọi `GET /api/stock/lookup?code=`.
- `order_items.quantity` luôn ghi theo **đơn vị chuẩn**, đơn giá theo đơn vị chuẩn: bán 1 thùng ghi `quantity = 24`. Phần hóa đơn của Lâm và trừ kho (`SaleOut`) không cần đổi. Chưa có giá riêng theo thùng.
- `StoreController` đã ghi `orders.buyer_id` khi người tạo đơn là Player.

## Giới hạn

- Excel chỉ nhập cho mặt hàng đã có; mã lạ báo lỗi, không tự tạo mặt hàng.
- Một phiếu nhập một mặt hàng theo nhiều đơn vị thì lịch sử chỉ ghi tổng theo đơn vị chuẩn.
- Phiếu kho cũ (trước upgrade 010) không có thời gian và đơn vị nhập.
- Quét bằng camera đã viết nhưng chưa thử trên thiết bị có camera; máy quét dạng bàn phím và gõ tay đã thử.

## Kiểm thử

Unit test: `tests/TKSC.Api.Tests/Stock`, chạy bằng `dotnet test tests/TKSC.Api.Tests`. Báo cáo Report 5.1 và evidence không nằm trong repository.
