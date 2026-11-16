import test from 'node:test';
import assert from 'node:assert/strict';
import { accounts, centers, defaultRange, report, transactions, vietnamDate } from './demoData.js';

const owner = accounts[0];
const read = (account, center, source) => report(account, center, defaultRange.from, defaultRange.to, source);
test('chain total equals the sum of each center, with refunds subtracted once', () => {
  const chain = read(owner, 'all');
  assert.equal(chain.totals.social, 24160000);
  assert.equal(chain.totals.goods, 5145000);
  assert.equal(chain.totals.refunds, 240000);
  assert.equal(chain.totals.net, 29065000);
  assert.equal(chain.unresolved, 2);
  for (const key of Object.keys(chain.totals))
    assert.equal(chain.totals[key], centers.reduce((sum, center) => sum + read(owner, center.id).totals[key], 0));
});
test('staff and managers cannot request another center or the entire chain', () => {
  for (const account of accounts.slice(1)) {
    assert.throws(() => read(account, 'all'));
    for (const center of centers.filter(item => item.id !== account.centerId)) assert.throws(() => read(account, center.id));
    assert.ok(read(account, account.centerId).transactions.every(row => row.centerId === account.centerId));
  }
  assert.throws(() => read({ role: 'SystemAdmin', centerId: null }, 'all'));
  assert.throws(() => read(owner, 'unknown'));
});
test('refund is recognized on completion day, not original receipt day', () => {
  const value = read(owner, 'SC01');
  assert.equal(value.days.find(day => day.date === '2026-10-30').refunds, 0);
  assert.equal(value.days.find(day => day.date === '2026-11-01').refunds, 80000);
});
test('Vietnam date handles UTC midnight boundaries', () => {
  assert.equal(vietnamDate('2026-11-04T17:00:00Z'), '2026-11-05');
  assert.equal(vietnamDate('2026-11-04T16:59:59Z'), '2026-11-04');
});
test('empty periods have zero-valued daily rows; invalid dates are rejected', () => {
  assert.equal(report(owner, 'all', '2026-12-01', '2026-12-03').days.length, 3);
  assert.equal(report(owner, 'all', '2026-12-01', '2026-12-03').totals.net, 0);
  for (const [from, to] of [['', '2026-11-05'], ['2026-11-05', '2026-11-01'], ['2026-02-30', '2026-03-05']])
    assert.throws(() => report(owner, 'all', from, to));
});
test('inter-center event has distinct receipts and no duplicate transaction IDs', () => {
  assert.equal(new Set(transactions.map(row => row.id)).size, transactions.length);
  const cup = transactions.filter(row => row.eventId === 'CUP-DEMO');
  assert.equal(read(owner, 'all', cup).totals.social, 4000000);
  assert.equal(read(accounts[1], 'SC01', cup).totals.social, 2400000);
});
