# Đức — evidence 07–10/11/2026

Nhánh DucTM từ upstream/LamNV (f1cac7d). Kiểm tra ngày 09/11/2026, Asia/Ho_Chi_Minh.
Ngày trên Jira là lịch phân công; lịch sử tạo task/commit giữ thời điểm thực hiện thật.

| Jira | Lịch | Commit / đầu ra |
| --- | --- | --- |
| [TSC-437](https://fpt-team-sqcd1fsr.atlassian.net/browse/TSC-437) | 07/11 | 1668d91: hai nút Chấp nhận/Từ chối, bắt buộc lý do |
| [TSC-438](https://fpt-team-sqcd1fsr.atlassian.net/browse/TSC-438) | 07–08/11 | cd8ab57: nhập lý do trước quyết định tạm ngưng/hủy sân |
| [TSC-439](https://fpt-team-sqcd1fsr.atlassian.net/browse/TSC-439) | 08–09/11 | 4056edb: Issue Hub, Player báo cáo, Staff/Manager reply |
| [TSC-440](https://fpt-team-sqcd1fsr.atlassian.net/browse/TSC-440) | 09/11 | 20b1162: tăng chữ, thu gọn khoảng cách, mobile |
| [TSC-441](https://fpt-team-sqcd1fsr.atlassian.net/browse/TSC-441) | 09–10/11 | Commit chứa tài liệu này: unit/component tests, ảnh, bổ sung lý do trong chi tiết sự cố |

Mã UC theo phân công/Jira: ngoại lệ UC-19, sân UC-15. SRS snapshot trong repo dùng UC-15 cho ngoại lệ; đợt này không sửa mã SRS.

## Kết quả

- Backend build target .NET 8: đạt, 0 warning, 0 error.
- TKSC.Api.Tests: **493 đạt, 3 bỏ qua, 0 lỗi**; trong đó 112 ca mới thuộc bốn lớp Duc*.
- PaymentTests: **65/65 đạt**.
- Node unit tests buildIssues: **4/4 đạt**.
- Chrome component tests: **22/22 đạt**, component thật với API fixture.
- Frontend build: đạt; còn cảnh báo Vite về bundle >500 kB.
- ESLint Issue Hub, xử lý vé và test harness mới: đạt. SkillReviews.jsx còn lỗi set-state-in-effect có sẵn trong effect đồng bộ defaultOpen, không thuộc handler quyết định mới.

Dữ liệu lấy từ runner: [results.html](results.html), [results.json](results.json), [ui-results.json](ui-results.json), [node-tests.txt](node-tests.txt).

## Ma trận ≥3 ca/hàm

| Hàm/handler | Các ca chính | File test |
| --- | --- | --- |
| TicketAftercareRules.VenueDecision | tạm ngưng, hủy, chuyển từ tạm ngưng, lặp, chặn completed/cancelled | DucWorkflowRuleTests |
| Evidence | trim; biên 10/100/1000; từ chối 0/9/1001 | DucWorkflowRuleTests |
| Disrupted | suspended, cancelled, open, completed | DucWorkflowRuleTests |
| Occupies | confirmed, checked-in, hold còn/hết hạn, exchanged | DucWorkflowRuleTests |
| Limit | cấu hình 4, 8, 16 | DucWorkflowRuleTests |
| CheckTarget | hợp lệ, sai môn/cùng buổi, đầy, đóng, đúng giờ UTC+7 | DucWorkflowRuleTests |
| Split | rẻ hơn, đắt hơn, bằng 0, nhiều nguồn; bảo toàn tiền | DucWorkflowRuleTests, TicketAftercareTests |
| SkillReviewRules.Reason | null/trắng, trim, biên 1/2000, vượt giới hạn | DucSkillReviewRuleTests |
| Future | open/published, trước/đúng/sau giờ, suspended/cancelled | DucSkillReviewRuleTests |
| CorrectedScore | null/-1/101; 0/50/100 | DucSkillReviewRuleTests |
| TicketSupportRules.Report | 5 loại, loại sai, mô tả ngắn/dài, trim, biên 10/1800 | DucWorkflowRuleTests |
| TicketSupportService.List | chủ vé/người trả/Staff; người khác/vé không tồn tại | DucTicketSupportTests |
| Report | 3 loại hợp lệ; sai loại, ngắn, người khác; actor/thông báo | DucTicketSupportTests |
| Send | chủ vé/người trả/Staff; trim, rỗng/trắng/null không ảnh | DucTicketSupportTests |
| Image | 3 người có quyền; sai người/vé/ảnh | DucTicketSupportTests |
| Inbox | 0/1/3 reply; nhóm theo vé, giữ người báo cáo | DucTicketSupportTests |
| ValidateImage | PNG, base64 sai, SVG/text, quá 2MB, không ảnh | TicketSupportTests (PaymentTests) |
| API reports/incidents/inbox | ≥3 vai trò sai; CSRF với 3 loại báo cáo | DucAftercareAuthorizationTests |
| UI ngoại lệ | lý do trắng, chấp nhận, từ chối, 409 giữ draft | workflows.spec.js |
| UI sự cố sân | thứ tự lý do/nút, ngắn/trắng, tạm ngưng, hủy | workflows.spec.js |
| UI báo cáo/reply | nội dung bắt buộc, category/payload, lỗi giữ draft, ảnh quá cỡ | workflows.spec.js |
| UI đổi/hoàn | bằng chứng/xác nhận, bank ref, chênh lệch, API lỗi | workflows.spec.js |
| UI khiếu nại | giữ điểm, chỉnh 100, chặn 101 | workflows.spec.js |
| buildIssues | rỗng, phiên gián đoạn, ID không trùng, nhãn reply | issueHubModel.test.js |

Đây là phạm vi rule/handler được kiểm tra, không phải mọi method của toàn repo.

## Ảnh

1. [Hai nút ngoại lệ](01-exception-decisions.png)
2. [Lý do trước quyết định sân](02-venue-reason-first.png)
3. [Issue Hub và báo cáo](03-issue-hub-player-report.png)
4. [Player báo cáo](04-player-report.png)
5. [Khiếu nại](05-appeal-decision.png)
6. [Mobile](06-mobile-issue-hub.png)
7. [Hoàn tiền](07-refund-evidence.png)
8. [Đổi vé](08-exchange-evidence.png)
9. [Kết quả test runner](09-test-results.png)

## Chạy lại

Yêu cầu Node hỗ trợ Vite 8 (đã dùng Node 24), SDK hỗ trợ .NET 8, Chrome.

Từ gốc repo: chạy dotnet build be_da/be_da.csproj; dotnet test tests/TKSC.Api.Tests; dotnet test tests/PaymentTests.
Trong fe_da: npm ci; npm run build; npm run test:duc; npm run test:duc:ui.

UI runner tự khởi động Vite tại 127.0.0.1:5173 nếu chưa chạy.
Để thu báo cáo, chạy hai lệnh dotnet test với --logger "trx;LogFileName=duc-api.trx" (hoặc duc-payment.trx) và --results-directory /tmp/ductm-test-results.
Sau đó từ gốc repo chạy python3 fe_da/tests/duc/collect-results.py /tmp/ductm-test-results và node fe_da/tests/duc/capture-results.mjs.

## Tích hợp / giới hạn

- Dùng bảng ticket_support_messages hiện có; loại báo cáo nằm trong nội dung có nhãn, actor/thời gian/ảnh giữ như hội thoại cũ. Không tạo migration hoặc chạy SQL.
- Báo cáo gắn vé mà Player là chủ vé/người thanh toán; không có báo cáo không gắn vé trong phạm vi này.
- Staff/Manager vào Sự cố để chọn người/vé và reply. Danh sách gom thanh toán cần đối soát, sân ngưng/hủy, hoàn tiền tồn và hội thoại; lý do sân lấy từ audit.
- Báo cáo không tự hủy vé, đổi điểm, kích hoạt vé hoặc xác nhận hoàn tiền.
- Quyền/CSRF do backend kiểm tra; Sport Center tiếp tục dựa trên middleware/RLS của LamNV.
- Ba test SQL Server bị bỏ qua do chưa có TKSC_TEST_SQL_CONNECTION. EF InMemory không xác minh SQL translation, transaction hay RLS; cần chạy test DB trên SQL Server của nhóm trước triển khai.
- Chưa kiểm tra ngân hàng/OAuth thật. Ảnh UI có nhãn API fixture. Không chạy backend nền hoặc thay dữ liệu phát triển.
