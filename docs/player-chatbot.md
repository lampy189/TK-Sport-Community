# Chatbot RAG cho Player

Yêu cầu ngày 09/11/2026: chatbot dùng SQL Server làm nguồn, dữ liệu vận hành được đọc lúc hỏi, chính sách chỉ dùng nội dung đã duyệt. Cùng code hoạt động với database demo hoặc thật qua cấu hình kết nối. Các quyền đọc liên quan RP3 UC-06 (lịch), UC-11–13 (thanh toán/vé), UC-21 (hoàn tiền) và quyền Player/Owner. Không tạo mã UC giả cho chatbot.

## Chức năng

- Khách chưa đăng nhập dùng khung chat trên trang Guest để hỏi lịch/giá/chỗ, chính sách công khai và liên hệ Messenger/Zalo. Endpoint `POST /api/chatbot/ask` cho phép anonymous nhưng vẫn yêu cầu CSRF; giới hạn 10 lượt/phút theo IP khi không đăng nhập. Endpoint công khai luôn sử dụng danh tính guest, kể cả khi body chứa userId hoặc trình duyệt có cookie.
- Hỏi vé/thanh toán/hoàn tiền cá nhân qua endpoint công khai trả `mode=login_required`, không truy vấn dữ liệu riêng, kèm nút đăng nhập trên giao diện. Player đăng nhập tiếp tục dùng endpoint riêng `/api/player/chatbot/ask` để xem dữ liệu của mình. Chính sách trong allowlist công khai cũng được đọc bởi Guest; không đưa nội dung dành riêng thành viên vào allowlist này.

- Nút **Hỏi trợ lý** hiện trong giao diện Player, nhập câu hỏi tự do, có trạng thái chờ/lỗi và phần mở nguồn tham khảo.
- `POST /api/player/chatbot/ask`, body ví dụ `{ "question": "Lịch cầu lông hôm nay", "scope": "sessions", "onDate": "2026-11-09", "sport": "badminton", "recordId": null }`. Yêu cầu cookie Player và `X-CSRF-TOKEN` lấy từ `/api/auth/csrf`. Không nhận userId/playerId làm danh tính tra cứu.
- Giao diện gửi `question` và `context` do câu trả lời trước cung cấp, không có menu/câu hỏi chọn sẵn. Với `scope=auto` (mặc định), **OpenAI diễn giải ý nghĩa trước khi tra cứu**, không dùng regex làm bộ hiểu chính. Structured Outputs trả về một trong các ý định cho phép, ngày tuyệt đối, môn, mã bản ghi, khoảng giờ bắt đầu và câu hỏi đã diễn giải. Backend kiểm tra lại mọi trường trước khi chọn truy vấn SQL có sẵn; AI không được sinh SQL, userId, quyền hay URL. Các client cũ truyền `scope` cụ thể vẫn dùng bộ lọc có cấu trúc.
- Lịch/giá/chỗ, vé, đơn thanh toán social, hoàn tiền: truy vấn SQL cố định có tham số mỗi lần hỏi, định dạng kết quả trực tiếp (`mode=live`). Không dùng LLM để đoán trạng thái/số tiền và không gửi những kết quả này sang OpenAI.
- Chính sách: đọc các hàng hiện đang được phép từ `policies`, chia đoạn tối đa 1.200 ký tự có chồng lặp 100 ký tự, xây BM25 cho request hiện tại và lấy tối đa ba đoạn liên quan cho OpenAI. Đây là RAG từ SQL dùng tìm kiếm từ khóa, chưa có vector search/embeddings. Không có cache kiến thức xuyên request, nên sửa/gỡ duyệt/hết hiệu lực sẽ phản ánh ở lần hỏi tiếp theo, không cần train lại hoặc rebuild ứng dụng.
- Câu trả lời gồm `answer`, `mode` (`live`, `generated`, `reference`, `no_match`, `unavailable`, `login_required`, `contact`, `clarification`, `conversation`), `sources`, `retrievedAt`, `scope`, `context`, `notice`. Nguồn SQL có bảng/phạm vi và mã bản ghi; thời điểm đọc hiện trên UI. ID nguồn AI được kiểm tra với tập vừa truy xuất; người dùng vẫn cần đối chiếu nội dung gốc.
- Khi thiếu key hoặc bước hiểu ý định lỗi, backend chỉ dùng nhận diện cơ bản với **notice hiển thị rõ trên UI**. Nếu câu hỏi có ngữ cảnh nối tiếp mà AI không sẵn sàng, không đoán từ khóa: báo lỗi để thử lại. Đầu ra ý định không hợp lệ sẽ không được truy vấn SQL. Ở bước sinh câu trả lời chính sách, lỗi AI hiển thị đoạn trích (`reference`), không giả lập sinh AI.
- Câu hỏi 2–1.200 ký tự, ngữ cảnh tối đa 1.200 ký tự câu hỏi đã diễn giải + 400 ký tự lời hỏi lại, body tối đa 16 KB. 10 lượt/phút/Player hoặc IP guest trên mỗi instance. Hiểu ý định tối đa 20 giây; sinh câu trả lời chính sách tối đa 40 giây; frontend chờ tối đa 80 giây cho cả luồng. Khi chạy nhiều replica, dùng thêm hạn mức chung tại API gateway nếu cần.
- Ngữ cảnh ngắn giữ chủ đề và điều kiện câu trước: ví dụ “có kèo đánh cầu tối mai không?” → “còn ngày kia?” → “đổi sang pickleball”. Ngày tương đối luôn tính theo thời điểm hiện tại UTC+7. Câu không rõ giờ hoặc thiếu thông tin được hỏi lại bằng lời. Nút **Bắt đầu cuộc trò chuyện mới** xóa ngữ cảnh và nội dung hiển thị. Tin nhắn chỉ nằm trong bộ nhớ trang, không lưu database/localStorage.
- Bước hiểu ý định gửi câu hỏi người dùng và ngữ cảnh đã diễn giải sang OpenAI; **không gửi các hàng kết quả vé/đơn/hoàn tiền từ SQL**, userId, email hoặc hồ sơ tài khoản. Ngữ cảnh do client gửi không có giá trị phân quyền. Nhánh chính sách còn gửi các đoạn đã duyệt/công khai để tạo câu trả lời. Không có công cụ ghi SQL, đặt chỗ, thanh toán, sửa điểm, đọc tin nhắn hoặc file tùy ý. SQL lỗi không fallback sang dữ liệu mẫu/cũ.

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

