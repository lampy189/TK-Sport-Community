import { test } from 'node:test';
import assert from 'node:assert/strict';
import { canCheckIn, hasEnded, venueToday } from '../../src/pages/sessions/sessionTime.js';

test('Vietnam date crosses midnight independently of host timezone', () => {
  assert.equal(venueToday(Date.parse('2026-11-23T17:00:00Z')), '2026-11-24');
});
test('attendance permits start but excludes exact end and suspended sessions', () => {
  const session = {sessionDate:'2026-11-23',startTime:'18:00:00',endTime:'20:00:00',sessionStatus:'Open',conditionCode:'not_ready'};
  assert.equal(canCheckIn(session, Date.parse('2026-11-23T17:59:59+07:00')), false);
  assert.equal(canCheckIn(session, Date.parse('2026-11-23T18:00:00+07:00')), true);
  assert.equal(canCheckIn(session, Date.parse('2026-11-23T20:00:00+07:00')), false);
  assert.equal(hasEnded(session, Date.parse('2026-11-23T20:00:00+07:00')), true);
  assert.equal(canCheckIn({...session,sessionStatus:'suspended'}, Date.parse('2026-11-23T19:00:00+07:00')), false);
});
