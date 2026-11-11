export const norm = value => String(value || '').trim().toLowerCase();
export const sessionTime = (ticket, field = 'startTime') => ticket.sessionDate && ticket[field]
  ? Date.parse(`${ticket.sessionDate}T${ticket[field]}+07:00`) : NaN;
export function paymentState(orderStatus, paymentStatus, expiresAt, now) {
  const order = norm(orderStatus), payment = norm(paymentStatus);
  if (order !== 'pending') return order;
  if (payment === 'review_required') return 'review_required';
  if (!expiresAt || !Number.isFinite(Date.parse(expiresAt)) || Date.parse(expiresAt) <= now) return 'expired';
  if (payment === 'failed') return 'failed';
  return payment === 'pending' ? 'processing' : 'pending';
}
export function ticketState(ticket, now) {
  const status = norm(ticket.status);
  if (['cancelled', 'expired', 'invalidated', 'exchanged'].includes(status)) return status;
<<<<<<< HEAD
=======
  if (ticket.ticketKind === 'remaining' && ticket.validUntil && Date.parse(ticket.validUntil) <= now) return 'completed';
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
  if (ticket.checkedIn || status === 'checkedin') return 'checkedin';
  if (['suspended','cancelled'].includes(norm(ticket.sessionStatus))) return 'venue_issue';
  const payment = paymentState(ticket.orderStatus, ticket.paymentStatus, ticket.expiresAt, now);
  // A pending checkout stays in the pending-ticket filter even with an open/failed payment attempt.
  if (['pending', 'processing', 'failed'].includes(payment)) return 'pending';
  if (payment !== 'paid') return payment;
  if (sessionTime(ticket, 'endTime') <= now) return 'completed';
  return ticket.valid ? 'confirmed' : 'processing';
}
