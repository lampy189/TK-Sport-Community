import { useEffect, useState } from 'react';
import { Building2, Clock3, Users, Zap, ArrowRight, RefreshCw } from 'lucide-react';
import { workflowRequest } from '../workflowClient';
import './late-entry.css';

const root = '/api/late-entry';
const money = n => new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(n);
const time = value => new Intl.DateTimeFormat('vi-VN', { hour: '2-digit', minute: '2-digit', timeZone: 'Asia/Ho_Chi_Minh' }).format(new Date(value));
const percent = value => new Intl.NumberFormat('vi-VN', { maximumFractionDigits: 1 }).format(value);
function Price({ offer }) {
  return <strong>{money(offer.priceVnd)}<small>{percent(offer.pricePercent)}% giá gốc {money(offer.basePriceVnd)} · / người / phần còn lại</small></strong>;
}
function remainingMinutes(endsAt, now = Date.now()) { return Math.max(0, Math.ceil((new Date(endsAt).getTime() - now) / 60000)); }
function changed() { window.dispatchEvent(new Event('tksc:late-entry-changed')); }
function useOffers(path) {
  const [state, setState] = useState({ data: [], loading: true, error: '' });
  const [version, setVersion] = useState(0);
  const reload = () => setVersion(v => v + 1);
  useEffect(() => {
    let active = true;
    async function load() {
      try { const data = await workflowRequest(path); if (active) setState({ data, loading: false, error: '' }); }
      catch (e) { if (active) setState({ data: [], loading: false, error: e.message }); }
    }
    void load(); const tick = setInterval(load, 15000);
    return () => { active = false; clearInterval(tick); };
  }, [path, version]);
  useEffect(() => {
    const refresh = () => setVersion(v => v + 1);
    window.addEventListener('tksc:late-entry-changed', refresh);
    return () => window.removeEventListener('tksc:late-entry-changed', refresh);
  }, []);
  return { ...state, reload };
}
function OfferFacts({ offer }) {
  return <><p className="le-location"><Building2 size={16}/>{offer.venueName} · {offer.courtName}</p><div className="le-facts"><span><Clock3 size={16}/>Đến {time(offer.endsAt)} · còn {remainingMinutes(offer.endsAt)} phút</span><span><Users size={16}/>{offer.remaining} suất đang rao · {offer.band || 'Theo điều kiện buổi'}</span></div></>;
}
export function LateEntryOffers({ venueId, sport, onSelectSession }) {
  const query = new URLSearchParams(); if (venueId && venueId !== 'ALL') query.set('venueId', venueId);
  const { data, loading, error, reload } = useOffers(`${root}?${query}`);
  const offers = data.filter(o => (!sport || sport === 'ALL' || o.sportName.toLowerCase() === sport.toLowerCase() || (sport === 'BADMINTON' && o.sportName.toLowerCase() === 'badminton') || (sport === 'PICKLEBALL' && o.sportName.toLowerCase() === 'pickleball')) && remainingMinutes(o.endsAt) > 0);
  if (loading || (!error && !offers.length)) return null;
  return <section className="le-section" aria-label="Vé phần buổi còn lại"><div className="le-heading"><div><span className="eyebrow">VÀO CHƠI NGAY</span><h2><Zap size={22}/>Chỗ trống trong buổi đang diễn ra</h2><p>Mua phần thời gian còn lại, vé kết thúc cùng buổi chơi.</p></div><button className="button button-secondary" onClick={reload} aria-label="Tải lại vé giờ lẻ"><RefreshCw size={16}/></button></div>{error && <p role="alert" className="payment-error">{error}</p>}<div className="le-grid">{offers.map(o => <article className="le-card" key={o.sessionId}><span className="le-badge">ĐANG RAO · {o.sportName}</span><h3>{o.courtName} · Vé giờ lẻ</h3><OfferFacts offer={o}/><div className="le-actions"><Price offer={o}/><button className="button button-primary" onClick={() => onSelectSession?.(o.sessionId)}>Xem & mua vé <ArrowRight size={16}/></button></div></article>)}</div></section>;
}
export function LateEntryPurchase({ sessionId, user, onLogin, onPayment }) {
  const { data, error, reload } = useOffers(`${root}?sessionId=${sessionId}`);
  const [busy, setBusy] = useState(false); const [feedback, setFeedback] = useState('');
  const offer = data[0];
  if (!offer || remainingMinutes(offer.endsAt) < 1) return null;
  async function hold() {
    if (!user) { onLogin?.(); return; }
    setBusy(true); setFeedback('');
    try { const result = await workflowRequest(`${root}/${sessionId}/holds`, 'POST', { expectedRevision: offer.revision }); changed(); onPayment?.(result.orderId); }
    catch (e) { setFeedback(e.message); reload(); }
    finally { setBusy(false); }
  }
  return <section className="le-purchase"><span className="le-badge">BUỔI ĐANG DIỄN RA · SUẤT THAM GIA MUỘN</span><h2>Vẫn kịp vào chơi cùng mọi người</h2><OfferFacts offer={offer}/><p>Vé hết hiệu lực lúc <strong>{time(offer.endsAt)}</strong>, kể cả khi bạn check-in muộn. Vẫn kiểm tra trình độ, trùng lịch và chỗ trống.</p><div className="le-actions"><Price offer={offer}/><button className="button button-primary" disabled={busy || !!error} onClick={hold}>{busy ? 'Đang kiểm tra chỗ…' : user ? 'Giữ chỗ & thanh toán' : 'Đăng nhập để mua vé'}<ArrowRight size={17}/></button></div>{(error || feedback) && <p role="alert" className="payment-error">{feedback || error}</p>}</section>;
}
function AutomaticOffer({ offer }) {
  return <article className="le-card">
    <span className="le-badge">{offer.isOpen ? 'TỰ ĐỘNG ĐANG RAO' : 'HẾT CHỖ'} · {offer.sportName}</span>
    <h3>{offer.courtName} · đến {time(offer.endsAt)}</h3>
    <p className="le-location">{offer.venueName} · {offer.band || 'Theo điều kiện buổi'}</p>
    <div className="le-capacity">
      <span><strong>{offer.playing}</strong>Đang chơi</span>
      <span><strong>{offer.confirmed}</strong>Đã mua, chưa đến</span>
      <span><strong>{offer.holds}</strong>Đang giữ chỗ</span>
      <span><strong>{offer.available}</strong>Có thể bán</span>
    </div>
    <div className="le-actions"><Price offer={offer}/></div>
    <p className="le-hint">Giá giảm theo từng phút đã trôi, tối đa giảm 50%. Vé hết lúc {time(offer.endsAt)}.</p>
    <p className="le-hint">Tự rao chỗ trống; không bán lại suất của người đã trả tiền nhưng chưa đến.</p>
  </article>;
}
export function LateEntryManagement() {
  const { data, error, loading, reload } = useOffers(`${root}/manage`);
  return <section className="le-section">
    <div className="le-heading"><div>
      <span className="eyebrow">VẬN HÀNH THEO CƠ SỞ</span>
      <h2>Vé giờ lẻ tự động</h2>
      <p>Buổi đang diễn ra còn chỗ sẽ tự được rao. Giá giảm theo tỷ lệ thời gian đã trôi, tối đa giảm 50% giá gốc.</p>
    </div><button className="button button-secondary" onClick={reload}><RefreshCw size={17}/>Tải lại</button></div>
    {error && <p role="alert" className="payment-error">{error}</p>}
    {loading && <p>Đang tải buổi chơi…</p>}
    {!loading && !error && !data.length && <p>Chưa có buổi đang diễn ra đủ điều kiện bán vé giờ lẻ tại cơ sở được phân công.</p>}
    <div className="le-grid">{data.map(o => <AutomaticOffer key={o.sessionId} offer={o}/>)}</div>
  </section>;
}