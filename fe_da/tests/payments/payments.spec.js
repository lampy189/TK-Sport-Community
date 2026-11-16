import { env } from 'node:process';
import { test,expect } from '@playwright/test';
import { mkdir,readFile } from 'node:fs/promises';
const item={id:4,kind:'goods',orderId:3,reference:'GOODS-TEST',amountVnd:50000,status:'review_required',transactions:[{id:1,reference:'payos:BANK-TEST',amountVnd:50000,status:'review_required'}],history:[{actorId:22,action:'payment_recovery_failed',detail:JSON.stringify({at:'2026-11-16T09:00:00+07:00',evidence:'Đã kiểm tra ngân hàng',reason:'Giữ tồn đã hết hạn'})}]};
const invoice={code:'INV-G-0000000003',issuedAt:'2026-11-16T09:00:00+07:00',orderId:3,orderCode:'GOODS-TEST',customerName:'Khách kiểm thử',totalAmount:50000,method:'cash',paymentReference:'CASH-TEST',lines:[{productId:1,name:'Cầu lông',quantity:2,unitPriceVnd:25000,subtotalVnd:50000}]};
const order={id:3,orderCode:'GOODS-TEST',customerName:'Khách kiểm thử',totalAmount:50000,status:'Invoiced',paymentState:'Paid',handoverState:'Pending',lines:invoice.lines,invoice};
async function fixture(page,fail=false){const posts=[];await page.route('**/api/**',async route=>{const req=route.request(),path=new URL(req.url()).pathname;
 if(req.method()==='POST'){posts.push({path,body:req.postDataJSON()});return route.fulfill({status:fail?409:200,json:fail?{message:'Chưa xác nhận đủ tiền, tiếp tục đối soát.'}:{status:'review_required'}});}
 return route.fulfill({json:path.endsWith('/csrf')?{token:'test'}:(path.endsWith('/payment-recovery')||path.endsWith('/ticket-recovery'))?[item]:path.includes('/issue-workflow/')?{resource:{canResolve:false},state:{status:'in_progress',version:1,assignedTo:22},people:[],history:[]}:path.endsWith('/goods-orders/3')?order:path.endsWith('/goods-orders')?[order]:[]});});return posts;}
async function shot(page,name){const directory=env.PAYMENT_EVIDENCE_DIR||'../docs/evidence/pay-04-07';await mkdir(directory,{recursive:true});await page.screenshot({path:`${directory}/${name}.png`,fullPage:true});}
test('recovery requires evidence and submits the selected bank once',async({page})=>{
 const posts=await fixture(page);await page.goto('/tests/payments/');await page.getByRole('button',{name:/Hàng hóa · GOODS-TEST/}).click();
 await expect(page.getByRole('button',{name:'Duyệt khoản đủ tiền'})).toBeDisabled();
 await page.getByLabel('Nội dung / bằng chứng').fill('Đã kiểm tra chứng từ BANK-TEST đủ tiền.');await page.getByLabel('Giao dịch ngân hàng').selectOption('1');
 await shot(page,'recovery-desktop');await page.getByRole('button',{name:'Duyệt khoản đủ tiền'}).click();
 await expect.poll(()=>posts.length).toBe(1);expect(posts[0]).toEqual({path:'/api/ticket-recovery/4/approve',body:{evidence:'Đã kiểm tra chứng từ BANK-TEST đủ tiền.',bankTransactionId:1}});
});
test('failed retry retains evidence and mobile layout fits',async({page})=>{
 await page.setViewportSize({width:390,height:844});await fixture(page,true);await page.goto('/tests/payments/');await page.getByRole('button',{name:/Hàng hóa · GOODS-TEST/}).click();
 await page.getByLabel('Nội dung / bằng chứng').fill('Kiểm tra lại sau khi mất kết nối.');await page.getByRole('button',{name:'Kiểm tra lại payOS'}).click();
 await expect(page.getByRole('alert')).toContainText('Chưa xác nhận đủ tiền');await expect(page.getByLabel('Nội dung / bằng chứng')).toHaveValue('Kiểm tra lại sau khi mất kết nối.');
 expect(await page.evaluate(()=>document.documentElement.scrollWidth<=window.innerWidth)).toBe(true);await shot(page,'recovery-mobile');
});
test('invoice download and print reuse saved invoice without writes',async({page})=>{
 const posts=await fixture(page);await page.goto('/tests/payments/?screen=goods');await expect(page.getByRole('heading',{name:'Hóa đơn INV-G-0000000003'})).toBeVisible();
 await page.evaluate(()=>{window.print=()=>{window.__printed=true;};});await page.getByRole('button',{name:'In lại hóa đơn'}).click();expect(await page.evaluate(()=>window.__printed)).toBe(true);
 const pending=page.waitForEvent('download');await page.getByRole('button',{name:'Tải hóa đơn'}).click();const download=await pending;
 expect(download.suggestedFilename()).toBe('INV-G-0000000003.txt');expect(await readFile(await download.path(),'utf8')).toContain('INV-G-0000000003');expect(posts).toHaveLength(0);
 await shot(page,'invoice-desktop');await page.emulateMedia({media:'print'});await expect(page.getByRole('button',{name:'In lại hóa đơn'})).toBeHidden();await shot(page,'invoice-print');
});
