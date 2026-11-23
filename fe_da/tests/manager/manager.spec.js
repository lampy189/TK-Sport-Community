import { test, expect } from '@playwright/test';

async function setup(page, active) {
  await page.clock.install({ time: new Date('2026-11-23T18:30:00+07:00') });
  await page.addInitScript(active => history.replaceState({ tkscHome: { role: 'manager', active } }, ''), active);
  const session = { sportId: 1, courtId: 1, timeSlotId: 1, sportName: 'Badminton', courtName: 'Sân 1', sessionDate: '2026-11-23', startTime: '18:00:00', endTime: '20:00:00', status: 'Open', conditionCode: 'ready', occupiedPlaces: 0, capacity: 8, priceVnd: 0, referees: [], staff: [] };
  await page.route('**/api/**', route => {
    const path = new URL(route.request().url()).pathname;
    let data = [];
    if (path === '/api/sessions/manage/options') data = { sports: [{id:1,name:'Badminton'}], courts: [{id:1,name:'Sân 1'}], personnel: [], timeSlots: [{id:1,startTime:'18:00:00',endTime:'20:00:00',label:'18–20'},{id:2,startTime:'20:00:00',endTime:'22:00:00',label:'20–22'}] };
    if (path === '/api/sessions/manage' || path === '/api/v1/referee/sessions') data = [{...session,id:1,sessionId:1}, {...session,id:2,sessionId:2,sessionDate:'2026-11-22',courtName:'Sân quá khứ'}, {...session,id:3,sessionId:3,startTime:'20:00:00',endTime:'22:00:00',conditionCode:'not_ready',courtName:'Sân tương lai'}];
    if (path === '/api/ticket-recovery') data = [{id:1,reference:'PAY-VÉ',kind:'social',status:'pending',amountVnd:100,transactions:[],history:[]},{id:2,reference:'PAY-HÀNG',kind:'goods',status:'review_required',amountVnd:200,transactions:[],history:[]}];
    if (path === '/api/ticket-aftercare/refunds') data = [{id:1,ticketId:41,recipient:'An',amountVnd:100,status:'requested'},{id:2,ticketId:42,recipient:'Bình',amountVnd:100,status:'completed'}];
    return route.fulfill({json:data});
  });
  await page.goto('/tests/manager/');
}

test('Manager hides past sessions and past time slots', async ({page}) => {
  await setup(page, 'Quản lý buổi chơi');
  await expect(page.getByText('Sân quá khứ')).toHaveCount(0);
  await expect(page.locator('.manager-session-row')).toHaveCount(2);
  await expect(page.getByLabel('Khung giờ').locator('option')).toHaveText(['Chọn giờ','20–22']);
  await expect(page.getByLabel('Ngày', {exact:true})).toHaveAttribute('min','2026-11-23');
  await expect(page.getByRole('button',{name:'Tải lại',exact:true})).toHaveCount(0);
});

test('Manager cannot open future or expired attendance; selected session expires live', async ({page}) => {
  await setup(page, 'Điểm danh');
  await expect(page.locator('.session-option')).toHaveCount(2);
  await expect(page.locator('.session-option').filter({hasText:'Sân tương lai'})).toBeDisabled();
  await page.locator('.session-option').filter({hasText:'Sân 1'}).click();
  await expect(page.getByRole('heading',{name:'Quét vé người chơi'})).toBeVisible();
  await page.clock.fastForward(90 * 60 * 1000);
  await expect(page.getByRole('heading',{name:'Quét vé người chơi'})).toHaveCount(0);
  await expect(page.getByText('Buổi hiện không thể điểm danh. Hãy chọn buổi khác.')).toBeVisible();
});

test('Manager payment filters stay separate from incident and refund functions', async ({page}) => {
  await setup(page, 'Thanh toán');
  await expect(page.locator('.recovery-list button').filter({hasText:'PAY-'})).toHaveCount(2);
  await page.getByLabel('Loại thanh toán').selectOption('goods');
  await expect(page.locator('.recovery-list button').filter({hasText:'PAY-'})).toHaveCount(1);
  await expect(page.locator('.recovery-list')).toContainText('PAY-HÀNG');
  await page.getByLabel('Trạng thái thanh toán').selectOption('pending');
  await expect(page.getByText('Không có giao dịch cần kiểm tra.')).toBeVisible();
  await expect(page.getByRole('navigation',{name:'Chức năng xử lý vé'})).toHaveCount(0);
  await page.getByRole('navigation',{name:'Điều hướng chính'}).getByRole('button',{name:'Đổi & hoàn vé',exact:true}).click();
  await page.getByRole('button',{name:'Hoàn tiền',exact:true}).click();
  await page.getByLabel('Trạng thái hoàn tiền').selectOption('completed');
  await expect(page.getByLabel('Trạng thái thanh toán')).toHaveCount(0);
  await expect(page.locator('.ticket-operations')).not.toContainText('Hoàn vé #41');
});

test('Manager navigation fits viewport and resets content position', async ({page}) => {
  await setup(page, 'Quản lý buổi chơi');
  const nav = page.locator('.nav-inner');
  expect(await nav.evaluate(el=>el.getBoundingClientRect().height)).toBeLessThanOrEqual(768);
  await page.evaluate(()=>window.scrollTo(0,document.body.scrollHeight));
  await page.getByRole('navigation',{name:'Điều hướng chính'}).getByRole('button',{name:'Thanh toán',exact:true}).click();
  await expect(page.getByLabel('Loại thanh toán')).toBeVisible();
  expect(await page.evaluate(()=>window.scrollY)).toBe(0);
});
