import test from 'node:test';
import assert from 'node:assert/strict';
import { paymentState, ticketState } from './ticketState.js';
const now=Date.parse('2026-10-29T10:00:00Z');
const held={status:'Pending',orderStatus:'Pending',expiresAt:'2026-10-29T10:15:00Z',sessionDate:'2026-10-28',startTime:'18:00:00',endTime:'20:00:00'};
test('payable orders always appear in waiting tickets, even with stale session dates',()=>{
 for(const paymentStatus of [null,'pending','failed']) {
  const t={...held,paymentStatus};
  assert.ok(['pending','processing','failed'].includes(paymentState(t.orderStatus,paymentStatus,t.expiresAt,now)));
  assert.equal(ticketState(t,now),'pending');
 }
});
test('expired and review orders agree across both screens',()=>{
 for(const t of [{...held,expiresAt:'2026-10-29T09:00:00Z'},{...held,paymentStatus:'review_required'}])
  assert.equal(ticketState(t,now),paymentState(t.orderStatus,t.paymentStatus,t.expiresAt,now));
});
test('paid ticket remains usable during the session, ends at end time',()=>{
 const t={...held,status:'confirmed',orderStatus:'paid',valid:true,sessionDate:'2026-10-29',startTime:'16:00:00',endTime:'18:00:00'};
 assert.equal(ticketState(t,now),'confirmed');
 assert.equal(ticketState(t,Date.parse('2026-10-29T11:00:00Z')),'completed');
});
test('cancelled and checked-in tickets never expose admission QR again',()=>{
 assert.equal(ticketState({...held,status:'cancelled',valid:true,orderStatus:'paid'},now),'cancelled');
 assert.equal(ticketState({...held,status:'checkedin',checkedIn:true,orderStatus:'paid'},now),'checkedin');
});
