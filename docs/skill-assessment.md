# UC-22 / UC-23 / UC-24 — rubric và phiếu chấm kỹ năng

Phạm vi backend theo yêu cầu ngày 30/10/2026. Mã UC của người dùng tương ứng về chức năng với UC-17/UC-18, FE-04 và BR-16–BR-20 trong bản SRS lưu tại repo. Công thức Cầu lông lấy từ Report 1 người dùng gửi, Phụ lục A; Report 2 không cung cấp thang điểm Pickleball.

## Chuẩn bị dữ liệu

`db/upgrades/005-badminton-pilot-rubric.sql` thêm một rubric `badminton-pilot-r1` và 5 tiêu chí D1–D5, trọng số 20%. Không sửa schema, không cập nhật điểm người chơi, không thay rubric khác đang hoạt động. Script không tự chạy khi khởi động. Cần người dùng chấp nhận trước khi áp dụng vào Sep490 theo AGENTS.md. Đọc database localhost/Sep490 ngày 30/10 thấy chưa có rubric cho cả hai môn.

Không seed thang điểm Pickleball bằng quy tắc Cầu lông. Khi có rubric Pickleball, API có thể lấy tiêu chí, mở phiếu và lưu bằng chứng với điểm null; điểm số/công bố trả 409 cho đến khi có quy tắc được chốt và triển khai.

## API và thứ tự gọi

Đăng nhập Referee. Các thao tác POST/PUT cần cookie, `X-TKSC-Request: 1` và `X-CSRF-TOKEN` lấy từ `/api/auth/csrf`.

1. `GET /api/v1/skill/sports/{sportId}/rubric`: rubric đang hoạt động, phiên bản, tiêu chí và `scoringSupported`.
2. `GET /api/v1/skill/sessions/{sessionId}/attendees`: chỉ người có vé paid + checkedin + checked_in_by; trọng tài phải được phân công. Không liệt kê bản thân. Hồ sơ thiếu có `profileId: null` và mở phiếu trả 409.
3. `POST /api/v1/skill/sessions/{sessionId}/players/{playerId}/assessment`: tạo hoặc mở lại một phiếu của trọng tài/người chơi/buổi chơi. Giữ phiên bản rubric cũ khi mở lại, không tạo trùng khi bấm lặp.
4. `GET /api/v1/skill/assessments/{id}`: đọc phiếu, bằng chứng, revision và điểm/hạng hiện tại.
5. `PUT /api/v1/skill/assessments/{id}`: thay toàn bộ marks, mỗi dimension đúng một lần. `revision` lấy từ lần đọc gần nhất; revision cũ trả 409.

```json
{
  "revision": 123,
  "marks": [
    { "dimensionId": 1, "score": 2, "evidence": "Giao cầu ổn định trong các lượt đã quan sát." },
    { "dimensionId": 2, "score": null, "evidence": "Chưa quan sát đủ di chuyển cuối sân." },
    { "dimensionId": 3, "score": 2, "evidence": null },
    { "dimensionId": 4, "score": 3, "evidence": null },
    { "dimensionId": 5, "score": 2, "evidence": null }
  ]
}
```

ID trong ví dụ chỉ minh họa; sử dụng ID trả về từ rubric. Điểm Cầu lông 0–4; null là chưa quan sát. Thiếu một quan sát thì totalScore/bandResult là null. Nháp không đổi hồ sơ hay vé. Ghi chú mỗi tiêu chí tối đa 2000 ký tự, lưu dạng JSON trong audit_logs hiện có để giữ lịch sử, không thêm cột ngầm vào database.

6. `POST /api/v1/skill/assessments/{id}/publish` với `{ "revision": 124, "expectedScore": 50, "expectedBand": "B3" }`. Hai giá trị expected phải khớp hồ sơ tại lần đọc trước (có thể null). Phải đủ 5 điểm và bằng chứng cho điểm 0/4. Công bố lặp trả lại kết quả, không tạo thêm lịch sử/hoàn tiền.
7. Player gọi `GET /api/v1/skill/my-results?sportId=...` để xem lịch sử điểm đã công bố của chính mình. Không trả ghi chú nội bộ.

