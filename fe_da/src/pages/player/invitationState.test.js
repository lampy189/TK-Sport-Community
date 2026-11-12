import test from 'node:test';
import assert from 'node:assert/strict';
import { invitationState, invitationPayable } from './invitationState.js';
const now = Date.parse('2026-11-12T08:00:00Z');
const invitation = { status: 'pending', expiresAt: '2026-11-12T09:00:00Z', payerUserId: 10, orderId: 1, paymentStatus: 'Pending', paymentDeadline: '2026-11-12T08:15:00Z' };
test('pending invitations expire at their exact deadline, accepted responses stay accepted', () => {
  assert.equal(invitationState(invitation, now), 'pending');
  assert.equal(invitationState(invitation, now + 3600000), 'expired');
  assert.equal(invitationState({ ...invitation, status: 'accepted' }, now + 3600000), 'accepted');
});
test('only the chosen payer can pay an accepted live hold', () => {
  assert.equal(invitationPayable(invitation, 10, now), false);
  const accepted = { ...invitation, status: 'accepted' };
  assert.equal(invitationPayable(accepted, '10', now), true);
  assert.equal(invitationPayable(accepted, 11, now), false);
  assert.equal(invitationPayable(accepted, 10, now + 900000), false);
  assert.equal(invitationPayable({ ...accepted, paymentStatus: 'Paid' }, 10, now), false);
  assert.equal(invitationPayable({ ...accepted, orderId: null }, 10, now), false);
});
