export function invitationState(item, now = Date.now()) {
  if (item.status === 'pending' && new Date(item.expiresAt).getTime() <= now) return 'expired';
  return item.status;
}
export function invitationPayable(item, userId, now = Date.now()) {
  return item.status === 'accepted' && Number(item.payerUserId) === Number(userId) && item.orderId > 0
    && String(item.paymentStatus).toLowerCase() === 'pending' && new Date(item.paymentDeadline).getTime() > now;
}
