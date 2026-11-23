import {test,expect} from '@playwright/test';
async function start(page, active, response) {
 await page.addInitScript(active=>history.replaceState({tkscHome:{role:'manager',active}},''),active);
 await page.route('**/api/**',r=>r.fulfill({json:new URL(r.request().url()).pathname==='/api/v1/sport-centers'?{centers:[{id:1,name:'TKSC Vĩnh Phúc'}],assignedVenueId:1}:response(new URL(r.request().url()).pathname)}));
 await page.goto('/tests/manager/');
}
test('policy table paginates without changing source total',async({page})=>{
 await start(page,'Chính sách & giá',p=>p.endsWith('/options')?{venues:[{id:1,name:'Vĩnh Phúc'}],products:[],sessions:[]}:Array.from({length:23},(_,i)=>({id:i+1,code:'POL-'+(i+1),name:'Chính sách '+(i+1),status:'submitted',kind:'policy'})));
 await expect(page.locator('.business-screen-rows tr')).toHaveCount(10);
 await page.getByRole('button',{name:'Trang 3',exact:true}).click();
 await expect(page.locator('.business-screen-rows tr')).toHaveCount(3);
 await page.emulateMedia({media:'print'});
 await expect(page.locator('.business-print-rows tr')).toHaveCount(23);
 await expect(page.locator('.business-screen-rows')).toBeHidden();
});
test('personnel filters match Vietnamese names without accents',async({page})=>{
 await start(page,'Phê duyệt nhân sự',()=>({users:[],venues:[],requests:[{id:1,target:'Nguyễn An',requester:'Lâm',status:'pending',payload:'{}'},{id:2,target:'Trần Bình',requester:'Lâm',status:'executed',payload:'{}'}]}));
 await page.getByLabel('Tìm người / mã yêu cầu').fill('nguyen');
 await expect(page.locator('.ops-grid .ops-card')).toHaveCount(1);
 await page.getByLabel('Trạng thái yêu cầu').selectOption('executed');
 await expect(page.getByText('Không có yêu cầu phù hợp.')).toBeVisible();
});
test('payment prioritizes transaction and collapses auxiliary controls on desktop and mobile',async({page})=>{
 await start(page,'Thanh toán',p=>p==='/api/ticket-recovery'?[{id:1,kind:'social',reference:'SOC-001',status:'review_required',amountVnd:100000,transactions:[],history:[]}]:{state:{status:'open',version:0},resource:{canResolve:true},people:[],history:[]});
 await page.locator('.recovery-list button').click();
 await expect(page.getByLabel('Giao dịch ngân hàng')).toBeVisible();
 await expect(page.locator('.manager-disclosure').first()).not.toHaveAttribute('open');
 await expect(page.locator('.recovery-list .tksc-pagination')).toHaveCount(0);
 await page.screenshot({path:test.info().outputPath('payment-desktop.png'),fullPage:true});
 await page.setViewportSize({width:390,height:844});
 expect(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth)).toBe(true);
 await page.screenshot({path:test.info().outputPath('payment-mobile.png'),fullPage:true});
});

test('Manager stock approval and supply report paginate independently',async({page})=>{
 const rows=Array.from({length:23},(_,i)=>({id:i+1,productId:i+1,name:'Mặt hàng '+(i+1),sku:'SP-'+i,status:'Pending',baseUnit:'cái',systemQuantity:10,countedQuantity:9,difference:-1,countedAt:'2026-11-23T10:00:00Z',countedByName:'Nhân viên',reason:'Kiểm kê'}));
 await start(page,'Kho & vật tư',p=>p.endsWith('/counts')?rows:p.endsWith('/reports/supplies')?{lines:rows}:[]);
 await expect(page.locator('tbody tr:not(.tksc-pagination-row)')).toHaveCount(10);
 await page.getByRole('button',{name:'Trang 3',exact:true}).click();
 await expect(page.locator('tbody tr:not(.tksc-pagination-row)')).toHaveCount(3);
 await page.getByRole('tab',{name:'Báo cáo & đối soát'}).click();
 await expect(page.locator('tbody tr:not(.tksc-pagination-row)')).toHaveCount(10);
 await page.getByRole('button',{name:'Trang 3',exact:true}).click();
 await expect(page.locator('tbody tr:not(.tksc-pagination-row)')).toHaveCount(3);
});
