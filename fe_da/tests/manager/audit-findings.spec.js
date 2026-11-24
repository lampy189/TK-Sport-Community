import { test, expect } from '@playwright/test';
async function open(page, active, data, status=200) {
 await page.clock.install({time:new Date('2026-11-23T18:30:00+07:00')});
 await page.addInitScript(active=>history.replaceState({tkscHome:{role:'manager',active}},''),active);
 await page.route('**/api/**',r=>r.fulfill({status,json:data(new URL(r.request().url()).pathname)}));
 await page.goto('/tests/manager/');
}
test('25 assigned sessions are paginated for Manager',async({page})=>{
 await open(page,'Điểm danh',path=>path.endsWith('/referee/sessions')?Array.from({length:25},(_,i)=>({sessionId:i+1,sportName:'Badminton',courtName:'Sân '+i,sessionDate:'2026-11-23',startTime:'18:00:00',endTime:'20:00:00',sessionStatus:'Open',conditionCode:'ready'})):[]);
 await expect(page.locator('.session-option')).toHaveCount(10);
 await page.getByRole('button',{name:'Trang sau'}).click();
 await expect(page.locator('.session-option')).toHaveCount(10);
 await page.getByRole('button',{name:'Trang sau'}).click();
 await expect(page.locator('.session-option')).toHaveCount(5);
 await expect(page.locator('.checkin-sessions .tksc-pagination')).toHaveCount(1);
 await page.screenshot({path:test.info().outputPath('attendance.png'),fullPage:true});
});
test('audit: skill review load failure has no local retry',async({page})=>{
 await open(page,'Khiếu nại đánh giá',()=>({message:'Dịch vụ tạm lỗi'}),503);
 await expect(page.locator('.skill-reviews [role=alert]')).toContainText('Dịch vụ tạm lỗi');
 await expect(page.locator('.skill-reviews button')).toHaveCount(0);
});
test('audit: ongoing selected session displays empty slot selector',async({page})=>{
 const session={id:1,sportId:1,courtId:1,timeSlotId:1,sportName:'Badminton',courtName:'Sân 1',sessionDate:'2026-11-23',startTime:'18:00:00',endTime:'20:00:00',status:'Open',capacity:8,priceVnd:0,referees:[],staff:[]};
 await open(page,'Quản lý buổi chơi',path=>path.endsWith('/manage/options')?{sports:[{id:1,name:'Badminton'}],courts:[{id:1,name:'Sân 1'}],timeSlots:[{id:1,startTime:'18:00:00',endTime:'20:00:00',label:'18–20'}],personnel:[]}:path.endsWith('/manage')?[session]:[]);
 await page.locator('.manager-session-row').click();
 await expect(page.getByLabel('Khung giờ')).toHaveValue('');
 await expect(page.getByRole('button',{name:'Lưu buổi & phân công'})).toBeDisabled();
 await page.screenshot({path:test.info().outputPath('ongoing-session.png'),fullPage:true});
});
