import { defineConfig } from '@playwright/test';
import { fileURLToPath } from 'node:url';
export default defineConfig({testDir:'.',testMatch:'**/*.spec.js',workers:1,reporter:'list',outputDir:'../../test-results/owner',webServer:{command:'npm run dev -- --host 127.0.0.1 --port 5196',url:'http://127.0.0.1:5196',reuseExistingServer:false,cwd:fileURLToPath(new URL('../..',import.meta.url))},use:{baseURL:'http://127.0.0.1:5196',channel:'chrome',headless:true,viewport:{width:1280,height:960}}});
