# TK Sport Community — Project Context

## Player and operations implementation (12 Nov 2026)

The user explicitly assigned Nhat's Player invitation/payer UX, Player journey/profile/community/notifications, venue/court/personnel management, approved account access and technical handover, plus an operations overview and trial feedback. Frontend changes for this scope are authorized. Implementation remains on `nhat-refactor-20261112` in the existing refactored single project. See `docs/nhat-player-operations.md` for behavior, requirement mapping, validation and material limits. On 14 Nov 2026 the user explicitly approved applying these database changes. Upgrade 017, the revenue triggers from 012 and the existing isolation policy with 014 compatibility are now applied to `Sep490_Nhat_20261112` after a verified backup and restore rehearsal. See `docs/db-update-20261114.md`. Production setup and real external integrations remain subsequent work; the local database backup/restore rehearsal is complete.

## Single-project architecture refactor (11 Nov 2026)

The user explicitly chose Clean Architecture by folders within the existing .NET 8 `be_da.csproj`, not multiple application projects. HTTP code is under `Presentation`; `Application` owns services, interfaces and DTOs; `Domain` owns entities/core rules; persistence and external implementations are under `Infrastructure`. `Program.cs` and `appsettings*.json` are at the project root. Keep existing SQL/EF transactions, API contracts and historical migrations. See `docs/architecture-refactor.md` for the file map, validation results and launch instructions.

## Current local source and database (12 Nov 2026)

The user requested preparing the latest refactored source and the supplied new database for upcoming work, allowing a new Nhat branch. The active Visual Studio checkout remains `C:\Users\shogu\Documents\ChatGPT\TK-Sport-Community`; active branch is `nhat-refactor-20261112`, based directly on `origin/main` commit `739c90d`. The prior local `NhatLT` branch remains unchanged. Do not merge the old implementation into this new baseline automatically.

Local SQL Server `localhost` now has `Sep490_Nhat_20261112`, imported from `C:\Users\shogu\Downloads\dbdoan.sql` (dump timestamp 11 Nov 2026 23:47:43; SHA256 `AD7F9B1A09C1AE08C362A75D4D3086579B9981809D5A7CE895A755C92C8070C7`). Only the database name and database creation file locations were adapted for a separate local import using SQL Server default directories. The existing `Sep490` database was not changed. The new baseline has 48 tables and 104 foreign keys; DBCC CHECKCONSTRAINTS completed without violations. The imported snapshot had SportCenterIsolation disabled; the explicitly approved 14 Nov update has now enabled it with 014 compatibility. Do not toggle it or run historical seed/rollout upgrades automatically. The imported venue IDs differ from earlier notes: 1 = Vinh Phuc, 11 = Ha Noi. Query the new database rather than relying on historic IDs, schema snapshots, demo data or old feature decisions below when they conflict with this baseline.

Existing local configuration was copied from `be_da/Controllers/` to the refactored project root; ignored `be_da/appsettings.Local.json` sets `ConnectionStrings:Sep490` to this new database with Windows Authentication. No credentials or database rows were added to Git. Continue using `be_da/be_da.slnx` and the Visual Studio `https` profile. Backend and frontend builds passed; API tests: 628 passed / 9 SQL integration tests skipped; payment tests: 67 passed. Development smoke checks returned HTTP 200 for Swagger and `/api/auth/centers` against the new database. The smoke-test backend was stopped; real Google OAuth, payments and email were not exercised.

## Precise slider scoring (10 Nov 2026)

The user requested retaining detailed slider points rather than rounding each criterion to 0–4 first. For new saves, each of four integer 0–10 observations contributes 0.5 points to its 20-point criterion. Sum all five precise criterion contributions and round once at the final 0–100 total (half up) to match existing integer profile storage. Level 0–4 is descriptive only. Save structured SliderScores in the existing assessment audit payload and validate/recalculate them at the server; player result contributions use those same values. Existing published assessments without structured slider data retain their original level-based calculations and history. No schema change is needed. Old drafts with recorded sliders adopt the precise calculation when explicitly saved.

## Optional assessment comments (10 Nov 2026)

The user explicitly made referee comments optional, including scores 0 and 4. Do not require evidence text to publish. Keep complete criterion scores, assigned-referee and checked-in attendance checks, review confirmation and rating history unchanged. Remove mandatory-comment wording and evidence checklist gates from the referee UI.

## Schedule and late-entry UI clarification (10 Nov 2026)

