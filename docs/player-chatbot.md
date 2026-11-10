# Chatbot RAG cho Player

Yêu cầu ngày 09/11/2026: chatbot dùng SQL Server làm nguồn, dữ liệu vận hành được đọc lúc hỏi, chính sách chỉ dùng nội dung đã duyệt. Cùng code hoạt động với database demo hoặc thật qua cấu hình kết nối. Các quyền đọc liên quan RP3 UC-06 (lịch), UC-11–13 (thanh toán/vé), UC-21 (hoàn tiền) và quyền Player/Owner. Không tạo mã UC giả cho chatbot.

## Chức năng

- Nút **Hỏi trợ lý** hiện trong giao diện Player, nhập câu hỏi tự do, có trạng thái chờ/lỗi và phần mở nguồn tham khảo.
- `POST /api/player/chatbot/ask`, body ví dụ `{ "question": "Lịch cầu lông hôm nay", "scope": "sessions", "onDate": "2026-11-09", "sport": "badminton", "recordId": null }`. Yêu cầu cookie Player và `X-CSRF-TOKEN` lấy từ `/api/auth/csrf`. Không nhận userId/playerId làm danh tính tra cứu.
- `scope`: `auto`, `sessions`, `tickets`, `payments`, `refunds`, `policies`. Giao diện chỉ có ô nhập và nút gửi, không có câu hỏi chọn sẵn hoặc bộ lọc. Frontend gửi duy nhất `question`; các tham số tùy chọn vẫn hỗ trợ client API khác. Backend tự nhận diện từ khóa tiếng Việt có/không dấu, hôm nay/ngày mai/hôm qua và ngày dd/MM/yyyy hoặc yyyy-MM-dd, mã `#123`/`ID 123`. Ngày tương đối theo UTC+7. Câu không xác định được mục hoặc khoảng ngày chưa hỗ trợ sẽ hỏi lại bằng lời, không đưa cho AI viết SQL.
- Lịch/giá/chỗ, vé, đơn thanh toán social, hoàn tiền: truy vấn SQL cố định có tham số mỗi lần hỏi, định dạng kết quả trực tiếp (`mode=live`). Không dùng LLM để đoán trạng thái/số tiền và không gửi những kết quả này sang OpenAI.
- Chính sách: đọc các hàng hiện đang được phép từ `policies`, chia đoạn tối đa 1.200 ký tự có chồng lặp 100 ký tự, xây BM25 cho request hiện tại và lấy tối đa ba đoạn liên quan cho OpenAI. Đây là RAG từ SQL dùng tìm kiếm từ khóa, chưa có vector search/embeddings. Không có cache kiến thức xuyên request, nên sửa/gỡ duyệt/hết hiệu lực sẽ phản ánh ở lần hỏi tiếp theo, không cần train lại hoặc rebuild ứng dụng.
- Câu trả lời gồm `answer`, `mode` (`live`, `generated`, `reference`, `no_match`, `unavailable`), `sources`, `retrievedAt`, `scope`. Nguồn SQL có bảng/phạm vi và mã bản ghi; thời điểm đọc hiện trên UI. ID nguồn AI được kiểm tra với tập vừa truy xuất; người dùng vẫn cần đối chiếu nội dung gốc.
- Khi thiếu key, OpenAI lỗi/timeout, phản hồi bị cắt hoặc nguồn không hợp lệ, hiển thị rõ **trích dẫn tài liệu**, không giả lập câu trả lời AI. Không tìm được nguồn thì không gọi OpenAI. AI không tìm thấy nguồn hỗ trợ cũng trả thông báo thiếu thông tin.
- Câu hỏi 2–1.200 ký tự, body tối đa 8 KB, 10 lượt/phút/Player trên mỗi instance, thời gian gọi AI tối đa 40 giây. Khi chạy nhiều replica, dùng thêm hạn mức chung tại API gateway nếu cần.
- Mỗi câu hỏi được xử lý độc lập. Các tin nhắn hiển thị chỉ nằm trong bộ nhớ trang, tối đa khoảng 20 lượt tin nhắn; không lưu lịch sử vào database và không gửi toàn bộ hội thoại sang OpenAI.
- Chỉ nhánh chính sách gửi câu hỏi hiện tại và đoạn chính sách công khai sang OpenAI. Không có công cụ để ghi SQL, đặt chỗ, thanh toán, sửa điểm, đọc tin nhắn hoặc file tùy ý. SQL không sẵn sàng trả `unavailable`, không fallback sang dữ liệu mẫu/cũ.

## Phạm vi đọc và quyền

