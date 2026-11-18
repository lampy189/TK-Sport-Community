import { chromium, expect as baseExpect } from '@playwright/test';
import { readFile,writeFile,mkdir } from 'node:fs/promises';
import path from 'node:path';
const expect=baseExpect.configure({timeout:20000});
const dir=path.resolve('../docs/testing/duc/round1'),base='http://127.0.0.1:5173',m=JSON.parse(await readFile(dir+'/manifest.json','utf8'));
const accounts=Object.fromEntries(m.accounts.map(a=>[+a.name.slice(-2),a])),sessions=Object.fromEntries(m.sessions.map(s=>[s.key,s])),tickets=Object.fromEntries(m.tickets.map(t=>[+t.account.slice(-2),t]));
const browser=await chromium.launch({channel:'chrome',headless:true}),clients=new Map(),results=[],state={};
let nextAuth=0,trace=[];await mkdir(dir+'/evidence',{recursive:true});
const esc=s=>String(s??'').replaceAll('&','&amp;').replaceAll('<','&lt;').replaceAll('>','&gt;');
async function pace(){const wait=Math.max(0,nextAuth-Date.now());nextAuth=Math.max(nextAuth,Date.now())+2400;if(wait)await new Promise(r=>setTimeout(r,wait));}
async function c(n){
 if(clients.has(n))return clients.get(n);
 const context=await browser.newContext({baseURL:base,viewport:{width:1440,height:1000},ignoreHTTPSErrors:true});
 await context.route('**/api/auth/**',async route=>{await pace();await route.continue();});
 await pace();let r=await context.request.get('/api/auth/csrf');expect(r.status()).toBe(200);let token=(await r.json()).token;
 await pace();r=await context.request.post('/api/auth/dev-login',{data:{email:accounts[n].email,portal:accounts[n].role==='Player'?'player':'staff'},headers:{'X-CSRF-TOKEN':token}});expect(r.status()).toBe(200);
 await pace();r=await context.request.get('/api/auth/csrf');expect(r.status()).toBe(200);token=(await r.json()).token;
 const client={context,token,page:await context.newPage(),role:accounts[n].role};client.page.setDefaultTimeout(15000);clients.set(n,client);return client;
}
async function api(n,p,b,status=200){
 const client=await c(n),r=await client.context.request.fetch(p,{method:b===undefined?'GET':'POST',data:b,headers:{'X-CSRF-TOKEN':client.token,'X-TKSC-Request':'1'}});
 const raw=await r.text();let data;try{data=JSON.parse(raw);}catch{data=raw;}
 trace.push({actor:accounts[n].name,method:b===undefined?'GET':'POST',path:p,input:b,status:r.status(),response:data});
 expect(r.status(),p+' '+raw.slice(0,250)).toBe(status);return data;
}
async function staff(n,module){const x=await c(n);await x.page.goto('/'+x.role.toLowerCase()+'/home');await x.page.locator('.site-nav').getByRole('button',{name:module,exact:true}).click();return x.page;}
async function playerTickets(n){const x=await c(n);await x.page.goto('/player/home?view=tickets');await expect(x.page.locator('.ticket-toolbar')).toBeVisible();return x.page;}
async function review(n,kind,subject){const page=await staff(n,kind==='exceptions'?'Ngoại lệ trình độ':'Khiếu nại đánh giá'),card=page.locator('.detail-card').filter({hasText:accounts[subject].name});await expect(card).toHaveCount(1);return {page,card};}
async function shot(r,page){const file=r.id+'-ui.png';await page.screenshot({path:dir+'/evidence/'+file,fullPage:true});r.evidence.push('evidence/'+file);}
async function ticket(n,id=tickets[n]?.ticketId){return (await api(n,'/api/v1/commerce/payment/tickets')).find(t=>t.id===id);}
async function notify(n,type,id){const d=await api(n,'/api/notifications');expect(d.items.some(i=>i.referenceType===type&&i.referenceId===id)).toBe(true);}
async function run(id,module,name,expected,n,fn,requires=[]){
 const r={id,module,description:name,expected,pre:'Real Development app; isolated SQL Server fixtures '+m.prefix+'; active '+accounts[n].role+' account '+accounts[n].name,procedure:'Authenticate in Development with the stated test role. Execute the requests/UI actions listed in the evidence trace. Verify HTTP status, persisted state and the stated expected outcome.',round1:'Pending',round2:'Pending',round3:'Pending',tester:'Tran Minh Duc',date:new Date().toISOString(),evidence:[]};trace=[];
 const blocked=requires.find(id=>results.find(x=>x.id===id)?.round1!=='Passed');
 if(blocked){r.actual='Blocked by failed prerequisite '+blocked;results.push(r);return;}
 try{r.actual=await fn(r)||'All stated assertions passed against the real application and SQL-backed API.';r.round1='Passed';}
 catch(e){r.actual=e.stack||String(e);r.round1='Failed';r.defect='DUC-R1-'+id.replace('INT-DUC-','');}
 r.trace=structuredClone(trace);
 const html='<!doctype html><meta charset="utf-8"><style>body{font:16px system-ui;margin:32px;color:#173a30}h1{font-size:24px}pre{white-space:pre-wrap;background:#eef4f1;padding:16px;font:13px monospace}p{line-height:1.6}</style><h1>'+esc(id+' — '+name)+'</h1><p>ROUND 1 · '+esc(r.date)+' · Real local app and SQL Server. Synthetic payments only, no real bank transfer.</p><h2>'+r.round1+'</h2><p>Expected: '+esc(expected)+'</p><p>Actual: '+esc(r.actual)+'</p><pre>'+esc(JSON.stringify(r.trace,null,2).slice(0,18000))+'</pre>';
 await writeFile(dir+'/evidence/'+id+'-api.html',html);const p=await browser.newPage({viewport:{width:1400,height:1000}});await p.setContent(html);await p.screenshot({path:dir+'/evidence/'+id+'-api.png',fullPage:true});await p.close();r.evidence.push('evidence/'+id+'-api.png');results.push(r);
 await writeFile(dir+'/results.json',JSON.stringify({build:process.env.DUC_BUILD,base,round:1,fixture:m.prefix,results},null,2));
 console.log(r.round1+' '+id+' '+name);if(r.round1==='Failed')console.log(r.actual.slice(0,900));
}
const issue=(id,name,exp,n,fn,deps=[])=>run('INT-DUC-ISS-'+id,'Duc_IssuesPayments',name,exp,n,fn,deps);
const tkt=(id,name,exp,n,fn,deps=[])=>run('INT-DUC-TKT-'+id,'Duc_TicketExchangeRefund',name,exp,n,fn,deps);
const exc=(id,name,exp,n,fn,deps=[])=>run('INT-DUC-EXC-'+id,'Duc_SkillExceptions',name,exp,n,fn,deps);
const apl=(id,name,exp,n,fn,deps=[])=>run('INT-DUC-APL-'+id,'Duc_RatingAppeals',name,exp,n,fn,deps);
try{
await issue('001','List flagged payment and exact source transaction','Review list contains payment and transaction; ticket is not yet confirmed.',2,async r=>{
 const row=(await api(2,'/api/ticket-aftercare/reviews')).find(x=>x.id===m.flagged.paymentId);expect(row.transactions.some(x=>x.id===m.flagged.bankId)).toBe(true);expect((await ticket(12,m.flagged.ticketId)).status).not.toBe('confirmed');
 const p=await staff(2,'Thanh toán');await expect(p.getByText(m.prefix+'-FLAG',{exact:true})).toBeVisible();await shot(r,p);
});
await issue('002','Approval requires transaction, evidence and verification','Approval button is disabled while required inputs are missing.',2,async r=>{
 const p=await staff(2,'Thanh toán');await expect(p.locator('.payment-card').filter({hasText:m.prefix+'-FLAG'}).getByRole('button',{name:'Duyệt và kích hoạt vé'})).toBeDisabled();await shot(r,p);
});
await issue('003','Wrong transaction does not activate a ticket','HTTP 409; ticket remains unconfirmed.',2,async()=>{
 await api(2,'/api/ticket-aftercare/reviews/'+m.flagged.paymentId+'/approve',{bankTransactionId:-1,evidence:'ROUND 1 wrong transaction negative case'},409);expect((await ticket(12,m.flagged.ticketId)).status).not.toBe('confirmed');
});
await issue('004','Approve flagged payment and activate the ticket','Staff approval sets payment/order paid and the ticket confirmed with QR.',2,async r=>{
 const p=await staff(2,'Thanh toán'),card=p.locator('.payment-card').filter({hasText:m.prefix+'-FLAG'});
 await card.getByLabel('Giao dịch cần đối soát').selectOption(String(m.flagged.bankId));await card.getByLabel('Nội dung đối soát / bằng chứng').fill('ROUND 1 SYNTHETIC reconciliation, exact transaction and amount; no real transfer.');await card.getByRole('checkbox').check();await card.getByRole('button',{name:'Duyệt và kích hoạt vé'}).click();await expect(p.getByRole('status')).toContainText('Đã xử lý thành công');
 const t=await ticket(12,m.flagged.ticketId);expect(t.status).toBe('confirmed');expect(t.ticketCode).toBeTruthy();await shot(r,await playerTickets(12));return 'Confirmed ticket #'+t.id+' with QR '+t.ticketCode;
},['INT-DUC-ISS-001']);
await issue('005','Payment notification and duplicate approval protection','Player receives ticket_paid; repeated approval returns 409.',12,async()=>{
 await notify(12,'ticket_paid',m.flagged.ticketId);await api(2,'/api/ticket-aftercare/reviews/'+m.flagged.paymentId+'/approve',{bankTransactionId:m.flagged.bankId,evidence:'ROUND 1 repeated approval negative test'},409);
},['INT-DUC-ISS-004']);
await issue('006','Report is available before disruption; description validation','Normal ticket can report; description below 10 trimmed characters disables submit.',6,async r=>{
 const p=await playerTickets(6);await p.getByRole('button',{name:'Báo cáo sự cố',exact:true}).click();await p.locator('textarea').fill('short');await expect(p.getByRole('button',{name:'Gửi báo cáo sự cố',exact:true})).toBeDisabled();await shot(r,p);
});
await issue('007','Player flood report reaches Staff Issue Hub','Report category/text/identity persist and Staff opens the correct conversation.',6,async r=>{
 const p=await playerTickets(6);await p.getByRole('button',{name:'Báo cáo sự cố',exact:true}).click();await p.getByLabel('Loại sự cố').selectOption('flood');await p.locator('textarea').fill('ROUND 1: Court flooded near the service line; unsafe to play.');await p.getByRole('button',{name:'Gửi báo cáo sự cố',exact:true}).click();await expect(p.getByRole('status')).toContainText('Đã gửi báo cáo sự cố');
 expect((await api(2,'/api/ticket-aftercare/inbox')).some(x=>x.ticketId===tickets[6].ticketId)).toBe(true);const s=await staff(2,'Sự cố');await s.locator('.issue-row').filter({hasText:accounts[6].name}).click();await expect(s.locator('.support-messages')).toContainText('ROUND 1: Court flooded');await shot(r,s);
});
await issue('008','Staff reply and Player notification','Reply is visible to the same Player and ticket_staff_reply is generated.',2,async r=>{
 const p=await staff(2,'Sự cố');await p.locator('.issue-row').filter({hasText:accounts[6].name}).click();await p.getByLabel('Phản hồi cho người chơi').fill('ROUND 1: Report received. Staff is checking the flooded court.');await p.getByRole('button',{name:'Gửi phản hồi',exact:true}).click();await expect(p.getByRole('status')).toContainText('Đã gửi tin nhắn');
 const d=await api(6,'/api/ticket-aftercare/tickets/'+tickets[6].ticketId+'/messages');expect(d.messages.some(x=>x.isStaff&&x.content.includes('Report received'))).toBe(true);await notify(6,'ticket_staff_reply',tickets[6].ticketId);await shot(r,p);
},['INT-DUC-ISS-007']);
await issue('009','Incident conversation privacy','Unrelated Player gets 404 for another conversation and 403 for Staff inbox.',11,async()=>{
 await api(11,'/api/ticket-aftercare/tickets/'+tickets[6].ticketId+'/messages',undefined,404);await api(11,'/api/ticket-aftercare/inbox',undefined,403);
});
await issue('010','Venue reason precedes and enables decisions','Short reason disables decisions; textarea is positioned before Suspend/Cancel.',2,async r=>{
 const p=await staff(2,'Sự cố');await p.locator('.ticket-filters').getByRole('button',{name:'Báo sự cố sân',exact:true}).click();const card=p.locator('.payment-card').filter({hasText:sessions.venue.date});await card.getByRole('button',{name:'Báo sân gặp sự cố',exact:true}).click();await card.getByLabel('Lý do / tình trạng sân').fill('short');const b=card.getByRole('button',{name:'Tạm ngưng buổi chơi',exact:true});await expect(b).toBeDisabled();expect((await card.getByLabel('Lý do / tình trạng sân').boundingBox()).y).toBeLessThan((await b.boundingBox()).y);await shot(r,p);
});
await issue('011','Suspend session and notify affected Players','Session becomes suspended; both affected holders receive ticket_venue_issue.',2,async r=>{
 await api(2,'/api/ticket-aftercare/sessions/'+sessions.venue.id+'/issue',{reason:'ROUND 1: Court flooding prevents safe play; suspend this session.',cancel:false});expect((await api(2,'/api/ticket-aftercare/sessions')).find(x=>x.id===sessions.venue.id).status).toBe('suspended');await notify(6,'ticket_venue_issue',tickets[6].ticketId);await notify(7,'ticket_venue_issue',tickets[7].ticketId);await shot(r,await staff(2,'Sự cố'));
});
await tkt('001','Reject venue refund before disruption','HTTP 409; original ticket stays confirmed.',8,async()=>{
 await api(8,'/api/ticket-aftercare/tickets/'+tickets[8].ticketId+'/refund',{},409);expect((await ticket(8)).status).toBe('confirmed');
});
await tkt('002','Request eligible venue refund','Ticket cancelled; exactly 100000 VND refund requested to original payer; request notification exists.',7,async r=>{
 const p=await playerTickets(7);await p.getByRole('button',{name:'Yêu cầu hoàn tiền',exact:true}).click();const completed=p.waitForResponse(r=>r.url().endsWith('/tickets/'+tickets[7].ticketId+'/refund')&&r.request().method()==='POST');await p.getByRole('button',{name:'Xác nhận yêu cầu hoàn',exact:true}).click();expect((await completed).status()).toBe(200);await expect(p.locator('.ticket-refund')).toContainText('Đã yêu cầu hoàn tiền');const t=await ticket(7);expect(t.status).toBe('cancelled');expect(t.refundStatus).toBe('requested');expect(t.refundAmountVnd).toBe(100000);await notify(7,'ticket_refund_requested',tickets[7].ticketId);state.refund=(await api(2,'/api/ticket-aftercare/refunds')).find(x=>x.ticketId===tickets[7].ticketId);await shot(r,p);
},['INT-DUC-ISS-011']);
await tkt('003','Duplicate refund is rejected','HTTP 409; exactly one pending refund remains.',7,async()=>{
 await api(7,'/api/ticket-aftercare/tickets/'+tickets[7].ticketId+'/refund',{},409);expect((await api(2,'/api/ticket-aftercare/refunds')).filter(x=>x.ticketId===tickets[7].ticketId)).toHaveLength(1);
},['INT-DUC-TKT-002']);
await tkt('004','Refund settlement requires evidence and checkbox','Completion is disabled with required inputs empty.',2,async r=>{
 const p=await staff(2,'Đổi & hoàn vé');await p.locator('.ticket-filters').getByRole('button',{name:'Hoàn tiền',exact:true}).click();await expect(p.locator('.payment-card').filter({hasText:accounts[7].name}).getByRole('button',{name:'Xác nhận đã hoàn tiền',exact:true})).toBeDisabled();await shot(r,p);
},['INT-DUC-TKT-002']);
await tkt('005','Complete synthetic refund and update Player state','Refund completed for 100000 VND; incoming payment remains paid; completion notification exists.',2,async r=>{
 const p=await staff(2,'Đổi & hoàn vé');await p.locator('.ticket-filters').getByRole('button',{name:'Hoàn tiền',exact:true}).click();const card=p.locator('.payment-card').filter({hasText:accounts[7].name});
 await card.getByLabel('Mã giao dịch ngân hàng').fill(m.prefix+'REFUND');await card.getByLabel('Nội dung đối soát / bằng chứng').fill('ROUND 1 SYNTHETIC repayment 100000 VND to original payer; no real transfer.');await card.getByRole('checkbox').check();await card.getByRole('button',{name:'Xác nhận đã hoàn tiền',exact:true}).click();await expect(p.getByRole('status')).toContainText('Đã xử lý thành công');
 expect((await ticket(7)).refundStatus).toBe('completed');expect((await api(7,'/api/v1/commerce/payment/orders/'+tickets[7].orderId)).status.toLowerCase()).toBe('paid');await notify(7,'ticket_refund_completed',tickets[7].ticketId);await shot(r,await playerTickets(7));
},['INT-DUC-TKT-002']);
await tkt('006','Completed refund cannot be posted twice','HTTP 409 when completion is repeated.',2,async()=>{
 await api(2,'/api/ticket-aftercare/refunds/'+state.refund.id+'/complete',{bankReference:m.prefix+'REFUND',evidence:'ROUND 1 duplicate repayment negative test'},409);
},['INT-DUC-TKT-005']);
await tkt('007','Select and request a cheaper replacement session','Pending change targets the selected 80000 VND session; 20000 VND difference and request notification exist.',8,async r=>{
 await api(2,'/api/ticket-aftercare/sessions/'+sessions['exchange-source'].id+'/issue',{reason:'ROUND 1: Damaged surface; suspend the source session.',cancel:false});const p=await playerTickets(8);await p.getByRole('button',{name:'Đổi sang buổi khác',exact:true}).click();await p.getByLabel('Buổi chơi còn chỗ').selectOption(String(sessions['exchange-target'].id));await expect(p.getByText(/Được yêu cầu hoàn chênh lệch/)).toContainText('20.000');const completed=p.waitForResponse(r=>r.url().endsWith('/tickets/'+tickets[8].ticketId+'/change')&&r.request().method()==='POST');await p.getByRole('button',{name:'Gửi yêu cầu đổi vé',exact:true}).click();expect((await completed).status()).toBe(200);
 state.change=(await api(2,'/api/ticket-aftercare/changes')).find(x=>x.ticketId===tickets[8].ticketId);expect(state.change).toBeTruthy();expect((await ticket(8)).status).toBe('confirmed');await notify(8,'ticket_change_requested',tickets[8].ticketId);await shot(r,p);
});
await tkt('008','Staff exchange updates old/new ticket, QR and difference refund','Old ticket exchanged; target ticket confirmed with new QR and 80000 VND; 20000 VND refund requested; notifications exist.',2,async r=>{
 const p=await staff(2,'Đổi & hoàn vé'),card=p.locator('.payment-card').filter({hasText:accounts[8].name});await card.getByLabel('Nội dung đối soát / bằng chứng').fill('ROUND 1: Verified replacement and pending difference refund of 20000 VND.');await card.getByRole('checkbox').check();await card.getByRole('button',{name:'Xác nhận đổi và cấp QR mới',exact:true}).click();await expect(p.getByRole('status')).toContainText('Đã xử lý thành công');
 const rows=await api(8,'/api/v1/commerce/payment/tickets'),old=rows.find(x=>x.id===tickets[8].ticketId),replacement=rows.find(x=>x.sessionId===sessions['exchange-target'].id);expect(old.status).toBe('exchanged');expect(replacement.status).toBe('confirmed');expect(replacement.amountVnd).toBe(80000);expect(replacement.ticketCode).not.toBe(old.ticketCode);expect(old.refundAmountVnd).toBe(20000);expect(old.refundStatus).toBe('requested');await notify(8,'ticket_changed',replacement.id);await notify(8,'ticket_difference_refund',old.id);await shot(r,await playerTickets(8));
},['INT-DUC-TKT-007']);
await tkt('009','Player cannot perform Staff refund completion','HTTP 403 for a Player settlement request.',11,async()=>{
 await api(11,'/api/ticket-aftercare/refunds/'+(state.refund?.id||-1)+'/complete',{bankReference:'NEGATIVE-TEST',evidence:'ROUND 1 unauthorized role negative test'},403);
});
await exc('001','Reject registration without a required skill exception','HTTP 400 SKILL_INELIGIBLE for B2 Player joining B3 session.',4,async()=>{
 expect((await api(4,'/api/sessions/'+sessions.exception.id+'/register',{},400)).code).toBe('SKILL_INELIGIBLE');
});
await exc('002','Exception reaches assigned Referee before a ticket exists','Pending request displays Player identity and bands; direct Accept/Reject buttons replace dropdown.',4,async r=>{
 state.exc4=(await api(4,'/api/v1/skill-reviews/sessions/'+sessions.exception.id+'/exceptions',{reason:'ROUND 1: Request B3 participation with my group.'})).id;const {page,card}=await review(3,'exceptions',4);await expect(card.getByRole('button',{name:'Chấp nhận',exact:true})).toBeVisible();await expect(card.getByRole('button',{name:'Từ chối',exact:true})).toBeVisible();await expect(card.locator('select')).toHaveCount(0);await shot(r,page);
});
await exc('003','Both exception decisions require a meaningful reason','Accept/Reject disabled for whitespace-only reason.',3,async r=>{
 const {page,card}=await review(3,'exceptions',4);await card.getByLabel('Lý do / nhận xét').fill('   ');await expect(card.getByRole('button',{name:'Chấp nhận',exact:true})).toBeDisabled();await expect(card.getByRole('button',{name:'Từ chối',exact:true})).toBeDisabled();await shot(r,page);
},['INT-DUC-EXC-002']);
await exc('004','Approval unlocks only the exact session without changing rating','Request Approved; named-session registration succeeds; another B3 session stays blocked; profile remains B2.',3,async r=>{
 const {page,card}=await review(3,'exceptions',4);await card.getByLabel('Lý do / nhận xét').fill('ROUND 1: Checked evidence; approve only for this exact session.');await card.getByRole('button',{name:'Chấp nhận',exact:true}).click();await expect(card.getByText('Đã duyệt',{exact:true})).toBeVisible();expect((await api(4,'/api/sessions/'+sessions.exception.id+'/register',{})).success).toBe(true);expect((await api(4,'/api/skills/me')).profiles.find(x=>x.sportId===m.sportId).currentBand).toBe('B2');await api(4,'/api/sessions/'+sessions.flagged.id+'/register',{},400);await shot(r,page);
},['INT-DUC-EXC-002']);
await exc('005','Rejection preserves registration restriction','Rejected request shows reason; Player registration stays blocked.',3,async r=>{
 state.exc5=(await api(5,'/api/v1/skill-reviews/sessions/'+sessions.exception.id+'/exceptions',{reason:'ROUND 1: Please review my eligibility.'})).id;const {page,card}=await review(3,'exceptions',5);await card.getByLabel('Lý do / nhận xét').fill('ROUND 1: Current evidence does not support B3 participation.');await card.getByRole('button',{name:'Từ chối',exact:true}).click();await expect(card.getByText('Từ chối',{exact:true})).toBeVisible();await api(5,'/api/sessions/'+sessions.exception.id+'/register',{},400);await shot(r,page);
});
await exc('006','Repeated decisions and Staff access are denied','HTTP 409 for repeat decision and 403 for Staff exception list.',3,async()=>{
 await api(3,'/api/v1/skill-reviews/exceptions/'+state.exc4+'/decision',{approved:false,reason:'ROUND 1 duplicate decision negative test'},409);await api(2,'/api/v1/skill-reviews/exceptions',undefined,403);
},['INT-DUC-EXC-004']);
await apl('001','Player appeals the exact published result','Pending appeal references own published 50/B3 assessment.',9,async()=>{
 state.appeal9=(await api(9,'/api/v1/skill-reviews/assessments/'+m.assessments.find(x=>x.account===accounts[9].name).assessmentId+'/appeals',{reason:'ROUND 1: Please recheck the published evidence.'})).id;expect((await api(9,'/api/v1/skill-reviews/appeals')).find(x=>x.id===state.appeal9).status).toBe('Pending');
});
await apl('002','Manager decision requires prior Referee review','HTTP 409 while appeal is Pending.',1,async()=>{
 await api(1,'/api/v1/skill-reviews/appeals/'+state.appeal9+'/decision',{reason:'ROUND 1 premature decision negative test',correct:false,score:null,expectedHistoryId:0},409);
},['INT-DUC-APL-001']);
await apl('003','Assigned Referee submits internal review','Appeal becomes UnderReview and ready for Manager decision.',3,async r=>{
 const {page,card}=await review(3,'appeals',9);await card.getByLabel('Lý do / nhận xét').fill('ROUND 1 INTERNAL REVIEW: Original marks support 50/B3.');await card.getByRole('button',{name:'Gửi nhận xét nội bộ',exact:true}).click();await expect(card.getByText('Đang xem xét',{exact:true})).toBeVisible();await shot(r,page);
},['INT-DUC-APL-001']);
await apl('004','Manager keeps the score and valid future ticket','Appeal Kept; profile 50/B3 and future ticket remain unchanged.',1,async r=>{
 const {page,card}=await review(1,'appeals',9);await card.getByLabel('Lý do / nhận xét').fill('ROUND 1: Keep original result after independent review.');await card.getByRole('button',{name:'Phê duyệt kết quả',exact:true}).click();await expect(card.getByText('Giữ nguyên điểm',{exact:true})).toBeVisible();const row=(await api(9,'/api/skills/me')).profiles.find(x=>x.sportId===m.sportId);expect(row.currentScore).toBe(50);expect(row.currentBand).toBe('B3');expect((await ticket(9)).status).toBe('confirmed');await shot(r,page);
},['INT-DUC-APL-003']);
await apl('005','Reject correction outside inclusive 0-100 range','HTTP 400 for score 101; profile/ticket not corrected.',1,async()=>{
 state.appeal10=(await api(10,'/api/v1/skill-reviews/assessments/'+m.assessments.find(x=>x.account===accounts[10].name).assessmentId+'/appeals',{reason:'ROUND 1: Re-evaluate the published result.'})).id;await api(3,'/api/v1/skill-reviews/appeals/'+state.appeal10+'/review',{reason:'ROUND 1 INTERNAL: A correction to 70 is supported.'});const row=(await api(1,'/api/v1/skill-reviews/appeals')).find(x=>x.id===state.appeal10);await api(1,'/api/v1/skill-reviews/appeals/'+state.appeal10+'/decision',{reason:'ROUND 1 invalid score negative test',correct:true,score:101,expectedHistoryId:row.expectedHistoryId},400);expect((await ticket(10)).status).toBe('confirmed');
});
await apl('006','Correction updates score/history and affected future ticket only','Profile 70/B4 with old 50/B3 retained in history; Player 10 B3 ticket cancelled/refund requested; Player 09 ticket valid.',1,async r=>{
 const {page,card}=await review(1,'appeals',10);await card.getByLabel('Quyết định').selectOption('correct');await card.getByLabel('Điểm điều chỉnh (0–100)').fill('70');await card.getByLabel('Lý do / nhận xét').fill('ROUND 1: Correct to 70/B4 based on reviewed evidence.');await card.getByRole('button',{name:'Phê duyệt kết quả',exact:true}).click();await expect(card.getByText('Đã điều chỉnh điểm',{exact:true})).toBeVisible();
 const row=(await api(10,'/api/skills/me')).profiles.find(x=>x.sportId===m.sportId);expect(row.currentScore).toBe(70);expect(row.currentBand).toBe('B4');const appeal=(await api(1,'/api/v1/skill-reviews/appeals')).find(x=>x.id===state.appeal10);expect(appeal.history.some(x=>x.oldScore===50&&x.newScore===70)).toBe(true);expect((await ticket(10)).status).toBe('cancelled');expect((await ticket(10)).refundStatus).toBe('requested');expect((await ticket(9)).status).toBe('confirmed');await shot(r,page);
},['INT-DUC-APL-005']);
await apl('007','Private Referee notes and unrelated appeals stay hidden','Player public events omit internal review; unrelated Player appeal list is empty.',10,async()=>{
 const row=(await api(10,'/api/v1/skill-reviews/appeals')).find(x=>x.id===state.appeal10);expect(JSON.stringify(row.events)).not.toContain('ROUND 1 INTERNAL');expect(await api(11,'/api/v1/skill-reviews/appeals')).toEqual([]);
},['INT-DUC-APL-006']);
await apl('008','Closed appeal cannot be approved again','HTTP 409 without duplicate score history.',1,async()=>{
 await api(1,'/api/v1/skill-reviews/appeals/'+state.appeal10+'/decision',{reason:'ROUND 1 repeated decision negative test',correct:true,score:70,expectedHistoryId:0},409);
},['INT-DUC-APL-006']);
await issue('012','Cancel disrupted session and prevent invalid reopening','Session cancelled and retained in Hub; cancelled-to-suspended attempt returns 409.',2,async r=>{
 await api(2,'/api/ticket-aftercare/sessions/'+sessions.venue.id+'/issue',{reason:'ROUND 1: Flood damage persists. Cancel the session.',cancel:true});await api(2,'/api/ticket-aftercare/sessions/'+sessions.venue.id+'/issue',{reason:'ROUND 1 negative attempt to reopen a cancelled session',cancel:false},409);expect((await api(2,'/api/ticket-aftercare/incidents')).find(x=>x.id===sessions.venue.id).status).toBe('cancelled');await shot(r,await staff(2,'Sự cố'));
},['INT-DUC-ISS-011']);
}finally{
 await writeFile(dir+'/results.json',JSON.stringify({build:process.env.DUC_BUILD,base,round:1,fixture:m.prefix,results},null,2));await browser.close();console.log('SUMMARY',JSON.stringify(results.reduce((a,r)=>(a[r.round1]=(a[r.round1]||0)+1,a),{})));
}
