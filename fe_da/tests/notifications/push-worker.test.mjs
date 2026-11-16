import test from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import vm from 'node:vm';
const source = await readFile(new URL('../../public/firebase-messaging-sw.js', import.meta.url), 'utf8');
function worker() {
  const handlers = {}, shown = [], visited = [], entries = new Map();
  const client = { url:'https://app.test/player/home', navigate:async url=>visited.push(url), focus:async()=>{} };
  const self = { location:{origin:'https://app.test'}, addEventListener:(name,fn)=>handlers[name]=fn,
    registration:{ showNotification:async (title,options)=>shown.push({title,...options}) },
    clients:{matchAll:async()=>[client],openWindow:async url=>visited.push(url)} };
  const cache = {match:async key=>entries.get(key.url),put:async(key,value)=>entries.set(key.url,value),keys:async()=>[...entries.keys()].map(k=>new Request(k)),delete:async key=>entries.delete(key.url)};
  vm.runInNewContext(source,{self,caches:{open:async()=>cache},Request,Response,URL});
  return {shown,visited, async push(data) {let pending; handlers.push({data:{json:()=>({data})},waitUntil:p=>pending=p});await pending;}, async click(notification) {let pending; handlers.notificationclick({notification:{...notification,close(){}},waitUntil:p=>pending=p});await pending;} };
}
test('concurrent duplicate event shows once and opens exact ticket',async()=>{
 const w=worker(),data={eventId:'10',title:'Ticket',body:'Confirmed',url:'https://app.test/player/home?view=tickets&ticketId=42'};
 await Promise.all([w.push(data),w.push(data)]);
 assert.equal(w.shown.length,1);
 await w.click(w.shown[0]); assert.equal(w.visited[0],data.url);
});
test('order notification navigates to the exact order',async()=>{
 const w=worker(),url='https://app.test/player/home?view=goods&goodsOrderId=82';
 await w.push({eventId:'11',title:'Order',url}); await w.click(w.shown[0]); assert.equal(w.visited[0],url);
});
test('foreign environment is never displayed or opened',async()=>{
 const w=worker(); await w.push({eventId:'12',title:'Wrong environment',url:'https://other.test/player/home'});
 assert.equal(w.shown.length,0); await w.click({data:{url:'https://other.test'}}); assert.equal(w.visited.length,0);
});
