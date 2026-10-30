import { useEffect, useRef, useState } from 'react';
import { Bell, X } from 'lucide-react';
import { request } from '../api';
import './notifications.css';

export default function NotificationBell({ onTickets }) {
  const [open, setOpen] = useState(false);
  const [items, setItems] = useState([]);
  const [unread, setUnread] = useState(0);
  const [toast, setToast] = useState(null);
  const [connected, setConnected] = useState(false);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const root = useRef(null);
  const refresh = useRef(null);
  useEffect(() => {
    let active = true, source, ready = false, fetching = false;
    const seen = new Set();
    function apply(data) {
      if (!active) return;
      const incoming = data.items.filter(n => !seen.has(n.id));
      if (ready && incoming.length) {
        setToast(incoming[0]);
        window.dispatchEvent(new Event('tksc:ticket-changed'));
      }
      data.items.forEach(n => seen.add(n.id));
      ready = true;
      setItems(data.items); setUnread(data.unread); setError('');
    }
    function connect() {
      if (!active || source || !window.EventSource) return;
      source = new EventSource('/api/notifications/stream');
      source.onopen = () => { if (active) setConnected(true); };
      source.onerror = () => { if (active) setConnected(false); };
      source.addEventListener('notifications', event => {
        try { apply(JSON.parse(event.data)); } catch { if (active) setError('Không đọc được thông báo. Hãy tải lại.'); }
      });
    }
    async function load() {
      if (fetching) return;
      fetching = true;
      try {
        const response = await request('/api/notifications', { cache: 'no-store' });
        if (response.status === 401) { source?.close(); source = null; if (active) setConnected(false); return; }
        if (!response.ok) throw new Error('Chưa tải được thông báo.');
        apply(await response.json()); connect();
      } catch (e) { if (active) setError(e.message); }
      finally { fetching = false; }
    }
    refresh.current = load;
    void load();
    // Recovery when a proxy/browser does not support streaming; also sync read states across tabs.
    const timer = setInterval(load, 30000);
    window.addEventListener('focus', load);
    return () => { active = false; source?.close(); clearInterval(timer); window.removeEventListener('focus', load); refresh.current = null; };
  }, []);
  useEffect(() => {
    if (!toast) return;
    const timer = setTimeout(() => setToast(null), 7000);
    return () => clearTimeout(timer);
  }, [toast]);
  useEffect(() => {
    function close(event) { if (!root.current?.contains(event.target)) setOpen(false); }
    function escape(event) { if (event.key === 'Escape') setOpen(false); }
    document.addEventListener('pointerdown', close); document.addEventListener('keydown', escape);
    return () => { document.removeEventListener('pointerdown', close); document.removeEventListener('keydown', escape); };
  }, []);
  async function mark(id) {
    if (busy) return;
    setBusy(true); setError('');
    try {
      const response = await request(`/api/notifications/${id ? `${id}/read` : 'read-all'}`, { method: 'POST' });
      if (!response.ok) throw new Error('Chưa thể đánh dấu đã đọc. Vui lòng thử lại.');
      await refresh.current?.();
      if (id && onTickets) { setOpen(false); setToast(null); onTickets(); }
    } catch (e) { setError(e.message); }
    finally { setBusy(false); }
  }
  return <div className="notification-root" ref={root}>
    <button className="notification-bell" aria-label={`Thông báo, ${unread} chưa đọc`} aria-expanded={open} onClick={() => { setOpen(!open); if (!open) void refresh.current?.(); }}>
      <Bell size={21}/>{unread > 0 && <span className="notification-count">{unread > 99 ? '99+' : unread}</span>}
    </button>
    {open && <section className="notification-panel" aria-label="Thông báo của bạn">
      <div className="notification-heading"><strong>Thông báo</strong><button disabled={busy || !unread} onClick={() => mark(null)}>Đọc tất cả</button></div>
      <small>{connected ? 'Đang cập nhật trực tiếp' : 'Đang kết nối lại · tự cập nhật định kỳ'}</small>
      {error && <p role="alert">{error}</p>}
      {!items.length && <p>Chưa có thông báo về vé.</p>}
      <div className="notification-items">{items.map(item => <button disabled={busy} className={`notification-item ${item.status === 'unread' ? 'is-unread' : ''}`} key={item.id} onClick={() => mark(item.id)}><span>{item.title}</span><small>{item.status === 'unread' ? 'Chưa đọc' : 'Đã đọc'}{onTickets ? ' · Xem vé' : ''}</small></button>)}</div>
      {items.length === 50 && <small>Hiển thị 50 thông báo gần nhất.</small>}
    </section>}
    {toast && <aside className="notification-toast" role="status"><Bell size={19}/><div><strong>Thông báo vé</strong><p>{toast.title}</p>{onTickets && <button onClick={() => mark(toast.id)}>Xem vé</button>}</div><button aria-label="Đóng thông báo" onClick={() => setToast(null)}><X size={18}/></button></aside>}
  </div>;
}
