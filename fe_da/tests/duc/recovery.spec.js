import { test,expect } from '@playwright/test';
import { mkdir } from 'node:fs/promises';
import { env } from 'node:process';
async function mock(page,{status='submitted',category='stock_adjustment',assigned=false,canResolve=true,payout=null}={}){
 const posts=[];
 const item={id:101,kind:'venues',subjectId:1,requestedBy:12,requesterRole:'staff',status,category,productId:category==='stock_adjustment'?17:null,quantityDelta:category==='stock_adjustment'?-2:null,amountVnd:100000,recipientId:99,bankTransactionId:11,decidedBy:status==='submitted'?null:33,reason:category==='stock_adjustment'?'Count shows two damaged units requiring correction':'Full return of verified unallocated incoming bank transfer'};
 await page.route('**/api/**',r=>{
  const request=r.request(),path=new URL(request.url()).pathname;
  if(request.method()==='POST'){posts.push({path,body:request.postDataJSON()});return r.fulfill({json:{}});}
  const data=path.endsWith('/csrf')?{token:'fixture'}:path.endsWith('/cases')?[{item,payout,resource:{title:category==='stock_adjustment'?'Inventory exception':'Unallocated payment return',canResolve}}]:path.endsWith('/products')?[{id:17,sku:'TEST-17',name:'Water'}]:path.endsWith('/ticket-recovery')?[{id:41,reference:'SOCIAL-41',orderId:1,kind:'social',amountVnd:100000,status:'review_required',transactions:[{id:11,reference:'BANK-11',amountVnd:100000,status:'review_required'}],history:[]}]:{resource:{canResolve},state:{version:7,status:assigned?'in_progress':'open',assignedTo:assigned?12:null,assignedName:assigned?'Staff one':null},people:[],history:[]};
  return r.fulfill({json:data});
 });return posts;
}
async function evidence(page,name){const dir=env.DUC_EVIDENCE_DIR||'/private/tmp/duc-recovery-evidence';await mkdir(dir,{recursive:true});await page.screenshot({path:`${dir}/${name}.png`,fullPage:true});}
test('operational proposal: inventory request retains approved product and signed base-unit quantity',async({page})=>{
 const posts=await mock(page);await page.goto('/tests/duc/?screen=operations');await page.getByText('Báo ngoại lệ chưa gắn vé/giao dịch').click();await page.getByLabel('Loại ngoại lệ').first().selectOption('stock_adjustment');await page.getByLabel('Mặt hàng').selectOption('17');await page.getByLabel('Số lượng điều chỉnh',{exact:false}).fill('-2');await page.getByLabel('Nguyên nhân, ảnh hưởng và phương án đề xuất').fill('Counted two damaged units with photos and signed count');await page.getByRole('button',{name:'Gửi ngoại lệ tại cơ sở của tôi'}).click();await expect.poll(()=>posts.length).toBe(1);expect(posts[0].body).toMatchObject({category:'stock_adjustment',productId:17,quantityDelta:-2});
});
for(const [status,button,endpoint] of [['submitted','Chấp thuận phương án','decision'],['approved','Thực hiện điều chỉnh kho đã duyệt','execute'],['executed','Xác nhận nghiệp vụ đã hoàn tất','close']]){
 test(`operational ${status}: evidence is required before ${endpoint}`,async({page})=>{
  const posts=await mock(page,{status});await page.goto('/tests/duc/?screen=operations');await page.locator('.issue-row').click();const submit=page.getByRole('button',{name:button,exact:true});await expect(submit).toBeDisabled();await page.getByLabel('Lý do quyết định / kết quả đã thực hiện').fill('Independently checked evidence for this handling step');await evidence(page,`11-operation-${status}`);await submit.click();await expect.poll(()=>posts.length).toBe(1);expect(posts[0].path).toBe(`/api/issue-workflow/cases/101/${endpoint}`);
 });
}
test('issue handler: claim includes optimistic version and leaves pending operations disabled',async({page})=>{
 const posts=await mock(page);await page.goto('/tests/duc/?screen=operations');await page.locator('.issue-row').click();const controls=page.getByRole('region',{name:'Theo dõi xử lý sự cố'});await expect(controls.getByRole('button',{name:'Chờ ngân hàng / đối soát'})).toBeDisabled();await controls.getByLabel('Căn cứ / kết quả / bước tiếp theo').fill('Taking ownership to coordinate the next handling step');await controls.getByRole('button',{name:'Nhận xử lý',exact:true}).click();await expect.poll(()=>posts.length).toBe(1);expect(posts[0].body).toMatchObject({action:'claim',expectedVersion:7});
});
test('issue handler: unresolved business source cannot be hidden by closing issue',async({page})=>{
 await mock(page,{assigned:true,canResolve:false});await page.goto('/tests/duc/?screen=operations');await page.locator('.issue-row').click();await page.getByLabel('Căn cứ / kết quả / bước tiếp theo').fill('Still waiting for verified outgoing bank transfer');await expect(page.getByRole('button',{name:'Ghi kết quả & đóng sự cố'})).toBeDisabled();
});
test('payment recovery: approval submits verified transaction and evidence through recovery API',async({page})=>{
 const posts=await mock(page,{assigned:true});await page.goto('/tests/duc/?screen=recovery');await page.getByRole('button',{name:'Vé · SOCIAL-41',exact:false}).click();await page.getByLabel('Giao dịch ngân hàng').selectOption('11');await page.getByLabel('Nội dung / bằng chứng').fill('Verified bank statement matches payer and full amount');await evidence(page,'12-payment-recovery');await page.getByRole('button',{name:'Duyệt khoản đủ tiền'}).click();await expect.poll(()=>posts.length).toBe(1);expect(posts[0]).toMatchObject({path:'/api/ticket-recovery/41/approve',body:{bankTransactionId:11}});
});

