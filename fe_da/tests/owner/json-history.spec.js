import { test, expect } from '@playwright/test';
import { mkdir } from 'node:fs/promises';
const reason='[TEST] Mở quét QR thủ công trong lúc kiểm tra thiết bị.';
const serialize=value=>JSON.stringify(value).replace(/[^\x00-\x7f]/g,char=>'\\u'+char.charCodeAt(0).toString(16).padStart(4,'0'));
const payload={Id:20723,Kind:'venues',SubjectId:1,Category:'other',Reason:reason,RequestedBy:6,RequesterRole:'staff',Status:'submitted',DecidedBy:null,Decision:null,RequestedAt:'2026-11-23T04:39:03+00:00'};
async function mock(page,{malformed=false}={}) {
  const errors=[];page.on('pageerror',e=>errors.push(e.message));
  await page.route('**/api/**',async route=>{
    const path=new URL(route.request().url()).pathname;
    const history=[{id:6,actorId:6,action:'operational_requested',newValue:malformed?'{"Reason":':serialize(payload)}];
    const json=path.endsWith('/csrf')?{token:'fixture'}:
      path.endsWith('/cases')?[{item:{id:20723,kind:'venues',subjectId:1,category:'other',reason,requesterRole:'staff',requestedBy:6,status:'submitted'},resource:{title:'TKSC Vĩnh Phúc',canResolve:false}}]:
      path.includes('/api/issue-workflow/')?{resource:{canResolve:false},state:{status:'open',version:0},people:[{id:6,name:'Nhân viên kiểm thử',role:'Staff'}],history}:
      path.endsWith('/refunds')?[{id:5,ticketId:41,status:'completed',amountVnd:100000,recipient:'Người chơi kiểm thử',email:'fixture@example.invalid'}]:
      path.endsWith('/refunds/5/history')?[{id:7,actorId:7,action:'refund_completed',newValue:serialize({at:'2026-11-23T05:00:00Z',payout:{AmountVnd:100000,RecipientId:99,BankReference:'BANK-TEST',Document:'Đã chuyển đủ tiền',RecordedBy:6},verification:'Đã xác minh độc lập'})}]:
      path.endsWith('/messages')?{messages:[]}:
      path.endsWith('/payment/reviews')?[{id:1,orderId:10,referenceCode:'SOC-TEST',amountVnd:100000,reasons:[{newValue:serialize({reason:'Cần đối chiếu tiền thừa',received:120000,expected:100000})}]}]:
      path.endsWith('/overview')?{sessions:[],tickets:[],feedback:[{id:1,actor:'Nhân viên kiểm thử',detail:serialize({Content:'Cần kiểm tra lại thiết bị',at:'2026-11-23T05:00:00Z'})}]}:[];
    await route.fulfill({json});
  });
  return errors;
}
for(const width of [1440,390]) test(`operational history decodes screenshot payload and fits ${width}px`,async({page})=>{
  const errors=await mock(page);await page.setViewportSize({width,height:1000});await page.goto('/tests/owner/?screen=exceptions');await page.locator('.issue-row').click();await page.getByText('Lịch sử xử lý (1)',{exact:true}).click();
  const history=page.locator('.audit-history');await expect(history.getByRole('heading',{name:'Đề xuất phương án xử lý',exact:true})).toBeVisible();await expect(history.getByText(reason,{exact:true})).toBeVisible();await expect(history.getByText('Nhân viên kiểm thử',{exact:false})).toBeVisible();await expect(history.getByText('Chờ duyệt',{exact:true})).toBeVisible();
  await expect(history).not.toContainText(/operational_requested|"Reason"|\\u[0-9a-f]{4}/i);expect(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth)).toBe(false);expect(errors).toEqual([]);
  await mkdir('/private/tmp/duc-json-history-evidence',{recursive:true});await page.screenshot({path:`/private/tmp/duc-json-history-evidence/operational-${width}.png`,fullPage:true});
});
test('refund history retains proof and verification without displaying serialized data',async({page})=>{
  const errors=await mock(page);await page.goto('/tests/owner/?screen=refunds');await page.getByText('Lịch sử xử lý (1)',{exact:true}).click();const history=page.locator('.audit-history');await expect(history.getByRole('heading',{name:'Xác minh và hoàn tất khoản hoàn',exact:true})).toBeVisible();await expect(history).toContainText('100.000 ₫');await expect(history).toContainText('BANK-TEST');await expect(history).toContainText('Đã xác minh độc lập');await expect(history).not.toContainText(/refund_completed|AmountVnd|\\u[0-9a-f]{4}/i);expect(errors).toEqual([]);
});
test('payment review reasons display human labels and amounts',async({page})=>{
  const errors=await mock(page);await page.goto('/tests/owner/?screen=payment-review');await expect(page.getByText('Cần đối chiếu tiền thừa',{exact:true})).toBeVisible();await expect(page.locator('.audit-fields')).toContainText('120.000 ₫');await expect(page.locator('.audit-fields')).not.toContainText('"reason"');expect(errors).toEqual([]);
});
test('feedback decodes JSON and displays the actual message',async({page})=>{
  const errors=await mock(page);await page.goto('/tests/owner/?screen=overview');await expect(page.getByText('Cần kiểm tra lại thiết bị',{exact:true})).toBeVisible();await expect(page.locator('main')).not.toContainText('"Content"');expect(errors).toEqual([]);
});
test('malformed history offers readable fallback and never breaks issue controls',async({page})=>{
  const errors=await mock(page,{malformed:true});await page.goto('/tests/owner/?screen=exceptions');await page.locator('.issue-row').click();await page.getByText('Lịch sử xử lý (1)',{exact:true}).click();await expect(page.locator('.audit-history')).toContainText('Chi tiết chưa đọc được');await expect(page.locator('.audit-history')).not.toContainText('"Reason"');await page.getByLabel('Căn cứ / kết quả / bước tiếp theo').fill('Kiểm tra bằng chứng và tiếp tục xử lý');await expect(page.getByRole('button',{name:'Nhận xử lý',exact:true})).toBeEnabled();expect(errors).toEqual([]);
});
