# TK Sport Community — Project Context

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
