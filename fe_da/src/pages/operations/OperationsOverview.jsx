import { useEffect, useState } from 'react';
import { workflowRequest } from '../workflowClient';
import './operations.css';
const labels = { open: 'Mở đăng ký', published: 'Đã công bố', pending: 'Chờ thanh toán', confirmed: 'Đã xác nhận', checkedin: 'Đã điểm danh', cancelled: 'Đã hủy', completed: 'Đã kết thúc', expired: 'Hết hạn', ongoing: 'Đang diễn ra' };
export default function OperationsOverview({ role }) {
  const [data, setData] = useState(null), [error, setError] = useState(''), [revision, setRevision] = useState(0);
  const [content, setContent] = useState(''), [busy, setBusy] = useState(false), [notice, setNotice] = useState('');
  useEffect(() => {
    let active = true;
    workflowRequest('/api/operations/overview').then(value => { if (active) { setData(value); setError(''); } }).catch(e => { if (active) setError(e.message); });
    return () => { active = false; };
  }, [revision]);
  async function submit(e) {
    e.preventDefault(); setBusy(true); setError(''); setNotice('');
    try { await workflowRequest('/api/operations/feedback', 'POST', { content }); setContent(''); setRevision(value => value + 1); setNotice('Đã ghi nhận phản hồi.'); }
    catch (e) { setError(e.message); } finally { setBusy(false); }
  }
  return <section className="ops-page"><h1>{role === 'system-admin' ? 'Phản hồi chạy thử' : 'Tổng quan vận hành hôm nay'}</h1><button onClick={() => setRevision(value => value + 1)}>Tải lại</button>{error && <p role="alert">{error}</p>}{notice && <p role="status">{notice}</p>}
    {!data ? !error && <p role="status">Đang tải tổng quan…</p> : <>
      {role !== 'system-admin' && [['sessions', 'Buổi chơi'], ['tickets', 'Vé']].map(([key, title]) => <div key={key}><h2>{title}</h2><div className="ops-grid">{data[key].map(row => <article className="ops-card" key={row.status}><strong className="ops-stat">{row.count}</strong><p>{labels[row.status] || row.status}</p></article>)}</div>{!data[key].length && <p>Chưa có dữ liệu hôm nay trong phạm vi của bạn.</p>}</div>)}
      <form className="ops-form" onSubmit={submit}><label>Phản hồi chạy thử<textarea required maxLength={2000} value={content} onChange={e => setContent(e.target.value)} /></label><button type="submit" disabled={busy}>{busy ? 'Đang gửi…' : 'Gửi phản hồi'}</button></form>
      <h2>Phản hồi gần đây</h2>{!data.feedback.length && <p>Chưa có phản hồi.</p>}{data.feedback.map(item => { let detail = {}; try { detail = JSON.parse(item.detail); } catch { /* retain readable fallback */ } return <article className="ops-card" key={item.id}><strong>{item.actor}</strong><p>{detail.Content || item.detail}</p><small>{detail.at && new Date(detail.at).toLocaleString('vi-VN')}</small></article>; })}
    </>}
  </section>;
}
