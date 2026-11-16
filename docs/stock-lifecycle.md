# Vòng đời hàng hóa và vật tư (Dũng, chia việc 13/11/2026)

Nguồn: RP3 UC-36..UC-38, UC-41..UC-44, JOB-04. Sau khi merge `main` (16/11/2026), phần giữ hàng của đơn dùng thiết kế chung của nhóm: bảng `goods_stock_reservations` (upgrade 018, Quân/Lâm). Phần riêng của Dũng nằm ở upgrade `db/upgrades/020-supply-requests-stock-counts.sql` (chỉ thêm bảng/cột/index, chạy lại an toàn, cần duyệt trước khi chạy; yêu cầu đã có 018). `db/schema.sql` và `db/Sep490_Full_Setup.sql` có phần 020.

## Nguyên tắc
- Tồn kho chỉ đổi qua phiếu kho (`stock_movements`). Không sửa số lượng trực tiếp trong DB.
- **Đang giữ** = các dòng `goods_stock_reservations` của mặt hàng ở trạng thái `Committed` (đã thanh toán) hoặc `Held` chưa hết hạn. **Còn bán được = tồn kho − đang giữ.** Trigger `TR_stock_items_preserve_goods_holds` (018) chặn mọi lệnh làm tồn thấp hơn số đang giữ.
- Lọc theo cơ sở dùng `CenterAccess` như phần còn lại của `main`.

## Hàng hóa (thiết kế của `main`)
Tạo giỏ giữ hàng 15 phút, có idempotency; thanh toán chuyển giữ hàng sang `Committed`; hủy đơn chưa thanh toán (`POST /api/v1/commerce/goods-orders/{id}/cancel`, người mua: `.../my-goods-orders/{id}/cancel`) trả đúng phần giữ của đơn; `GoodsHoldExpiryWorker` trả hàng hết hạn (bật bằng `GoodsReservations:ExpiryWorkerEnabled`); bấm Giao trừ kho đúng một lần (`SaleOut`, có người thực hiện). Phần Dũng thêm: cờ `canCancel` trong đơn để giao diện hiện nút Hủy.

## Kho và vật tư (Dũng)
- Danh mục kho, tra mã, xuất tay: hiện `reservedQuantity` / `availableQuantity`; xuất tay và cấp vật tư không lấy phần đang giữ.
- Vật tư: Referee gửi yêu cầu (`POST /api/stock/referee-supplies/{sessionId}/requests`) → Staff cấp (`.../session-supplies/{sessionId}/requests/{id}/issue`, có thể cấp ít hơn) hoặc từ chối (có lý do) → Referee báo đã dùng + trả lại (`.../usage`) → Staff xác nhận số thực nhận (`.../reconcile`); chênh lệch được ghi lại.
- Kiểm kê: `GET /api/stock/products/{id}/count-sheet` → `POST /api/stock/counts` (không đổi tồn; kho đổi trong lúc đếm → 409 `STOCK_CHANGED`) → Manager/Owner `POST /api/stock/counts/{id}/approve` (kèm `expectedDifference`) hoặc `/reject`. Không tự duyệt; duyệt ghi một phiếu `Adjustment` (index `UX_stock_movements_stock_count`), gọi lại trả kết quả cũ.
- Báo cáo: `GET /api/stock/reports/supplies?from&to`, đối soát `GET /api/stock/reconciliation` (tồn = tổng phiếu kho; đang giữ = dòng của các đơn đang giữ).

## Việc còn lại
- Đức đã có luồng "Ngoại lệ vận hành" loại `stock_adjustment` (duyệt rồi ghi phiếu kho một lần, `IncidentWorkflowController`). Nên thống nhất: phiếu kiểm kê có chênh lệch tạo hồ sơ trong luồng của Đức thay cho nút duyệt riêng, để chỉ còn một nơi duyệt.

## Kiểm thử
Unit test trong `tests/TKSC.Api.Tests/Stock` (StockCount, SupplyRequest, …) và `SessionSupplyTests`.
