const statuses = {
  submitted: 'Chờ duyệt', pending: 'Chờ xử lý', approved: 'Đã duyệt', rejected: 'Đã từ chối',
  executed: 'Đã thực hiện', closed: 'Đã đóng hồ sơ', completed: 'Đã hoàn tất', failed: 'Xử lý thất bại',
  processing: 'Đang xử lý', open: 'Chưa nhận xử lý', in_progress: 'Đang xử lý',
  waiting_bank: 'Chờ ngân hàng / đối soát', waiting_approval: 'Chờ phê duyệt', resolved: 'Đã xử lý',
  requested: 'Chờ duyệt', paid: 'Đã thanh toán', review_required: 'Cần đối soát',
  cancelled: 'Đã hủy', expired: 'Hết hạn', matched: 'Đã đối chiếu', refunded: 'Đã hoàn tiền',
};
const roles = { owner: 'Chủ cơ sở', manager: 'Quản lý', staff: 'Nhân viên', referee: 'Trọng tài', player: 'Người chơi', systemadmin: 'Quản trị hệ thống' };
const categories = { other: 'Ngoại lệ khác', stock_adjustment: 'Điều chỉnh tồn kho', court: 'Sự cố sân', payment_mismatch: 'Thanh toán chưa khớp', funding: 'Nguồn tiền chưa phân bổ', refund_funds: 'Thiếu quỹ / lỗi hoàn tiền', return_unallocated: 'Hoàn khoản thu chưa phân bổ' };
const kinds = { venues: 'Cơ sở', sessions: 'Buổi chơi', inbox: 'Báo cáo người chơi', reviews: 'Đối soát thanh toán', refunds: 'Hoàn tiền', products: 'Hàng hóa' };
const actions = {
  operational_requested: 'Đề xuất phương án xử lý', operational_submitted: 'Gửi phương án xử lý',
  operational_approved: 'Chấp thuận phương án', operational_rejected: 'Từ chối phương án',
  operational_executed: 'Thực hiện phương án', operational_closed: 'Đóng hồ sơ', operational_return_verified: 'Xác minh khoản chuyển trả',
  issue_open: 'Mở lại sự cố', issue_in_progress: 'Nhận / phân công xử lý', issue_waiting_bank: 'Chờ ngân hàng / đối soát', issue_waiting_approval: 'Chờ phê duyệt', issue_resolved: 'Đóng sự cố',
  refund_decision: 'Phê duyệt khoản hoàn', refund_rejected: 'Từ chối khoản hoàn', refund_payout: 'Ghi nhận chứng từ chuyển tiền', refund_completed: 'Xác minh và hoàn tất khoản hoàn', refund_failed: 'Ghi nhận lỗi hoàn tiền',
};
const labels = {
  reason: 'Lý do', note: 'Ghi nhận xử lý', decision: 'Quyết định / kết quả', evidence: 'Bằng chứng',
  verification: 'Kết quả xác minh', document: 'Chứng từ', content: 'Nội dung', message: 'Thông tin',
  status: 'Trạng thái', category: 'Loại ngoại lệ', kind: 'Đối tượng', basis: 'Vai trò người duyệt', requesterrole: 'Vai trò người đề nghị',
  amountvnd: 'Số tiền', quantitydelta: 'Số lượng điều chỉnh', productid: 'Mặt hàng', recipientid: 'Người nhận',
  requestedby: 'Người đề nghị', decidedby: 'Người quyết định', approvedby: 'Người duyệt', recordedby: 'Người ghi chứng từ',
  assignedto: 'Người phụ trách', assignedname: 'Tên người phụ trách', banktransactionid: 'Giao dịch tiền vào',
  bankreference: 'Mã giao dịch ngân hàng', reference: 'Mã tham chiếu', paidat: 'Thời điểm chuyển tiền',
  expected: 'Số tiền cần thu', received: 'Số tiền thực nhận', result: 'Kết quả', payout: 'Chứng từ chuyển tiền', request: 'Đề xuất',
};
const looksStructured = text => /^[{["]/.test(text) && !/^\[[\p{L}][^\]]*\](?:\s|$)/u.test(text);
const fallback = 'Chi tiết chưa đọc được. Vui lòng tải lại hoặc liên hệ người phụ trách.';
// Accept historical text, JSON objects and double-encoded audit strings. Never dump unknown fields.
export function parseAudit(value) {
  for (let i = 0; i < 4 && typeof value === 'string'; i++) {
    const text = value.trim();
    if (!text) return null;
    try { value = JSON.parse(text); }
    catch { return looksStructured(text) ? null : text.replace(/\\u([0-9a-f]{4})/gi, (_, code) => String.fromCharCode(parseInt(code, 16))); }
  }
  return typeof value === 'string' && looksStructured(value.trim()) ? null : value;
}
export function auditTime(value) {
  const date = value == null ? null : new Date(value);
  return date && Number.isFinite(date.getTime()) ? date.toLocaleString('vi-VN', { timeZone: 'Asia/Ho_Chi_Minh' }) : '';
}
export const roleLabel = value => roles[String(value).toLowerCase()] || 'Chưa xác định';
export const auditAction = value => actions[value] || 'Cập nhật xử lý';
export function auditFields(value, depth = 0) {
  const data = parseAudit(value);
  if (data == null || depth > 4) return [];
  if (typeof data !== 'object') return [{ label: 'Nội dung', value: String(data) }];
  if (Array.isArray(data)) return data.slice(0, 20).flatMap(item => auditFields(item, depth + 1));
  return Object.entries(data).flatMap(([key, raw]) => {
    const name = key.toLowerCase();
    if (!labels[name] || raw == null || raw === '') return [];
    const decoded = parseAudit(raw);
    if (decoded == null) return [];
    if (typeof decoded === 'object') return auditFields(decoded, depth + 1).map(field => ({ ...field, label: `${labels[name]} · ${field.label}` }));
    let text = String(decoded);
    if (name === 'status') text = statuses[text.toLowerCase()] || 'Chưa xác định';
    else if (name === 'kind') text = kinds[text] || 'Nghiệp vụ khác';
    else if (name === 'category') text = categories[text] || 'Ngoại lệ khác';
    else if (name === 'basis' || name === 'requesterrole') text = roleLabel(text);
    else if (['amountvnd', 'expected', 'received'].includes(name)) text = Number.isFinite(Number(decoded)) ? `${Number(decoded).toLocaleString('vi-VN')} ₫` : 'Chưa xác định';
    else if (name === 'paidat') text = auditTime(decoded);
    if (!text) return [];
    return [{ label: labels[name], value: text }];
  });
}
export function auditText(value) {
  const parsed = parseAudit(value);
  if (typeof parsed === 'string') return parsed;
  const fields = auditFields(parsed);
  return fields.length ? fields.map(f => `${f.label}: ${f.value}`).join(' · ') : fallback;
}
export function describeAudit(entry) {
  const payload = parseAudit(entry.newValue ?? entry.detail);
  const fields = auditFields(payload);
  const get = key => payload && typeof payload === 'object' ? Object.entries(payload).find(([k]) => k.toLowerCase() === key)?.[1] : null;
  return { title: auditAction(entry.action), fields: fields.length ? fields : [{ label: 'Thông tin', value: fallback }], at: auditTime(get('at') || get('updatedat') || get('requestedat')) };
}
