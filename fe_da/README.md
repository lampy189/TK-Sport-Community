# Template Home TK Sport Community

Template React dùng chung cho Guest, Player, Referee, Staff, Manager, Owner và System Admin. Các role được đối chiếu từ SRS trong thư mục docs. Chỉ phần giao diện và dữ liệu minh họa được triển khai; chưa có đăng nhập, API hay phân quyền thực tế.

## Chạy dự án

Dùng Node.js 22.12+ hoặc Node.js 24 LTS. Mở terminal tại `fe_da`:

```sh
npm ci
npm run dev
```

Chọn vai trò bằng bộ chọn trên đầu trang. Menu, thẻ chức năng và CTA mở vùng nội dung mẫu; nút “Về tổng quan” đưa về Home. Menu trên điện thoại cuộn ngang. `npm run lint` kiểm tra mã; `npm run build` tạo bản build.

## Cấu trúc thư mục

```text
src/
├── components/
│   └── layout/
│       ├── Header.jsx          # Nhận diện thương hiệu, thông tin tài khoản
│       ├── Navbar.jsx          # Menu được truyền từ màn hình
│       ├── HeroLayout.jsx      # Tiêu đề, mô tả, CTA, vùng minh họa
│       ├── MainContainer.jsx   # Thẻ main và chiều rộng nội dung
│       ├── SectionLayout.jsx   # Tiêu đề section, action và children
│       └── Footer.jsx          # Chân trang dùng chung
├── layouts/
│   └── HomeLayout.jsx          # Ghép khung trang từ các component
├── config/
│   └── roleHomeConfig.js       # Nội dung demo và menu của 7 role
├── pages/
│   ├── home/HomePage.jsx       # Home dùng chung, nhận prop role
│   └── <feature>/              # Thành viên tạo màn hình nghiệp vụ tại đây
├── styles/
│   ├── tokens.css             # Màu sắc, bo góc, chiều rộng chung
│   └── home.css               # Layout và responsive
├── index.css                  # Reset và typography toàn ứng dụng
├── App.jsx                    # Bộ chọn role phục vụ preview
└── main.jsx                   # React entry point
```

Thứ tự hiển thị: Header → Navbar → MainContainer (HeroLayout → các SectionLayout) → Footer. Hero nằm trong main để giữ đúng ngữ nghĩa HTML; một trang chỉ có một main và một h1.

## Hợp đồng component

| Component     | Props                                                                     | Cách dùng                                             |
| ------------- | ------------------------------------------------------------------------- | ----------------------------------------------------- |
| Header        | roleLabel, accountName?                                                   | Tên tài khoản và vai trò                              |
| Navbar        | items: [{id, label}], activeId, onNavigate(id)                            | Trang cha xử lý điều hướng                            |
| HeroLayout    | eyebrow, title, description, action?, aside?                              | action/aside nhận JSX; bỏ aside dùng sân vẽ bằng CSS  |
| MainContainer | children                                                                  | Vùng nội dung chính; không lồng thêm main             |
| SectionLayout | title, description?, action?, className?, children                        | Dùng nhiều lần trong cùng một trang                   |
| Footer        | Không có                                                                  | Chân trang chung                                      |
| HomeLayout    | roleLabel, accountName?, navigation, activeId, onNavigate, hero, children | hero là props HeroLayout; children chứa SectionLayout |

## Gắn vào màn hình của thành viên

Ví dụ tạo `src/pages/bookings/BookingsPage.jsx`:

```jsx
import HomeLayout from "../../layouts/HomeLayout";
import SectionLayout from "../../components/layout/SectionLayout";

// onNavigate do app/router cung cấp. Không đặt logic API trong layout.
export default function BookingsPage({ onNavigate, bookings = [] }) {
  return (
    <HomeLayout
      roleLabel="Người chơi"
      navigation={[
        { id: "home", label: "Trang chủ" },
        { id: "bookings", label: "Vé của tôi" },
      ]}
      activeId="bookings"
      onNavigate={onNavigate}
      hero={{
        eyebrow: "MY BOOKINGS",
        title: "Lịch ra sân của bạn",
        description: "Theo dõi các buổi chơi đã đăng ký.",
      }}
    >
      <SectionLayout title="Vé sắp tới" description="Danh sách vé của bạn">
        {bookings.length === 0 ? (
          <p>Bạn chưa có vé sắp tới.</p>
        ) : (
          <ul>
            {bookings.map((booking) => (
              <li key={booking.id}>{booking.name}</li>
            ))}
          </ul>
        )}
      </SectionLayout>
    </HomeLayout>
  );
}
```

1. Tạo thư mục `pages/<feature>` và component màn hình. Component chỉ dùng trong feature đặt tại `pages/<feature>/components`.
2. Dùng `HomeLayout` và `SectionLayout`, truyền menu, hero và nội dung riêng. Không sao chép Header/Footer sang mỗi màn hình.
3. Gắn component trong `App.jsx` hoặc router khi nhóm bổ sung router. Hiện dự án chưa có thư viện routing; `onNavigate` là điểm tích hợp, menu demo chỉ thay nội dung trong trang và không có URL riêng.
4. Với màn hình nghiệp vụ, dùng ID menu ổn định như `bookings`, `sessions`, `stock`; ID `module-0` trong Home chỉ dành cho demo. Thay vùng `module-placeholder` bằng component thực khi tích hợp.
5. CSS riêng đặt cùng feature (ưu tiên CSS Modules). Chỉ sửa `styles/tokens.css` khi đổi thiết kế chung; phối hợp nhóm trước khi sửa các component layout.
6. Khi nối API, xử lý loading, empty, error và dữ liệu tại page/feature. Truyền JSX vào children/action/aside, giữ layout độc lập với nghiệp vụ.

## Vai trò và tích hợp đăng nhập

Các key frontend: `guest`, `player`, `referee`, `staff`, `manager`, `owner`, `system-admin`. Backend mới có model Role với Code, chưa có danh sách mã seed được xác nhận; cần map Code thực tế sang các key này tại lớp xác thực.

Bộ chọn role ở `App.jsx` là công cụ preview, không phải quyền truy cập. Khi triển khai đăng nhập, bỏ bộ chọn, lấy role từ phiên xác thực, xử lý role không hợp lệ và kiểm tra quyền cả ở API. HomePage mặc định về giao diện guest nếu key không được biết. Nếu đổi role trên cùng component, dùng `key={role}` để reset menu, như ví dụ App hiện tại.

System Admin chỉ có nhóm kỹ thuật/tài khoản; phê duyệt kinh doanh thuộc Owner/Manager; khiếu nại đánh giá thuộc Manager với bằng chứng từ Referee. Dữ liệu thống kê trong config là minh họa, không dùng làm báo cáo thật.

## Checklist trước khi ghép màn hình

- Giữ đủ khung dùng chung, một h1 và một main.
- Menu active khớp màn hình; onNavigate có xử lý thật khi nối router.
- Kiểm tra 320px, 390px, tablet và desktop; có thể dùng bàn phím để chọn menu.
- Chạy lint và build; không thêm dữ liệu riêng tư hay secret vào config.
