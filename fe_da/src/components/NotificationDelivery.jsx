import { useEffect, useState } from 'react';
import { request } from '../api';
import { enablePush, disablePush, pushConfigured } from '../push';

const statusLabel = { pending: 'Đang chờ gửi', sent: 'Máy chủ thư đã nhận', failed: 'Gửi thất bại', suppressed: 'Đã ngừng gửi' };
export default function NotificationDelivery() {
  const [items, setItems] = useState([]), [error, setError] = useState(''), [busy, setBusy] = useState(false), [notice, setNotice] = useState('');
  async function load() {
    const response = await request('/api/notifications/email', { cache: 'no-store' });
    if (!response.ok) throw new Error('Chưa tải được trạng thái email.');
    setItems(await response.json());
  }
  useEffect(() => {
    let active = true;
    request('/api/notifications/email', { cache: 'no-store' }).then(async response => {
      if (!response.ok) throw new Error('Chưa tải được trạng thái email.');
      const data = await response.json();
      if (active) setItems(data);
    }).catch(e => { if (active) setError(e.message); });
    return () => { active = false; };
  }, []);
  async function run(action) {
    if (busy) return;
    setBusy(true); setError(''); setNotice('');
    try { await action(); } catch (e) { setError(e.message); } finally { setBusy(false); }
  }
  return <details><summary>Email & thông báo thiết bị</summary>
    <button disabled={busy || !pushConfigured} onClick={() => run(async () => { await enablePush(); setNotice('Đã bật thông báo trên thiết bị.'); })}>Bật thông báo thiết bị</button>
    <button disabled={busy} onClick={() => run(async () => { await disablePush(); setNotice('Đã tắt thông báo trên thiết bị.'); })}>Tắt thông báo thiết bị</button>
    <button disabled={busy} onClick={() => run(load)}>Cập nhật email</button>
    {error && <p role="alert">{error}</p>}{notice && <p role="status">{notice}</p>}
    {items.map(item => <div key={item.id}><p>{item.title}</p><small>{statusLabel[item.status] || (item.status?.startsWith('retry') ? 'Đang thử gửi lại' : item.status)}</small>
      {item.canRetry && <button disabled={busy} onClick={() => run(async () => {
        const response = await request(`/api/notifications/email/${item.id}/retry`, { method: 'POST' });
        if (!response.ok) throw new Error('Không thể gửi lại thư này. Hãy cập nhật trạng thái.');
        await load(); setNotice('Đã xếp hàng gửi lại.');
      })}>Gửi lại email</button>}
    </div>)}
  </details>;
}
