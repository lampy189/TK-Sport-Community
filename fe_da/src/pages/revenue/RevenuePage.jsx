import { useEffect, useState } from 'react';
import { request } from '../../api';
import './revenue-demo.css';

const money = n => new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(n);
const label = s => s.split('-').reverse().join('/');
async function load(path, signal) {
  const response = await request(path, { signal, cache: 'no-store' });
  const data = await response.json().catch(() => ({}));
  if (!response.ok) throw new Error(data.message || `Không thể tải dữ liệu (HTTP ${response.status}).`);
  return data;
}
function dates() {
  const now = new Date(Date.now() + 7 * 3600000);
  return { to: now.toISOString().slice(0, 10), from: new Date(now.getTime() - 6 * 86400000).toISOString().slice(0, 10) };
}
export default function RevenuePage() {
  const [scope, setScope] = useState(null);
  const [venue, setVenue] = useState('');
  const [range, setRange] = useState(dates);
  const [state, setState] = useState({ loading: true });
  const [retry, setRetry] = useState(0);
  useEffect(() => {
    const controller = new AbortController();
    load('/api/v1/sport-centers', controller.signal).then(value => {
      setScope(value); setVenue(value.canViewAll ? String(value.selectedVenueId || '') : String(value.assignedVenueId || ''));
    }).catch(error => { if (!controller.signal.aborted) setState({ error: error.message }); });
    return () => controller.abort();
  }, [retry]);
  const key = `${venue}|${range.from}|${range.to}|${retry}`;
  useEffect(() => {
    if (!scope) return;
    const controller = new AbortController();
    const query = new URLSearchParams({ from: range.from, to: range.to });
    if (venue) query.set('venueId', venue);
    load(`/api/v1/revenue/daily?${query}`, controller.signal)
      .then(data => setState({ key, data }))
      .catch(error => { if (!controller.signal.aborted) setState({ key, error: error.message }); });
    return () => controller.abort();
  }, [scope, venue, range.from, range.to, key]);
  const data = state.key === key ? state.data : null;
  const error = state.key === key || !scope ? state.error : null;
  return <section className="revenue-demo">
    <header className="revenue-demo-header"><h1>Thống kê doanh số</h1></header>
    <div className="revenue-demo-filters">
      <label>Sport Center<select value={venue} disabled={!scope?.canViewAll} onChange={e => setVenue(e.target.value)}>
        {scope?.canViewAll && <option value="">Tổng hợp toàn bộ chuỗi</option>}
        {!scope?.canViewAll && !scope?.assignedVenueId && <option value="">Chưa được phân công</option>}
        {scope?.centers.map(center => <option key={center.id} value={center.id}>{center.name}{center.active ? '' : ' (ngừng hoạt động)'}</option>)}
      </select></label>
      <label>Từ ngày<input type="date" value={range.from} onChange={e => setRange({ ...range, from: e.target.value })} /></label>
      <label>Đến ngày<input type="date" value={range.to} onChange={e => setRange({ ...range, to: e.target.value })} /></label>
      <button className="button button-secondary" onClick={() => setRetry(n => n + 1)}>Tải lại</button>
    </div>
    <details className="revenue-demo-note"><summary>Cách tính doanh số</summary><p>Ngày hạch toán theo giờ Việt Nam (UTC+7). Thu ròng = tiền đã thu − tiền đã hoàn, chưa trừ chi phí. Không tính các khoản đang chờ thanh toán hoặc hoàn tiền.</p></details>
    {error && <p role="alert">{error}</p>}
    {!error && !data && <p role="status">Đang tải doanh số…</p>}
    {data && <>
      <section className="revenue-demo-metrics" aria-label="Tổng số liệu">{[['Thu buổi chơi / giải', data.totals.social], ['Thu bán hàng', data.totals.goods], ['Đã hoàn tiền', data.totals.refunds], ['Thu ròng', data.totals.net]].map(([name, amount]) => <article key={name}><span>{name}</span><strong>{money(amount)}</strong></article>)}</section>
      {data.unpostedPayments > 0 && <p role="status" className="revenue-demo-banner">{data.unpostedPayments} khoản đã thu cần đối soát, chưa tính vào báo cáo (mọi ngày trong phạm vi cơ sở đang xem).</p>}
      <div className="revenue-demo-panel"><h2>Doanh số từng ngày</h2><div className="revenue-demo-table"><table><caption>{venue ? scope.centers.find(c => String(c.id) === venue)?.name : 'Tổng hợp toàn bộ chuỗi'} · {label(data.from)} — {label(data.to)}</caption><thead><tr><th>Ngày</th><th>Buổi chơi / giải</th><th>Bán hàng</th><th>Đã hoàn</th><th>Thu ròng</th></tr></thead><tbody>{data.days.map(day => <tr key={day.date}><th scope="row">{label(day.date)}</th><td>{money(day.social)}</td><td>{money(day.goods)}</td><td>{money(day.refunds)}</td><td><strong>{money(day.net)}</strong></td></tr>)}</tbody><tfoot><tr><th>Tổng</th><td>{money(data.totals.social)}</td><td>{money(data.totals.goods)}</td><td>{money(data.totals.refunds)}</td><td>{money(data.totals.net)}</td></tr></tfoot></table></div></div>
    </>}
  </section>;
}
