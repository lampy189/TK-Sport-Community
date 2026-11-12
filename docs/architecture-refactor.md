# Refactor một project theo hướng Clean Architecture

## Cấu trúc

Vẫn chỉ có một project ứng dụng: `be_da/be_da.csproj`, target .NET 8; các project test có sẵn được giữ nguyên. Không thêm package, CQRS, MediatR, Generic Repository hoặc UnitOfWork.

```text
be_da/
  Domain/Entities, Assessments, Payments, Exceptions
  Application/
    Interfaces/Services, Repositories, External
    Services, DTOs, Validators, Mappings
  Infrastructure/
    Persistence/Contexts, Configurations, Repositories, Migrations
    ExternalServices
  Presentation/
    Controllers, Authorization, Middlewares, Filters, Swagger, BackgroundServices
  Program.cs
  Composition.cs
  appsettings*.json
  be_da.csproj
```

## Thay đổi chính

- Controller gọi service interface; các truy vấn EF/SQL của giỏ hàng, thông báo, kết quả kỹ năng, doanh thu và quản lý cơ sở chuyển sang repository.
- Các service thanh toán hàng hóa, chấm điểm, review, hậu mãi vé và hỗ trợ vé dùng repository interface. Include, tracking, materialization, SQL, SaveChanges và khóa database ở Infrastructure.
- Nghiệp vụ giỏ hàng, cập nhật hồ sơ/ảnh, cộng đồng/thông báo bạn bè và validation kho chuyển vào Application. DTO và request contract được tập trung tại Application/DTOs.
- Interface repository ở Application/Interfaces/Repositories; tích hợp thanh toán, AI, SMTP và lưu ảnh có abstraction ở Application/Interfaces/External.
- HttpDataAccessContext cung cấp danh tính và cơ sở cho Infrastructure qua IDataAccessContext; không truyền HttpContext vào Application/Infrastructure. SQL session context vẫn được khởi tạo khi mở kết nối, kể cả worker không có HTTP request.
- Worker email ở Presentation; TicketEmailService điều phối, TicketEmailRepository giữ khóa dòng/transaction, SmtpTicketEmailSender thực hiện SMTP. Giữ lịch retry, timeout, Message-ID và thời điểm lưu trạng thái.
- Mapping Table/Column/NotMapped của dữ liệu hỗ trợ và hậu mãi vé được chuyển sang Fluent API trong Infrastructure, giữ tên bảng/cột và các thuộc tính không được persist.
- DbContext, repository và service dùng scoped lifetime. Worker tạo scope như trước; bộ phân loại lỗi database không giữ trạng thái và dùng singleton.
- Các SQL repository có sẵn được tái sử dụng, giữ nguyên thao tác nguyên tử, khóa và kiểm tra nhất quán bên trong transaction. Không chuyển toàn bộ ADO.NET sang EF Core.

## Luồng thực tế: tạo giỏ hàng

`POST /api/v1/store/orders/cart` → StoreController → IStoreService/StoreService → IStoreRepository/StoreRepository → Sep490Context → SQL Server.

Controller lấy danh tính và cơ sở từ phiên đăng nhập. Service kiểm tra giỏ rỗng, cơ sở, sản phẩm, số lượng và tồn kho, tính tổng tiền và tạo Order. Repository giữ hai lần SaveChanges như cũ: lưu Order lấy ID, rồi lưu OrderItem; cập nhật cơ sở của đơn trước khi commit. Nếu đã có transaction thì tiếp tục dùng transaction đó. Entity và DTO là dữ liệu trong luồng này, không phải một bước truy vấn riêng.

## Bảo toàn và kiểm chứng

- Không chạy migration hoặc chỉnh sửa database. File migration SQL lịch sử được di chuyển nguyên nội dung; không có EF migration/model snapshot mới.
- Giữ file .csproj, tên assembly, solution, profile HTTPS và đường dẫn Docker tới project. Cập nhật đường dẫn appsettings, resource Swagger và tài liệu chịu ảnh hưởng.
- Giữ nội dung thay đổi có sẵn của người dùng trong appsettings và file mẫu chatbot khi di chuyển. Tài liệu chatbot chỉ được cập nhật đường dẫn liên quan. Không sửa thay đổi frontend CheckInPage.jsx của người dùng.
- Đã đối chiếu 24 Controller với HEAD: route, HTTP verb, authorization, response attributes và tham số đầu vào HTTP không đổi; tham số DI FromServices được thay bằng service interface.
- Không commit/push. Không khởi động backend nền.
- Restore solution và build solution: thành công, 0 warning, 0 error.
- TKSC.Api.Tests: 628 đạt, 9 bỏ qua, 0 lỗi. Bao gồm kiểm tra ranh giới tầng, constructor Controller, repository không trả IQueryable, Fluent mapping và dịch SQL Server của truy vấn đã di chuyển. Kiểm tra dịch SQL chủ động dừng trước khi mở kết nối thật.
- PaymentTests: 67 đạt, 0 lỗi, đã chạy lại sau thay đổi cuối.
- Chín test SQL tích hợp bị bỏ qua vì chưa cấu hình TKSC_TEST_SQL_CONNECTION. Chưa xác minh transaction/RLS trên database thật, OAuth Google thật, payOS webhook thật, SMTP hoặc OpenAI thật. Test API dùng các fake/backchannel có sẵn; không coi chúng là kiểm chứng tích hợp production.

## Chạy ứng dụng và EF Core

Từ gốc repository:

```powershell
dotnet restore be_da/be_da.slnx
dotnet build be_da/be_da.slnx
dotnet test tests/TKSC.Api.Tests
dotnet test tests/PaymentTests
dotnet run --project be_da/be_da.csproj --launch-profile https
```

Visual Studio: vẫn mở `be_da/be_da.slnx`, chọn project `be_da` và profile `https`. appsettings hiện nằm ở `be_da/appsettings*.json`; cấu hình bí mật vẫn dùng User Secrets, biến môi trường hoặc appsettings.Local.json được ignore.

DbContext hiện ở `Infrastructure/Persistence/Contexts/Sep490Context.cs`, namespace `TKSC.Infrastructure.Persistence.Contexts`. Assembly vẫn là `be_da`, không cần đổi migrations assembly. Nếu đã cài dotnet-ef tương thích EF Core 8, có thể kiểm tra metadata (không áp dụng migration) bằng:

```powershell
dotnet ef dbcontext info --project be_da/be_da.csproj --startup-project be_da/be_da.csproj --context TKSC.Infrastructure.Persistence.Contexts.Sep490Context
```

Lệnh EF trên là hướng dẫn, chưa chạy trong lần refactor. File migration SQL cũ hiện ở `be_da/Infrastructure/Persistence/Migrations/20261020-payos.sql`; không tự chạy file này.

## Danh sách file

Các file chia nhỏ có thể xuất hiện dưới [-] ở vị trí cũ và [+] ở vị trí mới; mã chức năng được chuyển trước khi bỏ file chứa cũ. Chi tiết tách type nằm ở bảng cuối.

