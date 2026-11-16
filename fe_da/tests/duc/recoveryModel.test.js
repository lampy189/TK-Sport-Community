import test from 'node:test';
import assert from 'node:assert/strict';
import { historyDetails } from '../../src/pages/payment/recoveryModel.js';
test('recovery history preserves gateway failure as a visible reason',()=>assert.equal(historyDetails({detail:JSON.stringify({result:'Gateway unavailable',evidence:'Bank checked'})}).reason,'Gateway unavailable'));
test('recovery history retains legacy text evidence',()=>assert.equal(historyDetails({detail:'Legacy bank reconciliation note'}).evidence,'Legacy bank reconciliation note'));
test('recovery history retains structured review outcome',()=>assert.equal(historyDetails({detail:JSON.stringify({result:{reason:'Session full',status:'review_required'}})}).reason,'Session full'));
