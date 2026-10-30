# Tài liệu nguồn cho TKSC

Các file `.docx` là bản sao từ thư mục Drive của dự án. File `.txt` được trích từ nội dung Word để Codex tìm nhanh bằng `rg`; khi bảng, ảnh hoặc định dạng quan trọng, đọc `.docx` gốc.

| File | Dùng cho | Nguồn |
| --- | --- | --- |
| `RP1-Project-Introduction.docx` / `.txt` | Bối cảnh, mục tiêu và phạm vi | [Report 1](https://drive.google.com/file/d/1_tuKeOvvu5WsTrDDmJNXGteocrC8sfQf/view) |
| `RP2-Project-Management-Plan.docx` / `.txt` | Kế hoạch, nhóm, stack và tiến độ | [Report 2](https://drive.google.com/file/d/193yIPyQhDW1CAoeBF4bNTvRdD_n8-p4V/view) |
| `RP3-Software-Requirements.docx` / `.txt` | SRS đầy đủ, UC/FR, vai trò và quy tắc | [Report 3](https://drive.google.com/file/d/1BeBlmmL8pF7ctAItF_GF8W8itT4svSKr/view) |
| `RP3-Diagrams-Reference.docx` | Hình/sơ đồ RP3 mà người dùng chỉ định là chuẩn hơn | [Bản RP3 người dùng gửi](https://docs.google.com/document/d/1z3Fo5vXdXoOqaAaivdPsZIgHE0_MV4oS/edit) |

**Lưu ý:** Bản RP3 tham chiếu hình còn lẫn nội dung mẫu của template về tuyển dụng. Dùng nó cho hình/sơ đồ; dùng `RP3-Software-Requirements` cho nội dung yêu cầu TKSC. Nếu sơ đồ và mô tả khác nhau, ghi lại điểm khác biệt để đối chiếu trước khi triển khai. Các bản trên là snapshot tại thời điểm đưa vào repo; khi Drive thay đổi, phải cập nhật bản local và ghi nhận thay đổi.

Sau khi thay một file `.docx` nguồn, chạy `powershell -NoProfile -ExecutionPolicy Bypass -File docs/requirements/extract-text.ps1` ở gốc repo để cập nhật `.txt`. Tùy chọn này chỉ áp dụng cho lần chạy đó. File `.txt` chỉ phục vụ tìm kiếm; ảnh và bố cục bảng vẫn phải xem trong Word.

ERD vật lý hiện tại do người dùng gửi bằng Mermaid được lưu tại `../../db/erd.mmd`. Tài liệu RP4 chưa được xác nhận là thiết kế hoàn chỉnh và chưa dùng làm chuẩn triển khai.
