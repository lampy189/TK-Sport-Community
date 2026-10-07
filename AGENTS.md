# Hướng dẫn cho Codex trong dự án TKSC

## Phạm vi hiện tại

- Theo lựa chọn ngày 06/10/2026, triển khai trực tiếp trong `be_da/` của repo GitHub, giữ target .NET 8. Solution là `be_da/be_da.slnx` (mở bằng Visual Studio mới hoặc SDK 10); không thêm lại bốn project backend cũ. Xem `be_da/README.md`. Không sửa frontend nếu chưa được yêu cầu.
- SQL Server trên máy dùng database `Sep490`. `db/schema.sql` và `db/erd.mmd` là baseline vật lý hiện tại, chưa được coi là thiết kế cuối cùng.
- Chưa tự thêm chức năng, endpoint, bảng, tích hợp hoặc thư viện chỉ vì chúng xuất hiện trong tài liệu. Chọn phần cần làm theo yêu cầu hiện tại của người dùng.

## Đọc yêu cầu trước khi sửa code

1. Đọc `PROJECT_CONTEXT.md` và `docs/requirements/README.md` để định vị đúng tài liệu.
2. Với chức năng đang làm, tìm mã UC/FR/quy tắc liên quan trong `docs/requirements/RP3-Software-Requirements.txt`, rồi đối chiếu đoạn tương ứng trong file `.docx` khi cần bảng hoặc bố cục gốc.
3. Dùng RP1 để hiểu mục tiêu/phạm vi; RP2 để hiểu kế hoạch và stack. Với sơ đồ/hình của RP3, ưu tiên `docs/requirements/RP3-Diagrams-Reference.docx` theo chỉ dẫn của người dùng.
4. Đối chiếu yêu cầu dữ liệu với `db/erd.mmd` và `db/schema.sql` trước khi thêm model hoặc migration. Nếu có mâu thuẫn ảnh hưởng nghiệp vụ/dữ liệu, nêu rõ nguồn và hỏi đúng điểm còn thiếu; các lựa chọn kỹ thuật thông thường có thể tự quyết.

## Quyết định đã chốt

- Hệ thống phục vụ cầu lông và pickleball; hồ sơ/đánh giá kỹ năng tách theo môn.
- Mỗi `users` có đúng một role hiện tại qua `users.role_id`; không tạo lại bảng `user_roles`.
- Đơn hàng hóa dùng `orders` và `order_items`; đơn tham gia buổi chơi dùng `social_booking_orders`. Hai luồng thanh toán phải giữ tách biệt.
- Guest không có tài khoản. System Admin xử lý kỹ thuật, không quyết định điểm kỹ năng hoặc phê duyệt nghiệp vụ thay Manager/Owner.
- Actor là vai trò và quyền truy cập, không phải lý do để sao chép cùng một use case vào nhiều project hoặc thư mục theo actor.
- RP3 có phần mẫu không thuộc TKSC trong bản tham chiếu hình; không biến nội dung mẫu tuyển dụng, CV, VNPay/SePay ví dụ thành yêu cầu TKSC nếu không được xác nhận ở bản SRS đầy đủ.

## Cách làm việc

- Người dùng chạy backend bằng profile `https` trong Visual Studio. Sau kiểm thử, dừng tiến trình backend do Codex khởi động để tránh khóa `be_da.exe` hoặc chiếm cổng 7207 khi người dùng bấm F5. Chỉ dừng tiến trình đã xác minh thuộc đúng checkout; không tự khởi động lại nền khi bàn giao cho Visual Studio.
- Theo yêu cầu ngày 07/10/2026, cập nhật trực tiếp checkout đang mở trong Visual Studio: `C:\Users\shogu\Documents\ChatGPT\TK-Sport-Community`, nhánh `NhatLT`, solution `be_da/be_da.slnx`. Đây là nơi làm việc chính trên máy này; không chuyển sang worktree cũ hoặc scaffold `Sep490/backend` nếu người dùng chưa yêu cầu. Kiểm tra thay đổi của người dùng trước mỗi lần sửa; không tự merge vào `main`.
- Khi triển khai một tính năng, nêu ngắn gọn mã UC/FR/quy tắc nguồn và phần nào đã được thực hiện.
- Không lưu mật khẩu, API key hoặc connection string có bí mật vào Git. Dùng cấu hình phát triển an toàn.
- Không xóa dữ liệu hoặc sửa schema đã có dữ liệu mà chưa kiểm tra tác động; thêm migration và hướng nâng cấp khi bắt đầu dùng EF Core.
- Sau thay đổi backend, chạy `dotnet build be_da/be_da.csproj` và `dotnet test tests/TKSC.Api.Tests`, báo kết quả.
- Google sign-in hiện là phạm vi đăng nhập được người dùng chọn; tự đăng ký chỉ nhận Player. Không tự liên kết tài khoản chỉ bằng email. Thay đổi database phải trình bày cụ thể và chờ người dùng chấp nhận trước khi áp dụng. Upgrade 001 (google_subject và unique filtered index) đã được chấp nhận; không suy ra sự đồng ý cho các thay đổi khác.
