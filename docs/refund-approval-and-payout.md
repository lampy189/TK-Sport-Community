# REF-01 / REF-02 — quyết định và thực hiện hoàn vé

Nguồn: RP3 UC-21, UC-44, SCR-17; quy tắc chứng từ/hạn mức hoàn và phân quyền nghiệp vụ. Triển khai trong backend hiện tại, không thay schema hay dữ liệu đang có. Không sửa frontend trong đợt này.

## Quyền và trạng thái

- Manager/Owner quyết định đúng số tiền và người nhận đã có trên yêu cầu; Staff và System Admin không có quyền duyệt. Không suy ra quyền từ tên Đức hay tên người dùng. Chặn cùng ID, Google subject hoặc email ở các tài khoản liên quan.
- `requested`: chờ duyệt. `processing`: đã có quyết định, chờ chuyển hoặc kiểm tra chứng từ. `failed`: lỗi, vẫn giữ hạn mức. `completed`: đã kiểm tra chứng từ tiền ra. `rejected`: bị từ chối.
- Hoàn do công bố hạng theo UC-21 được hệ thống ghi căn cứ chính sách và chuyển `processing`, không thêm phê duyệt tùy ý. Các yêu cầu ngoại lệ khác phải có quyết định Manager/Owner.
- Người ghi chứng từ không được tự xác minh chứng từ của mình. Người nhận không được duyệt, thực hiện hoặc kiểm tra khoản hoàn cho mình.
- Khoản đã duyệt không được đổi số tiền/người nhận qua API thực hiện. Quyết định lưu số tiền, người nhận, lý do, căn cứ, thời điểm và người duyệt; `ApprovedBy` không bị ghi đè bằng người thực hiện.

## API dưới `/api/ticket-aftercare/refunds`

Các POST yêu cầu CSRF như các thao tác thanh toán khác.

1. `POST /{id}/decision`: `{ approve, amountVnd, recipientId, reason }`. Số tiền và người nhận phải khớp yêu cầu gốc. Ghi `approvals` và audit quyết định trong cùng transaction.
2. `POST /{id}/payout`: `{ amountVnd, recipientId, bankReference, document, paidAt }`. `document` là mã/đường dẫn hoặc mô tả chứng từ ngân hàng để người kiểm tra đối chiếu; không phải xác nhận tự động từ ngân hàng. Lưu chứng từ và giữ `processing`.
3. `POST /{id}/complete`: `{ evidence, bankReference }`. Người kiểm tra độc lập đối chiếu chứng từ thực tế với người trả gốc, số tiền, mã giao dịch và ghi nhận kết quả kiểm tra. Chỉ bước này chuyển `completed`.
4. `POST /{id}/fail`: `{ reason }`. Giữ hồ sơ và quyết định để tiếp tục, không tạo khoản hoàn mới. Nếu đã lưu chứng từ thì phải đối soát chứng từ đó; API không cho thay chứng từ rồi chuyển lần hai.
5. `GET /` trả tất cả trạng thái cùng quyết định/chứng từ mới nhất; `GET /{id}/history` trả lịch sử. Giới hạn cơ sở được giữ qua refund/ticket query filters trước khi đọc audit.

## Kiểm soát và tương thích

Kiểm tra lại nguồn thanh toán vé đã trả, người trả trên payment, hạn mức payment, phần phân bổ vào vé và tổng giá vé ở bước duyệt, ghi chứng từ và hoàn tất. Các khoản đang chờ, lỗi và đã hoàn đều chiếm hạn mức; chỉ khoản bị từ chối được loại khỏi tổng. Khóa payment và order trong transaction Serializable. Mã tiền ra được giữ duy nhất trong audit, không dùng lại mã khoản thu. Gửi lại chứng từ hoặc hoàn tất giống nhau không tạo thêm ghi nhận tiền ra/thông báo.

Hồ sơ cũ chưa có audit quyết định không thể gọi thẳng `complete`. Manager/Owner có thể duyệt lại hồ sơ cũ `requested`, `processing` hoặc `failed` chưa lưu chứng từ theo luồng mới. Không tự sửa hồ sơ `completed` cũ. Frontend cũ chỉ gọi `complete` sẽ nhận lỗi yêu cầu bổ sung quyết định/chứng từ; cần nối các bước API trên khi triển khai giao diện.

Chưa có tích hợp xác thực tiền ra tự động: kiểm tra chứng từ là thao tác có trách nhiệm của người dùng độc lập. Không coi webhook tiền vào, ghi chú đơn lẻ hay gửi thông báo là bằng chứng hoàn tiền. Không mở rộng quy trình này sang hoàn hàng hóa hoặc điều chỉnh kho.

## Kiểm thử

`RefundWorkflowTests` bao phủ quyền, cùng người, thiếu quyết định/chứng từ, tự kiểm tra, sửa phạm vi duyệt, sai người trả, ba hạn mức, mã bị dùng, lỗi/thử lại và hoàn tất lặp. Chạy cùng `dotnet build be_da/be_da.csproj` và `dotnet test tests/TKSC.Api.Tests`.
