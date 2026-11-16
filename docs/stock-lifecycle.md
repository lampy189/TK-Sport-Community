# Vòng đời hàng hóa và vật tư (Dũng, chia việc 13/11/2026)

Nguồn: RP3 UC-36..UC-38, UC-41..UC-44, JOB-04. Database: upgrade `db/upgrades/017-stock-reservation-supply-count.sql` (đã duyệt 13/11/2026, chỉ thêm cột/bảng/index, chạy lại an toàn). `db/schema.sql` và `db/Sep490_Full_Setup.sql` đã có phần 017.

## Nguyên tắc
- Tồn kho chỉ đổi qua phiếu kho (`stock_movements`). Không sửa số lượng trực tiếp trong DB.
- **Còn bán được = tồn kho − đang giữ** (`stock_items.reserved_quantity`). CHECK: `0 ≤ reserved ≤ on hand`, tồn không âm.
- Mọi API kho/vật tư/đơn hàng tự lọc theo cơ sở của người đăng nhập (Row-Level Security của DB có thể đang tắt).

## Hàng hóa
| Bước | Kết quả |
|---|---|
| Tạo giỏ (`POST /api/v1/store/orders/cart`) | Gộp dòng trùng sản phẩm, giữ hàng ngay trong cùng transaction. Hai người mua cùng lúc: chỉ người đến trước được giữ, người sau nhận 409 `stock_unavailable`. Hạn giữ `GoodsOrders:HoldMinutes` (mặc định 15 phút). |
| Thanh toán | Hàng của đơn đã thanh toán được giữ đến khi giao, không hủy được. Link payOS kéo dài hạn giữ bằng hạn link. |
| Hủy (`POST /api/v1/commerce/goods-orders/{id}/cancel`, người mua: `.../my-goods-orders/{id}/cancel`) | Chỉ đơn chưa có tiền. Trả đúng phần giữ của đơn, một lần. |
| Hết hạn (JOB-04, `GoodsReservationWorker`, mỗi `GoodsOrders:ExpirySweepSeconds` giây) | Đơn chưa thanh toán quá hạn → `Expired`, trả hàng. Tiền về muộn → `NeedsReview`, không bán lại hàng đã trả. |
| Giao (`deliver`) → nhận (`receive`) | Lưu người/giờ giao (`orders.delivered_by/at`). Xuất kho `SaleOut` đúng một lần (index `UX_stock_movements_sale_out`), trừ cả tồn và phần đang giữ, lưu người thực hiện. |

## Vật tư buổi chơi
Referee gửi yêu cầu (`POST /api/stock/referee-supplies/{sessionId}/requests`) → Staff cấp (`.../session-supplies/{sessionId}/requests/{id}/issue`, có thể cấp ít hơn) hoặc từ chối (có lý do) → Referee báo số đã dùng và số trả lại (`.../usage` với `usedQuantity`, `returnQuantity`) → Staff xác nhận số thực nhận (`.../reconcile`). Chênh lệch (đã cấp − thực nhận − báo dùng) được ghi lại để kiểm kê. Cấp vật tư không lấy phần đang giữ cho đơn.

## Kiểm kê và điều chỉnh
1. Staff mở phiếu (`GET /api/stock/products/{id}/count-sheet`, có `stockVersion`) và gửi số đếm (`POST /api/stock/counts`). Tồn kho **không đổi**. Kho đổi trong lúc đếm → 409 `STOCK_CHANGED` kèm số mới.
2. Chênh lệch ≠ 0 bắt buộc lý do; tạo bản ghi `approvals` (`entity_type = 'StockCount'`).
3. Manager/Owner duyệt (`POST /api/stock/counts/{id}/approve` với `expectedDifference`) hoặc từ chối (`/reject` với lý do). Không tự duyệt phiếu của mình; System Admin không có quyền.
4. Duyệt ghi một phiếu `Adjustment` gắn với phiếu kiểm kê (index `UX_stock_movements_stock_count`). Gọi lại trả kết quả cũ.

**Điểm nối cho Đức:** module duyệt có thể gọi `IStockCountService.ApproveAsync(countId, deciderId, new StockCountDecisionRequest(note, expectedDifference))` / `RejectAsync(...)`; kho được cập nhật đúng một lần trong cùng lời gọi.

## Số liệu cho báo cáo (Lâm)
- `GET /api/stock/reports/supplies?from=yyyy-MM-dd&to=yyyy-MM-dd`: theo mặt hàng của cơ sở — nhập, bán, cấp buổi chơi, thu hồi, đã dùng, thất thoát, xuất khác, điều chỉnh +/−, tồn, đang giữ.
- `GET /api/stock/reconciliation[?onlyMismatches=true]`: kiểm tra tồn = tổng phiếu kho, đang giữ = tổng dòng đơn còn giữ.

## Giao diện
Staff: Kho & vật tư (cột Đang giữ/Khả dụng, nút Kiểm kê, tab Kiểm kê & điều chỉnh, tab Báo cáo & đối soát, yêu cầu vật tư trong tab Vật tư buổi chơi). Manager: Kho & vật tư → Duyệt điều chỉnh kho. Referee: Vật tư buổi chơi (gửi/hủy yêu cầu, báo dùng + trả lại). Bán hàng và Đơn hàng của tôi: số còn bán, hạn giữ, nút Hủy đơn.

## Kiểm thử
Unit test trong `tests/TKSC.Api.Tests/Stock` (GoodsReservation, StockCount, SupplyRequest) và `SessionSupplyTests`. Đã chạy thử với DB `dbdoan.sql`: 5 request song song cho 2 khăn còn lại → đúng 2 đơn được giữ, 3 nhận 409; hủy, hết hạn, giao/nhận, cấp/thu hồi vật tư, kiểm kê/duyệt đều khớp đối soát.