### Đặt sân qua Messenger/Zalo

Người chơi nhập câu hỏi tự do như “Tôi muốn đặt sân cầu lông tối mai” hoặc “Cho tôi Zalo của sân”. Chatbot trả lời kèm nội dung có thể sao chép và nút mở kênh chat đã cấu hình. Không thêm câu hỏi chọn sẵn hay menu đặt sân. Tính năng này là chuyển người chơi sang liên hệ sân, không tự gửi tin, giữ chỗ, tạo vé/đơn, hay khẳng định sân đã nhận/xác nhận yêu cầu.

Theo yêu cầu ngày 10/11/2026, hai link để trống trong `be_da/Controllers/appsettings.json`:

```json
"Chatbot": {
  "MessengerUrl": "",
  "ZaloUrl": ""
}
```

Điền URL thật vào các trường trên sau này, hoặc dùng biến môi trường `Chatbot__MessengerUrl` / `Chatbot__ZaloUrl` trên server rồi khởi động lại backend. Chấp nhận `https://m.me/TEN_PAGE`, `https://www.messenger.com/t/PAGE_ID` và `https://zalo.me/SO_DIEN_THOAI_HOAC_ID`. URL phải là HTTPS, không có query/fragment hoặc thông tin đăng nhập; không điền URL trang chủ Facebook thay cho link chat. Đây là liên hệ chung của sân/đơn vị vận hành, chưa phân tuyến nhiều cơ sở.

Kênh trống hoặc URL không hợp lệ sẽ không có nút bấm. Nếu cả hai trống, chatbot báo chưa thiết lập liên hệ và vẫn cung cấp nội dung để chuẩn bị. URL chỉ lấy từ cấu hình server, không lấy từ câu hỏi hay AI. Người chơi sao chép, mở ứng dụng, dán và tự gửi. Nếu trình duyệt không cho phép clipboard, vẫn có thể sao chép thủ công. Không cần Facebook/Zalo access token; OpenAI key cần cho hiểu cách diễn đạt tự nhiên, còn nhận diện liên hệ cơ bản vẫn chạy khi không có key.

Code nằm trong Git; dữ liệu được đọc từ `ConnectionStrings:Sep490` của môi trường chạy. Không cần Ollama, vector database hoặc đường dẫn máy tác giả. Kho hướng dẫn mẫu cũ đã chuyển sang `tests/TKSC.Api.Tests/Fixtures` chỉ phục vụ test, không nhúng vào backend và không dùng khi trả lời.

Các biến môi trường trên **backend**:

| Biến | Ý nghĩa |
| --- | --- |
| `Chatbot__ApiKey` | OpenAI API key có quyền gọi model và hạn mức sử dụng; bắt buộc để sinh câu trả lời AI |
| `Chatbot__Model` | Mặc định `gpt-5-mini`; có thể đổi sang model hỗ trợ Chat Completions và Structured Outputs |
| `ConnectionStrings__Sep490` | Database ứng dụng đang dùng; chatbot dùng cùng kết nối, không có database demo riêng ngầm định |
| `Chatbot__PublicPolicyCodes__0` | Mã policy đã được duyệt công khai cho Player; thêm `__1`, `__2`… cho mã tiếp theo (tối đa 100 mã) |

Không đặt key trong frontend, biến `VITE_*`, source code, Dockerfile hoặc Git. Mỗi môi trường cung cấp key qua secret manager/biến môi trường của server. Không có key thì các mục SQL vận hành vẫn chạy và chính sách trả đoạn trích, nhưng chưa sinh câu trả lời AI.

**Key cũng là điều kiện để bật hiểu ngôn ngữ tự nhiên và hội thoại nối tiếp.** Ngày 10/11/2026 chưa tìm thấy key trong cấu hình phát triển được kiểm tra, nên các test NLP hiện dùng giả lập đầu ra mô hình để kiểm tra luồng/giới hạn; chúng không chứng minh chất lượng hiểu tiếng Việt của mô hình thật. Sau khi cấu hình key, kiểm tra bằng câu người dùng thực sự nói, lỗi gõ và câu nối, không chỉ câu mẫu trùng từ khóa. Ví dụ: “bên mình nhận dc khoản em gửi chưa ạ”, “suất em mua còn dùng được chứ”, “có kèo đánh cầu nào tối mai”, “còn ngày kia thì sao?”.

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