### [→] Di chuyển

| Cũ | Mới |
|---|---|
| `be_da/Controllers/appsettings.json` | `be_da/appsettings.json` |
| `be_da/Controllers/appsettings.Payment.example.json` | `be_da/appsettings.Payment.example.json` |
| `be_da/Controllers/AssessmentsController.cs` | `be_da/Presentation/Controllers/AssessmentsController.cs` |
| `be_da/Controllers/AuthController.cs` | `be_da/Presentation/Controllers/AuthController.cs` |
| `be_da/Controllers/CommunityController.cs` | `be_da/Presentation/Controllers/CommunityController.cs` |
| `be_da/Controllers/GoodsHandoverController.cs` | `be_da/Presentation/Controllers/GoodsHandoverController.cs` |
| `be_da/Controllers/GoodsPaymentsController.cs` | `be_da/Presentation/Controllers/GoodsPaymentsController.cs` |
| `be_da/Controllers/LateEntryController.cs` | `be_da/Presentation/Controllers/LateEntryController.cs` |
| `be_da/Controllers/Middlewares/PaymentExceptionHandler.cs` | `be_da/Presentation/Middlewares/PaymentExceptionHandler.cs` |
| `be_da/Controllers/Middlewares/ValidatedRequestAttribute.cs` | `be_da/Presentation/Filters/ValidatedRequestAttribute.cs` |
| `be_da/Controllers/NotificationsController.cs` | `be_da/Presentation/Controllers/NotificationsController.cs` |
| `be_da/Controllers/PaymentsController.cs` | `be_da/Presentation/Controllers/PaymentsController.cs` |
| `be_da/Controllers/PlayerChatbotController.cs` | `be_da/Presentation/Controllers/PlayerChatbotController.cs` |
| `be_da/Controllers/PlayerGoodsOrdersController.cs` | `be_da/Presentation/Controllers/PlayerGoodsOrdersController.cs` |
| `be_da/Controllers/PlayerSkillResultsController.cs` | `be_da/Presentation/Controllers/PlayerSkillResultsController.cs` |
| `be_da/Controllers/ProfileController.cs` | `be_da/Presentation/Controllers/ProfileController.cs` |
| `be_da/Controllers/Program.cs` | `be_da/Program.cs` |
| `be_da/Controllers/RefereeSessionsController.cs` | `be_da/Presentation/Controllers/RefereeSessionsController.cs` |
| `be_da/Controllers/RefereeSuppliesController.cs` | `be_da/Presentation/Controllers/RefereeSuppliesController.cs` |
| `be_da/Controllers/RevenueController.cs` | `be_da/Presentation/Controllers/RevenueController.cs` |
| `be_da/Controllers/SessionController.cs` | `be_da/Presentation/Controllers/SessionController.cs` |
| `be_da/Controllers/SessionSuppliesController.cs` | `be_da/Presentation/Controllers/SessionSuppliesController.cs` |
| `be_da/Controllers/SkillProfileController.cs` | `be_da/Presentation/Controllers/SkillProfileController.cs` |
| `be_da/Controllers/SkillReviewsController.cs` | `be_da/Presentation/Controllers/SkillReviewsController.cs` |
| `be_da/Controllers/SportCentersController.cs` | `be_da/Presentation/Controllers/SportCentersController.cs` |
| `be_da/Controllers/StockController.cs` | `be_da/Presentation/Controllers/StockController.cs` |
| `be_da/Controllers/StoreController.cs` | `be_da/Presentation/Controllers/StoreController.cs` |
| `be_da/Controllers/TicketAftercareController.cs` | `be_da/Presentation/Controllers/TicketAftercareController.cs` |
| `be_da/Controllers/TicketCheckInController.cs` | `be_da/Presentation/Controllers/TicketCheckInController.cs` |
| `be_da/Authorization/AccountCookieEvents.cs` | `be_da/Presentation/Authorization/AccountCookieEvents.cs` |
| `be_da/Authorization/AccountPrincipal.cs` | `be_da/Presentation/Authorization/AccountPrincipal.cs` |
| `be_da/Authorization/CenterSelection.cs` | `be_da/Presentation/Authorization/CenterSelection.cs` |
| `be_da/Swagger/AuthDocumentationFilter.cs` | `be_da/Presentation/Swagger/AuthDocumentationFilter.cs` |
| `be_da/Swagger/tksc-auth.js` | `be_da/Presentation/Swagger/tksc-auth.js` |
| `be_da/Application/Abstractions/IAccountStore.cs` | `be_da/Application/Interfaces/Repositories/IAccountStore.cs` |
| `be_da/Application/Abstractions/IFirebaseNotificationService.cs` | `be_da/Application/Interfaces/External/IFirebaseNotificationService.cs` |
| `be_da/Application/Abstractions/IFriendshipStore.cs` | `be_da/Application/Interfaces/Repositories/IFriendshipStore.cs` |
| `be_da/Application/Abstractions/ILateEntryStore.cs` | `be_da/Application/Interfaces/Repositories/ILateEntryStore.cs` |
| `be_da/Application/Abstractions/IProfileStore.cs` | `be_da/Application/Interfaces/Repositories/IProfileStore.cs` |
| `be_da/Application/Abstractions/ISessionStore.cs` | `be_da/Application/Interfaces/Repositories/ISessionStore.cs` |
| `be_da/Application/Abstractions/ISessionSupplyStore.cs` | `be_da/Application/Interfaces/Repositories/ISessionSupplyStore.cs` |
| `be_da/Application/Abstractions/ISkillProfileStore.cs` | `be_da/Application/Interfaces/Repositories/ISkillProfileStore.cs` |
| `be_da/Application/Abstractions/IStockStore.cs` | `be_da/Application/Interfaces/Repositories/IStockStore.cs` |
| `be_da/Application/Interfaces/IPaymentRepository.cs` | `be_da/Application/Interfaces/Repositories/IPaymentRepository.cs` |
| `be_da/Application/Interfaces/IPaymentService.cs` | `be_da/Application/Interfaces/Services/IPaymentService.cs` |
| `be_da/Application/Interfaces/IPaymentGateway.cs` | `be_da/Application/Interfaces/External/IPaymentGateway.cs` |
| `be_da/Infrastructure/DbContext/Sep490Context.cs` | `be_da/Infrastructure/Persistence/Contexts/Sep490Context.cs` |
| `be_da/Infrastructure/Configurations/PayOsClientFactory.cs` | `be_da/Infrastructure/ExternalServices/PayOsClientFactory.cs` |
| `be_da/Infrastructure/Configurations/Sep490ModelConfiguration.cs` | `be_da/Infrastructure/Persistence/Configurations/Sep490ModelConfiguration.cs` |
| `be_da/Infrastructure/Migrations/20261020-payos.sql` | `be_da/Infrastructure/Persistence/Migrations/20261020-payos.sql` |
| `be_da/Infrastructure/Repositories/PaymentRepository.cs` | `be_da/Infrastructure/Persistence/Repositories/PaymentRepository.cs` |
| `be_da/Infrastructure/Services/OpenAiChatAnswerGenerator.cs` | `be_da/Infrastructure/ExternalServices/OpenAiChatAnswerGenerator.cs` |
| `be_da/Infrastructure/Services/OpenAiChatIntentInterpreter.cs` | `be_da/Infrastructure/ExternalServices/OpenAiChatIntentInterpreter.cs` |
| `be_da/Infrastructure/Services/PayOsGateway.cs` | `be_da/Infrastructure/ExternalServices/PayOsGateway.cs` |
| `be_da/Infrastructure/Notifications/FirebaseNotificationService.cs` | `be_da/Infrastructure/ExternalServices/FirebaseNotificationService.cs` |
| `be_da/Infrastructure/Interfaces/IPayOsClientFactory.cs` | `be_da/Infrastructure/ExternalServices/Interfaces/IPayOsClientFactory.cs` |
| `be_da/Infrastructure/Persistence/LateEntrySql.cs` | `be_da/Infrastructure/Persistence/Repositories/LateEntrySql.cs` |
| `be_da/Infrastructure/Persistence/SqlAccountStore.cs` | `be_da/Infrastructure/Persistence/Repositories/SqlAccountStore.cs` |
| `be_da/Infrastructure/Persistence/SqlFriendshipStore.cs` | `be_da/Infrastructure/Persistence/Repositories/SqlFriendshipStore.cs` |
| `be_da/Infrastructure/Persistence/SqlLateEntryStore.cs` | `be_da/Infrastructure/Persistence/Repositories/SqlLateEntryStore.cs` |
| `be_da/Infrastructure/Persistence/SqlPlayerChatData.cs` | `be_da/Infrastructure/Persistence/Repositories/SqlPlayerChatData.cs` |
| `be_da/Infrastructure/Persistence/SqlProfileStore.cs` | `be_da/Infrastructure/Persistence/Repositories/SqlProfileStore.cs` |
| `be_da/Infrastructure/Persistence/SqlSessionStore.cs` | `be_da/Infrastructure/Persistence/Repositories/SqlSessionStore.cs` |
| `be_da/Infrastructure/Persistence/SqlSessionSupplyStore.cs` | `be_da/Infrastructure/Persistence/Repositories/SqlSessionSupplyStore.cs` |
| `be_da/Infrastructure/Persistence/SqlSkillProfileStore.cs` | `be_da/Infrastructure/Persistence/Repositories/SqlSkillProfileStore.cs` |
| `be_da/Infrastructure/Persistence/SqlStockStore.cs` | `be_da/Infrastructure/Persistence/Repositories/SqlStockStore.cs` |
| `be_da/Application/Stock/SessionSupplyDtos.cs` | `be_da/Application/Validators/SupplyRules.cs` |
| `be_da/Application/Stock/StockDtos.cs` | `be_da/Application/Validators/StockRules.cs` |
| `be_da/Application/Stock/StockReceiptRules.cs` | `be_da/Application/Validators/StockReceiptRules.cs` |
| `be_da/Application/Stock/StockUnitRules.cs` | `be_da/Application/Validators/StockUnitRules.cs` |
| `be_da/Application/Chatbot/BookingContactReply.cs` | `be_da/Application/Services/Chatbot/BookingContactReply.cs` |
| `be_da/Application/Chatbot/ChatIntentValidation.cs` | `be_da/Application/Services/Chatbot/ChatIntentValidation.cs` |
| `be_da/Application/Chatbot/ChatLookupRouter.cs` | `be_da/Application/Services/Chatbot/ChatLookupRouter.cs` |
| `be_da/Application/Chatbot/PlayerChatbot.cs` | `be_da/Application/Services/Chatbot/PlayerChatbot.cs` |
| `be_da/Application/Chatbot/PlayerKnowledgeIndex.cs` | `be_da/Application/Services/Chatbot/PlayerKnowledgeIndex.cs` |
| `be_da/Application/Accounts/GoogleSignIn.cs` | `be_da/Application/Services/Accounts/GoogleSignIn.cs` |
| `be_da/Application/Accounts/LoginPortal.cs` | `be_da/Application/Services/Accounts/LoginPortal.cs` |
| `be_da/Application/Notifications/TicketNotifications.cs` | `be_da/Application/Services/Notifications/TicketNotifications.cs` |
| `be_da/Authorization/SportCenterConnection.cs` | `be_da/Infrastructure/Persistence/SportCenterConnection.cs` |
| `be_da/Infrastructure/Notifications/TicketEmailWorker.cs` | `be_da/Presentation/BackgroundServices/TicketEmailWorker.cs` |

