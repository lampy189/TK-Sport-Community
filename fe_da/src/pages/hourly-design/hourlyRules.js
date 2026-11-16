export const DEMO_NOW = 18 * 60 + 35;
export function clock(minutes) {
  return `${String(Math.floor(minutes / 60)).padStart(2, '0')}:${String(minutes % 60).padStart(2, '0')}`;
}
export function durationLabel(minutes) {
  const remaining = Math.max(0, minutes);
  return remaining >= 60 ? `${Math.floor(remaining / 60)} giờ${remaining % 60 ? ` ${remaining % 60} phút` : ''}` : `${remaining} phút`;
}
export function availableSeats(session) {
  return Math.max(0, session.capacity - session.playing - session.confirmed - session.holds);
}
export function offerProblem(session, seats, price, now = DEMO_NOW) {
  if (now < session.start || now >= session.end) return 'Chỉ rao chỗ trong buổi đang diễn ra.';
  if (!Number.isInteger(seats) || seats < 1 || seats > availableSeats(session)) return 'Số vé rao vượt quá số chỗ có thể bán.';
  if (!Number.isInteger(price) || price < 1000) return 'Nhập giá vé hợp lệ, từ 1.000đ.';
  return '';
}
export function purchaseProblem(session, offer, band, now = DEMO_NOW) {
  if (!offer?.active || offer.remaining < 1) return 'Vé đã ngừng bán hoặc hết chỗ.';
  if (now < session.start || now >= session.end) return 'Buổi chơi chưa bắt đầu hoặc đã kết thúc, không thể mua vé giờ lẻ.';
  if (availableSeats(session) < 1) return 'Chỗ cuối vừa được giữ. Vui lòng chọn buổi khác.';
  if (session.band !== band) return `Buổi này yêu cầu ${session.band}. Bạn đang ở ${band}.`;
  return '';
}
