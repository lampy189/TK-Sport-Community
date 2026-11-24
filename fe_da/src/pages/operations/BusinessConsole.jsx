import { ManagerList } from '../../components/ui/ManagerUI';
import { useEffect, useState } from 'react';
import { workflowRequest } from '../workflowClient';
import './operations.css';
import './business-console.css';

const money = n => Number(n ?? 0).toLocaleString('vi-VN') + ' ₫';
const time = value => value ? new Date(value).toLocaleString('vi-VN') : '—';
const status = value => ({ submitted: 'Chờ duyệt', approved: 'Đã duyệt', rejected: 'Từ chối', pending: 'Chờ duyệt', provisioned: 'Chờ kiểm tra', verified: 'Chờ bàn giao', executed: 'Hoàn tất' })[value] || value;
export function DataTable({ title, rows = [], columns }) {
  return <section className="business-section"><h2>{title}</h2>{!rows.length ? <p>Chưa có dữ liệu.</p> : <div className="business-table"><table><thead><tr>{columns.map(([key, label]) => <th key={key}>{label}</th>)}</tr></thead><tbody>{<ManagerList items={rows} columns={columns.length}>{(row, i) => <tr key={row.id ?? i}>{columns.map(([key, , format]) => <td key={key}>{format ? format(row[key], row) : String(row[key] ?? '—')}</td>)}</tr>}</ManagerList>}</tbody></table></div>}</section>;
}

