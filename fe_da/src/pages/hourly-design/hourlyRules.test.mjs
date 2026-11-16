import test from 'node:test';
import assert from 'node:assert/strict';
import { availableSeats, offerProblem, purchaseProblem } from './hourlyRules.js';
const s={start:1080,end:1200,capacity:8,playing:4,confirmed:1,holds:1,band:'B2'};
test('paid no-shows and live holds both prevent resale of occupied capacity',()=>{
 assert.equal(availableSeats(s),2);
 assert.match(offerProblem(s,3,80000),/vượt/);
 assert.equal(availableSeats({...s,confirmed:3}),0);
});
test('remaining-time tickets can be sold with less than an hour; session end is the hard cutoff',()=>{
 assert.equal(offerProblem(s,2,80000,1140),'');
 assert.equal(offerProblem(s,2,80000,1141),'');
 assert.equal(offerProblem(s,2,80000,1199),'');
 assert.match(offerProblem(s,2,80000,1200),/đang diễn ra/);
 assert.match(offerProblem(s,2,80000,1079),/đang diễn ra/);
});
test('joining a running session does not waive skill or capacity checks',()=>{
 const offer={active:true,remaining:2,price:80000};
 assert.equal(purchaseProblem(s,offer,'B2'),'');
 assert.match(purchaseProblem(s,offer,'B3'),/B2/);
 assert.match(purchaseProblem({...s,holds:3},offer,'B2'),/Chỗ cuối/);
 assert.match(purchaseProblem(s,{...offer,active:false},'B2'),/ngừng/);
 assert.match(purchaseProblem(s,offer,'B2',1200),/kết thúc/);
});
