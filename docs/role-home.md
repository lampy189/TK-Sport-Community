# Trang chủ theo vai trò

Trang chủ dùng vai trò do `/api/auth/me` trả về, theo mô hình một role/tài khoản và Permission Matrix trong RP3. Không có bộ chọn role trong giao diện đăng nhập thật.

## Thành phần

- `components/layout/Header.jsx`: thương hiệu, tên tài khoản, role, đăng nhập/đăng xuất.
- `components/layout/Navbar.jsx`: menu theo role, trạng thái mục đang mở.
- `components/layout/HeroLayout.jsx`: tiêu đề, lời giới thiệu, nút hành động.
- `components/layout/MainContainer.jsx`: khung nội dung chính.
- `components/layout/SectionLayout.jsx`: khối nội dung có tiêu đề.
- `components/layout/Footer.jsx`: thông tin cộng đồng và về đầu trang.
- `layouts/HomeLayout.jsx`: ghép các phần layout dùng chung.
- `pages/home/HomePage.jsx`: điều hướng home và các chức năng.
- `pages/home/HomeOverview.jsx`: thẻ chức năng, thông tin không gian, gợi ý theo role.
- `pages/home/HomeWorkspace.jsx`: mở các màn hình nghiệp vụ đã có.
- `config/roleHomeConfig.js`: nội dung/menu cho Guest, Player, Referee, Staff, Manager, Owner, System Admin.

## Phạm vi

Sau đăng nhập, tất cả role vào home tương ứng. Player mở lịch, chi tiết buổi chơi, thanh toán, vé; Staff/Manager mở kiểm tra thanh toán. URL có `orderId` tiếp tục vào thanh toán khi quay về từ payOS. Guest có home và lịch công khai.

Các chức năng chưa có nghiệp vụ hiển thị trạng thái đang hoàn thiện. Trang thật hiển thị thông tin tĩnh đã biết (số module, bộ môn, vai trò), không hiển thị số liệu giao dịch giả. `?preview=1` trong Development giữ bản mẫu với số liệu minh họa và không gọi các chức năng nghiệp vụ.

## Đường dẫn home riêng

- Guest: `/guest/home` (`/` tự chuyển vào đây khi chưa đăng nhập).
- Player: `/player/home`.
- Referee: `/referee/home`.
- Staff: `/staff/home`.
- Manager: `/manager/home`.
- Owner: `/owner/home`.
- System Admin: `/admin/home`.

Đăng nhập Player tại `/player/login`, đội ngũ tại `/staff/login`, Admin tại `/admin/login`. Đăng nhập thành công tự chuyển tới home theo role từ server; truy cập home của role khác tự chuyển về home của mình. Người chưa đăng nhập vào đường dẫn bảo vệ được chuyển về cổng đăng nhập phù hợp. Đăng xuất trở về cổng tương ứng. Bộ định tuyến tại `config/homeRoutes.js`; API vẫn kiểm soát quyền độc lập. Máy chủ production cần cấu hình SPA fallback về index.html cho các URL frontend này.
