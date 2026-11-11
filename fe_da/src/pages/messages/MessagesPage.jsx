import { useEffect, useState } from 'react';
import { request } from '../../api';
import TicketSupport from '../payment/TicketSupport';
import './messages.css';

const isConversation = item => ['ticket_staff_reply', 'ticket_player_message'].includes(item?.referenceType) && item.referenceId > 0;

export default function MessagesPage({ preview = false }) {
  const [items, setItems] = useState([]);
  const [selected, setSelected] = useState(null);
  const [loading, setLoading] = useState(!preview);
  const [error, setError] = useState('');
  useEffect(() => {
    if (preview) return;
    let active = true;
    async function load() {
      try {
        const response = await request('/api/notifications', { cache: 'no-store' });
        if (!response.ok) throw new Error('Chưa tải được thông báo. Vui lòng tải lại.');
        const data = await response.json();
        if (active) {
          setItems(data.items);
          setError('');
        }
      } catch (e) { if (active) setError(e.message); }
      finally { if (active) setLoading(false); }
    }
    void load();
    const timer = setInterval(load, 10000);
    window.addEventListener('focus', load);
    return () => { active = false; clearInterval(timer); window.removeEventListener('focus', load); };
  }, [preview]);

  async function select(item) {
    setSelected(item);
    if (item.status !== 'unread') return;
    try {
      const response = await request(`/api/notifications/${item.id}/read`, { method: 'POST' });
      if (!response.ok) throw new Error('Chưa thể đánh dấu đã đọc.');
      setItems(current => current.map(value => value.id === item.id ? { ...value, status: 'read' } : value));
    } catch (e) { setError(e.message); }
  }

  return <section className="messages-page" aria-labelledby="messages-title">
    <h1 id="messages-title">Thông báo</h1>
    <p>Xem đầy đủ thông báo từ chuông và nội dung trao đổi về vé của bạn.</p>
    {error && <p role="alert">{error}</p>}
    <div className="messages-layout">
      <aside className="messages-list" aria-label="Danh sách thông báo">
        <h2>Thông báo gần đây</h2>
        <small>Hiển thị 50 thông báo gần nhất, giống danh sách ở chuông.</small>
        {loading ? <p role="status">Đang tải thông báo…</p> : !items.length && <p>{preview ? 'Thông báo sẽ hiển thị khi bạn đăng nhập.' : 'Chưa có thông báo về vé.'}</p>}
        {items.map(item => <button key={item.id} className={`messages-entry ${selected?.id === item.id ? 'is-selected' : ''}`} aria-pressed={selected?.id === item.id} onClick={() => select(item)}>
          <strong>{item.title}</strong>
          <small>{item.referenceId > 0 && item.referenceType?.startsWith('ticket_') ? `Vé #${item.referenceId} · ` : ''}{item.status === 'unread' ? 'Chưa đọc' : 'Đã đọc'}</small>
        </button>)}
      </aside>
      <article className="messages-detail" aria-label="Chi tiết thông báo">
        {selected ? <>
          <h2>{isConversation(selected) ? 'Trao đổi về vé' : 'Chi tiết thông báo'}</h2>
          <h3>Nội dung chi tiết</h3>
          <p className="messages-description">{selected.description || selected.title}</p>
          {isConversation(selected) && <TicketSupport key={selected.referenceId} ticketId={selected.referenceId} staff={selected.referenceType === 'ticket_player_message'} initialOpen />}
        </> : <p>Chọn một thông báo để xem đầy đủ nội dung và ảnh đính kèm.</p>}
      </article>
    </div>
  </section>;
}
