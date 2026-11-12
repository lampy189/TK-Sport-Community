import fs from 'node:fs';
import { transactions } from '../fe_da/src/pages/revenue/demoData.js';
const q=s=>"N'"+s.replaceAll("'","''")+"'";
let sql=`-- Synthetic business documents in the real local database. No gateway/email calls.\nSET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON; SET XACT_ABORT ON; SET NOCOUNT ON;\nIF DB_NAME()<>N'Sep490' THROW 50000,'Select Sep490.',1;\nEXEC sys.sp_set_session_context @key=N'tksc_role',@value=N'service';\nBEGIN TRANSACTION;\nDECLARE @venue bigint,@staff bigint,@payer bigint,@player bigint,@court bigint,@session bigint,@order bigint,@payment bigint,@ticket bigint,@product bigint,@refund bigint;\nDECLARE @sport bigint=(SELECT TOP(1) id FROM dbo.sports WHERE code='badminton');\nDECLARE @slot bigint=(SELECT MIN(id) FROM dbo.time_slots);\nIF @sport IS NULL OR @slot IS NULL THROW 50000,'Sports and time slots are required.',1;\n`;
for(const [index,name] of ['Thủ Đức','Bình Thạnh','Quận 7'].entries()){
 const code='SC0'+(index+1); const venueName='[DEMO-SC] '+name;
 sql+=`IF NOT EXISTS(SELECT 1 FROM dbo.venues WHERE name=${q(venueName)}) INSERT dbo.venues(name,is_active) VALUES(${q(venueName)},1);
SELECT @venue=id FROM dbo.venues WHERE name=${q(venueName)};
`;
 for(const role of ['Staff','Manager','Referee','Player',...(index===0?['Owner']:[])]){
  const email=`demo-${role.toLowerCase()}-${code.toLowerCase()}@example.invalid`;
  sql+=`IF NOT EXISTS(SELECT 1 FROM dbo.users WHERE email=${q(email)}) INSERT dbo.users(role_id,email,full_name,status,venue_id) SELECT id,${q(email)},${q('[DEMO] '+role+' '+name)},'Active',${['Owner','Player'].includes(role)?'NULL':'@venue'} FROM dbo.roles WHERE code=${q(role)};
`;
 }
 sql+=`SELECT @payer=id FROM dbo.users WHERE email=${q(`demo-player-${code.toLowerCase()}@example.invalid`)};
IF NOT EXISTS(SELECT 1 FROM dbo.players WHERE user_id=@payer) INSERT dbo.players(user_id) VALUES(@payer);
SELECT @player=id FROM dbo.players WHERE user_id=@payer;
SELECT @staff=id FROM dbo.users WHERE email=${q(`demo-staff-${code.toLowerCase()}@example.invalid`)};
IF NOT EXISTS(SELECT 1 FROM dbo.courts WHERE venue_id=@venue AND name='DEMO-SC-Court') INSERT dbo.courts(venue_id,name,sport_id,is_active) VALUES(@venue,'DEMO-SC-Court',@sport,1);
SELECT @court=id FROM dbo.courts WHERE venue_id=@venue AND name='DEMO-SC-Court';
IF NOT EXISTS(SELECT 1 FROM dbo.products WHERE sku=${q('DEMO-SC-'+code)}) INSERT dbo.products(sku,name,category,unit_price_vnd,is_active,venue_id) VALUES(${q('DEMO-SC-'+code)},${q('[DEMO] Combo cầu và nước '+name)},'Demo',150000,1,@venue);
SELECT @product=id FROM dbo.products WHERE sku=${q('DEMO-SC-'+code)};
IF NOT EXISTS(SELECT 1 FROM dbo.stock_items WHERE product_id=@product) INSERT dbo.stock_items(product_id,quantity_on_hand,reorder_level,venue_id) VALUES(@product,100,10,@venue);
`;
 for(const row of transactions.filter(t=>t.centerId===code&&t.kind!=='refund')){
  const ref='DEMO-SC-'+row.id;
  sql+=`IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=${q(ref)}) BEGIN
`;
  if(row.kind==='social'){
   // One independent court for each tournament receipt avoids time-slot collisions.
   if(row.eventId) sql+=`INSERT dbo.courts(venue_id,name,sport_id,is_active) VALUES(@venue,${q(ref)},@sport,1); SET @court=SCOPE_IDENTITY();\n`;
   sql+=`IF NOT EXISTS(SELECT 1 FROM dbo.sessions WHERE court_id=@court AND session_date='${row.occurredAt.slice(0,10)}' AND time_slot_id=@slot)
INSERT dbo.sessions(sport_id,court_id,time_slot_id,session_date,capacity,price_vnd,status,created_by) VALUES(@sport,@court,@slot,'${row.occurredAt.slice(0,10)}',8,${row.amount},'Completed',@staff);
SELECT @session=id FROM dbo.sessions WHERE court_id=@court AND session_date='${row.occurredAt.slice(0,10)}' AND time_slot_id=@slot;
INSERT dbo.social_booking_orders(order_code,payer_id,total_amount,status) VALUES(${q(ref)},@payer,${row.amount},'${row.status==='confirmed'?'paid':'pending'}'); SET @order=SCOPE_IDENTITY();
INSERT dbo.tickets(order_id,session_id,player_id,ticket_code,status,amount_vnd) VALUES(@order,@session,@player,${q(ref)},'${row.status==='confirmed'?'confirmed':'reserved'}',${row.amount});
INSERT dbo.payments(order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,${row.amount},'cash','${row.status==='confirmed'?'paid':'pending'}',${q(ref)},@staff); SET @payment=SCOPE_IDENTITY();
`;
   if(row.eventId) sql+=`SELECT @court=id FROM dbo.courts WHERE venue_id=@venue AND name='DEMO-SC-Court';\n`;
  }else{
   sql+=`INSERT dbo.orders(order_code,staff_id,customer_name,total_amount,status,payment_state,handover_state,venue_id) VALUES(${q(ref)},@staff,N'[DEMO] Khách mẫu',${row.amount},'Paid','Paid','Pending',@venue); SET @order=SCOPE_IDENTITY();
INSERT dbo.order_items(order_id,product_id,quantity,unit_price_vnd,subtotal_vnd) VALUES(@order,@product,1,${row.amount},${row.amount});
INSERT dbo.payments(goods_order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,${row.amount},'cash','paid',${q(ref)},@staff); SET @payment=SCOPE_IDENTITY();
`;
  }
  sql+=`UPDATE dbo.revenue_entries SET recognized_at='${row.occurredAt}',note=${q('[DEMO] '+row.note)},event_code=${row.eventId?q(row.eventId):'NULL'} WHERE payment_id=@payment AND refund_id IS NULL;
END;
`;
 }
 for(const row of transactions.filter(t=>t.centerId===code&&t.kind==='refund')){
  sql+=`SELECT @payment=p.id,@ticket=t.id FROM dbo.payments p JOIN dbo.tickets t ON t.order_id=p.order_id WHERE p.reference_code=${q('DEMO-SC-'+row.sourceId)};
IF NOT EXISTS(SELECT 1 FROM dbo.refunds WHERE payment_id=@payment AND ticket_id=@ticket AND initiated_by=@staff) BEGIN
INSERT dbo.refunds(payment_id,ticket_id,recipient_id,amount_vnd,status,initiated_by) VALUES(@payment,@ticket,@payer,${row.amount},'${row.status}',@staff); SET @refund=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='${row.occurredAt}',note=${q('[DEMO] '+row.note)} WHERE refund_id=@refund;
END;
`;
 }
}
sql+=`COMMIT;\nSELECT v.name,e.entry_type,SUM(e.amount_vnd) total_vnd FROM dbo.revenue_entries e JOIN dbo.venues v ON v.id=e.venue_id WHERE v.name LIKE '[[]DEMO-SC]%' GROUP BY v.name,e.entry_type ORDER BY v.name,e.entry_type;\nEXEC sys.sp_set_session_context @key=N'tksc_role',@value=NULL;\n`;
fs.writeFileSync('db/seed-sport-center-demo.sql',sql);
