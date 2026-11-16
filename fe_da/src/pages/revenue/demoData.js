// Synthetic fixtures only. Never sent to the API or used for production authorization.
export const centers = [
  { id: 'SC01', name: 'TK Sport · Thủ Đức' },
  { id: 'SC02', name: 'TK Sport · Bình Thạnh' },
  { id: 'SC03', name: 'TK Sport · Quận 7' },
];
export const accounts = [
  { id: 'owner', name: 'Tổng bộ', role: 'Owner', centerId: null },
  { id: 'manager-1', name: 'Quản lý Thủ Đức', role: 'Manager', centerId: 'SC01' },
  { id: 'staff-2', name: 'Nhân viên Bình Thạnh', role: 'Staff', centerId: 'SC02' },
  { id: 'manager-3', name: 'Quản lý Quận 7', role: 'Manager', centerId: 'SC03' },
];
export const defaultRange = { from: '2026-10-30', to: '2026-11-05' };
const receipts = [];
for (let day = 0; day < 7; day++) {
  const date = new Date(Date.UTC(2026, 9, 30 + day)).toISOString().slice(0, 10);
  centers.forEach((center, index) => {
    receipts.push(
      { id: `SOC-${day}-${index}`, centerId: center.id, kind: 'social', amount: 600000 + day * 80000 + index * 120000, occurredAt: `${date}T18:15:00+07:00`, status: 'confirmed', note: 'Thu vé buổi chơi' },
      { id: `GOODS-${day}-${index}`, centerId: center.id, kind: 'goods', amount: 150000 + day * 20000 + index * 35000, occurredAt: `${date}T20:30:00+07:00`, status: 'confirmed', note: 'Bán cầu, bóng và nước' },
    );
  });
}
export const transactions = [
  ...receipts,
  { id: 'REF-01', sourceId: 'SOC-0-0', centerId: 'SC01', kind: 'refund', amount: 80000, occurredAt: '2026-11-01T10:00:00+07:00', status: 'completed', note: 'Hoàn vé đã thu ngày 30/10' },
  { id: 'REF-02', sourceId: 'SOC-3-1', centerId: 'SC02', kind: 'refund', amount: 160000, occurredAt: '2026-11-04T09:00:00+07:00', status: 'completed', note: 'Hoàn hai vé' },
  { id: 'CUP-01', centerId: 'SC01', kind: 'social', amount: 2400000, occurredAt: '2026-11-03T08:00:00+07:00', status: 'confirmed', eventId: 'CUP-DEMO', note: 'Giải giao hữu liên cơ sở · phần thu Thủ Đức' },
  { id: 'CUP-02', centerId: 'SC03', kind: 'social', amount: 1600000, occurredAt: '2026-11-03T08:05:00+07:00', status: 'confirmed', eventId: 'CUP-DEMO', note: 'Giải giao hữu liên cơ sở · phần thu Quận 7' },
  { id: 'PENDING-01', centerId: 'SC02', kind: 'social', amount: 320000, occurredAt: '2026-11-05T10:00:00+07:00', status: 'pending', note: 'Chưa xác nhận chuyển khoản' },
  { id: 'REF-PENDING', sourceId: 'SOC-5-2', centerId: 'SC03', kind: 'refund', amount: 80000, occurredAt: '2026-11-05T11:00:00+07:00', status: 'requested', note: 'Yêu cầu hoàn tiền, chưa chi' },
];

export function vietnamDate(value) {
  return new Date(new Date(value).getTime() + 7 * 60 * 60 * 1000).toISOString().slice(0, 10);
}
export function allowedCenters(account) {
  return account.role === 'Owner' ? centers : centers.filter(center => center.id === account.centerId);
}
export function report(account, centerId, from, to, source = transactions) {
  if (!account || !allowedCenters(account).some(center => center.id === centerId) && !(account.role === 'Owner' && centerId === 'all'))
    throw new Error('Bạn không có quyền xem Sport Center này.');
  const validDate = value => /^\d{4}-\d{2}-\d{2}$/.test(value) && Number.isFinite(Date.parse(value)) && new Date(value).toISOString().slice(0, 10) === value;
  if (!validDate(from) || !validDate(to) || from > to || Date.parse(to) - Date.parse(from) > 365 * 86400000)
    throw new Error('Chọn khoảng ngày hợp lệ, tối đa 366 ngày.');
  const visible = source.filter(row => (centerId === 'all' || row.centerId === centerId) && vietnamDate(row.occurredAt) >= from && vietnamDate(row.occurredAt) <= to);
  const posted = visible.filter(row => row.kind === 'refund' ? row.status === 'completed' : row.status === 'confirmed');
  const days = [];
  for (let day = Date.parse(from); day <= Date.parse(to); day += 86400000) {
    const date = new Date(day).toISOString().slice(0, 10);
    const values = posted.filter(row => vietnamDate(row.occurredAt) === date);
    const sum = kind => values.filter(row => row.kind === kind).reduce((total, row) => total + row.amount, 0);
    const social = sum('social'), goods = sum('goods'), refunds = sum('refund');
    days.push({ date, social, goods, refunds, net: social + goods - refunds });
  }
  const totals = days.reduce((sum, row) => ({ social: sum.social + row.social, goods: sum.goods + row.goods, refunds: sum.refunds + row.refunds, net: sum.net + row.net }), { social: 0, goods: 0, refunds: 0, net: 0 });
  return { days, totals, transactions: visible, unresolved: visible.length - posted.length };
}
