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
  if (ticket.ticketKind === 'remaining' && ticket.validUntil && Date.parse(ticket.validUntil) <= now) return 'completed';
  if (ticket.checkedIn || status === 'checkedin') return 'checkedin';
  if (['suspended','cancelled'].includes(norm(ticket.sessionStatus))) return 'venue_issue';
  const payment = paymentState(ticket.orderStatus, ticket.paymentStatus, ticket.expiresAt, now);
  // A pending checkout stays in the pending-ticket filter even with an open/failed payment attempt.
  if (['pending', 'processing', 'failed'].includes(payment)) return 'pending';
  if (payment !== 'paid') return payment;
  if (sessionTime(ticket, 'endTime') <= now) return 'completed';
  return ticket.valid ? 'confirmed' : 'processing';
}

const paidCancelWindow = 12 * 60 * 60 * 1000;
const cancellationDeadline = new Intl.DateTimeFormat('vi-VN', {
  timeZone: 'Asia/Ho_Chi_Minh', dateStyle: 'short', timeStyle: 'short',
});

export function ticketCancellation(ticket, now) {
  const status = ticketState(ticket, now);
  if (status !== 'pending' && status !== 'confirmed') return null;

  const startsAt = sessionTime(ticket);
  if (ticket.canManage === false) return { reason: 'Chỉ người thanh toán mới có thể hủy vé.' };
  if (ticket.pendingChangeId) return { reason: 'Hãy rút yêu cầu đổi vé đang chờ trước khi hủy.' };
  if (!Number.isFinite(startsAt)) return { reason: 'Chưa có giờ bắt đầu để xác định hạn hủy vé.' };
  if (startsAt <= now) return { reason: 'Buổi chơi đã bắt đầu. Vé không thể hủy.' };

  if (status === 'confirmed' && startsAt - now < paidCancelWindow) {
    const deadline = cancellationDeadline.format(startsAt - paidCancelWindow);
    return { reason: `Đã hết hạn hủy lúc ${deadline}. Vé đã thanh toán chỉ được hủy trước giờ chơi ít nhất 12 giờ.` };
  }
  return { reason: null };
}