### [+] Thêm mới / tách thành file riêng

- [+] `be_da/Application/DTOs/AppealDecision.cs`
- [+] `be_da/Application/DTOs/AssessmentDimensionScoreDto.cs`
- [+] `be_da/Application/DTOs/AssessmentEvidence.cs`
- [+] `be_da/Application/DTOs/AssessmentResultDto.cs`
- [+] `be_da/Application/DTOs/AssessmentView.cs`
- [+] `be_da/Application/DTOs/AssignRequest.cs`
- [+] `be_da/Application/DTOs/AttendeeView.cs`
- [+] `be_da/Application/DTOs/AvatarFile.cs`
- [+] `be_da/Application/DTOs/BatchLineError.cs`
- [+] `be_da/Application/DTOs/BatchReceiptLine.cs`
- [+] `be_da/Application/DTOs/BatchReceiptRequest.cs`
- [+] `be_da/Application/DTOs/BatchReceiptResult.cs`
- [+] `be_da/Application/DTOs/BookingContact.cs`
- [+] `be_da/Application/DTOs/BookingLink.cs`
- [+] `be_da/Application/DTOs/BuyerGoodsOrder.cs`
- [+] `be_da/Application/DTOs/CashRequest.cs`
- [+] `be_da/Application/DTOs/ChangeRequest.cs`
- [+] `be_da/Application/DTOs/ChatAnswer.cs`
- [+] `be_da/Application/DTOs/ChatContext.cs`
- [+] `be_da/Application/DTOs/ChatData.cs`
- [+] `be_da/Application/DTOs/ChatIntent.cs`
- [+] `be_da/Application/DTOs/ChatLookup.cs`
- [+] `be_da/Application/DTOs/ChatMessageItem.cs`
- [+] `be_da/Application/DTOs/ChatQuestion.cs`
- [+] `be_da/Application/DTOs/ChatSource.cs`
- [+] `be_da/Application/DTOs/ChatbotOptions.cs`
- [+] `be_da/Application/DTOs/CheckInRequest.cs`
- [+] `be_da/Application/DTOs/CreateProductRequest.cs`
- [+] `be_da/Application/DTOs/CriterionView.cs`
- [+] `be_da/Application/DTOs/DevLoginRequest.cs`
- [+] `be_da/Application/DTOs/ExceptionDecision.cs`
- [+] `be_da/Application/DTOs/FlaggedPaymentApprovalRequest.cs`
- [+] `be_da/Application/DTOs/FriendItem.cs`
- [+] `be_da/Application/DTOs/FriendSuggestionItem.cs`
- [+] `be_da/Application/DTOs/FriendshipActionResult.cs`
- [+] `be_da/Application/DTOs/FriendshipRecord.cs`
- [+] `be_da/Application/DTOs/GeneratedAnswer.cs`
- [+] `be_da/Application/DTOs/GoodsCheckoutSnapshot.cs`
- [+] `be_da/Application/DTOs/GoodsInvoice.cs`
- [+] `be_da/Application/DTOs/GoodsLine.cs`
- [+] `be_da/Application/DTOs/GoodsOrderView.cs`
- [+] `be_da/Application/DTOs/GoodsStockLine.cs`
- [+] `be_da/Application/DTOs/HoldLateEntryRequest.cs`
- [+] `be_da/Application/DTOs/IssueRequest.cs`
- [+] `be_da/Application/DTOs/IssueSupplyRequest.cs`
- [+] `be_da/Application/DTOs/KnowledgeArticle.cs`
- [+] `be_da/Application/DTOs/LateEntryOffer.cs`
- [+] `be_da/Application/DTOs/MarkInput.cs`
- [+] `be_da/Application/DTOs/MemberSearchResult.cs`
- [+] `be_da/Application/DTOs/MergedReceiptLine.cs`
- [+] `be_da/Application/DTOs/NotificationContracts.cs`
- [+] `be_da/Application/DTOs/NotifyChatMessageRequest.cs`
- [+] `be_da/Application/DTOs/OperationResult.cs`
- [+] `be_da/Application/DTOs/PendingFriendRequestItem.cs`
- [+] `be_da/Application/DTOs/Persistence/GoodsPaymentRepositoryData.cs`
- [+] `be_da/Application/DTOs/Persistence/SkillAssessmentRepositoryData.cs`
- [+] `be_da/Application/DTOs/Persistence/SkillReviewRepositoryData.cs`
- [+] `be_da/Application/DTOs/Persistence/TicketAftercareRepositoryData.cs`
- [+] `be_da/Application/DTOs/Persistence/TicketSupportRepositoryData.cs`
- [+] `be_da/Application/DTOs/PlayerSkillOverviewDto.cs`
- [+] `be_da/Application/DTOs/PlayerSportProfileInfo.cs`
- [+] `be_da/Application/DTOs/PlayerSportSkillProfileDto.cs`
- [+] `be_da/Application/DTOs/ProductUnit.cs`
- [+] `be_da/Application/DTOs/ProductUnitInput.cs`
- [+] `be_da/Application/DTOs/PublicPlayerProfileDetails.cs`
- [+] `be_da/Application/DTOs/PublicRatingHistoryItem.cs`
- [+] `be_da/Application/DTOs/PublishAssessmentRequest.cs`
- [+] `be_da/Application/DTOs/RatingHistoryItemDto.cs`
- [+] `be_da/Application/DTOs/ReceiptCatalogItem.cs`
- [+] `be_da/Application/DTOs/RegisterFcmTokenRequest.cs`
- [+] `be_da/Application/DTOs/RegisterSessionResult.cs`
- [+] `be_da/Application/DTOs/ReportSupplyUseRequest.cs`
- [+] `be_da/Application/DTOs/ResolvedReceiptLine.cs`
- [+] `be_da/Application/DTOs/RespondFriendRequest.cs`
- [+] `be_da/Application/DTOs/ReturnSupplyRequest.cs`
- [+] `be_da/Application/DTOs/RevenueContracts.cs`
- [+] `be_da/Application/DTOs/ReviewEvent.cs`
- [+] `be_da/Application/DTOs/ReviewInput.cs`
- [+] `be_da/Application/DTOs/ReviewRequest.cs`
- [+] `be_da/Application/DTOs/RubricDimensionDto.cs`
- [+] `be_da/Application/DTOs/RubricView.cs`
- [+] `be_da/Application/DTOs/SaveAssessmentRequest.cs`
- [+] `be_da/Application/DTOs/SelectCenterRequest.cs`
- [+] `be_da/Application/DTOs/SendFriendRequest.cs`
- [+] `be_da/Application/DTOs/SendMessageRequest.cs`
- [+] `be_da/Application/DTOs/SessionDetails.cs`
- [+] `be_da/Application/DTOs/SessionFilter.cs`
- [+] `be_da/Application/DTOs/SessionParticipant.cs`
- [+] `be_da/Application/DTOs/SessionSummary.cs`
- [+] `be_da/Application/DTOs/SessionSupplyDetails.cs`
- [+] `be_da/Application/DTOs/Settlement.cs`
- [+] `be_da/Application/DTOs/SportProfileItem.cs`
- [+] `be_da/Application/DTOs/SportProfileResult.cs`
- [+] `be_da/Application/DTOs/SportRubricDto.cs`
- [+] `be_da/Application/DTOs/StockCodeLookup.cs`
- [+] `be_da/Application/DTOs/StockMovementEntry.cs`
- [+] `be_da/Application/DTOs/StockMovementRequest.cs`
- [+] `be_da/Application/DTOs/StockProduct.cs`
- [+] `be_da/Application/DTOs/StockProductFilter.cs`
- [+] `be_da/Application/DTOs/StockResult.cs`
- [+] `be_da/Application/DTOs/SubmitSkillSurveyRequest.cs`
- [+] `be_da/Application/DTOs/SubmitSkillSurveyResult.cs`
- [+] `be_da/Application/DTOs/SupplyAllocation.cs`
- [+] `be_da/Application/DTOs/SupplyRecipient.cs`
- [+] `be_da/Application/DTOs/SupplyResult.cs`
- [+] `be_da/Application/DTOs/SupplySession.cs`
- [+] `be_da/Application/DTOs/SurveyDimensionAnswerInput.cs`
- [+] `be_da/Application/DTOs/TicketEmailDetails.cs`
- [+] `be_da/Application/DTOs/TicketReportInput.cs`
- [+] `be_da/Application/DTOs/TicketSupportInput.cs`
- [+] `be_da/Application/DTOs/TimeSlotSummary.cs`
- [+] `be_da/Application/DTOs/UpdateProductRequest.cs`
- [+] `be_da/Application/DTOs/UpdateProfileRequest.cs`
- [+] `be_da/Application/DTOs/UpdateSportProfileRequest.cs`
- [+] `be_da/Application/DTOs/UserProfileDetails.cs`
- [+] `be_da/Application/DTOs/VenueSummary.cs`
- [+] `be_da/Application/Interfaces/External/IAvatarStorage.cs`
- [+] `be_da/Application/Interfaces/External/IChatAnswerGenerator.cs`
- [+] `be_da/Application/Interfaces/External/IChatIntentInterpreter.cs`
- [+] `be_da/Application/Interfaces/External/IDataAccessContext.cs`
- [+] `be_da/Application/Interfaces/External/ITicketEmailSender.cs`
- [+] `be_da/Application/Interfaces/Repositories/IGoodsPaymentRepository.cs`
- [+] `be_da/Application/Interfaces/Repositories/INotificationRepository.cs`
- [+] `be_da/Application/Interfaces/Repositories/IPersistenceFailureClassifier.cs`
- [+] `be_da/Application/Interfaces/Repositories/IPlayerChatData.cs`
- [+] `be_da/Application/Interfaces/Repositories/IPlayerSkillResultRepository.cs`
- [+] `be_da/Application/Interfaces/Repositories/IRevenueRepository.cs`
- [+] `be_da/Application/Interfaces/Repositories/ISkillAssessmentRepository.cs`
- [+] `be_da/Application/Interfaces/Repositories/ISkillReviewRepository.cs`
- [+] `be_da/Application/Interfaces/Repositories/ISportCenterRepository.cs`
- [+] `be_da/Application/Interfaces/Repositories/IStoreRepository.cs`
- [+] `be_da/Application/Interfaces/Repositories/ITicketAftercareRepository.cs`
- [+] `be_da/Application/Interfaces/Repositories/ITicketEmailRepository.cs`
- [+] `be_da/Application/Interfaces/Repositories/ITicketSupportRepository.cs`
- [+] `be_da/Application/Interfaces/Services/IAccountService.cs`
- [+] `be_da/Application/Interfaces/Services/ICommunityService.cs`
- [+] `be_da/Application/Interfaces/Services/IGoodsPaymentService.cs`
- [+] `be_da/Application/Interfaces/Services/IGoogleSignIn.cs`
- [+] `be_da/Application/Interfaces/Services/ILateEntryService.cs`
- [+] `be_da/Application/Interfaces/Services/INotificationService.cs`
- [+] `be_da/Application/Interfaces/Services/IPlayerChatbot.cs`
- [+] `be_da/Application/Interfaces/Services/IPlayerSkillResultService.cs`
- [+] `be_da/Application/Interfaces/Services/IProfileService.cs`
- [+] `be_da/Application/Interfaces/Services/IRevenueService.cs`
- [+] `be_da/Application/Interfaces/Services/ISessionService.cs`
- [+] `be_da/Application/Interfaces/Services/ISessionSupplyService.cs`
- [+] `be_da/Application/Interfaces/Services/ISkillAssessmentService.cs`
- [+] `be_da/Application/Interfaces/Services/ISkillProfileService.cs`
- [+] `be_da/Application/Interfaces/Services/ISkillReviewService.cs`
- [+] `be_da/Application/Interfaces/Services/ISportCenterService.cs`
- [+] `be_da/Application/Interfaces/Services/IStockService.cs`
- [+] `be_da/Application/Interfaces/Services/IStoreService.cs`
- [+] `be_da/Application/Interfaces/Services/ITicketAftercareService.cs`
- [+] `be_da/Application/Interfaces/Services/ITicketCheckInService.cs`
- [+] `be_da/Application/Interfaces/Services/ITicketEmailService.cs`
- [+] `be_da/Application/Interfaces/Services/ITicketSupportService.cs`
- [+] `be_da/Application/Services/AccountService.cs`
- [+] `be_da/Application/Services/CommunityService.cs`
- [+] `be_da/Application/Services/LateEntryService.cs`
- [+] `be_da/Application/Services/NotificationService.cs`
- [+] `be_da/Application/Services/PlayerSkillResultService.cs`
- [+] `be_da/Application/Services/ProfileService.cs`
- [+] `be_da/Application/Services/RevenueService.cs`
- [+] `be_da/Application/Services/SessionService.cs`
- [+] `be_da/Application/Services/SessionSupplyService.cs`
- [+] `be_da/Application/Services/SkillProfileService.cs`
- [+] `be_da/Application/Services/SportCenterService.cs`
- [+] `be_da/Application/Services/StockService.cs`
- [+] `be_da/Application/Services/StoreService.cs`
- [+] `be_da/Application/Services/TicketEmailService.cs`
- [+] `be_da/Composition.cs`
- [+] `be_da/Domain/Exceptions/AssessmentException.cs`
- [+] `be_da/Domain/Exceptions/GatewayException.cs`
- [+] `be_da/Domain/Exceptions/PaymentException.cs`
- [+] `be_da/Domain/Exceptions/SignInRejectedException.cs`
- [+] `be_da/Infrastructure/ExternalServices/LocalAvatarStorage.cs`
- [+] `be_da/Infrastructure/ExternalServices/SmtpTicketEmailSender.cs`
- [+] `be_da/Infrastructure/Persistence/PersistenceFailureClassifier.cs`
- [+] `be_da/Infrastructure/Persistence/Repositories/GoodsPaymentRepository.cs`
- [+] `be_da/Infrastructure/Persistence/Repositories/NotificationRepository.cs`
- [+] `be_da/Infrastructure/Persistence/Repositories/PlayerSkillResultRepository.cs`
- [+] `be_da/Infrastructure/Persistence/Repositories/RevenueRepository.cs`
- [+] `be_da/Infrastructure/Persistence/Repositories/SkillAssessmentRepository.cs`
- [+] `be_da/Infrastructure/Persistence/Repositories/SkillReviewRepository.cs`
- [+] `be_da/Infrastructure/Persistence/Repositories/SportCenterRepository.cs`
- [+] `be_da/Infrastructure/Persistence/Repositories/StoreRepository.cs`
- [+] `be_da/Infrastructure/Persistence/Repositories/TicketAftercareRepository.cs`
- [+] `be_da/Infrastructure/Persistence/Repositories/TicketEmailRepository.cs`
- [+] `be_da/Infrastructure/Persistence/Repositories/TicketSupportRepository.cs`
- [+] `be_da/Presentation/Authorization/HttpDataAccessContext.cs`
- [+] `be_da/Presentation/Filters/OperationResponse.cs`
- [+] `be_da/Presentation/Middlewares/SportCenterMiddleware.cs`
- [+] `be_da/appsettings.Chatbot.example.json`
- [+] `docs/architecture-refactor.md`
- [+] `tests/TKSC.Api.Tests/ArchitectureBoundaryTests.cs`
- [+] `tests/TKSC.Api.Tests/RepositorySqlTranslationTests.cs`