test('unallocated transfer: sends proposal with verified bank source',async({page})=>{
 const posts=await mock(page,{assigned:true});await page.goto('/tests/duc/?screen=recovery');await page.getByRole('button',{name:'Vé · SOCIAL-41',exact:false}).click();await page.getByLabel('Giao dịch ngân hàng').selectOption('11');await page.getByLabel('Nội dung / bằng chứng').fill('Request full return of unallocated incoming bank transfer');await page.getByRole('button',{name:'Đề nghị hoàn toàn bộ khoản chưa phân bổ'}).click();await expect.poll(()=>posts.length).toBe(1);expect(posts[0].body).toMatchObject({kind:'reviews',subjectId:41,category:'return_unallocated',bankTransactionId:11});
});
test('unallocated transfer: outgoing evidence required before recording',async({page})=>{
 const posts=await mock(page,{category:'return_unallocated',status:'approved'});await page.goto('/tests/duc/?screen=operations');await page.locator('.issue-row').click();const submit=page.getByRole('button',{name:'Lưu chứng từ chuyển trả'});await expect(submit).toBeDisabled();await page.getByLabel('Mã giao dịch tiền ra').fill('BANK-RETURN-01');await page.getByLabel('Chứng từ chuyển trả').fill('Outgoing transfer verified against original payer');await page.getByLabel('Thời điểm chuyển trả').fill('2026-11-01T10:00');await page.getByLabel('Đã thực sự chuyển đủ khoản',{exact:false}).check();await evidence(page,'13-unallocated-return-payout');await submit.click();await expect.poll(()=>posts.length).toBe(1);expect(posts[0]).toMatchObject({path:'/api/issue-workflow/cases/101/payout',body:{amountVnd:100000,recipientId:99}});
});
test('unallocated transfer: manager verifies saved evidence before closing',async({page})=>{
 const posts=await mock(page,{category:'return_unallocated',status:'approved',payout:{bankReference:'RETURN-01',recordedBy:12,document:'Saved bank proof'}});await page.goto('/tests/duc/?screen=operations');await page.locator('.issue-row').click();await expect(page.getByRole('button',{name:'Lưu chứng từ chuyển trả'})).toHaveCount(0);await page.getByLabel('Kết quả xác minh độc lập').fill('Independent manager verified outgoing bank transfer');await evidence(page,'14-unallocated-return-verification');await page.getByRole('button',{name:'Xác minh hoàn khoản chưa phân bổ',exact:true}).click();await expect.poll(()=>posts.length).toBe(1);expect(posts[0].path).toBe('/api/issue-workflow/cases/101/verify-return');
});
