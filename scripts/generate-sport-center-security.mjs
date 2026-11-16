// Reproducible SQL Server predicates for both EF and direct SqlClient repositories.
import fs from 'node:fs';
const bypass = "CONVERT(nvarchar(30), SESSION_CONTEXT(N'tksc_role')) IN (N'Owner',N'owner',N'Player',N'player',N'public',N'service')";
let sql = `-- Upgrade 011: server-owned connection context, row-level filters and write guards.\nSET XACT_ABORT ON;\nIF DB_NAME() <> N'Sep490' THROW 50000,'Select Sep490.',1;\nGO\n`;
const functions = [];
function fn(name, parameters, body) {
  functions.push(name);
  const definition = `CREATE FUNCTION dbo.sc_${name}(${parameters}) RETURNS TABLE WITH SCHEMABINDING AS RETURN SELECT 1 AS allowed WHERE ${bypass} OR (${body});`;
  sql += `IF OBJECT_ID(N'dbo.sc_${name}',N'IF') IS NULL EXEC(N'${definition.replaceAll("'", "''")}');\nGO\n`;
}
fn('venue', '@venue bigint', "@venue = TRY_CONVERT(bigint,SESSION_CONTEXT(N'tksc_venue')) AND CONVERT(nvarchar(30),SESSION_CONTEXT(N'tksc_role')) IN (N'Staff',N'staff',N'Manager',N'manager',N'Referee',N'referee')");
fn('court','@id bigint','EXISTS(SELECT 1 FROM dbo.courts p CROSS APPLY dbo.sc_venue(p.venue_id) a WHERE p.id=@id)');
fn('session','@id bigint','EXISTS(SELECT 1 FROM dbo.sessions p CROSS APPLY dbo.sc_court(p.court_id) a WHERE p.id=@id)');
fn('ticket','@id bigint','EXISTS(SELECT 1 FROM dbo.tickets p CROSS APPLY dbo.sc_session(p.session_id) a WHERE p.id=@id)');
fn('social','@id bigint',"EXISTS(SELECT 1 FROM dbo.tickets p CROSS APPLY dbo.sc_session(p.session_id) a WHERE p.order_id=@id)");
fn('goods','@id bigint','EXISTS(SELECT 1 FROM dbo.orders p CROSS APPLY dbo.sc_venue(p.venue_id) a WHERE p.id=@id)');
fn('payment_source','@social bigint,@goods bigint','(@social IS NOT NULL AND EXISTS(SELECT 1 FROM dbo.sc_social(@social))) OR (@goods IS NOT NULL AND EXISTS(SELECT 1 FROM dbo.sc_goods(@goods)))');
fn('payment','@id bigint','EXISTS(SELECT 1 FROM dbo.payments p CROSS APPLY dbo.sc_payment_source(p.order_id,p.goods_order_id) a WHERE p.id=@id)');
fn('assessment','@id bigint','EXISTS(SELECT 1 FROM dbo.assessments p CROSS APPLY dbo.sc_session(p.session_id) a WHERE p.id=@id)');
fn('stock','@id bigint','EXISTS(SELECT 1 FROM dbo.stock_items p CROSS APPLY dbo.sc_venue(p.venue_id) a WHERE p.id=@id)');
fn('shift','@id bigint','EXISTS(SELECT 1 FROM dbo.shift_reconciliations p CROSS APPLY dbo.sc_venue(p.venue_id) a WHERE p.id=@id)');
fn('player','@id bigint','EXISTS(SELECT 1 FROM dbo.tickets p CROSS APPLY dbo.sc_session(p.session_id) a WHERE p.player_id=@id)');
fn('profile','@id bigint','EXISTS(SELECT 1 FROM dbo.player_sport_profiles p CROSS APPLY dbo.sc_player(p.player_id) a WHERE p.id=@id)');
// Functions are first installed on an empty policy; on upgrades drop policy in a maintenance window first.
const rules = [
 ['venues','venue','id'], ['courts','venue','venue_id'], ['sessions','court','court_id'],
 ['orders','venue','venue_id'], ['order_items','goods','order_id'],
 ['social_booking_orders','social','id',false], ['tickets','session','session_id'],
 ['payments','payment_source','order_id,goods_order_id'], ['bank_transactions','payment','matched_payment_id'],
 ['refunds','payment','payment_id'], ['revenue_entries','venue','venue_id'],
 ['stock_items','venue','venue_id'], ['stock_movements','stock','stock_item_id'], ['stock_counts','stock','stock_item_id'],
 ['shift_reconciliations','venue','venue_id'], ['reconciliation_items','shift','reconciliation_id'],
 ['session_staff','session','session_id'], ['session_referees','session','session_id'], ['session_exceptions','session','session_id'],
 ['assessments','session','session_id'], ['assessment_scores','assessment','assessment_id'], ['rating_appeals','assessment','assessment_id'],
 ['ticket_changes','ticket','ticket_id'], ['ticket_funding','ticket','ticket_id'], ['ticket_support_messages','ticket','ticket_id'],
 ['players','player','id',false], ['player_sport_profiles','player','player_id'], ['player_rating_history','profile','player_sport_profile_id'],
];
sql += "IF NOT EXISTS(SELECT 1 FROM sys.security_policies WHERE name='SportCenterIsolation')\nEXEC(N'CREATE SECURITY POLICY dbo.SportCenterIsolation\n";
sql += rules.flatMap(([table,f,columns,block=true]) => [
 `ADD FILTER PREDICATE dbo.sc_${f}(${columns}) ON dbo.${table}`,
 ...(block ? [`ADD BLOCK PREDICATE dbo.sc_${f}(${columns}) ON dbo.${table} AFTER INSERT`, `ADD BLOCK PREDICATE dbo.sc_${f}(${columns}) ON dbo.${table} AFTER UPDATE`] : [])
]).join(',\n') + "\nWITH (STATE=ON)');\nGO\n";
// A default attaches newly created stock/orders to the authenticated employee's scope.
for (const table of ['orders','stock_items','shift_reconciliations']) {
 sql += `IF NOT EXISTS(SELECT 1 FROM sys.default_constraints WHERE name='DF_${table}_venue') ALTER TABLE dbo.${table} ADD CONSTRAINT DF_${table}_venue DEFAULT TRY_CONVERT(bigint,SESSION_CONTEXT(N'tksc_venue')) FOR venue_id;\nGO\n`;
}
fs.writeFileSync('db/upgrades/011-sport-center-security.sql',sql);