### [~] Chỉnh sửa tại vị trí cũ

- [~] `.gitignore`
- [~] `PROJECT_CONTEXT.md`
- [~] `be_da/Application/DTOs/PaymentContracts.cs`
- [~] `be_da/Application/DTOs/StoreOrderContracts.cs`
- [~] `be_da/Application/Mappings/PaymentMappings.cs`
- [~] `be_da/Application/Services/GoodsPaymentService.Handover.cs`
- [~] `be_da/Application/Services/GoodsPaymentService.cs`
- [~] `be_da/Application/Services/PaymentService.cs`
- [~] `be_da/Application/Services/SkillAssessmentService.cs`
- [~] `be_da/Application/Services/SkillReviewRules.cs`
- [~] `be_da/Application/Services/SkillReviewService.cs`
- [~] `be_da/Application/Services/TicketAftercareRules.cs`
- [~] `be_da/Application/Services/TicketAftercareService.cs`
- [~] `be_da/Application/Services/TicketCheckInService.cs`
- [~] `be_da/Application/Services/TicketSupportRules.cs`
- [~] `be_da/Application/Services/TicketSupportService.cs`
- [~] `be_da/Application/Validators/PaymentValidator.cs`
- [~] `be_da/Domain/Entities/Ticket.LateEntry.cs`
- [~] `be_da/Domain/Entities/TicketAftercare.cs`
- [~] `be_da/Domain/Entities/TicketSupportMessage.cs`
- [~] `be_da/Infrastructure/DependencyInjection.cs`
- [~] `be_da/README.md`
- [~] `be_da/be_da.csproj`
- [~] `docs/late-entry-sales.md`
- [~] `docs/payment-flow.md`
- [~] `docs/player-chatbot.md`
- [~] `docs/stock-handover-and-supplies.md`
- [~] `docs/stock-units-barcode.md`
- [~] `scripts/duc-fixtures/Program.cs`
- [~] `scripts/duc-round1/Program.cs`
- [~] `tests/PaymentTests/PayOsTests.cs`
- [~] `tests/PaymentTests/PaymentServiceTests.cs`
- [~] `tests/PaymentTests/TicketAftercareTests.cs`
- [~] `tests/PaymentTests/TicketNotificationTests.cs`
- [~] `tests/PaymentTests/TicketSupportTests.cs`
- [~] `tests/TKSC.Api.Tests/BookingContactReplyTests.cs`
- [~] `tests/TKSC.Api.Tests/CenterSelectionTests.cs`
- [~] `tests/TKSC.Api.Tests/ChatIntentTests.cs`
- [~] `tests/TKSC.Api.Tests/CommunityTests.cs`
- [~] `tests/TKSC.Api.Tests/DatabaseReadTests.cs`
- [~] `tests/TKSC.Api.Tests/DucSkillReviewRuleTests.cs`
- [~] `tests/TKSC.Api.Tests/DucTicketSupportTests.cs`
- [~] `tests/TKSC.Api.Tests/DucWorkflowRuleTests.cs`
- [~] `tests/TKSC.Api.Tests/GoodsPaymentTests.cs`
- [~] `tests/TKSC.Api.Tests/GoogleLoginTests.cs`
- [~] `tests/TKSC.Api.Tests/GuestChatbotTests.cs`
- [~] `tests/TKSC.Api.Tests/LateEntryApiTests.cs`
- [~] `tests/TKSC.Api.Tests/LateEntryDatabaseReadTests.cs`
- [~] `tests/TKSC.Api.Tests/MultiCenterDatabaseTests.cs`
- [~] `tests/TKSC.Api.Tests/PlayerChatbotTests.cs`
- [~] `tests/TKSC.Api.Tests/ProfileTests.cs`
- [~] `tests/TKSC.Api.Tests/SessionSupplyTests.cs`
- [~] `tests/TKSC.Api.Tests/SessionTests.cs`
- [~] `tests/TKSC.Api.Tests/SkillAssessmentTests.cs`
- [~] `tests/TKSC.Api.Tests/SkillExceptionVisibilityDatabaseTests.cs`
- [~] `tests/TKSC.Api.Tests/SkillProfileTests.cs`
- [~] `tests/TKSC.Api.Tests/SportCenterDatabaseTests.cs`
- [~] `tests/TKSC.Api.Tests/SportCenterTests.cs`
- [~] `tests/TKSC.Api.Tests/SqlPlayerChatbotTests.cs`
- [~] `tests/TKSC.Api.Tests/Stock/GoodsHandoverUnitTests.cs`
- [~] `tests/TKSC.Api.Tests/Stock/SessionSupplyUnitTests.cs`
- [~] `tests/TKSC.Api.Tests/Stock/StockApiValidationTests.cs`
- [~] `tests/TKSC.Api.Tests/Stock/StockCatalogUnitTests.cs`
- [~] `tests/TKSC.Api.Tests/Stock/StockReceiptUnitTests.cs`
- [~] `tests/TKSC.Api.Tests/Stock/UnitConversionUnitTests.cs`
- [~] `tests/TKSC.Api.Tests/StockTests.cs`
- [~] `tests/TKSC.Api.Tests/TicketAftercareReadTests.cs`
- [~] `tests/TKSC.Api.Tests/TicketSupportVenueDatabaseTests.cs`

