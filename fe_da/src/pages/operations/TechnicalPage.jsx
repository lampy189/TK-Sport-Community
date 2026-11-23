import { useEffect, useState } from 'react';
import { workflowRequest } from '../workflowClient';
import { DataTable } from './BusinessConsole';
import './business-console.css';
const labels = { 'Email:Enabled': 'Xử lý hàng đợi email', 'Firebase:Enabled': 'Xử lý hàng đợi push vé', 'GoodsReservations:ExpiryWorkerEnabled': 'Dọn giữ hàng hết hạn' };
const states = { open: 'Mới', in_progress: 'Đang xử lý', resolved: 'Đã xử lý' };
export default function TechnicalPage({ incidentsOnly = false }) {
  const [data, setData] = useState(null), [incidents, setIncidents] = useState([]), [error, setError] = useState(''), [notice, setNotice] = useState(''), [busy, setBusy] = useState(false), [revision, setRevision] = useState(0);
  const [setting, setSetting] = useState(null), [reason, setReason] = useState(''), [editing, setEditing] = useState(null), [form, setForm] = useState({ title: '', note: '', status: 'open', revision: 0 });
  useEffect(() => { let live = true; Promise.all([workflowRequest('/api/technical'), workflowRequest('/api/technical/incidents')]).then(([d, i]) => { if (live) { setData(d); setIncidents(i); setError(''); } }).catch(e => { if (live) setError(e.message); }); return () => { live = false; }; }, [revision]);
  async function save(path, method, body, done) {
    setBusy(true); setError(''); setNotice('');
    try { await workflowRequest('/api/technical' + path, method, body); done?.(); setRevision(r => r + 1); setNotice('Đã lưu.'); }
    catch (e) { setError(e.message); } finally { setBusy(false); }
  }
  return <section className="ops-page"><h1>{incidentsOnly ? 'Sự cố kỹ thuật' : 'Cấu hình kỹ thuật'}</h1><button disabled={busy} onClick={() => setRevision(r => r + 1)}>Tải lại</button>{error && <p role="alert">{error}</p>}{notice && <p role="status">{notice}</p>}{!data && !error && <p role="status">Đang tải…</p>}
    {!incidentsOnly && data && <><div className="ops-grid">{[['Email', data.emailReady], ['Firebase', data.firebaseReady], ['payOS', data.payosReady]].map(([name, ready]) => <article className="ops-card" key={name}><h2>{name}</h2><p>{ready ? 'Có cấu hình máy chủ; cần kiểm tra kết nối thực tế' : 'Chưa đủ cấu hình máy chủ'}</p></article>)}</div>
      <DataTable title="Tác vụ nền" rows={data.settings} columns={[[ 'key', 'Tác vụ', key => labels[key]], ['enabled', 'Trạng thái', value => value ? 'Bật' : 'Tắt'], ['revision', 'Thao tác', (_, row) => <button disabled={busy} onClick={() => { setSetting(row); setReason(''); }}>{row.enabled ? 'Tắt' : 'Bật'}</button>]]} />
      {setting && <form className="ops-form" onSubmit={e => { e.preventDefault(); void save('/settings/' + encodeURIComponent(setting.key), 'PUT', { enabled: !setting.enabled, revision: setting.revision, reason }, () => setSetting(null)); }}><h2>{setting.enabled ? 'Tắt' : 'Bật'} {labels[setting.key]}</h2><label>Lý do<textarea required maxLength={1000} value={reason} onChange={e => setReason(e.target.value)} /></label><div className="ops-actions"><button disabled={busy || !reason.trim()}>Lưu</button><button type="button" onClick={() => setSetting(null)}>Hủy</button></div></form>}
      <p>Thiết lập áp dụng ở lượt xử lý nền tiếp theo. Khóa kết nối được cấu hình trên máy chủ.</p>
      <DataTable title="Trạng thái hàng đợi" rows={data.queues} columns={[[ 'channel', 'Kênh'], ['status', 'Trạng thái'], ['count', 'Số lượng']]} />
    </>}
    <section className="business-section"><h2>{editing ? `Cập nhật sự cố #${editing}` : 'Ghi nhận sự cố'}</h2><form className="ops-form" onSubmit={e => { e.preventDefault(); void save('/incidents' + (editing ? `/${editing}` : ''), editing ? 'PUT' : 'POST', form, () => { setEditing(null); setForm({ title: '', note: '', status: 'open', revision: 0 }); }); }}>
      <label>Tiêu đề<input required maxLength={200} value={form.title} onChange={e => setForm({ ...form, title: e.target.value })} /></label><label>Trạng thái<select value={form.status} onChange={e => setForm({ ...form, status: e.target.value })}>{Object.entries(states).map(([key, label]) => <option key={key} value={key}>{label}</option>)}</select></label><label>Ghi nhận xử lý<textarea required maxLength={2000} placeholder="Không nhập mật khẩu, token hoặc thông tin giao dịch riêng tư" value={form.note} onChange={e => setForm({ ...form, note: e.target.value })} /></label><div className="ops-actions"><button disabled={busy}>Lưu sự cố</button>{editing && <button type="button" onClick={() => { setEditing(null); setForm({ title: '', note: '', status: 'open', revision: 0 }); }}>Hủy sửa</button>}</div></form></section>
    <DataTable title="Sự cố gần đây" rows={incidents} columns={[[ 'id', 'Mã'], ['title', 'Tiêu đề'], ['status', 'Trạng thái', v => states[v]], ['note', 'Ghi nhận'], ['at', 'Cập nhật', v => new Date(v).toLocaleString('vi-VN')], ['revision', 'Thao tác', (_, row) => <button onClick={() => { setEditing(row.id); setForm({ title: row.title, status: row.status, note: row.note, revision: row.revision }); }}>Cập nhật</button>]]} />
  </section>;
}
