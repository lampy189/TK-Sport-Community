# PAY-02 — Thanh toán hộ bạn đã chấp nhận

Nguồn: RP3 UC-10 (FE-09, BR-03–BR-08), UC-11 (FE-10, BR-08/09/12/13).

## API và dữ liệu

`POST /api/sessions/{sessionId}/invitations/{invitationId}/accept-sponsored`

Yêu cầu đăng nhập Player và CSRF. Đây là thao tác của **người được mời**, đồng ý để người mời trả tiền. Không nhận playerId hoặc payerId từ client. Áp dụng cho lời mời Pending đã tồn tại. API gửi lời mời và lựa chọn tự trả tiền không thuộc thay đổi này.

Trong cùng transaction Serializable:
- Khóa người chơi, buổi và lời mời; kiểm tra đúng người được mời, cơ sở đang chọn và trạng thái.
- Kiểm tra tài khoản, hồ sơ môn, band/ngoại lệ, giờ bắt đầu, đăng ký trùng, lịch trùng giữa các môn và sức chứa (gồm Pending/Reserved chưa hết hạn).
- Tạo một đơn và một vé giữ chỗ cho riêng người được mời; payer_id là user_id của người mời, player_id là người giữ vé. Giá lấy từ buổi tại thời điểm giữ chỗ.
- Lưu Accepted, order_id, ticket_id; ghi audit và thông báo cho người trả tiền. Lỗi làm rollback toàn bộ.
- Gửi lại yêu cầu đã Accepted trả về đăng ký cũ, không tạo vé hoặc gia hạn. Vé cũ hết hạn/hủy vẫn trả về liên kết cũ; client đọc trạng thái hiện tại qua API vé.

Hạn thanh toán dùng thời gian giữ chỗ 15 phút hiện có, giới hạn không vượt giờ bắt đầu. Mỗi bạn có đơn riêng nên giá, trạng thái và hạn thanh toán không bị thay đổi bởi bạn khác. Dùng API thanh toán hiện có với orderId trả về; chỉ payer của đơn được thanh toán. Chấp nhận không xác nhận đã trả tiền.

TicketSummary bổ sung payerId, bên cạnh playerId, holderName, amountVnd, expiresAt và trạng thái thanh toán đã có.

## Phạm vi và kiểm chứng

Không thay schema/database hoặc frontend. Schema lời mời hiện không có cột hạn lời mời hay lựa chọn người trả tiền: endpoint có tên rõ `accept-sponsored`, từ chối các trạng thái khác Pending/Accepted và buổi đã bắt đầu. Việc cấu hình hạn lời mời riêng cần thay đổi dữ liệu ở phạm vi UC-09/JOB-01.

Kiểm thử `SponsoredInvitationTests` bao phủ danh tính từ phiên đăng nhập, CSRF, phản hồi lỗi và phân biệt người giữ vé/người trả tiền/giá/hạn. Chạy thêm toàn bộ TKSC.Api.Tests và PaymentTests. Các kiểm thử này không thay thế kiểm chứng đồng thời trên SQL Server; cần kiểm tra hai người chấp nhận chỗ cuối, chấp nhận lại, sai band, trùng giờ, hết hạn và webhook trên database kiểm thử trước triển khai.
