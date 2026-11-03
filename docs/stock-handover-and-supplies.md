# UC-31, UC-41, UC-42 — Giao nhận hàng hóa và vật tư buổi chơi

Phần của Dũng theo file chia task 31/10 (SWL 3 khúc cuối, SWL 2 nối SWL 3). Đối chiếu RP3: UC-38 (Hand over sold items), UC-41, UC-42, SCR-22 FR-22.3/22.4. Không thêm/sửa schema.

## UC-31 — Giao hàng → Nhận hàng → Trừ kho

Nhận đơn `orders` ở trạng thái `Invoiced` từ luồng của Lâm (`docs/goods-payment.md`). Staff → **Bán hàng** → chọn đơn → khung **Giao nhận hàng**.

| Method | Đường dẫn dưới `/api/v1/commerce/goods-orders` | Kết quả |
| --- | --- | --- |
| POST | `/{id}/deliver` | Cần `Invoiced` + `Paid` + hóa đơn. `handover_state = 'Delivered'` |
| POST | `/{id}/receive` | Cần `Delivered`. Trừ `stock_items` theo dòng hóa đơn, ghi `stock_movements` `SaleOut` (`reference_type = 'GoodsOrder'`), `handover_state = 'Received'`, `status = 'Completed'` |

Chỉ Staff; cần `X-TKSC-Request: 1` và `X-CSRF-TOKEN` như API của Lâm. Dùng chung transaction Serializable và app lock của `GoodsPaymentService`. Gọi lại không trừ kho lần hai. Thiếu tồn kho hoặc dòng hàng khác hóa đơn trả 409 và giữ nguyên `Delivered` để đối soát.

Cần chốt với nhóm:

- Đơn Online: `orders` chưa có cột người mua, nên hiện chỉ Staff xác nhận nhận hàng thay khách. Player tự bấm Nhận hàng cần thêm cột liên kết người chơi (Quân cũng cần cho đơn Online) — phải duyệt schema.
- RP3 UC-38 trừ kho ngay khi Staff bàn giao; file chia task trừ khi khách nhận. Code theo file chia task.
- Chưa có giữ hàng khi tạo Cart (xem ghi chú của Lâm). Nếu kho hết trước khi nhận hàng, đơn phải đối soát.

## UC-41 / UC-42 — Cấp phát và chốt vật tư buổi chơi

Staff → **Kho & vật tư** → tab **Vật tư buổi chơi**: chọn ngày, chọn ca, cấp vật tư cho người nhận, cuối buổi nhập số còn dư để chốt.

| Method | Đường dẫn dưới `/api/stock/session-supplies` | Kết quả |
| --- | --- | --- |
| GET | `?date=yyyy-MM-dd` | Ca trong ngày (mặc định hôm nay giờ VN), số vật tư chưa chốt/đã chốt |
| GET | `/{sessionId}` | Người nhận hợp lệ và các vật tư đã cấp |
| POST | `/{sessionId}/issue` | `{ productId, quantity, recipientId }`: trừ kho, ghi `SessionIssue` |
| POST | `/{sessionId}/products/{productId}/reconcile` | `{ returnedQuantity }`: nhập lại kho (`SessionReturn`), đã dùng = cấp − trả, khóa cấp thêm |

GET cho Staff/Manager; POST chỉ Staff. Người nhận phải là trọng tài được phân công (`session_referees`) hoặc Quản lý. Trả lại không vượt số đã cấp; chốt lại lần hai không ghi thêm.

Trọng tài → **Vật tư buổi chơi**: chỉ thấy buổi mình được phân công, xem vật tư đã cấp và báo số đã dùng. Báo dùng không đổi tồn kho; Staff thấy cột "TT báo dùng", form chốt điền sẵn số trả lại = cấp − báo dùng, Staff sửa theo số thực nhận rồi chốt.

| Method | Đường dẫn dưới `/api/stock/referee-supplies` (role Referee) | Kết quả |
| --- | --- | --- |
| GET | `?date=yyyy-MM-dd` | Buổi được phân công trong ngày |
| GET | `/{sessionId}` | Vật tư của buổi; buổi không được phân công trả 404 |
| POST | `/{sessionId}/products/{productId}/usage` | `{ usedQuantity }`: ghi `supply_use_reported` vào `audit_logs`; không vượt số đang cấp, không sửa sau khi Staff chốt |

Chưa có chức năng Quản lý phân công trọng tài (không code nào ghi `session_referees`); Điểm danh, Chấm điểm và Vật tư của trọng tài đều cần dữ liệu này. Khi test local, thêm dòng `session_referees` bằng tay.

Chưa có bảng phân bổ vật tư: số lượng nằm trong `stock_movements` (`reference_type = 'Session'`), người nhận và kết quả chốt nằm trong `audit_logs` (`entity_type = 'SessionSupply'`). Chưa làm bước Trọng tài/Quản lý gửi yêu cầu vật tư trước buổi; nếu nhóm cần trạng thái Requested → Issued → Reconciled đầy đủ thì đề xuất bảng riêng.

## Database cần có

Ngoài `db/schema.sql`, code thanh toán hàng hóa cần `be_da/Infrastructure/Migrations/20261020-payos.sql` (cột `payments.checkout_url`, `payments.gateway_payment_link_id`, `bank_transactions.raw_payload`). Thiếu file này thì mọi API đơn hàng hóa trả 500.

## Kiểm thử

`dotnet test tests/TKSC.Api.Tests` (SessionSupplyTests, StockTests) và `dotnet test tests/PaymentTests`. Đã chạy thử trên Sep490 local với đơn Cart tạo tay: tiền mặt → hóa đơn → giao → nhận trừ đúng 2 sản phẩm một lần; cấp 4 + 2, trả 2, đã dùng 4, tồn kho khớp.