## Công thức và giới hạn công bố

- Chỉ version `badminton-pilot-r1`, môn `badminton`, đúng D1–D5 và trọng số 20% được dùng công thức Report 1: tổng 5 điểm × 5. B1: 0–19; B2: 20–39; B3: 40–59; B4: 60–79; B5: 80–100.
- Lần xác nhận đầu: dùng quan sát đầy đủ thay mức provisional. Các lần định kỳ: cần quan sát đầy đủ khác buổi, cùng rubric; dùng trung bình hai lần và làm tròn .5 lên. `Assessment.totalScore/bandResult` giữ kết quả quan sát gốc; `PlayerRatingHistory` và hồ sơ giữ kết quả công bố.
- Đổi hạng đã xác nhận trả 409 và giữ nháp. Phụ lục A yêu cầu review, hai quan sát hỗ trợ và kiểm tra năng lực di chuyển/phối hợp đôi, nhưng chưa xác định ngưỡng bắt buộc cụ thể. Chưa triển khai luồng quyết định/correction của Manager; không tự phê duyệt thay Manager.
- Khi xác nhận lần đầu làm đổi hạng provisional, chỉ hủy vé paid/confirmed tương lai cùng môn không còn hợp lệ, trừ vé có exception được duyệt. Tạo refund `requested` cho người trả tiền gốc, không đánh dấu đã chuyển tiền. Không đổi vé đã điểm danh, vé quá khứ, vé cùng hạng hoặc đơn hàng hóa.
- Ghi lịch sử, cập nhật hồ sơ, audit và xử lý vé trong một transaction Serializable. Application lock tuần tự hóa các lần sửa/công bố trên nhiều API instance. Phiếu đã công bố không chỉnh sửa.
- Chặn tự chấm dựa trên liên kết `players.user_id`. Database chưa có định danh người thật dùng chung nhiều tài khoản, nên không thể phát hiện tài khoản khác của cùng một người.

## Kiểm thử

Chạy `dotnet build be_da/be_da.csproj` và `dotnet test tests/TKSC.Api.Tests`. Tests kiểm tra ví dụ 55/B3, biên hạng, null khác 0, điểm ngoài thang, đúng rubric/môn, làm tròn, truy cập ẩn danh, vai trò và CSRF. Chưa chạy kiểm thử ghi/công bố tích hợp trên SQL Server; database thật chưa được seed hoặc sửa trong quá trình triển khai.

## Hoàn thiện SWL 2 ngày 02/11/2026

Theo yêu cầu UC-23/24: màn **Referee → Đánh giá kỹ năng** lấy buổi được phân công qua `GET /api/v1/skill/sessions`, chọn người đã điểm danh, nhập điểm/bằng chứng, lưu bản ghi và công bố. Nút công bố chỉ bật sau khi lưu đủ điểm; server vẫn kiểm tra revision, quyền, rubric và điều kiện nghiệp vụ. Phiếu công bố rồi không sửa được, gọi lại không thêm lịch sử.

Đã sửa trường hợp lịch sử SelfSurvey khiến lần công bố chính thức đầu tiên bị tính nhầm là đánh giá định kỳ. Chỉ lịch sử đánh giá trên sân đã công bố mới tính là lịch sử chính thức.

Màn Hồ sơ kỹ năng hiện có của Nhật dùng `/api/skills/me`: đọc đúng profile `Rated`, điểm/hạng mới và lịch sử. Truy vấn latest assessment lọc `published`, chuẩn hóa `on_court` thành `OnCourt`, không đưa phiếu nháp lên màn người chơi. Màn kết quả tự tải lại khi cửa sổ được focus và mỗi 10 giây khi đang hiển thị; dừng tải nền khi đang làm khảo sát. API `/api/v1/skill/my-results` chỉ trả lịch sử trên sân đã công bố, không trả SelfSurvey.

Chưa thay đổi các giới hạn đã ghi ở trên: đổi hạng chính thức cần review; Pickleball cần rubric/quy tắc riêng; seed 005 không tự chạy. Cần kiểm thử ghi/công bố trên SQL Server có dữ liệu phù hợp để xác minh luồng tích hợp hoàn chỉnh.
