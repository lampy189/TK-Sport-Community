import { useEffect, useState } from 'react';
import { api } from '../../api';
import { invitationClient, invitationState, invitationPayable } from './invitationClient';
import '../operations/operations.css';
const labels = { pending: 'Chờ trả lời', accepted: 'Đã nhận lời', declined: 'Đã từ chối', expired: 'Hết hạn' };
export default function InvitationsView({ user, onPayment, onSelectSession }) {
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(null);
  const [now, setNow] = useState(Date.now);
  const [reload, setReload] = useState(0);
  const [tab, setTab] = useState('received');
  const userId = Number(user?.id ?? user?.userId);
  useEffect(() => {
    let active = true, pending = false;
    async function load(background = false) {
      if (pending || (background && document.hidden)) return;
      pending = true;
      try { const data = await invitationClient.list(); if (active) { setItems(data); setError(''); } }
      catch (e) { if (active) setError(e.message); }
      finally { pending = false; if (active) setLoading(false); }
    }
    void load();
    const refresh = () => { if (!document.hidden) void load(); };
    const timer = setInterval(() => { setNow(Date.now()); void load(true); }, 10000);
    window.addEventListener('focus', refresh);
    document.addEventListener('visibilitychange', refresh);
    return () => { active = false; clearInterval(timer); window.removeEventListener('focus', refresh); document.removeEventListener('visibilitychange', refresh); };
  }, [reload]);
  async function reply(item, accept) {
    setBusy(item.id); setError('');
    try {
      if (accept && Number(user.selectedVenueId) !== item.venueId) {
        await api.selectCenter(item.venueId);
        window.location.assign(`${window.location.pathname}?view=invitations`);
        return;
      }
      await invitationClient.reply(item.id, accept);
      setReload(value => value + 1);
    } catch (e) { setError(e.message); }
    finally { setBusy(null); }
  }
  const shown = items.filter(item => tab === 'received' ? item.inviteeUserId === userId : item.inviterUserId === userId);
  return <section className="ops-page"><h1>Lời mời buổi chơi</h1>
    <div className="ops-actions">{[['received', 'Đã nhận'], ['sent', 'Đã gửi']].map(([id, label]) => <button key={id} aria-pressed={tab === id} onClick={() => setTab(id)}>{label}</button>)}<button onClick={() => { setLoading(true); setReload(value => value + 1); }}>Tải lại</button></div>
    {error && <p role="alert">{error}</p>}
    {loading ? <p role="status">Đang tải lời mời…</p> : !shown.length && <p>Chưa có lời mời.</p>}
    <div className="ops-grid">{shown.map(item => {
      const state = invitationState(item, now);
      const received = item.inviteeUserId === userId;
      const needsCenter = Number(user.selectedVenueId) !== item.venueId;
      const paymentExpired = state === 'accepted' && String(item.paymentStatus).toLowerCase() === 'pending' && new Date(item.paymentDeadline).getTime() <= now;
      return <article className="ops-card" key={item.id}><h2>{received ? item.inviter : item.invitee}</h2>
        <p>{item.sport} · {item.date} · {item.startsAt.slice(0, 5)}</p><p>{item.venue} · {item.court}</p>
        <strong>{item.amountVnd.toLocaleString('vi-VN')} ₫ · {labels[state] || state}</strong>
        <p>Người trả: {item.payerUserId === item.inviterUserId ? item.inviter : item.invitee}</p>
        {state === 'pending' && <p>Hạn trả lời: {new Date(item.expiresAt).toLocaleString('vi-VN')}</p>}
        {state === 'accepted' && <p>{paymentExpired ? 'Đã hết hạn giữ chỗ' : ({ pending: 'Chờ thanh toán', paid: 'Đã thanh toán', cancelled: 'Đã hủy', expired: 'Hết hạn thanh toán', review_required: 'Cần đối soát' }[String(item.paymentStatus).toLowerCase()] || item.paymentStatus)}{item.paymentDeadline && !paymentExpired && ` · hạn ${new Date(item.paymentDeadline).toLocaleString('vi-VN')}`}</p>}
        <div className="ops-actions">
          {received && state === 'pending' && <><button disabled={busy !== null} onClick={() => reply(item, true)}>{busy === item.id ? 'Đang xử lý…' : needsCenter ? 'Chuyển cơ sở để nhận lời' : 'Nhận lời và giữ chỗ'}</button><button disabled={busy !== null} onClick={() => reply(item, false)}>Từ chối</button></>}
          {invitationPayable(item, userId, now) && <button onClick={() => onPayment(item.orderId)}>Thanh toán</button>}
          {!needsCenter && <button onClick={() => onSelectSession(item.sessionId)}>Xem buổi chơi</button>}
        </div>
      </article>;
    })}</div>
  </section>;
}
