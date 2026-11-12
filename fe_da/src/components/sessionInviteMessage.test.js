import test from 'node:test';
import assert from 'node:assert/strict';
import { buildSessionInviteMessage, parseSessionInviteMessage } from './sessionInviteMessage.js';
test('real invitations carry their server ID while legacy shares do not pretend to record consent', () => {
  const session = { id: 12, sportCode: 'badminton', sessionDate: '2026-11-20', startTime: '16:00', endTime: '18:00', priceVnd: 100000 };
  assert.equal(parseSessionInviteMessage(buildSessionInviteMessage(session)).session.invitationId, null);
  assert.equal(parseSessionInviteMessage(buildSessionInviteMessage(session, 'Cùng chơi nhé', 42)).session.invitationId, 42);
});
test('malformed or object-valued card fields remain ordinary text and cannot crash chat rendering', () => {
  for (const payload of ['null', '[]', '{"id":1,"sport":{}}', '{"id":-1}', 'bad json']) {
    const content = `hello [TKSC_SESSION:${payload}]`;
    assert.deepEqual(parseSessionInviteMessage(content), { text: content, session: null });
  }
});