The user requested removing center selectors and redundant center labels from player session browsing and detail screens: use the center selected at login and enforced by the server. Keep only one late-entry ticket category, "Vé giờ lẻ", for the remaining duration of an ongoing social session. A session lasting 60 minutes must not create a second player-facing ticket category. Preserve the existing per-minute pricing formula, maximum 50% discount and expiry at the original session end. Personal profiles/history remain shared as previously approved.

## Login correction (10 Nov 2026)

The user explicitly requested restoring the existing Google OAuth login controls, including the Email entry button that enters the provider flow. Removing OAuth controls was an assistant misunderstanding, not a user decision. Preserve the backend Google OAuth integration and the selected-center parameter. TKSC must not add its own password field; password or change-password screens shown by accounts.google.com are controlled by the identity provider. Keep Development email login as the existing separate fallback, not as a replacement for OAuth.

## UI wording preference (10 Nov 2026)

The user prefers short functional labels and minimal copy. Use "Chọn cơ sở" rather than explanatory dropdown prompts; remove promotional introductions and redundant instructions next to controls. Keep actionable errors and necessary transaction details such as price and ticket expiry.

## Approved multi-center rollout (10 Nov 2026)

The user approved multi-center login and then clarified that ALL pre-existing business data belongs to Vinh Phuc. Venue 2 is TKSC Vĩnh Phúc; all nine legacy courts, 129 legacy sessions, old goods/stock/orders and existing operational personnel belong there. Venue 1 is the new TKSC Hà Nội branch, populated with explicitly requested demo courts/sessions/catalog/stock and three passwordless Development-only personnel accounts. Owners retain global authority. Players choose a browsing center per signed session and may change it; profiles, friends and personal ticket/order histories remain global and ownership-protected. New bookings/products are restricted to the selected center at the server; existing orders retain their original center. See docs/multi-center-login.md, upgrades 015/016 and db/seed-hanoi-demo.sql. This supersedes the earlier temporary standalone late-entry mode and one-venue assumption below.

## Approved assessment update (3 Nov 2026)

The user approved referee scoring for Pickleball using the existing Profile internal scale: `PK-IPTPA-DUPR-V1`, five sport-specific criteria at 0–4 and 20% each, total 0–100 and internal B1–B5 bands. These are not official DUPR ratings. This explicitly supersedes the earlier restriction against this internal scale for Pickleball. New badminton forms use `BM-CLB-V1`; deactivate the old pilot rubric for new forms while retaining all existing forms and history. Upgrade 009 records the approved active-flag changes. Legacy pilot forms remain readable and scoreable under their original rubric.

## Source priority
- RP1: project purpose, venue workflow, scope and operating assumptions.
- RP2: project plan, stack and timeline.
- RP3: detailed requirements and approved scope update (23 Sep 2026). For diagrams, use the image/layout in the user-provided RP3 copy as the visual reference; extracted text can contain template residue. Cross-check against the clean Report 3 file in the shared Drive folder.
- RP3 supplies a conceptual data model, not the final physical SQL schema. Check RP4 (Software Design Specification) before finalizing table structure/migrations if available.

## Product
TK Sport Community (TKSC), for TK Sport, is a web application for badminton and pickleball social sessions. It aims to reduce skill mismatches and manual session booking/payment work. Both sports share workflows but must keep separate player sport profiles and assessment histories. The scope also includes venue counter sales, stock, session supplies, refunds/returns and shift reconciliation.

## Stack and delivery context (RP2)
- Frontend: ReactJS.
- Backend: C# / .NET.
- Database: SQL Server.
- Source control: Git, repository on GitHub.
- Deployment target: AWS.
- Coding/design tool is written as “VMcode” in RP2; likely a typo for VS Code, verify before relying on it.
- One venue/operator. Desktop and mobile browsers.

## Venue assumptions (RP1/RP2)
- About 20–30 participants per day; community about 100 members.
- Seven days/week; proposed slots 16:00–18:00, 18:00–20:00, 20:00–22:00.
- Two courts and two referees per slot; max eight participants per court.
- Badminton and pickleball.
- Proposed pilot badminton bands B1–B5 and rubric are recommendations, not confirmed existing rules. Pickleball assessment must remain separate; do not assume badminton rubric applies.
- A one-to-two-week venue trial is planned for 26 Nov–9 Dec 2026 (per RP1/RP2).