| Mục | Nguồn và giới hạn |
| --- | --- |
| Lịch / giá / chỗ | `sessions`, `sports`, `courts`, `venues`, `time_slots`; chỉ Open/Published. Đếm vé đã xác nhận và giữ chỗ còn hạn. Mặc định 7 ngày kể từ hôm nay, hoặc một ngày cụ thể; tìm ID buổi không giới hạn ngày nếu không truyền ngày. Chỗ trống không bảo đảm Player đủ điều kiện đăng ký. |
| Vé của tôi | `tickets` nối `players.user_id = userId` từ cookie; không lấy vé đứng tên bạn bè. Giữ nguyên trạng thái SQL, cảnh báo chỗ giữ hết hạn. |
| Thanh toán của tôi | `social_booking_orders.payer_id = userId`; trạng thái đơn và giao dịch gần nhất. Không bao gồm goods orders, không lộ chi tiết giao dịch/người trả của vé được người khác trả hộ. |
| Hoàn tiền của tôi | `refunds.recipient_id = userId`; không lộ thông tin tài khoản ngân hàng hoặc chứng từ. |
| Chính sách | `policies.code` trong danh sách công khai cấu hình; `approved_by` có giá trị; `effective_from` đã tới; `effective_to` chưa tới hoặc NULL; nội dung 1–16.000 ký tự. |

Các mục vận hành trả tối đa 20 bản ghi và báo nếu còn kết quả; bộ lọc thu hẹp theo môn/ngày/mã. Ngày trong bộ lọc thanh toán/hoàn tiền là **ngày buổi chơi**, không phải ngày giao dịch. Mã số phải đúng loại mục đã chọn. Thanh toán không lọc môn; policy không dùng bộ lọc ngày/mã/môn mà tìm theo câu hỏi. Không hỗ trợ tổng hợp doanh thu hoặc tra cứu người khác. Các truy vấn có điều kiện quyền ở SQL, không lấy tất cả rồi lọc bằng AI.

## Cấu hình dùng chung cho mọi môi trường

Code nằm trong Git; dữ liệu được đọc từ `ConnectionStrings:Sep490` của môi trường chạy. Không cần Ollama, vector database hoặc đường dẫn máy tác giả. Kho hướng dẫn mẫu cũ đã chuyển sang `tests/TKSC.Api.Tests/Fixtures` chỉ phục vụ test, không nhúng vào backend và không dùng khi trả lời.

Các biến môi trường trên **backend**:

| Biến | Ý nghĩa |
| --- | --- |
| `Chatbot__ApiKey` | OpenAI API key có quyền gọi model và hạn mức sử dụng; bắt buộc để sinh câu trả lời AI |
| `Chatbot__Model` | Mặc định `gpt-5-mini`; có thể đổi sang model hỗ trợ Chat Completions và Structured Outputs |
| `ConnectionStrings__Sep490` | Database ứng dụng đang dùng; chatbot dùng cùng kết nối, không có database demo riêng ngầm định |
| `Chatbot__PublicPolicyCodes__0` | Mã policy đã được duyệt công khai cho Player; thêm `__1`, `__2`… cho mã tiếp theo (tối đa 100 mã) |

Không đặt key trong frontend, biến `VITE_*`, source code, Dockerfile hoặc Git. Mỗi môi trường cung cấp key qua secret manager/biến môi trường của server. Không có key thì các mục SQL vận hành vẫn chạy và chính sách trả đoạn trích, nhưng chưa sinh câu trả lời AI.

Khi chuyển demo sang thật: triển khai schema tương thích, cấu hình `ConnectionStrings__Sep490` tới database thật, kiểm tra allowlist policy và khởi động lại ứng dụng. Không cần train lại AI. Chatbot đọc đúng dữ liệu trong database đang kết nối; nó **không tự nhận biết hoặc xóa các dòng demo nếu vẫn còn trong database thật**. Tách database demo/production và chuẩn bị dữ liệu production trong quy trình triển khai của dự án.

Máy phát triển có thể dùng **Manage User Secrets** cho `be_da` với khóa `Chatbot:ApiKey`, hoặc đặt các biến môi trường ở trên trước khi chạy backend. User Secrets chỉ là lựa chọn phát triển, không phải điều kiện để ứng dụng chạy ở nơi khác. Chạy frontend bằng `npm ci` và `npm run dev`; proxy Vite hiện tại gọi backend HTTPS cổng 7207.

## Triển khai bằng Docker từ checkout

```sh
docker build -t tksc:latest .
docker run --rm --env-file deploy/runtime.env -p 127.0.0.1:8080:8080 tksc:latest
```

Trước khi chạy, sao chép `deploy/runtime.env.example` thành `deploy/runtime.env` và điền thông tin của môi trường triển khai. File thật được Git bỏ qua; Docker build loại trừ secrets, `node_modules`, output cá nhân và cấu hình local. Docker build tạo React production assets và .NET publish, phục vụ cùng origin nên frontend gọi `/api` trên chính server đang triển khai, không gọi localhost của người chơi.

