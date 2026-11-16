import { chromium } from '@playwright/test';
import { readFile } from 'node:fs/promises';
const root = new URL('../../../docs/evidence/duc-nov-07-10/', import.meta.url);
const browser = await chromium.launch({ channel:'chrome', headless:true });
try {
 const page = await browser.newPage({viewport:{width:1280,height:900}});
 await page.setContent(await readFile(new URL('results.html',root),'utf8'));
 await page.screenshot({path:new URL('09-test-results.png',root).pathname,fullPage:true});
} finally { await browser.close(); }
