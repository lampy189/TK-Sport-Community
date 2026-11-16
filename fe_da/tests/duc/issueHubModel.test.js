import test from 'node:test';
import assert from 'node:assert/strict';
import { buildIssues } from '../../src/pages/payment/issueHubModel.js';
test('buildIssues: empty source produces no incident',()=>assert.deepEqual(buildIssues({}),[]));
test('buildIssues: only disrupted sessions enter hub',()=>{
 const rows=buildIssues({sessions:[{id:1,status:'Open'},{id:2,status:'suspended'},{id:3,status:'CANCELLED'}]});
 assert.deepEqual(rows.map(r=>r.key),['sessions-2','sessions-3']);
});
test('buildIssues: same numeric IDs across sources remain unique and retain original records',()=>{
 const inbox={id:1,ticketId:41,holder:'Player',lastSender:'Payer',lastMessage:'Sân ngập',lastIsStaff:false};
 const rows=buildIssues({inbox:[inbox],reviews:[{id:1,amountVnd:100}],refunds:[{id:1,amountVnd:100}]});
 assert.equal(new Set(rows.map(r=>r.key)).size,3);assert.equal(rows[0].item,inbox);
 assert.match(rows[0].title,/Payer/);assert.equal(rows[0].status,'Chờ phản hồi');
});
test('buildIssues: staff response changes queue label without marking refund completed',()=>{
 const row=buildIssues({inbox:[{id:2,ticketId:4,holder:'Player',lastIsStaff:true}]})[0];
 assert.equal(row.status,'Đã phản hồi');assert.equal(row.summary,'Ảnh đính kèm');
});