export function PolicyPage({ role, user }) {
  const [rows, setRows] = useState([]), [options, setOptions] = useState(null), [venue, setVenue] = useState('');
  const [revision, setRevision] = useState(0), [error, setError] = useState(''), [notice, setNotice] = useState(''), [busy, setBusy] = useState(false), [loading, setLoading] = useState(true);
  const [decision, setDecision] = useState(null), [reason, setReason] = useState('');
  const fresh = () => ({ code: '', name: '', value: '', reason: '', kind: 'policy', targetId: '', priceVnd: '', effectiveFrom: '', effectiveTo: '' });
  const [form, setForm] = useState(fresh);
  useEffect(() => {
    let live = true;
    Promise.all([workflowRequest('/api/v1/owner-policies' + (venue ? `?venueId=${venue}` : '')), workflowRequest('/api/v1/owner-policies/options')])
      .then(([items, choices]) => { if (live) { setRows(items); setOptions(choices); setError(''); } })
      .catch(e => { if (live) setError(e.message); }).finally(() => { if (live) setLoading(false); });
    return () => { live = false; };
  }, [venue, revision]);
  const selectedVenue = venue || (options?.venues?.length === 1 ? String(options.venues[0].id) : '');
  const targets = (form.kind === 'product' ? options?.products : options?.sessions)?.filter(item => String(item.venueId) === selectedVenue) || [];
  async function run(path, body, done) {
    setBusy(true); setError(''); setNotice('');
    try { await workflowRequest('/api/v1/owner-policies' + path, 'POST', body); done?.(); setRevision(r => r + 1); setNotice('Đã lưu.'); }
    catch (e) { setError(e.message); } finally { setBusy(false); }
  }
  function submit(e) {
    e.preventDefault();
    if (!selectedVenue || !form.effectiveFrom) return;
    const effectiveFrom = new Date(form.effectiveFrom), effectiveTo = form.effectiveTo ? new Date(form.effectiveTo) : null;
    if (effectiveFrom <= new Date() || effectiveTo && effectiveTo <= effectiveFrom) { setError('Chọn thời gian hiệu lực trong tương lai và ngày kết thúc hợp lệ.'); return; }
    void run('', { ...form, venueId: Number(selectedVenue), targetId: form.kind === 'policy' ? null : Number(form.targetId), priceVnd: form.kind === 'policy' ? null : Number(form.priceVnd), effectiveFrom: effectiveFrom.toISOString(), effectiveTo: effectiveTo?.toISOString() ?? null }, () => setForm(fresh()));
  }
  return <section className="ops-page"><h1>Chính sách & giá</h1><div className="ops-actions"><label>Cơ sở<select value={venue} onChange={e => { setVenue(e.target.value); setForm(fresh()); }}>{role === 'owner' && <option value="">Tất cả cơ sở</option>}{options?.venues?.map(v => <option key={v.id} value={v.id}>{v.name}</option>)}</select></label>{(role !== 'manager' || error) && <button disabled={busy || loading} onClick={() => setRevision(r => r + 1)}>Tải lại</button>}</div>
    {error && <p role="alert">{error}</p>}{notice && <p role="status">{notice}</p>}{loading && <p role="status">Đang tải…</p>}
    {options && <details className="business-section"><summary>Đề xuất phiên bản mới</summary><form className="ops-form" onSubmit={submit}><p>Cơ sở: {options.venues.find(v => String(v.id) === selectedVenue)?.name || 'Chọn cơ sở ở bộ lọc phía trên'}</p>
      <label>Loại<select value={form.kind} onChange={e => setForm({ ...form, kind: e.target.value, targetId: '', priceVnd: '' })}><option value="policy">Chính sách</option><option value="product">Giá hàng hóa</option><option value="session">Giá buổi chơi</option></select></label>
      <label>Mã<input required pattern="[A-Za-z0-9_-]+" maxLength={100} value={form.code} onChange={e => setForm({ ...form, code: e.target.value })} /></label>
      <label>Tên<input required maxLength={255} value={form.name} onChange={e => setForm({ ...form, name: e.target.value })} /></label>
      {form.kind !== 'policy' && <><label>Áp dụng cho<select required value={form.targetId} onChange={e => setForm({ ...form, targetId: e.target.value })}><option value="">Chọn {form.kind === 'product' ? 'sản phẩm' : 'buổi chơi'}</option>{targets.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label><label>Giá (VND)<input required type="number" min="0" max="100000000" step="1" value={form.priceVnd} onChange={e => setForm({ ...form, priceVnd: e.target.value })} /></label></>}
      <label>Nội dung<textarea required maxLength={20000} value={form.value} onChange={e => setForm({ ...form, value: e.target.value })} /></label>
      <label>Hiệu lực từ<input required type="datetime-local" value={form.effectiveFrom} onChange={e => setForm({ ...form, effectiveFrom: e.target.value })} /></label><label>Đến ngày<input type="datetime-local" value={form.effectiveTo} onChange={e => setForm({ ...form, effectiveTo: e.target.value })} /></label>
      <label>Lý do<textarea required maxLength={1000} value={form.reason} onChange={e => setForm({ ...form, reason: e.target.value })} /></label><button disabled={busy || !selectedVenue}>Gửi đề xuất</button></form></details>}
    <DataTable title="Phiên bản và quyết định" rows={rows} columns={[
      ['code', 'Mã / phiên bản', (_, r) => `${r.code} / ${r.version}`], ['name', 'Tên'], ['kind', 'Loại', v => ({ policy: 'Chính sách', product: 'Giá hàng', session: 'Giá buổi' })[v]], ['priceVnd', 'Giá', v => v == null ? '—' : money(v)], ['effectiveFrom', 'Hiệu lực từ', time], ['effectiveTo', 'Đến', time], ['status', 'Trạng thái', status], ['value', 'Nội dung'], ['decisionReason', 'Quyết định'], ['id', 'Thao tác', (_, r) => role === 'owner' && r.status === 'submitted' ? Number(r.submittedBy) === Number(user?.id) ? 'Chờ Owner khác duyệt' : <button disabled={busy} onClick={() => { setDecision(r); setReason(''); }}>Xem xét</button> : '—'],
    ]} />
    {decision && <form className="ops-form" onSubmit={e => e.preventDefault()}><h2>Duyệt {decision.name} / v{decision.version}</h2><p>{decision.value}</p><p>Lý do đề xuất: {decision.submitReason}</p><label>Lý do quyết định<textarea required maxLength={1000} value={reason} onChange={e => setReason(e.target.value)} /></label><div className="ops-actions">{[true, false].map(approve => <button key={String(approve)} disabled={busy || !reason.trim()} onClick={() => run(`/${decision.id}/decision`, { approve, reason: reason.trim() }, () => setDecision(null))}>{approve ? 'Duyệt' : 'Từ chối'}</button>)}<button disabled={busy} onClick={() => setDecision(null)}>Đóng</button></div></form>}
  </section>;
}

export function BusinessReportPage() {
  const today = () => new Date(Date.now() + 7 * 3600000).toISOString().slice(0, 10);
  const [from, setFrom] = useState(today), [to, setTo] = useState(today), [venue, setVenue] = useState(''), [scope, setScope] = useState(null), [data, setData] = useState(null), [error, setError] = useState(''), [busy, setBusy] = useState(false);
  useEffect(() => { let live = true; workflowRequest('/api/v1/sport-centers').then(s => { if (live) { setScope(s); setVenue(s.canViewAll ? '' : String(s.assignedVenueId || '')); } }).catch(e => { if (live) setError(e.message); }); return () => { live = false; }; }, []);
  async function load(e) {
    e?.preventDefault(); setBusy(true); setError(''); setData(null);
    try { const q = new URLSearchParams({ from, to }); if (venue) q.set('venueId', venue); setData(await workflowRequest('/api/v1/business-reports?' + q)); }
    catch (e) { setError(e.message); } finally { setBusy(false); }
  }
  const r = data?.report;
  const centerName = id => scope?.centers?.find(v => v.id === id)?.name || ('#' + id);
  const productName = id => r?.inventory?.find(p => p.productId === id)?.product || ('#' + id);
  return <section className="ops-page"><h1>Báo cáo tổng hợp</h1><form className="ops-form business-filters" onSubmit={load}><label>Từ<input required type="date" value={from} onChange={e => setFrom(e.target.value)} /></label><label>Đến<input required type="date" min={from} value={to} onChange={e => setTo(e.target.value)} /></label><label>Cơ sở<select disabled={!scope?.canViewAll} value={venue} onChange={e => setVenue(e.target.value)}>{scope?.canViewAll && <option value="">Toàn chuỗi</option>}{scope?.centers?.map(v => <option value={v.id} key={v.id}>{v.name}</option>)}</select></label><button disabled={busy || !scope}>Xem báo cáo</button></form>{error && <p role="alert">{error}</p>}{busy && <p role="status">Đang tải…</p>}
    {r && <><div className="stats-grid">{[['Tiền vé', data.totals.social], ['Tiền hàng', data.totals.goods], ['Hoàn tiền', data.totals.refunds], ['Thu ròng', data.totals.net]].map(([label, value]) => <div className="stat-card" key={label}><strong>{money(value)}</strong><span>{label}</span></div>)}</div><button onClick={() => window.print()}>In báo cáo</button>{r.currentOutstanding.some(p => p.kind === "unposted_receipt") && <p role="alert">Có khoản đã thu chưa ghi nhận doanh thu. Số thu ròng hiện chưa bao gồm các khoản này; xem danh sách chờ xử lý bên dưới.</p>}
      <DataTable title="Doanh thu theo cơ sở" rows={r.centers} columns={[[ 'venue', 'Cơ sở'], ['social', 'Vé', money], ['goods', 'Hàng hóa', money], ['refunds', 'Hoàn', money], ['net', 'Thu ròng', money]]} />
      <DataTable title="Tồn kho" rows={r.inventory} columns={[[ 'venueId', 'Cơ sở', centerName], ['product', 'Mặt hàng'], ['opening', 'Đầu kỳ'], ['received', 'Nhập'], ['issued', 'Xuất'], ['closing', 'Cuối kỳ'], ['currentOnHand', 'Thực ghi hiện tại'], ['undatedMovements', 'Biến động thiếu ngày']]} />
      <DataTable title="Vật tư buổi chơi" rows={r.supplies} columns={[[ 'venueId', 'Cơ sở', centerName], ['sessionId', 'Buổi'], ['productId', 'Vật tư', productName], ['issued', 'Đã cấp'], ['returned', 'Đã trả'], ['reconciledConsumption', 'Đã dùng'], ['unresolved', 'Chưa đối chiếu']]} />
      <DataTable title="Đối soát ca đã ghi nhận" rows={r.reconciliations} columns={[[ 'venueId', 'Cơ sở', centerName], ['reconciliationId', 'Phiếu'], ['date', 'Ngày'], ['status', 'Trạng thái', status], ['expectedBank', 'Dự kiến', money], ['actualBank', 'Thực tế', money], ['difference', 'Chênh lệch', money]]} />
      <DataTable title="Khoản còn chờ xử lý hiện tại" rows={r.currentOutstanding} columns={[[ 'venueId', 'Cơ sở', centerName], ['paymentId', 'Giao dịch'], ['refundId', 'Khoản hoàn'], ['kind', 'Loại', v => ({ unposted_receipt: 'Đã thu, chưa ghi doanh thu', pending_refund: 'Hoàn tiền chờ xử lý' })[v] || v], ['amount', 'Số tiền', money]]} />
    </>}
  </section>;
}

const approvalGroups = [["policies", "Chính sách và giá", "/api/v1/owner-policies", "Chính sách & giá"], ["access", "Bổ nhiệm và quyền truy cập", "/api/operations/access", "Bổ nhiệm quản lý"], ["cases", "Điều chỉnh và yêu cầu vượt quyền", "/api/issue-workflow/cases", "Ngoại lệ vận hành"], ["refunds", "Hoàn tiền vé", "/api/ticket-aftercare/refunds", "Đổi & hoàn vé"]];
export function ApprovalHub({ onOpen }) {
  const [counts, setCounts] = useState({}), [errors, setErrors] = useState([]), [revision, setRevision] = useState(0), [loading, setLoading] = useState(true);
  const groups = approvalGroups;
  useEffect(() => { let live = true; Promise.allSettled(groups.map(g => workflowRequest(g[2]))).then(results => { if (!live) return; const next = {}, failures = []; results.forEach((r, i) => { const g = groups[i]; if (r.status === 'rejected') { failures.push(`${g[1]}: ${r.reason.message}`); return; } const rows = Array.isArray(r.value) ? r.value : r.value.requests || []; next[g[0]] = rows.filter(row => ['submitted', 'pending', 'requested', 'waiting_approval'].includes(String(row.item?.status ?? row.status).toLowerCase())).length; }); setCounts(next); setErrors(failures); setLoading(false); }); return () => { live = false; }; }, [revision, groups]);
  return <section className="ops-page"><h1>Phê duyệt</h1><button disabled={loading} onClick={() => setRevision(r => r + 1)}>Tải lại</button>{errors.map(e => <p role="alert" key={e}>{e}</p>)}{loading && <p role="status">Đang tải…</p>}<div className="ops-grid">{groups.map(([key, label, , destination]) => <article className="ops-card" key={key}><h2>{label}</h2><strong className="ops-stat">{counts[key] ?? '—'}</strong><p>Yêu cầu chờ xử lý</p><button onClick={() => onOpen(destination)}>Mở danh sách</button></article>)}</div></section>;
}
