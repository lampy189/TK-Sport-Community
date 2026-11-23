import { refereeNotificationActions } from "../../components/refereeNotificationActions";
import PaginatedList from '../../components/ui/PaginatedList';
import { useEffect, useState } from 'react';
import { request } from '../../api';
import TicketSupport from '../payment/TicketSupport';
import './messages.css';

const isConversation = item => ['ticket_staff_reply', 'ticket_player_message'].includes(item?.referenceType) && item.referenceId > 0;

export default function MessagesPage({ preview = false, embedded = false, onOpen, role }) {
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
    setSelected(selected?.id === item.id ? null : item);
    if (item.status !== 'unread') return;
    try {
      const response = await request(`/api/notifications/${item.id}/read`, { method: 'POST' });
      if (!response.ok) throw new Error('Chưa thể đánh dấu đã đọc.');
      setItems(current => current.map(value => value.id === item.id ? { ...value, status: 'read' } : value));
    } catch (e) { setError(e.message); }
  }

  return <section className="messages-page" aria-label="Danh sách thông báo">
    {!embedded && <h1>Thông báo</h1>}
    {error && <p role="alert">{error}</p>}
    {loading ? <p role="status">Đang tải thông báo…</p> : !items.length && <p>{preview ? 'Thông báo sẽ hiển thị khi bạn đăng nhập.' : 'Chưa có thông báo.'}</p>}
    <PaginatedList hideShortPager items={items}>{item => <article className="inbox-entry" key={item.id}>
      <button className="inbox-entry-toggle" aria-expanded={selected?.id === item.id} onClick={() => select(item)}>
        <strong>{item.title}</strong>
        <small>{item.referenceId > 0 && item.referenceType?.startsWith('ticket_') ? `Vé #${item.referenceId} · ` : ''}{item.status === 'unread' ? 'Chưa đọc' : 'Đã đọc'}</small>
        <span aria-hidden="true" className="inbox-chevron">⌄</span>
      </button>
      {selected?.id === item.id && <div className="inbox-body">
        {item.description && item.description !== item.title && <p className="messages-description">{item.description}</p>}
          {role === 'referee' && refereeNotificationActions[item.referenceType] &&
            <button onClick={() => onOpen?.(refereeNotificationActions[item.referenceType])}>
              {refereeNotificationActions[item.referenceType]}
            </button>}
          {item.referenceType === 'session_invitation' && <button onClick={() => onOpen?.('Lời mời buổi chơi')}>Mở lời mời buổi chơi</button>}
          {item.referenceType === 'access_request' && <button onClick={() => onOpen?.(role === 'owner' ? 'Bổ nhiệm quản lý' : role === 'manager' ? 'Phê duyệt nhân sự' : 'Tài khoản & truy cập')}>Mở yêu cầu quyền tài khoản</button>}
          {['friend_request', 'friend_accepted', 'chat_message'].includes(item.referenceType) && role === 'player' && <button onClick={() => onOpen?.('Cộng đồng')}>Mở cộng đồng và tin nhắn</button>}
          {isConversation(item) && <TicketSupport key={item.referenceId} ticketId={item.referenceId} staff={item.referenceType === 'ticket_player_message'} initialOpen />}

      </div>}
    </article>}</PaginatedList>
  </section>;
}