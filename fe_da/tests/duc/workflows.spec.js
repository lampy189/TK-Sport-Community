import { Buffer } from 'node:buffer';
import { test, expect } from '@playwright/test';
import { mkdir } from 'node:fs/promises';
const evidence='../docs/evidence/duc-nov-07-10';
const session={id:22,sport:'Cầu lông',court:'Sân 1',venue:'TK Sport',sessionDate:'2026-11-10',startTime:'18:00:00',endTime:'20:00:00',status:'suspended'};
const exception={id:7,player:'Nguyễn An (fixture)',sport:'Cầu lông',status:'Pending',sessionId:22,sessionDate:'2026-11-10',sessionBand:'B3',currentBand:'B2',reason:'Xin tham gia cùng nhóm bạn'};
const appeal={...exception,status:'UnderReview',disputedScore:65,disputedBand:'B3',currentScore:65,currentBand:'B3',expectedHistoryId:8};
const refund={id:5,ticketId:41,amountVnd:100000,recipient:'Nguyễn An (fixture)',email:'player@example.test',referenceCode:'SOCIAL-41'};
const change={id:6,ticketId:41,holder:'Nguyễn An (fixture)',payer:'Nguyễn An (fixture)',court:'Sân 2',sessionDate:'2026-11-11',startTime:'18:00:00',endTime:'20:00:00',oldPriceVnd:100000,newPriceVnd:120000};
async function fixture(page,{fail=false,empty=false}={}) {
 const posts=[];
 await page.route('**/api/**',async route=>{
  const req=route.request(),path=new URL(req.url()).pathname;
  if(req.method()==='POST'){
   posts.push({path,body:req.postDataJSON()});await new Promise(r=>setTimeout(r,120));
   return route.fulfill({status:fail?409:200,json:fail?{message:'Dữ liệu đã thay đổi. Tải lại để xem.'}:{id:1}});
  }
  const data=path.endsWith('/csrf')?{token:'fixture'}:
   path.endsWith('/exceptions')?[exception]:path.endsWith('/appeals')?[appeal]:
   (path.endsWith('/sessions')||path.endsWith('/incidents'))?[session]:path.endsWith('/refunds')?[refund]:
   path.endsWith('/changes')?[change]:path.endsWith('/reviews')?[{id:9,referenceCode:'SOCIAL-ERROR',payer:'Nguyễn Bình',amountVnd:100000,transactions:[]}]:
   path.endsWith('/inbox')?[{id:41,ticketId:41,holder:'Nguyễn An (fixture)',status:'confirmed',lastSender:'Nguyễn An (fixture)',lastMessage:'[Báo cáo sự cố · Sân ngập]\nSân bị ngập nước, không thể chơi.',lastIsStaff:false}]:
   path.endsWith('/messages')?{canProvidePayout:true,messages:[{id:1,sender:'Nguyễn An (fixture)',content:'[Báo cáo sự cố · Sân ngập]\nSân bị ngập nước, không thể chơi.',isStaff:false,createdAt:'2026-11-09T08:00:00Z'}]}:[];
  return route.fulfill({json:empty&&Array.isArray(data)?[]:data});
 });
 return posts;
}
async function screen(page,name){await page.goto('/tests/duc/?screen='+name);}
async function shot(page,name){await mkdir(evidence,{recursive:true});await page.screenshot({path:evidence+'/'+name+'.png',fullPage:true});}
test('exception: empty or whitespace reason blocks both direct decisions',async({page})=>{
 await fixture(page);await screen(page,'exception');
 await expect(page.getByRole('button',{name:'Chấp nhận',exact:true})).toBeDisabled();
 await expect(page.getByRole('button',{name:'Từ chối',exact:true})).toBeDisabled();
 await expect(page.locator('select')).toHaveCount(0);
 await page.getByLabel('Lý do / nhận xét').fill('   ');
 await expect(page.getByRole('button',{name:'Chấp nhận',exact:true})).toBeDisabled();
 await page.getByLabel('Lý do / nhận xét').fill('Đã xem lịch sử, phù hợp tham gia.');
 await shot(page,'01-exception-decisions');
});
for(const [label,approved] of [['Chấp nhận',true],['Từ chối',false]]){
 test('exception: '+label+' sends explicit decision once',async({page})=>{
  const posts=await fixture(page);await screen(page,'exception');
  await page.getByLabel('Lý do / nhận xét').fill('  Đã kiểm tra lịch sử kỹ năng  ');
  await page.getByRole('button',{name:label,exact:true}).click();
  await expect.poll(()=>posts.length).toBe(1);
  expect(posts[0].body).toEqual({reason:'Đã kiểm tra lịch sử kỹ năng',approved});
 });
}
test('exception: error retains reason for retry',async({page})=>{
 await fixture(page,{fail:true});await screen(page,'exception');
 await page.getByLabel('Lý do / nhận xét').fill('Lý do vẫn được giữ khi lỗi');
 await page.getByRole('button',{name:'Chấp nhận',exact:true}).click();
 await expect(page.getByRole('alert')).toContainText('Dữ liệu đã thay đổi');
 await expect(page.getByLabel('Lý do / nhận xét')).toHaveValue('Lý do vẫn được giữ khi lỗi');
});
test('venue: reason precedes decisions, short reason blocks submission',async({page})=>{
 await fixture(page);await screen(page,'venue');
 await page.getByRole('button',{name:'Báo sân gặp sự cố',exact:true}).click();
 for(const reason of ['', '   ', 'Sân ngập']){
  await page.getByLabel('Lý do / tình trạng sân').fill(reason);
  await expect(page.getByRole('button',{name:'Hủy buổi chơi',exact:true})).toBeDisabled();
 }
 await page.getByLabel('Lý do / tình trạng sân').fill('Sân ngập nước, không đảm bảo an toàn.');
 const top=await page.getByLabel('Lý do / tình trạng sân').boundingBox();
 const button=await page.getByRole('button',{name:'Tạm ngưng buổi chơi',exact:true}).boundingBox();
 expect(top.y).toBeLessThan(button.y);await shot(page,'02-venue-reason-first');
});
for(const [label,cancel] of [['Tạm ngưng buổi chơi',false],['Hủy buổi chơi',true]]){
 test('venue: '+label+' submits reason and decision',async({page})=>{
  const posts=await fixture(page);await screen(page,'venue');
  await page.getByRole('button',{name:'Báo sân gặp sự cố',exact:true}).click();
  await page.getByLabel('Lý do / tình trạng sân').fill('  Sân ngập nước không thể sử dụng  ');
  await page.getByRole('button',{name:label,exact:true}).click();
  await expect.poll(()=>posts.length).toBe(1);
  expect(posts[0]).toEqual({path:'/api/ticket-aftercare/sessions/22/issue',body:{reason:'Sân ngập nước không thể sử dụng',cancel}});
 });
}
test('hub: four issue sources and selected Player detail',async({page})=>{
 await fixture(page);await screen(page,'hub');await expect(page.locator('.issue-row')).toHaveCount(4);
 await page.locator('.issue-row').filter({hasText:'Báo cáo / trao đổi Player'}).click();
 await expect(page.getByRole('heading',{name:'Trao đổi về vé #41'})).toBeVisible();await shot(page,'03-issue-hub-player-report');
});
test('hub: empty state',async({page})=>{
 await fixture(page,{empty:true});await screen(page,'hub');await expect(page.getByText('Không có sự cố cần xử lý.')).toBeVisible();
});
test('hub: partial failure retains available reports',async({page})=>{
 await fixture(page);await page.route('**/api/ticket-aftercare/refunds',r=>r.fulfill({status:503,json:{message:'Hoàn tiền tạm lỗi'}}));
 await screen(page,'hub');await expect(page.getByRole('alert')).toContainText('Hoàn tiền tạm lỗi');await expect(page.locator('.issue-row')).toHaveCount(3);
});
test('player report: reason required, category and content submitted',async({page})=>{
 const posts=await fixture(page);await screen(page,'player');await page.getByRole('button',{name:'Báo cáo sự cố',exact:true}).click();
 await expect(page.getByRole('button',{name:'Gửi báo cáo sự cố',exact:true})).toBeDisabled();
 await page.getByLabel('Loại sự cố').selectOption('flood');await page.locator('textarea').fill('Sân bị ngập nước không thể chơi.');
 await shot(page,'04-player-report');await page.getByRole('button',{name:'Gửi báo cáo sự cố',exact:true}).click();
 await expect(page.getByRole('status')).toContainText('Đã gửi báo cáo sự cố');
 expect(posts[0].body).toMatchObject({category:'flood',content:'Sân bị ngập nước không thể chơi.'});expect(posts[0].path).toBe('/api/ticket-aftercare/tickets/41/reports');
});
test('player report: API failure preserves draft',async({page})=>{
 await fixture(page,{fail:true});await screen(page,'player');await page.getByRole('button',{name:'Báo cáo sự cố',exact:true}).click();
 await page.locator('textarea').fill('Không nhận được trạng thái thanh toán');await page.getByRole('button',{name:'Gửi báo cáo sự cố',exact:true}).click();
 await expect(page.getByRole('alert')).toContainText('Dữ liệu đã thay đổi');await expect(page.locator('textarea')).toHaveValue('Không nhận được trạng thái thanh toán');
});
test('player report: oversized attachment rejected',async({page})=>{
 await fixture(page);await screen(page,'player');await page.getByRole('button',{name:'Báo cáo sự cố',exact:true}).click();
 await page.locator('input[type=file]').setInputFiles({name:'large.png',mimeType:'image/png',buffer:Buffer.alloc(2097153)});
 await expect(page.getByRole('alert')).toContainText('tối đa 2 MB');
});
test('reply: empty disabled then sends to selected ticket',async({page})=>{
 const posts=await fixture(page);await screen(page,'hub');await page.locator('.issue-row').first().click();
 await expect(page.getByRole('button',{name:'Gửi phản hồi',exact:true})).toBeDisabled();
 await page.getByLabel('Phản hồi cho người chơi').fill('Đã nhận báo cáo, đang kiểm tra sân.');await page.getByRole('button',{name:'Gửi phản hồi',exact:true}).click();
 await expect(page.getByRole('status')).toContainText('Đã gửi tin nhắn');expect(posts[0].path).toBe('/api/ticket-aftercare/tickets/41/messages');
});
test('reply: API error retains draft',async({page})=>{
 await fixture(page,{fail:true});await screen(page,'hub');await page.locator('.issue-row').first().click();
 await page.getByLabel('Phản hồi cho người chơi').fill('Đang kiểm tra, vui lòng chờ.');await page.getByRole('button',{name:'Gửi phản hồi',exact:true}).click();
 await expect(page.getByRole('alert')).toContainText('Dữ liệu đã thay đổi');await expect(page.getByLabel('Phản hồi cho người chơi')).toHaveValue('Đang kiểm tra, vui lòng chờ.');
});
test('refund settlement: requires checked evidence and bank reference',async({page})=>{
 const posts=await fixture(page);await screen(page,'hub');await page.getByRole('button',{name:'Hoàn tiền',exact:true}).click();
 const submit=page.getByRole('button',{name:'Xác nhận đã hoàn tiền',exact:true});await expect(submit).toBeDisabled();
 await page.getByLabel('Nội dung đối soát / bằng chứng').fill('Đã chuyển 100.000đ về người thanh toán.');await page.getByRole('checkbox').check();await submit.click();expect(posts).toHaveLength(0);
 await page.getByLabel('Mã giao dịch ngân hàng').fill('BANK-123456');await shot(page,'07-refund-evidence');await submit.click();await expect.poll(()=>posts.length).toBe(1);
 expect(posts[0].path).toBe('/api/ticket-aftercare/refunds/5/complete');expect(posts[0].body.bankReference).toBe('BANK-123456');
});
test('exchange settlement: additional payment includes bank reference',async({page})=>{
 const posts=await fixture(page);await screen(page,'hub');await page.getByRole('button',{name:'Yêu cầu đổi vé',exact:true}).click();
 await page.getByLabel('Nội dung đối soát / bằng chứng').fill('Đã nhận 20.000đ chênh lệch và đối chiếu.');await page.getByRole('checkbox').check();await page.getByLabel('Mã giao dịch ngân hàng').fill('BANK-CHANGE');
 await shot(page,'08-exchange-evidence');await page.getByRole('button',{name:'Xác nhận đổi và cấp QR mới',exact:true}).click();await expect.poll(()=>posts.length).toBe(1);expect(posts[0].path).toBe('/api/ticket-aftercare/changes/6/complete');
});
test('settlement: API failure never shows completed payout',async({page})=>{
 await fixture(page,{fail:true});await screen(page,'hub');await page.getByRole('button',{name:'Hoàn tiền',exact:true}).click();
 await page.getByLabel('Nội dung đối soát / bằng chứng').fill('Đối soát chuyển khoản hoàn về người trả.');await page.getByRole('checkbox').check();await page.getByLabel('Mã giao dịch ngân hàng').fill('BANK-ERROR');
 await page.getByRole('button',{name:'Xác nhận đã hoàn tiền',exact:true}).click();await expect(page.getByRole('alert')).toContainText('Dữ liệu đã thay đổi');await expect(page.locator('.payment-notice')).toHaveCount(0);
});
test('appeal: keep score sends expected history and no correction',async({page})=>{
 const posts=await fixture(page);await screen(page,'appeal');await page.getByLabel('Lý do / nhận xét').fill('Giữ nguyên điểm theo bằng chứng trọng tài.');await shot(page,'05-appeal-decision');
 await page.getByRole('button',{name:'Phê duyệt kết quả',exact:true}).click();await expect.poll(()=>posts.length).toBe(1);
 expect(posts[0].body).toEqual({reason:'Giữ nguyên điểm theo bằng chứng trọng tài.',correct:false,score:null,expectedHistoryId:8});
});
test('appeal: corrected score boundary 100',async({page})=>{
 const posts=await fixture(page);await screen(page,'appeal');await page.getByLabel('Quyết định').selectOption('correct');await page.getByLabel('Điểm điều chỉnh (0–100)').fill('100');
 await page.getByLabel('Lý do / nhận xét').fill('Điều chỉnh theo review đã được xác minh.');await page.getByRole('button',{name:'Phê duyệt kết quả',exact:true}).click();
 await expect.poll(()=>posts.length).toBe(1);expect(posts[0].body).toMatchObject({correct:true,score:100,expectedHistoryId:8});
});
test('appeal: invalid score blocks submission',async({page})=>{
 const posts=await fixture(page);await screen(page,'appeal');await page.getByLabel('Quyết định').selectOption('correct');await page.getByLabel('Điểm điều chỉnh (0–100)').fill('101');
 await page.getByLabel('Lý do / nhận xét').fill('Điểm điều chỉnh cần được kiểm tra');await page.getByRole('button',{name:'Phê duyệt kết quả',exact:true}).click();expect(posts).toHaveLength(0);
});
test('mobile: hub fits viewport and opens details',async({page})=>{
 await page.setViewportSize({width:390,height:844});await fixture(page);await screen(page,'hub');await page.locator('.issue-row').first().click();
 await expect(page.getByRole('heading',{name:'Trao đổi về vé #41'})).toBeVisible();expect(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth)).toBe(true);await shot(page,'06-mobile-issue-hub');
});
