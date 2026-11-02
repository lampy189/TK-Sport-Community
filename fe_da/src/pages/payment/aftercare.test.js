import test from 'node:test';
import assert from 'node:assert/strict';
import { ticketState } from './ticketState.js';
for (const sessionStatus of ['suspended','cancelled']) test(`disrupted ${sessionStatus} session never exposes valid QR`,()=>{
 assert.equal(ticketState({status:'confirmed',valid:true,orderStatus:'paid',sessionStatus},Date.now()),'venue_issue');
});
test('old exchanged ticket stays in history despite paid original order',()=>{
 assert.equal(ticketState({status:'exchanged',orderStatus:'paid',sessionStatus:'suspended'},Date.now()),'exchanged');
});