## Roles and authority (RP1/RP3)
Seven human actors: Guest (unauthenticated), Player, Referee, Staff, Manager, Owner, System Admin. Authenticated accounts each have one role; a username never grants a role. Owner is the venue owner. Trần Minh Đức provides initial System Admin support; Lâm operates as Manager. System Admin handles technical access/settings and handover, but cannot change ratings or approve business adjustments. Manager makes final disputed-rating decisions after referee review and handles operational exceptions within approved authority. Owner approves policies/prices and Manager appointments. Staff handles sales, stock, payments/refunds and reconciliation. Separate accounts for separate roles/personas must preserve history and must not permit self-approval.

## Main workflows and constraints (RP3)
- Players register, maintain profiles, take sport-specific surveys and receive provisional skill levels until on-court assessment.
- Referees record evidence/attendance and publish supported assessments. Rating disputes are decided by Manager after referee review; retain rating history.
- Manager publishes sessions under Owner-approved policies. Enforce court/referee conflicts, capacity, sport, time and skill eligibility.
- Individual registrations/tickets, friend invitations, consent, eligibility checks and payments are tracked separately. An invitation is not a ticket. Expired unpaid places release separately.
- Social booking orders contain participation charges only. Goods use separate Sales Orders. Unique payment references; one bank transaction allocation cannot be reused across social and goods orders. Incoming payment confirmation does not mean an outgoing refund completed.
- A rating change can invalidate only future affected tickets and trigger refund handling; it must not rewrite past paid prices or unrelated tickets/orders.
- Stock movements are traceable/append-only; sales handover, session supplies, returns, counts and approved adjustments must be recorded without duplicate posting.
- Keep private assessment notes, payment/contact data, and private messages access-controlled. Audit important account, role, rating, payment, stock and approval changes.
- External services in RP3: Google OAuth, bank/payment integration, Cloudinary, Firebase messaging/push, and email service. Payment webhooks require signature/authentication.

## Conceptual data model (RP3)
Core entities include accounts/roles/people/appointments/audit; sports, player sport profiles, versioned rubrics, assessments, score evidence and rating reviews/history; venue/courts/time slots/sessions/assignments/attendance; registrations/tickets/invitations/exceptions; social booking orders/items, payments, bank transactions/allocations/refunds; friendships/messages/partner requests; owner policies/prices; products/sales orders/items/allocations/handover; stock items/movements/counts/adjustments/session-supply allocations; shift reconciliation; and support handover. RP3 describes these as business entities, with IDs/states/links proposed for detailed design.

## Database/codebase guidance
Use SQL Server Developer on this workstation; the installed default instance is MSSQLSERVER, so connect to `localhost` with Windows Authentication. The database name proposed in setup is `Sep490`. Do not generate a guessed schema just from the conceptual ERD; first check RP4 and agree on initial implementation scope, then create EF Core migrations and seed only safe development data.

## Local database baseline (5 Oct 2026)
The user supplied a physical Mermaid ERD and requested creation of the missing tables. The local `Sep490` database now exists with 40 tables and 79 foreign keys from `db/schema.sql`. The corrected diagram is `db/erd.mmd`; the pasted original is `db/erd-source.mmd`. Per the user's direction, `sales_orders` is now `orders` and `sales_order_items` is `order_items`, while social participation continues in `social_booking_orders`. Each `users` row has one required `role_id` FK; there is no `user_roles` table. `payments.goods_order_id` targets `orders`, and `payments.order_id` targets `social_booking_orders`. The schema generator and SQL file are in `db/`. This baseline should be compared with RP4 before building application migrations or claiming the physical model is final.

## Current backend decision (6 Oct 2026)
The user chose to implement in the existing GitHub `be_da` project on branch `NhatLT`, based on `main`, retaining .NET 8. The earlier local four-project .NET 10 scaffold is not the implementation target on this branch. Application, Domain and Infrastructure are folders inside be_da; see be_da/README.md.

The requested authentication flow is Google sign-in only. First sign-in creates a Player account and player profile; later sign-ins resolve the stable Google subject, not email alone. Existing role and account status remain authoritative. This user decision supersedes the SRS local-username/password flow for this implementation. Google OAuth configuration must be supplied locally before real sign-in can be tested.

The user approved adding nullable users.google_subject VARCHAR(255) and a unique filtered index; upgrade 001 records that additive change. Database changes beyond this require another specific approval. No EF Core or automatic startup migration is used. The baseline ERD has been aligned with actual users columns; the historical erd-source remains unchanged.
