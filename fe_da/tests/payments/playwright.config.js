import { fileURLToPath } from 'node:url';
import { defineConfig } from '@playwright/test';
export default defineConfig({testDir:'.',testMatch:'**/*.spec.js',workers:1,reporter:'list',outputDir:'../../test-results/payments',
 webServer:{command:'npm run dev -- --host 127.0.0.1 --port 5184 --strictPort',url:'http://127.0.0.1:5184',reuseExistingServer:false,cwd:fileURLToPath(new URL('../..',import.meta.url))},
 use:{baseURL:'http://127.0.0.1:5184',channel:'chrome',headless:true,viewport:{width:1280,height:960}}});
