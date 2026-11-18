# Thống nhất phân trang giao diện — 18/11/2026

Nguồn: yêu cầu trực tiếp của người dùng; RP3 NFR-U03 (desktop/mobile) và phần hiển thị danh sách của NFR-P01. Chưa coi NFR-P01 hoàn tất ở cấp API.

Dùng chung `fe_da/src/components/ui/PaginatedList.jsx`: 10/20/50 mục, số trang rút gọn, trước/sau, phạm vi và tổng mục. Mỗi danh sách có trạng thái riêng; đổi tập kết quả đưa về trang đầu. Callback giữ chỉ số gốc để thao tác đúng bản ghi. Hỗ trợ thẻ, bảng và danh sách HTML; bộ lọc và các tổng nghiệp vụ vẫn dùng đầy đủ dữ liệu đầu vào.

Đã áp dụng cho danh sách buổi chơi, ca/phân công, đánh giá, lịch sử điểm, cộng đồng/bạn bè/lời mời, thông báo, cửa hàng, kho/cấp phát, vé/đơn hàng, sự cố/hoàn tiền/đối soát, cơ sở/nhân sự/quyền truy cập và doanh thu theo ngày. Bỏ cắt 20 buổi đầu trong hộp mời chơi. Giữ lịch tuần, các thẻ tổng quan rút gọn, hội thoại chat, dropdown và biểu mẫu chi tiết theo luồng hiện có.

Giới hạn: đây là phân trang phía giao diện. Giới hạn API hiện tại vẫn giữ (ví dụ thông báo 50, đơn hàng cá nhân 50, đối soát 150, lịch sử kho 200); tổng hiển thị là số mục API trả về, không phải tổng toàn database. Không sửa backend/schema trong công việc này.

Kiểm tra:
- `npm run build`: đạt; còn cảnh báo bundle >500 kB.
- `npm run test:duc`: 10/10 đạt.
- `npx playwright test --config tests/pagination/playwright.config.js`: đạt; kiểm tra trang cuối, chỉ số gốc, lọc/bỏ lọc, đổi số mục, xóa dữ liệu, bảng độc lập, màn hình 375px, không có lỗi JavaScript.
- ESLint bộ phân trang và kiểm thử riêng: đạt.
- Lint toàn dự án: còn 11 lỗi và 8 cảnh báo tại mã cũ (DevLoginModal, MyTicketsView, Navbar, SkillProfileView, SkillReviews và các hook hiện hữu).
