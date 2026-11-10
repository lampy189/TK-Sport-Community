-- Upgrade 011: server-owned connection context, row-level filters and write guards.
SET XACT_ABORT ON;
IF DB_NAME() <> N'Sep490' THROW 50000,'Select Sep490.',1;
GO
IF OBJECT_ID(N'dbo.sc_venue',N'IF') IS NULL EXEC(N'CREATE FUNCTION dbo.sc_venue(@venue bigint) RETURNS TABLE WITH SCHEMABINDING AS RETURN SELECT 1 AS allowed WHERE CONVERT(nvarchar(30), SESSION_CONTEXT(N''tksc_role'')) IN (N''Owner'',N''owner'',N''Player'',N''player'',N''public'',N''service'') OR (@venue = TRY_CONVERT(bigint,SESSION_CONTEXT(N''tksc_venue'')) AND CONVERT(nvarchar(30),SESSION_CONTEXT(N''tksc_role'')) IN (N''Staff'',N''staff'',N''Manager'',N''manager'',N''Referee'',N''referee''));');
GO
IF OBJECT_ID(N'dbo.sc_court',N'IF') IS NULL EXEC(N'CREATE FUNCTION dbo.sc_court(@id bigint) RETURNS TABLE WITH SCHEMABINDING AS RETURN SELECT 1 AS allowed WHERE CONVERT(nvarchar(30), SESSION_CONTEXT(N''tksc_role'')) IN (N''Owner'',N''owner'',N''Player'',N''player'',N''public'',N''service'') OR (EXISTS(SELECT 1 FROM dbo.courts p CROSS APPLY dbo.sc_venue(p.venue_id) a WHERE p.id=@id));');
GO
IF OBJECT_ID(N'dbo.sc_session',N'IF') IS NULL EXEC(N'CREATE FUNCTION dbo.sc_session(@id bigint) RETURNS TABLE WITH SCHEMABINDING AS RETURN SELECT 1 AS allowed WHERE CONVERT(nvarchar(30), SESSION_CONTEXT(N''tksc_role'')) IN (N''Owner'',N''owner'',N''Player'',N''player'',N''public'',N''service'') OR (EXISTS(SELECT 1 FROM dbo.sessions p CROSS APPLY dbo.sc_court(p.court_id) a WHERE p.id=@id));');
GO
IF OBJECT_ID(N'dbo.sc_ticket',N'IF') IS NULL EXEC(N'CREATE FUNCTION dbo.sc_ticket(@id bigint) RETURNS TABLE WITH SCHEMABINDING AS RETURN SELECT 1 AS allowed WHERE CONVERT(nvarchar(30), SESSION_CONTEXT(N''tksc_role'')) IN (N''Owner'',N''owner'',N''Player'',N''player'',N''public'',N''service'') OR (EXISTS(SELECT 1 FROM dbo.tickets p CROSS APPLY dbo.sc_session(p.session_id) a WHERE p.id=@id));');
GO
IF OBJECT_ID(N'dbo.sc_social',N'IF') IS NULL EXEC(N'CREATE FUNCTION dbo.sc_social(@id bigint) RETURNS TABLE WITH SCHEMABINDING AS RETURN SELECT 1 AS allowed WHERE CONVERT(nvarchar(30), SESSION_CONTEXT(N''tksc_role'')) IN (N''Owner'',N''owner'',N''Player'',N''player'',N''public'',N''service'') OR (EXISTS(SELECT 1 FROM dbo.tickets p CROSS APPLY dbo.sc_session(p.session_id) a WHERE p.order_id=@id));');
GO
IF OBJECT_ID(N'dbo.sc_goods',N'IF') IS NULL EXEC(N'CREATE FUNCTION dbo.sc_goods(@id bigint) RETURNS TABLE WITH SCHEMABINDING AS RETURN SELECT 1 AS allowed WHERE CONVERT(nvarchar(30), SESSION_CONTEXT(N''tksc_role'')) IN (N''Owner'',N''owner'',N''Player'',N''player'',N''public'',N''service'') OR (EXISTS(SELECT 1 FROM dbo.orders p CROSS APPLY dbo.sc_venue(p.venue_id) a WHERE p.id=@id));');
GO
IF OBJECT_ID(N'dbo.sc_payment_source',N'IF') IS NULL EXEC(N'CREATE FUNCTION dbo.sc_payment_source(@social bigint,@goods bigint) RETURNS TABLE WITH SCHEMABINDING AS RETURN SELECT 1 AS allowed WHERE CONVERT(nvarchar(30), SESSION_CONTEXT(N''tksc_role'')) IN (N''Owner'',N''owner'',N''Player'',N''player'',N''public'',N''service'') OR ((@social IS NOT NULL AND EXISTS(SELECT 1 FROM dbo.sc_social(@social))) OR (@goods IS NOT NULL AND EXISTS(SELECT 1 FROM dbo.sc_goods(@goods))));');
GO
IF OBJECT_ID(N'dbo.sc_payment',N'IF') IS NULL EXEC(N'CREATE FUNCTION dbo.sc_payment(@id bigint) RETURNS TABLE WITH SCHEMABINDING AS RETURN SELECT 1 AS allowed WHERE CONVERT(nvarchar(30), SESSION_CONTEXT(N''tksc_role'')) IN (N''Owner'',N''owner'',N''Player'',N''player'',N''public'',N''service'') OR (EXISTS(SELECT 1 FROM dbo.payments p CROSS APPLY dbo.sc_payment_source(p.order_id,p.goods_order_id) a WHERE p.id=@id));');
GO
IF OBJECT_ID(N'dbo.sc_assessment',N'IF') IS NULL EXEC(N'CREATE FUNCTION dbo.sc_assessment(@id bigint) RETURNS TABLE WITH SCHEMABINDING AS RETURN SELECT 1 AS allowed WHERE CONVERT(nvarchar(30), SESSION_CONTEXT(N''tksc_role'')) IN (N''Owner'',N''owner'',N''Player'',N''player'',N''public'',N''service'') OR (EXISTS(SELECT 1 FROM dbo.assessments p CROSS APPLY dbo.sc_session(p.session_id) a WHERE p.id=@id));');
GO
IF OBJECT_ID(N'dbo.sc_stock',N'IF') IS NULL EXEC(N'CREATE FUNCTION dbo.sc_stock(@id bigint) RETURNS TABLE WITH SCHEMABINDING AS RETURN SELECT 1 AS allowed WHERE CONVERT(nvarchar(30), SESSION_CONTEXT(N''tksc_role'')) IN (N''Owner'',N''owner'',N''Player'',N''player'',N''public'',N''service'') OR (EXISTS(SELECT 1 FROM dbo.stock_items p CROSS APPLY dbo.sc_venue(p.venue_id) a WHERE p.id=@id));');
GO
IF OBJECT_ID(N'dbo.sc_shift',N'IF') IS NULL EXEC(N'CREATE FUNCTION dbo.sc_shift(@id bigint) RETURNS TABLE WITH SCHEMABINDING AS RETURN SELECT 1 AS allowed WHERE CONVERT(nvarchar(30), SESSION_CONTEXT(N''tksc_role'')) IN (N''Owner'',N''owner'',N''Player'',N''player'',N''public'',N''service'') OR (EXISTS(SELECT 1 FROM dbo.shift_reconciliations p CROSS APPLY dbo.sc_venue(p.venue_id) a WHERE p.id=@id));');
GO
IF OBJECT_ID(N'dbo.sc_player',N'IF') IS NULL EXEC(N'CREATE FUNCTION dbo.sc_player(@id bigint) RETURNS TABLE WITH SCHEMABINDING AS RETURN SELECT 1 AS allowed WHERE CONVERT(nvarchar(30), SESSION_CONTEXT(N''tksc_role'')) IN (N''Owner'',N''owner'',N''Player'',N''player'',N''public'',N''service'') OR (EXISTS(SELECT 1 FROM dbo.tickets p CROSS APPLY dbo.sc_session(p.session_id) a WHERE p.player_id=@id));');
GO
IF OBJECT_ID(N'dbo.sc_profile',N'IF') IS NULL EXEC(N'CREATE FUNCTION dbo.sc_profile(@id bigint) RETURNS TABLE WITH SCHEMABINDING AS RETURN SELECT 1 AS allowed WHERE CONVERT(nvarchar(30), SESSION_CONTEXT(N''tksc_role'')) IN (N''Owner'',N''owner'',N''Player'',N''player'',N''public'',N''service'') OR (EXISTS(SELECT 1 FROM dbo.player_sport_profiles p CROSS APPLY dbo.sc_player(p.player_id) a WHERE p.id=@id));');
GO
IF NOT EXISTS(SELECT 1 FROM sys.security_policies WHERE name='SportCenterIsolation')
EXEC(N'CREATE SECURITY POLICY dbo.SportCenterIsolation
ADD FILTER PREDICATE dbo.sc_venue(id) ON dbo.venues,
ADD BLOCK PREDICATE dbo.sc_venue(id) ON dbo.venues AFTER INSERT,
ADD BLOCK PREDICATE dbo.sc_venue(id) ON dbo.venues AFTER UPDATE,
ADD FILTER PREDICATE dbo.sc_venue(venue_id) ON dbo.courts,
ADD BLOCK PREDICATE dbo.sc_venue(venue_id) ON dbo.courts AFTER INSERT,
ADD BLOCK PREDICATE dbo.sc_venue(venue_id) ON dbo.courts AFTER UPDATE,
ADD FILTER PREDICATE dbo.sc_court(court_id) ON dbo.sessions,
ADD BLOCK PREDICATE dbo.sc_court(court_id) ON dbo.sessions AFTER INSERT,
ADD BLOCK PREDICATE dbo.sc_court(court_id) ON dbo.sessions AFTER UPDATE,
ADD FILTER PREDICATE dbo.sc_venue(venue_id) ON dbo.orders,
ADD BLOCK PREDICATE dbo.sc_venue(venue_id) ON dbo.orders AFTER INSERT,
ADD BLOCK PREDICATE dbo.sc_venue(venue_id) ON dbo.orders AFTER UPDATE,
ADD FILTER PREDICATE dbo.sc_goods(order_id) ON dbo.order_items,
ADD BLOCK PREDICATE dbo.sc_goods(order_id) ON dbo.order_items AFTER INSERT,
ADD BLOCK PREDICATE dbo.sc_goods(order_id) ON dbo.order_items AFTER UPDATE,
ADD FILTER PREDICATE dbo.sc_social(id) ON dbo.social_booking_orders,
ADD FILTER PREDICATE dbo.sc_session(session_id) ON dbo.tickets,
ADD BLOCK PREDICATE dbo.sc_session(session_id) ON dbo.tickets AFTER INSERT,
ADD BLOCK PREDICATE dbo.sc_session(session_id) ON dbo.tickets AFTER UPDATE,
ADD FILTER PREDICATE dbo.sc_payment_source(order_id,goods_order_id) ON dbo.payments,
ADD BLOCK PREDICATE dbo.sc_payment_source(order_id,goods_order_id) ON dbo.payments AFTER INSERT,
ADD BLOCK PREDICATE dbo.sc_payment_source(order_id,goods_order_id) ON dbo.payments AFTER UPDATE,
ADD FILTER PREDICATE dbo.sc_payment(matched_payment_id) ON dbo.bank_transactions,
ADD BLOCK PREDICATE dbo.sc_payment(matched_payment_id) ON dbo.bank_transactions AFTER INSERT,
ADD BLOCK PREDICATE dbo.sc_payment(matched_payment_id) ON dbo.bank_transactions AFTER UPDATE,
ADD FILTER PREDICATE dbo.sc_payment(payment_id) ON dbo.refunds,
ADD BLOCK PREDICATE dbo.sc_payment(payment_id) ON dbo.refunds AFTER INSERT,
ADD BLOCK PREDICATE dbo.sc_payment(payment_id) ON dbo.refunds AFTER UPDATE,
ADD FILTER PREDICATE dbo.sc_venue(venue_id) ON dbo.revenue_entries,
ADD BLOCK PREDICATE dbo.sc_venue(venue_id) ON dbo.revenue_entries AFTER INSERT,
ADD BLOCK PREDICATE dbo.sc_venue(venue_id) ON dbo.revenue_entries AFTER UPDATE,
ADD FILTER PREDICATE dbo.sc_venue(venue_id) ON dbo.stock_items,
ADD BLOCK PREDICATE dbo.sc_venue(venue_id) ON dbo.stock_items AFTER INSERT,
ADD BLOCK PREDICATE dbo.sc_venue(venue_id) ON dbo.stock_items AFTER UPDATE,
ADD FILTER PREDICATE dbo.sc_stock(stock_item_id) ON dbo.stock_movements,
ADD BLOCK PREDICATE dbo.sc_stock(stock_item_id) ON dbo.stock_movements AFTER INSERT,
ADD BLOCK PREDICATE dbo.sc_stock(stock_item_id) ON dbo.stock_movements AFTER UPDATE,
ADD FILTER PREDICATE dbo.sc_stock(stock_item_id) ON dbo.stock_counts,
ADD BLOCK PREDICATE dbo.sc_stock(stock_item_id) ON dbo.stock_counts AFTER INSERT,
ADD BLOCK PREDICATE dbo.sc_stock(stock_item_id) ON dbo.stock_counts AFTER UPDATE,
ADD FILTER PREDICATE dbo.sc_venue(venue_id) ON dbo.shift_reconciliations,
ADD BLOCK PREDICATE dbo.sc_venue(venue_id) ON dbo.shift_reconciliations AFTER INSERT,
ADD BLOCK PREDICATE dbo.sc_venue(venue_id) ON dbo.shift_reconciliations AFTER UPDATE,
ADD FILTER PREDICATE dbo.sc_shift(reconciliation_id) ON dbo.reconciliation_items,
ADD BLOCK PREDICATE dbo.sc_shift(reconciliation_id) ON dbo.reconciliation_items AFTER INSERT,
ADD BLOCK PREDICATE dbo.sc_shift(reconciliation_id) ON dbo.reconciliation_items AFTER UPDATE,
ADD FILTER PREDICATE dbo.sc_session(session_id) ON dbo.session_staff,
ADD BLOCK PREDICATE dbo.sc_session(session_id) ON dbo.session_staff AFTER INSERT,
ADD BLOCK PREDICATE dbo.sc_session(session_id) ON dbo.session_staff AFTER UPDATE,
ADD FILTER PREDICATE dbo.sc_session(session_id) ON dbo.session_referees,
ADD BLOCK PREDICATE dbo.sc_session(session_id) ON dbo.session_referees AFTER INSERT,
ADD BLOCK PREDICATE dbo.sc_session(session_id) ON dbo.session_referees AFTER UPDATE,
ADD FILTER PREDICATE dbo.sc_session(session_id) ON dbo.session_exceptions,
ADD BLOCK PREDICATE dbo.sc_session(session_id) ON dbo.session_exceptions AFTER INSERT,
ADD BLOCK PREDICATE dbo.sc_session(session_id) ON dbo.session_exceptions AFTER UPDATE,
ADD FILTER PREDICATE dbo.sc_session(session_id) ON dbo.assessments,
ADD BLOCK PREDICATE dbo.sc_session(session_id) ON dbo.assessments AFTER INSERT,
ADD BLOCK PREDICATE dbo.sc_session(session_id) ON dbo.assessments AFTER UPDATE,
ADD FILTER PREDICATE dbo.sc_assessment(assessment_id) ON dbo.assessment_scores,
ADD BLOCK PREDICATE dbo.sc_assessment(assessment_id) ON dbo.assessment_scores AFTER INSERT,
ADD BLOCK PREDICATE dbo.sc_assessment(assessment_id) ON dbo.assessment_scores AFTER UPDATE,
ADD FILTER PREDICATE dbo.sc_assessment(assessment_id) ON dbo.rating_appeals,
ADD BLOCK PREDICATE dbo.sc_assessment(assessment_id) ON dbo.rating_appeals AFTER INSERT,
ADD BLOCK PREDICATE dbo.sc_assessment(assessment_id) ON dbo.rating_appeals AFTER UPDATE,
ADD FILTER PREDICATE dbo.sc_ticket(ticket_id) ON dbo.ticket_changes,
ADD BLOCK PREDICATE dbo.sc_ticket(ticket_id) ON dbo.ticket_changes AFTER INSERT,
ADD BLOCK PREDICATE dbo.sc_ticket(ticket_id) ON dbo.ticket_changes AFTER UPDATE,
ADD FILTER PREDICATE dbo.sc_ticket(ticket_id) ON dbo.ticket_funding,
ADD BLOCK PREDICATE dbo.sc_ticket(ticket_id) ON dbo.ticket_funding AFTER INSERT,
ADD BLOCK PREDICATE dbo.sc_ticket(ticket_id) ON dbo.ticket_funding AFTER UPDATE,
ADD FILTER PREDICATE dbo.sc_ticket(ticket_id) ON dbo.ticket_support_messages,
ADD BLOCK PREDICATE dbo.sc_ticket(ticket_id) ON dbo.ticket_support_messages AFTER INSERT,
ADD BLOCK PREDICATE dbo.sc_ticket(ticket_id) ON dbo.ticket_support_messages AFTER UPDATE,
ADD FILTER PREDICATE dbo.sc_player(id) ON dbo.players,
ADD FILTER PREDICATE dbo.sc_player(player_id) ON dbo.player_sport_profiles,
ADD BLOCK PREDICATE dbo.sc_player(player_id) ON dbo.player_sport_profiles AFTER INSERT,
ADD BLOCK PREDICATE dbo.sc_player(player_id) ON dbo.player_sport_profiles AFTER UPDATE,
ADD FILTER PREDICATE dbo.sc_profile(player_sport_profile_id) ON dbo.player_rating_history,
ADD BLOCK PREDICATE dbo.sc_profile(player_sport_profile_id) ON dbo.player_rating_history AFTER INSERT,
ADD BLOCK PREDICATE dbo.sc_profile(player_sport_profile_id) ON dbo.player_rating_history AFTER UPDATE
WITH (STATE=ON)');
GO
IF NOT EXISTS(SELECT 1 FROM sys.default_constraints WHERE name='DF_orders_venue') ALTER TABLE dbo.orders ADD CONSTRAINT DF_orders_venue DEFAULT TRY_CONVERT(bigint,SESSION_CONTEXT(N'tksc_venue')) FOR venue_id;
GO
IF NOT EXISTS(SELECT 1 FROM sys.default_constraints WHERE name='DF_stock_items_venue') ALTER TABLE dbo.stock_items ADD CONSTRAINT DF_stock_items_venue DEFAULT TRY_CONVERT(bigint,SESSION_CONTEXT(N'tksc_venue')) FOR venue_id;
GO
IF NOT EXISTS(SELECT 1 FROM sys.default_constraints WHERE name='DF_shift_reconciliations_venue') ALTER TABLE dbo.shift_reconciliations ADD CONSTRAINT DF_shift_reconciliations_venue DEFAULT TRY_CONVERT(bigint,SESSION_CONTEXT(N'tksc_venue')) FOR venue_id;
GO
