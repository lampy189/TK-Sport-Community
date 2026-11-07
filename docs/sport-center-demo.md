# Dữ liệu mẫu Sport Center

Trang mô phỏng frontend của lượt đầu đã được gỡ. Chức năng hiện dùng API và SQL Server thật.

Xem [hướng dẫn triển khai và tài khoản mẫu](sport-center-database-rollout.md).

Fixture nguồn trong fe_da/src/pages/revenue/demoData.js và docs/demo/sport-center-revenue.json chỉ phục vụ tạo seed/kiểm thử, không được import vào màn báo cáo thật. Script scripts/generate-sport-center-seed.mjs tạo db/seed-sport-center-demo.sql từ fixture.
