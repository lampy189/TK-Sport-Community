# Kết hợp database ngày 10/11/2026

Đã ghép backup `Sep490_20261110_204229.bak` vào `localhost/Sep490`, giữ dữ liệu test đã tạo trước đó.

- SHA256 backup đã khớp manifest. Restore riêng tại `Sep490_Import_20261110` và DBCC CHECKDB thành công.
- Bản sao trước khi ghép: `C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\Backup\Sep490_before_merge_20261110.bak`.
- Ghép theo email/mã nghiệp vụ và ánh xạ lại khóa ngoại; giữ Google subject và role của tài khoản hiện có. Nhân sự trùng được gán cơ sở theo bản nhập.
- Cơ sở chính cũ đổi tên thành TKSC Vĩnh Phúc; thêm TKSC Hà Nội; giữ ba cơ sở demo doanh thu.
- Bổ sung hai bảng vé vào muộn bằng migration 014 có sẵn.
- Lịch sử tài chính nhập được ghi nhận lại vào sổ doanh thu local qua trigger; không gọi cổng thanh toán hay gửi email.
- Giữ số dư kho theo bản nhập đối với sản phẩm trùng; bỏ bút toán mở kho giả tương ứng.
- Xử lý 4 phân công trọng tài trùng giờ từ 10/11 trở đi, ưu tiên buổi có người check-in/vé xác nhận; các buổi bị bỏ phân công cần Manager phân công lại khi test.
- Dữ liệu lịch sử nguồn có buổi 30/10 với 11 vé/sức chứa 8 và các vé trùng giờ trong quá khứ; giữ nguyên lịch sử này.

Kết quả: 46 tài khoản (15 Google), 852 buổi, 389 vé, 11 sản phẩm, 59 đánh giá, 26 đơn hàng hóa, 11 tin nhắn, 1 offer và 1 vé vào muộn.

Kiểm tra: không thiếu tài khoản nguồn; không còn phân công trọng tài trùng giờ từ 10/11; khóa ngoại hợp lệ; RLS và trigger hoạt động; DBCC CHECKDB không báo lỗi. Chưa chạy kiểm thử giao diện/API ở lượt này. Mã buổi/vé nhập đã được ánh xạ, không dùng nguyên ID trong README backup để tìm dữ liệu.

`db/merge-backup-20261110.sql` là bản chạy thử có ROLLBACK. `db/apply-merge-backup-20261110.sql` là bản đã áp dụng; có chốt audit ngăn nhập cùng backup lần nữa. Không chạy lại script reset cũ sau khi ghép.
