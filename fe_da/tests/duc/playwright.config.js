import { fileURLToPath } from 'node:url';
import { env } from 'node:process';
import { defineConfig } from '@playwright/test';
export default defineConfig({
 testDir:'.',testMatch:'**/*.spec.js',workers:1,
 reporter:[['list'],['json',{outputFile:env.DUC_UI_RESULTS || '../../../docs/evidence/duc-nov-07-10/ui-results.json'}]],
 outputDir:'../../test-results/duc',
 webServer:{command:'npm run dev -- --host 127.0.0.1',url:'http://127.0.0.1:5173',reuseExistingServer:true,cwd:fileURLToPath(new URL('../..',import.meta.url))},
 use:{baseURL:'http://127.0.0.1:5173',channel:'chrome',headless:true,viewport:{width:1280,height:960}},
});
