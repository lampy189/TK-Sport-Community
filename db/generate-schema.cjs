const fs = require('node:fs');
const path = require('node:path');

const outputDir = __dirname;
let erd = fs.readFileSync(path.join(outputDir, 'erd-source.mmd'), 'utf8');

erd = erd.replace(/\n\s*user_roles\s*\{[^}]*\}\s*/s, '\n');
erd = erd.replace(/(users\s*\{\s*BIGINT id PK ""\s*)/, '$1\n\t\tBIGINT role_id FK "one current role"  ');
erd = erd.replace(/(NVARCHAR full_name\s+""\s*)/, '$1\n\t\tNVARCHAR password_hash "local sign-in; nullable for OAuth"  ');
// Match the inspected physical baseline plus approved Google upgrade. Keep one field per line.
erd = erd.replace(/users\s*\{[^}]*\}/, `users {
        BIGINT id PK ""
        BIGINT role_id FK "one current role"
        NVARCHAR email UK ""
        NVARCHAR full_name ""
        NVARCHAR password_hash "nullable; unused by Google login"
        VARCHAR status ""
        DATETIMEOFFSET deleted_at ""
        VARCHAR google_subject UK "stable Google identity; nullable"
    }`);
erd = erd.replace(/\bsales_order_items\b/g, 'order_items');
erd = erd.replace(/\bsales_orders\b/g, 'orders');
erd = erd.replace(/(payments\s*\{[^}]*?)\bBIGINT sales_order_id FK/, '$1BIGINT goods_order_id FK');
erd = erd.replace(/(order_items\s*\{[^}]*?)\bBIGINT sales_order_id FK/, '$1BIGINT order_id FK');
erd = erd.replace(/users\|\|--o\{user_roles:"has"\s*roles\|\|--o\{user_roles:"granted"/, 'roles||--o{users:"assigned"');
erd = erd.replace(/^\s*rubrics\}\|--\|\{Untitled-Entity-10:.*$/m, '');
fs.writeFileSync(path.join(outputDir, 'erd.mmd'), erd.trim() + '\n');

const blocks = [...erd.matchAll(/^\s*([a-z_]+)\s*\{([^}]*)\}/gm)].map((m) => ({name:m[1], body:m[2]}));
const names = new Set(blocks.map(b => b.name));
const fk = {
  users: {role_id:'roles'}, account_audit_logs:{user_id:'users',actor_id:'users'},
  players:{user_id:'users'}, player_sport_profiles:{player_id:'players',sport_id:'sports'},
  player_rating_history:{player_sport_profile_id:'player_sport_profiles',changed_by:'users',assessment_id:'assessments'},
  rubrics:{sport_id:'sports'}, rubric_dimensions:{rubric_id:'rubrics'},
  sessions:{sport_id:'sports',court_id:'courts',time_slot_id:'time_slots',created_by:'users'},
  assessments:{player_sport_profile_id:'player_sport_profiles',session_id:'sessions',rubric_id:'rubrics',assessor_id:'users'},
  assessment_scores:{assessment_id:'assessments',dimension_id:'rubric_dimensions'},
  rating_appeals:{player_sport_profile_id:'player_sport_profiles',submitted_by:'users',assessment_id:'assessments',reviewed_by:'users'},
  courts:{venue_id:'venues',sport_id:'sports'},
  session_referees:{session_id:'sessions',referee_id:'users',assigned_by:'users'},
  session_staff:{session_id:'sessions',staff_id:'users'},
  session_exceptions:{session_id:'sessions',player_id:'players',approved_by:'users'},
  social_booking_orders:{payer_id:'users'},
  tickets:{order_id:'social_booking_orders',session_id:'sessions',player_id:'players',checked_in_by:'users'},
  invitations:{order_id:'social_booking_orders',inviter_id:'players',invitee_id:'players',session_id:'sessions',ticket_id:'tickets'},
  partner_requests:{session_id:'sessions',requester_id:'players',partner_id:'players'},
  orders:{staff_id:'users'},
  payments:{order_id:'social_booking_orders',goods_order_id:'orders',payer_id:'users',confirmed_by:'users'},
  bank_transactions:{matched_payment_id:'payments'},
  refunds:{payment_id:'payments',ticket_id:'tickets',recipient_id:'users',initiated_by:'users',approved_by:'users'},
  order_items:{order_id:'orders',product_id:'products'}, stock_items:{product_id:'products'},
  stock_movements:{stock_item_id:'stock_items',created_by:'users'},
  stock_counts:{stock_item_id:'stock_items',counted_by:'users',approved_by:'users'},
  shift_reconciliations:{staff_id:'users',approved_by:'users'},
  reconciliation_items:{reconciliation_id:'shift_reconciliations'}, policies:{approved_by:'users'},
  approvals:{requested_by:'users',approved_by:'users'},audit_logs:{actor_id:'users'},
  friendships:{requester_id:'users',addressee_id:'users'},
  messages:{sender_id:'users',recipient_id:'users'}, notifications:{user_id:'users'}
};
const nullableFk = new Set([
  'account_audit_logs.actor_id','player_rating_history.assessment_id','assessments.session_id',
  'rating_appeals.assessment_id','rating_appeals.reviewed_by','courts.sport_id',
  'session_exceptions.approved_by','tickets.checked_in_by','invitations.order_id',
  'invitations.ticket_id','payments.order_id','payments.goods_order_id','payments.payer_id',
  'payments.confirmed_by','bank_transactions.matched_payment_id','refunds.approved_by',
  'stock_movements.created_by','stock_counts.approved_by','shift_reconciliations.approved_by',
  'policies.approved_by','approvals.approved_by','audit_logs.actor_id'
]);
const nonNull = new Set([
  'roles.code','roles.name','users.username','users.email','users.full_name','users.status',
  'sports.code','sports.name','rubrics.version','rubrics.name',
  'rubric_dimensions.code','rubric_dimensions.name','sessions.session_date',
  'sessions.capacity','sessions.price_vnd','sessions.status',
  'time_slots.start_time','time_slots.end_time','social_booking_orders.order_code',
  'social_booking_orders.total_amount','social_booking_orders.status',
  'tickets.amount_vnd','tickets.status','orders.order_code','orders.total_amount',
  'orders.status','payments.amount_vnd','payments.status','payments.reference_code',
  'bank_transactions.external_id','bank_transactions.amount_vnd',
  'products.sku','products.name','products.unit_price_vnd',
  'order_items.quantity','order_items.unit_price_vnd','order_items.subtotal_vnd',
  'stock_items.quantity_on_hand','stock_items.reorder_level',
  'stock_movements.quantity','stock_counts.counted_quantity',
  'policies.code','policies.name','friendships.status'
]);
function sqlType(type, col){
  if(type==='BIGINT'||type==='SMALLINT'||type==='INT'||type==='BIT'||type==='DATE') return type;
  if(type==='DATETIMEOFFSET') return 'DATETIMEOFFSET(0)';
  if(type==='TIME') return 'TIME(0)';
  if(type==='NVARCHAR') {
    if(['content','old_value','new_value','value'].includes(col)) return 'NVARCHAR(MAX)';
    if(col==='email') return 'NVARCHAR(320)';
    if(col==='username') return 'NVARCHAR(100)';
    if(col==='address') return 'NVARCHAR(500)';
    return 'NVARCHAR(255)';
  }
  if(type==='VARCHAR') {
    if(col==='avatar_url') return 'VARCHAR(2048)';
    if(col==='google_subject') return 'VARCHAR(255) COLLATE Latin1_General_100_BIN2';
    if(col==='external_id') return 'VARCHAR(200)';
    return 'VARCHAR(100)';
  }
  throw new Error(`Unknown type ${type}`);
}
const quote = s => `[${s}]`;
const lines = [
  '-- TKSC development schema from the supplied Mermaid ERD.',
  '-- Install target: local SQL Server default instance; database Sep490.',
  '-- Social booking orders and goods orders are separate.',
  `IF DB_ID(N'Sep490') IS NULL CREATE DATABASE [Sep490];`,
  'GO','USE [Sep490];','GO',
  'SET ANSI_NULLS ON;',
  'SET ANSI_PADDING ON;',
  'SET ANSI_WARNINGS ON;',
  'SET ARITHABORT ON;',
  'SET CONCAT_NULL_YIELDS_NULL ON;',
  'SET QUOTED_IDENTIFIER ON;',
  'SET NUMERIC_ROUNDABORT OFF;',
  'SET NOCOUNT ON;','GO'
];
const colsByTable={};
for(const b of blocks){
  const cols=[...b.body.matchAll(/^\s*(BIGINT|VARCHAR|NVARCHAR|SMALLINT|INT|BIT|DATE|DATETIMEOFFSET|TIME)\s+([a-z_]+)(?:\s+(PK|FK|UK))?/gm)]
    .map(m=>({type:m[1],name:m[2],tag:m[3]}));
  colsByTable[b.name]=cols;
  if(!cols.length || cols[0].name!=='id') throw new Error(`Bad table ${b.name}`);
  for(const c of cols.filter(c=>c.tag==='FK')){
    if(!fk[b.name]?.[c.name] || !names.has(fk[b.name][c.name])) throw new Error(`Unmapped FK ${b.name}.${c.name}`);
  }
  const defs=cols.map(c=>{
    if(c.name==='id') return '    [id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_'+b.name+'] PRIMARY KEY';
    const key=`${b.name}.${c.name}`;
    const required=(c.tag==='FK'&&!nullableFk.has(key))||nonNull.has(key)||(c.tag==='UK'&&!['users.google_subject','tickets.ticket_code'].includes(key));
    return `    ${quote(c.name)} ${sqlType(c.type,c.name)} ${required?'NOT NULL':'NULL'}`;
  });
  if(b.name==='users') defs.push('    CONSTRAINT [CK_users_role_positive] CHECK ([role_id] > 0)');
  if(b.name==='payments') defs.push('    CONSTRAINT [CK_payments_one_order] CHECK ((CASE WHEN [order_id] IS NULL THEN 0 ELSE 1 END) + (CASE WHEN [goods_order_id] IS NULL THEN 0 ELSE 1 END) = 1)');
  if(b.name==='sessions') defs.push('    CONSTRAINT [CK_sessions_capacity] CHECK ([capacity] BETWEEN 1 AND 8)');
  if(b.name==='player_sport_profiles') defs.push('    CONSTRAINT [CK_profiles_score] CHECK ([current_score] BETWEEN 0 AND 100)');
  if(b.name==='stock_items') defs.push('    CONSTRAINT [CK_stock_nonnegative] CHECK ([quantity_on_hand] >= 0 AND [reorder_level] >= 0)');
  lines.push(`IF OBJECT_ID(N'dbo.${quote(b.name)}', N'U') IS NULL`, `CREATE TABLE dbo.${quote(b.name)} (`, defs.join(',\n'), ');','GO');
}
for(const [table,mapping] of Object.entries(fk)){
  for(const [col,target] of Object.entries(mapping)){
    const cname=`FK_${table}_${col}`;
    lines.push(`IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'${cname}')`,
      `    ALTER TABLE dbo.${quote(table)} ADD CONSTRAINT ${quote(cname)} FOREIGN KEY (${quote(col)}) REFERENCES dbo.${quote(target)}([id]);`, 'GO');
  }
}
for(const b of blocks){
  for(const c of colsByTable[b.name].filter(c=>c.tag==='UK')){
    const index=`UX_${b.name}_${c.name}`;
    lines.push(`IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'${index}' AND object_id = OBJECT_ID(N'dbo.${quote(b.name)}'))`,
      `    CREATE UNIQUE INDEX ${quote(index)} ON dbo.${quote(b.name)}(${quote(c.name)})${['users.google_subject','tickets.ticket_code'].includes(`${b.name}.${c.name}`)?` WHERE ${quote(c.name)} IS NOT NULL`:''};`,'GO');
  }
}
const composite=[
  ['players','user_id'],['player_sport_profiles','player_id,sport_id'],
  ['rubric_dimensions','rubric_id,code'],['stock_items','product_id'],
  ['sessions','court_id,session_date,time_slot_id'],
  ['session_referees','session_id,referee_id'],['session_staff','session_id,staff_id'],
  ['assessment_scores','assessment_id,dimension_id'],['order_items','order_id,product_id']
];
for(const [table,fields] of composite){
  const index=`UX_${table}_${fields.replace(/,/g,'_')}`;
  lines.push(`IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'${index}' AND object_id = OBJECT_ID(N'dbo.${quote(table)}'))`,
    `    CREATE UNIQUE INDEX ${quote(index)} ON dbo.${quote(table)}(${fields.split(',').map(quote).join(',')});`,'GO');
}
lines.push("IF NOT EXISTS (SELECT 1 FROM dbo.[roles] WHERE [code] = 'Player')",
  "INSERT INTO dbo.[roles] ([code],[name]) VALUES ('Player',N'Player');",'GO');
for(const role of ['Referee','Staff','Manager','Owner','SystemAdmin']){
  lines.push(`IF NOT EXISTS (SELECT 1 FROM dbo.[roles] WHERE [code] = '${role}')`,
    `INSERT INTO dbo.[roles] ([code],[name]) VALUES ('${role}',N'${role}');`,'GO');
}
for(const [code,label] of [['badminton','Badminton'],['pickleball','Pickleball']]){
  lines.push(`IF NOT EXISTS (SELECT 1 FROM dbo.[sports] WHERE [code] = '${code}')`,
    `INSERT INTO dbo.[sports] ([code],[name]) VALUES ('${code}',N'${label}');`,'GO');
}
fs.writeFileSync(path.join(outputDir,'schema.sql'),lines.join('\n')+'\n');
console.log(`Wrote ${blocks.length} tables, ${Object.values(fk).reduce((n,m)=>n+Object.keys(m).length,0)} foreign keys`);
