import { test, expect } from '@playwright/test';
import { mkdir } from 'node:fs/promises';
import { DIMENSION_ANCHORS } from '../../src/pages/skill/dimensionAnchors.js';
const evidence='../docs/evidence/ui-his-qa-20261116';
const criteria=Object.keys(DIMENSION_ANCHORS).filter(k=>k.startsWith('BM_')).slice(0,5).map((code,i)=>({id:i+1,code,name:code,weightPercent:20,maxScore:4}));
const form={id:1,status:'draft',revision:1,playerName:'Người chơi kiểm thử',rubric:{name:'BM-CLB-V1',scoringSupported:true,criteria},marks:criteria.map(c=>({dimensionId:c.id,score:2,sliderScores:[5,5,5,5],evidence:''}))};
for(const width of [1440,390,320]) test(`assessment large controls at ${width}px`,async({page})=>{
  await page.setViewportSize({width,height:900});
  await page.route('**/api/**',route=>{
    const path=new URL(route.request().url()).pathname;
    return route.fulfill({json:path.endsWith('/csrf')?{token:'fixture'}:path.endsWith('/sessions')?[{id:1,name:'Cầu lông',court:'Sân kiểm thử',sessionDate:'2026-11-16',startTime:'18:00'}]:path.endsWith('/attendees')?[{playerId:1,name:'Người chơi kiểm thử',profileId:1}]:form});
  });
  await page.goto('/tests/duc/?screen=assessment');
  await page.getByLabel('Buổi được phân công').selectOption('1');
  await page.getByRole('button',{name:/Người chơi kiểm thử.*Chưa chấm/}).click();
  const slider=page.getByRole('slider').first();
  await expect(slider).toBeVisible();
  await expect(slider).toHaveValue('5');
  await slider.focus(); await slider.press('ArrowRight'); await expect(slider).toHaveValue('6');
  expect((await slider.boundingBox()).height).toBeGreaterThanOrEqual(44);
  const ticks=page.locator('.assessment-tick-btn');
  for(const box of await ticks.evaluateAll(nodes=>nodes.map(n=>({w:n.getBoundingClientRect().width,h:n.getBoundingClientRect().height})))){
    expect(box.w).toBeGreaterThanOrEqual(43);expect(box.h).toBeGreaterThanOrEqual(44);
  }
  expect(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth)).toBe(true);
  await mkdir(evidence,{recursive:true});await page.screenshot({path:`${evidence}/assessment-${width}.png`,fullPage:false});
});
test('ticket history filters type, payer and refund without overflow',async({page})=>{
  await page.setViewportSize({width:390,height:844});
  await page.addInitScript(()=>{window.__tickets=[
    {id:1,orderId:1,orderCode:'SOC-TEST',status:'cancelled',ticketKind:'remaining',holderName:'Người giữ (fixture)',payerName:'Người trả (fixture)',paidForOther:true,amountVnd:40000,refundStatus:'processing',refundAmountVnd:40000,payments:[{id:1,referenceCode:'FIXTURE-REFERENCE',amountVnd:40000,status:'confirmed'}],history:[{sequence:1,label:'Đã hủy vé'},{sequence:2,label:'Đã ghi chứng từ tiền ra, chờ xác minh'}]},
    {id:2,orderId:2,status:'cancelled',ticketKind:'social',holderName:'Người khác (fixture)',amountVnd:50000}
  ];});
  await page.route('**/api/**',r=>r.fulfill({json:[]}));
  await page.goto('/tests/duc/?screen=ticket-history');
  await expect(page.locator('.ticket-stub')).toHaveCount(2);
  for(const kind of ['remaining','refund']){await page.getByLabel('Loại vé').selectOption(kind);await expect(page.locator('.ticket-stub')).toHaveCount(1);}
  await page.getByRole('button',{name:'Chi tiết vé'}).click();
  await expect(page.getByLabel('Diễn biến trạng thái')).toContainText('chờ xác minh');
  await expect(page.getByLabel('Chứng từ thanh toán')).toContainText('FIXTURE-REFERENCE');
  expect(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth)).toBe(true);
  await mkdir(evidence,{recursive:true});await page.screenshot({path:`${evidence}/ticket-history-mobile.png`,fullPage:true});
});
test('goods history handles network failure then retry and terminal statuses',async({page})=>{
  let failed=true;
  await page.route('**/api/**',route=>failed?route.abort('internetdisconnected'):route.fulfill({json:[{id:1,orderCode:'GOODS-FIXTURE',status:'Cancelled',paymentState:'Cancelled',lines:[],totalAmount:1000}]}));
  await page.goto('/tests/duc/?screen=goods-history');
  await expect(page.getByRole('alert')).toBeVisible();failed=false;
  await page.getByRole('button',{name:'Cập nhật'}).click();
  await expect(page.locator('.my-order-list')).toContainText('Đã hủy');
  await expect(page.locator('.my-order-list')).not.toContainText('Chờ thanh toán');
});