### [-] Bỏ file chứa cũ sau khi chuyển thành phần

- [-] `be_da/Application/Assessments/AssessmentContracts.cs`
- [-] `be_da/Application/Chatbot/ChatbotContracts.cs`
- [-] `be_da/Application/Community/CommunityDtos.cs`
- [-] `be_da/Application/Profiles/UpdateProfileRequest.cs`
- [-] `be_da/Application/Profiles/UpdateSportProfileRequest.cs`
- [-] `be_da/Application/Sessions/LateEntryContracts.cs`
- [-] `be_da/Application/Sessions/SessionDtos.cs`
- [-] `be_da/Application/Skills/SkillDtos.cs`
- [-] `be_da/Domain/Entities/CommunityEntities.cs`
- [-] `be_da/Domain/Entities/SessionEntities.cs`
- [-] `be_da/Domain/Entities/UserProfileDetails.cs`

### Chi tiết tách DTO, interface và exception

- be_da/Application/Accounts/GoogleSignIn.cs (SignInRejectedException) → be_da/Domain/Exceptions/SignInRejectedException.cs
- be_da/Application/Assessments/AssessmentContracts.cs (CriterionView) → be_da/Application/DTOs/CriterionView.cs
- be_da/Application/Assessments/AssessmentContracts.cs (RubricView) → be_da/Application/DTOs/RubricView.cs
- be_da/Application/Assessments/AssessmentContracts.cs (AttendeeView) → be_da/Application/DTOs/AttendeeView.cs
- be_da/Application/Assessments/AssessmentContracts.cs (MarkInput) → be_da/Application/DTOs/MarkInput.cs
- be_da/Application/Assessments/AssessmentContracts.cs (SaveAssessmentRequest) → be_da/Application/DTOs/SaveAssessmentRequest.cs
- be_da/Application/Assessments/AssessmentContracts.cs (PublishAssessmentRequest) → be_da/Application/DTOs/PublishAssessmentRequest.cs
- be_da/Application/Assessments/AssessmentContracts.cs (AssessmentView) → be_da/Application/DTOs/AssessmentView.cs
- be_da/Application/Assessments/AssessmentContracts.cs (AssessmentEvidence) → be_da/Application/DTOs/AssessmentEvidence.cs
- be_da/Application/Assessments/AssessmentContracts.cs (AssessmentException) → be_da/Domain/Exceptions/AssessmentException.cs
- be_da/Application/Chatbot/ChatbotContracts.cs (ChatQuestion) → be_da/Application/DTOs/ChatQuestion.cs
- be_da/Application/Chatbot/ChatbotContracts.cs (ChatContext) → be_da/Application/DTOs/ChatContext.cs
- be_da/Application/Chatbot/ChatbotContracts.cs (KnowledgeArticle) → be_da/Application/DTOs/KnowledgeArticle.cs
- be_da/Application/Chatbot/ChatbotContracts.cs (ChatSource) → be_da/Application/DTOs/ChatSource.cs
- be_da/Application/Chatbot/ChatbotContracts.cs (ChatAnswer) → be_da/Application/DTOs/ChatAnswer.cs
- be_da/Application/Chatbot/ChatbotContracts.cs (BookingLink) → be_da/Application/DTOs/BookingLink.cs
- be_da/Application/Chatbot/ChatbotContracts.cs (BookingContact) → be_da/Application/DTOs/BookingContact.cs
- be_da/Application/Chatbot/ChatbotContracts.cs (GeneratedAnswer) → be_da/Application/DTOs/GeneratedAnswer.cs
- be_da/Application/Chatbot/ChatbotContracts.cs (IChatAnswerGenerator) → be_da/Application/Interfaces/External/IChatAnswerGenerator.cs
- be_da/Application/Chatbot/ChatbotContracts.cs (ChatbotOptions) → be_da/Application/DTOs/ChatbotOptions.cs
- be_da/Application/Chatbot/ChatbotContracts.cs (ChatLookup) → be_da/Application/DTOs/ChatLookup.cs
- be_da/Application/Chatbot/ChatbotContracts.cs (ChatIntent) → be_da/Application/DTOs/ChatIntent.cs
- be_da/Application/Chatbot/ChatbotContracts.cs (IChatIntentInterpreter) → be_da/Application/Interfaces/External/IChatIntentInterpreter.cs
- be_da/Application/Chatbot/ChatbotContracts.cs (ChatData) → be_da/Application/DTOs/ChatData.cs
- be_da/Application/Chatbot/ChatbotContracts.cs (IPlayerChatData) → be_da/Application/Interfaces/Repositories/IPlayerChatData.cs
- be_da/Application/Community/CommunityDtos.cs (SendFriendRequest) → be_da/Application/DTOs/SendFriendRequest.cs
- be_da/Application/Community/CommunityDtos.cs (RespondFriendRequest) → be_da/Application/DTOs/RespondFriendRequest.cs
- be_da/Application/Community/CommunityDtos.cs (NotifyChatMessageRequest) → be_da/Application/DTOs/NotifyChatMessageRequest.cs
- be_da/Application/Community/CommunityDtos.cs (RegisterFcmTokenRequest) → be_da/Application/DTOs/RegisterFcmTokenRequest.cs
- be_da/Application/Community/CommunityDtos.cs (FriendshipActionResult) → be_da/Application/DTOs/FriendshipActionResult.cs
- be_da/Application/Community/CommunityDtos.cs (SendMessageRequest) → be_da/Application/DTOs/SendMessageRequest.cs
- be_da/Application/Community/CommunityDtos.cs (ChatMessageItem) → be_da/Application/DTOs/ChatMessageItem.cs
- be_da/Application/DTOs/PaymentContracts.cs (PaymentException) → be_da/Domain/Exceptions/PaymentException.cs
- be_da/Application/DTOs/PaymentContracts.cs (GatewayException) → be_da/Domain/Exceptions/GatewayException.cs
- be_da/Application/Profiles/UpdateProfileRequest.cs (UpdateProfileRequest) → be_da/Application/DTOs/UpdateProfileRequest.cs
- be_da/Application/Profiles/UpdateSportProfileRequest.cs (UpdateSportProfileRequest) → be_da/Application/DTOs/UpdateSportProfileRequest.cs
- be_da/Application/Profiles/UpdateSportProfileRequest.cs (SportProfileResult) → be_da/Application/DTOs/SportProfileResult.cs
- be_da/Application/Services/GoodsPaymentService.cs (GoodsLine) → be_da/Application/DTOs/GoodsLine.cs
- be_da/Application/Services/GoodsPaymentService.cs (GoodsCheckoutSnapshot) → be_da/Application/DTOs/GoodsCheckoutSnapshot.cs
- be_da/Application/Services/GoodsPaymentService.cs (GoodsInvoice) → be_da/Application/DTOs/GoodsInvoice.cs
- be_da/Application/Services/GoodsPaymentService.cs (GoodsOrderView) → be_da/Application/DTOs/GoodsOrderView.cs
- be_da/Application/Services/GoodsPaymentService.Handover.cs (BuyerGoodsOrder) → be_da/Application/DTOs/BuyerGoodsOrder.cs
- be_da/Application/Services/GoodsPaymentService.Handover.cs (GoodsStockLine) → be_da/Application/DTOs/GoodsStockLine.cs
- be_da/Application/Services/SkillReviewService.cs (ReviewInput) → be_da/Application/DTOs/ReviewInput.cs
- be_da/Application/Services/SkillReviewService.cs (ExceptionDecision) → be_da/Application/DTOs/ExceptionDecision.cs
- be_da/Application/Services/SkillReviewService.cs (AppealDecision) → be_da/Application/DTOs/AppealDecision.cs
- be_da/Application/Services/SkillReviewService.cs (ReviewEvent) → be_da/Application/DTOs/ReviewEvent.cs
- be_da/Application/Sessions/LateEntryContracts.cs (LateEntryOffer) → be_da/Application/DTOs/LateEntryOffer.cs
- be_da/Application/Sessions/LateEntryContracts.cs (HoldLateEntryRequest) → be_da/Application/DTOs/HoldLateEntryRequest.cs
- be_da/Application/Sessions/SessionDtos.cs (SessionFilter) → be_da/Application/DTOs/SessionFilter.cs
- be_da/Application/Sessions/SessionDtos.cs (RegisterSessionResult) → be_da/Application/DTOs/RegisterSessionResult.cs
- be_da/Application/Skills/SkillDtos.cs (RubricDimensionDto) → be_da/Application/DTOs/RubricDimensionDto.cs
- be_da/Application/Skills/SkillDtos.cs (SportRubricDto) → be_da/Application/DTOs/SportRubricDto.cs
- be_da/Application/Skills/SkillDtos.cs (AssessmentDimensionScoreDto) → be_da/Application/DTOs/AssessmentDimensionScoreDto.cs
- be_da/Application/Skills/SkillDtos.cs (AssessmentResultDto) → be_da/Application/DTOs/AssessmentResultDto.cs
- be_da/Application/Skills/SkillDtos.cs (RatingHistoryItemDto) → be_da/Application/DTOs/RatingHistoryItemDto.cs
- be_da/Application/Skills/SkillDtos.cs (PlayerSportSkillProfileDto) → be_da/Application/DTOs/PlayerSportSkillProfileDto.cs
- be_da/Application/Skills/SkillDtos.cs (PlayerSkillOverviewDto) → be_da/Application/DTOs/PlayerSkillOverviewDto.cs
- be_da/Application/Skills/SkillDtos.cs (SurveyDimensionAnswerInput) → be_da/Application/DTOs/SurveyDimensionAnswerInput.cs
- be_da/Application/Skills/SkillDtos.cs (SubmitSkillSurveyRequest) → be_da/Application/DTOs/SubmitSkillSurveyRequest.cs
- be_da/Application/Skills/SkillDtos.cs (SubmitSkillSurveyResult) → be_da/Application/DTOs/SubmitSkillSurveyResult.cs
- be_da/Application/Stock/SessionSupplyDtos.cs (SupplySession) → be_da/Application/DTOs/SupplySession.cs
- be_da/Application/Stock/SessionSupplyDtos.cs (SupplyRecipient) → be_da/Application/DTOs/SupplyRecipient.cs
- be_da/Application/Stock/SessionSupplyDtos.cs (SupplyAllocation) → be_da/Application/DTOs/SupplyAllocation.cs
- be_da/Application/Stock/SessionSupplyDtos.cs (SessionSupplyDetails) → be_da/Application/DTOs/SessionSupplyDetails.cs
- be_da/Application/Stock/SessionSupplyDtos.cs (IssueSupplyRequest) → be_da/Application/DTOs/IssueSupplyRequest.cs
- be_da/Application/Stock/SessionSupplyDtos.cs (ReturnSupplyRequest) → be_da/Application/DTOs/ReturnSupplyRequest.cs
- be_da/Application/Stock/SessionSupplyDtos.cs (ReportSupplyUseRequest) → be_da/Application/DTOs/ReportSupplyUseRequest.cs
- be_da/Application/Stock/SessionSupplyDtos.cs (SupplyResult) → be_da/Application/DTOs/SupplyResult.cs
- be_da/Application/Stock/StockDtos.cs (StockProduct) → be_da/Application/DTOs/StockProduct.cs
- be_da/Application/Stock/StockDtos.cs (StockMovementEntry) → be_da/Application/DTOs/StockMovementEntry.cs
- be_da/Application/Stock/StockDtos.cs (StockProductFilter) → be_da/Application/DTOs/StockProductFilter.cs
- be_da/Application/Stock/StockDtos.cs (CreateProductRequest) → be_da/Application/DTOs/CreateProductRequest.cs
- be_da/Application/Stock/StockDtos.cs (UpdateProductRequest) → be_da/Application/DTOs/UpdateProductRequest.cs
- be_da/Application/Stock/StockDtos.cs (StockMovementRequest) → be_da/Application/DTOs/StockMovementRequest.cs
- be_da/Application/Stock/StockDtos.cs (StockResult) → be_da/Application/DTOs/StockResult.cs
- be_da/Application/Stock/StockReceiptRules.cs (BatchReceiptLine) → be_da/Application/DTOs/BatchReceiptLine.cs
- be_da/Application/Stock/StockReceiptRules.cs (BatchReceiptRequest) → be_da/Application/DTOs/BatchReceiptRequest.cs
- be_da/Application/Stock/StockReceiptRules.cs (ReceiptCatalogItem) → be_da/Application/DTOs/ReceiptCatalogItem.cs
- be_da/Application/Stock/StockReceiptRules.cs (ResolvedReceiptLine) → be_da/Application/DTOs/ResolvedReceiptLine.cs
- be_da/Application/Stock/StockReceiptRules.cs (MergedReceiptLine) → be_da/Application/DTOs/MergedReceiptLine.cs
- be_da/Application/Stock/StockReceiptRules.cs (BatchLineError) → be_da/Application/DTOs/BatchLineError.cs
- be_da/Application/Stock/StockReceiptRules.cs (BatchReceiptResult) → be_da/Application/DTOs/BatchReceiptResult.cs
- be_da/Application/Stock/StockUnitRules.cs (ProductUnit) → be_da/Application/DTOs/ProductUnit.cs
- be_da/Application/Stock/StockUnitRules.cs (ProductUnitInput) → be_da/Application/DTOs/ProductUnitInput.cs
- be_da/Application/Stock/StockUnitRules.cs (StockCodeLookup) → be_da/Application/DTOs/StockCodeLookup.cs
- be_da/Domain/Entities/CommunityEntities.cs (FriendshipRecord) → be_da/Application/DTOs/FriendshipRecord.cs
- be_da/Domain/Entities/CommunityEntities.cs (MemberSearchResult) → be_da/Application/DTOs/MemberSearchResult.cs
- be_da/Domain/Entities/CommunityEntities.cs (FriendItem) → be_da/Application/DTOs/FriendItem.cs
- be_da/Domain/Entities/CommunityEntities.cs (PendingFriendRequestItem) → be_da/Application/DTOs/PendingFriendRequestItem.cs
- be_da/Domain/Entities/CommunityEntities.cs (FriendSuggestionItem) → be_da/Application/DTOs/FriendSuggestionItem.cs
- be_da/Domain/Entities/SessionEntities.cs (SessionSummary) → be_da/Application/DTOs/SessionSummary.cs
- be_da/Domain/Entities/SessionEntities.cs (VenueSummary) → be_da/Application/DTOs/VenueSummary.cs
- be_da/Domain/Entities/SessionEntities.cs (SessionParticipant) → be_da/Application/DTOs/SessionParticipant.cs
- be_da/Domain/Entities/SessionEntities.cs (SessionDetails) → be_da/Application/DTOs/SessionDetails.cs
- be_da/Domain/Entities/SessionEntities.cs (TimeSlotSummary) → be_da/Application/DTOs/TimeSlotSummary.cs
- be_da/Domain/Entities/SessionEntities.cs (PlayerSportProfileInfo) → be_da/Application/DTOs/PlayerSportProfileInfo.cs
- be_da/Domain/Entities/UserProfileDetails.cs (UserProfileDetails) → be_da/Application/DTOs/UserProfileDetails.cs
- be_da/Domain/Entities/UserProfileDetails.cs (SportProfileItem) → be_da/Application/DTOs/SportProfileItem.cs
- be_da/Domain/Entities/UserProfileDetails.cs (PublicRatingHistoryItem) → be_da/Application/DTOs/PublicRatingHistoryItem.cs
- be_da/Domain/Entities/UserProfileDetails.cs (PublicPlayerProfileDetails) → be_da/Application/DTOs/PublicPlayerProfileDetails.cs