Ứng dụng hiện hữu vẫn cần SQL Server với schema/upgrades phù hợp, Google OAuth với callback `https://YOUR_DOMAIN/signin-google`, và cấu hình các dịch vụ thanh toán/email nếu sử dụng. Chatbot không thêm bảng/migration hoặc tự chạy SQL nâng cấp. Xem `be_da/README.md` và các hướng dẫn tích hợp tương ứng; không dùng schema baseline để ghi đè database đã có dữ liệu.

Đặt image sau reverse proxy/hosting có HTTPS. Container chỉ mở trên loopback trong lệnh mẫu; proxy chuyển tiếp toàn bộ `/`, `/api`, `/signin-google` tới nó, giữ nguyên Host, đặt `X-Forwarded-Proto: https` và `X-Forwarded-For`. Đặt `ReverseProxy__KnownProxies__0` bằng IP thực tế của proxy mà backend nhìn thấy; có thể thêm `__1` cho proxy tin cậy khác. Backend chỉ đọc forwarded headers từ các IP được cấu hình để tạo Google callback đúng HTTPS. Không bật tin cậy mọi proxy trên server public. Lưu Data Protection keys bền vững và chia sẻ key ring khi dùng nhiều replica để cookie đăng nhập không mất sau restart. Các bước HTTPS/OAuth/database cần xác nhận trên môi trường triển khai thật; image không tự tạo hạ tầng đó.

Không dùng `npm run preview` để thay cho cấu hình production API proxy. Nếu deploy frontend riêng, reverse proxy `/api` và `/signin-google` về backend cùng origin; không đưa OpenAI key xuống trình duyệt.

## Cập nhật kiến thức

Owner/người có quyền quản lý dữ liệu chuẩn bị nội dung trong bảng `policies` hiện có: `code`, `name`, `value`, `approved_by`, `effective_from`, `effective_to`. Chỉ đưa mã đã xác nhận công khai vào `Chatbot:PublicPolicyCodes`; mặc định danh sách rỗng để không công bố nhầm policy nội bộ. Không có UI xuất bản policy mới trong thay đổi này. Không tự chèn nội dung mẫu, tự gán người duyệt hoặc thay đổi schema.

Ví dụ cấu hình (mã phải thực sự tồn tại và được duyệt): `Chatbot__PublicPolicyCodes__0=PLAYER_BOOKING_GUIDE`. Thay nội dung/rút duyệt/đặt hết hiệu lực trong SQL sẽ tác động lần hỏi tiếp theo; không cần copy JSON hay tạo lại index lâu dài. Các policy tương lai, hết hạn, chưa duyệt, không trong allowlist hoặc vượt giới hạn độ dài không được dùng. Chỉ thêm nội dung công khai, không thêm dữ liệu cá nhân/bí mật. Khi kiểm tra ngày 09/11/2026, `Sep490` trên localhost có dữ liệu vận hành nhưng `policies` trống, nên cần chuẩn bị chính sách thật trước khi kiểm thử RAG chính sách có nguồn.

## Kiểm tra

```sh
dotnet build be_da/be_da.csproj
dotnet test tests/TKSC.Api.Tests
cd fe_da
npm ci
npm run build
npx eslint src/components/PlayerChatbot.jsx src/pages/home/HomePage.jsx
```

Test mặc định dùng giả lập OpenAI/nguồn dữ liệu, không cần key hoặc ghi SQL. Có test SQL chỉ đọc, bật bằng `TKSC_TEST_SQL_CONNECTION` rồi chạy `dotnet test tests/TKSC.Api.Tests --filter FullyQualifiedName~SqlPlayerChatbotTests`. Test kiểm tra chủ vé/chủ đơn và tài khoản khác, mapping lịch/chỗ, policy allowlist và các truy vấn hiện có; không insert/update/delete dữ liệu.

Sau deploy, đăng nhập Player hỏi “Lịch cầu lông hôm nay”, “Vé của tôi”, “Trạng thái thanh toán của tôi”; kiểm tra `mode=live`, thời điểm đọc, mã và trạng thái với màn hình nghiệp vụ. Thử đổi dữ liệu bằng quy trình nghiệp vụ bình thường rồi hỏi lại. Với policy có dữ liệu đã duyệt/allowlist và OpenAI key hợp lệ, xác nhận `mode=generated`; khi key sai phải thấy `reference`. Chưa có key thì không thể xác nhận tài khoản/model/quota hoặc chất lượng câu trả lời AI thật.

API đối chiếu với [OpenAI Structured Outputs](https://developers.openai.com/api/docs/guides/structured-outputs?api-mode=chat) và [GPT-5 mini](https://developers.openai.com/api/docs/models/gpt-5-mini). Request đặt `store: false` và không ghi câu hỏi hoặc response body của nhà cung cấp vào log của ứng dụng; chính sách dữ liệu của dịch vụ OpenAI vẫn áp dụng.
