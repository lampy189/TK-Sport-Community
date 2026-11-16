import { useState } from 'react';
import { ArrowLeft, ArrowRight, Building2, Check, Clock3, MapPin, Ticket, Users, Zap, ShieldCheck, LayoutGrid, SlidersHorizontal, Plus, Pause, Play } from 'lucide-react';
import { availableSeats, clock, durationLabel, DEMO_NOW, offerProblem, purchaseProblem } from './hourlyRules';
import './hourly-design.css';

const centers = [
  { id: 1, name: 'TK Sport · Cầu Giấy', address: 'Cầu Giấy, Hà Nội', courts: 4 },
  { id: 2, name: 'TK Sport · Hà Đông', address: 'Hà Đông, Hà Nội', courts: 6 },
];
const initialSessions = [
  { id: 101, venue: 1, court: 'Sân 01', sport: 'Cầu lông', band: 'B2', start: 1080, end: 1200, capacity: 8, playing: 4, confirmed: 1, holds: 1 },
  { id: 102, venue: 1, court: 'Sân 02', sport: 'Pickleball', band: 'B2', start: 1080, end: 1200, capacity: 8, playing: 8, confirmed: 0, holds: 0 },
  { id: 103, venue: 1, court: 'Sân 03', sport: 'Pickleball', band: 'B2', start: 1050, end: 1170, capacity: 8, playing: 5, confirmed: 1, holds: 0 },
  { id: 201, venue: 2, court: 'Sân 03', sport: 'Cầu lông', band: 'B3', start: 1080, end: 1200, capacity: 8, playing: 3, confirmed: 1, holds: 0 },
];
const money = value => new Intl.NumberFormat('vi-VN').format(value) + 'đ';
export default function HourlyDesign() {
  const [view, setView] = useState('browse');
  const [venue, setVenue] = useState(1);
  const [sport, setSport] = useState('all');
  const [sessions, setSessions] = useState(initialSessions);
  const [offers, setOffers] = useState({ 101: { active: true, remaining: 2, price: 80000 }, 103: { active: true, remaining: 2, price: 50000 }, 201: { active: true, remaining: 2, price: 90000 } });
  const [selected, setSelected] = useState(101);
  const [order, setOrder] = useState(null);
  const [seats, setSeats] = useState(2);
  const [price, setPrice] = useState(80000);
  const [message, setMessage] = useState('');
  const [checkedIn, setCheckedIn] = useState(false);
  const center = centers.find(c => c.id === (view === 'staff' ? 1 : venue));
  const session = sessions.find(s => s.id === selected);
  const offer = offers[selected];
  const playerBand = 'B2';
  const staffSessions = sessions.filter(s => s.venue === 1);
  const visible = sessions.filter(s => s.venue === venue && (sport === 'all' || s.sport === sport) && offers[s.id]?.active && offers[s.id].remaining > 0);
  const problem = purchaseProblem(session, offer, playerBand);
  const formSession = staffSessions.find(s => s.id === selected) || staffSessions[0];
  const formError = offerProblem(formSession, Number(seats), Number(price));
  function navigate(next) {
    if (next === 'ticket' && order?.status === 'held') { setSelected(order.sessionId); setView('checkout'); }
    else setView(next);
    setMessage('');
  }
  function inspect(s) { setSelected(s.id); setView('detail'); setMessage(''); }
  function publish() {
    if (formError) return;
    setOffers(old => ({ ...old, [formSession.id]: { active: true, remaining: Number(seats), price: Number(price) } }));
    setMessage(`Đã mở bán ${seats} vé cho ${formSession.court} trong bản mẫu.`);
  }
  function hold() {
    if (problem) return;
    setSessions(old => old.map(s => s.id === selected ? { ...s, holds: s.holds + 1 } : s));
    setOffers(old => ({ ...old, [selected]: { ...old[selected], remaining: old[selected].remaining - 1 } }));
    setOrder({ sessionId: selected, price: offer.price, status: 'held' });
    setView('checkout');
  }
  function cancelHold() {
    setSessions(old => old.map(s => s.id === order.sessionId ? { ...s, holds: s.holds - 1 } : s));
    setOffers(old => ({ ...old, [order.sessionId]: { ...old[order.sessionId], remaining: old[order.sessionId].remaining + 1 } }));
    setOrder(null);setView('browse');
  }
  function pay() {
    setSessions(old => old.map(s => s.id === order.sessionId ? { ...s, holds: s.holds - 1, confirmed: s.confirmed + 1 } : s));
    setOrder({ ...order, status: 'paid' });setView('ticket');
  }
  function checkIn() {
    setSessions(old => old.map(s => s.id === order.sessionId ? { ...s, confirmed: s.confirmed - 1, playing: s.playing + 1 } : s));
    setCheckedIn(true);
  }
  return <div className="hourly-design">
    <div className="hd-preview"><span>BẢN THIẾT KẾ TƯƠNG TÁC · Dữ liệu minh họa</span><span>09/11 · {clock(DEMO_NOW)} · Múi giờ GMT+7</span></div>
    <header className="hd-header"><a className="hd-logo" href="?design=hourly"><span>TK</span> SPORT COMMUNITY</a><div className="hd-header-right"><button onClick={() => navigate('centers')}><Building2 size={17}/>{center.name}<span>⌄</span></button><span className="hd-avatar">N</span></div></header>
    <div className="hd-shell"><aside className="hd-sidebar"><span className="hd-nav-label">KHÔNG GIAN NGƯỜI CHƠI</span><button className={['browse','detail','checkout'].includes(view) ? 'active' : ''} onClick={() => navigate('browse')}><LayoutGrid size={18}/>Lịch social</button><button className={view === 'ticket' ? 'active' : ''} onClick={() => navigate('ticket')}><Ticket size={18}/>Vé của tôi{order?.status === 'paid' && <span className="hd-count">1</span>}</button><button className={view === 'centers' ? 'active' : ''} onClick={() => navigate('centers')}><Building2 size={18}/>Chọn Sport Center</button><div className="hd-nav-divider"/><span className="hd-nav-label">XEM THIẾT KẾ VẬN HÀNH</span><button className={view === 'staff' ? 'active' : ''} onClick={() => { setSelected(101);navigate('staff'); }}><SlidersHorizontal size={18}/>Điểm danh & rao vé</button><div className="hd-sidebar-note"><ShieldCheck size={20}/><span>Nhân sự chỉ thao tác tại cơ sở được phân công.</span></div></aside>
    <main className="hd-main">
      {view === 'centers' && <><div className="hd-heading"><span className="hd-eyebrow">CHỌN KHÔNG GIAN</span><h1>Bạn muốn chơi ở đâu?</h1><p>Một tài khoản, chọn cơ sở phù hợp với bạn.</p></div><div className="hd-center-grid">{centers.map(c => <button key={c.id} className={`hd-center ${venue === c.id ? 'selected' : ''}`} onClick={() => {setVenue(c.id);navigate('browse');}}><div className="hd-center-art"><Building2 size={44}/></div><div><h2>{c.name}</h2><p><MapPin size={15}/>{c.address}</p><span>{c.courts} sân con · Cầu lông & Pickleball</span><strong>Vào cơ sở <ArrowRight size={18}/></strong></div></button>)}</div><div className="hd-notice">Sau đăng nhập, Staff/Manager/Referee chỉ thấy cơ sở được cấp quyền. Bản mẫu nhân sự được phân công tại Cầu Giấy.</div></>}
      {view === 'browse' && <><div className="hd-heading hd-heading-row"><div><span className="hd-eyebrow">CÙNG RA SÂN HÔM NAY</span><h1>Chưa hết giờ. Vẫn kịp chơi.</h1><p>Tham gia buổi đang diễn ra khi cơ sở mở bán thêm chỗ.</p></div><span className="hd-date">09 THÁNG 11 <span>THỨ HAI</span></span></div>
        <section className="hd-live-banner"><div><span className="hd-live-label"><span/> ĐANG MỞ BÁN</span><h2>Vé giờ lẻ · Vào chơi ngay</h2><p>Chọn chỗ trống trong buổi social, thanh toán và vào sân sau khi check-in.</p></div><div className="hd-banner-stamp"><Zap size={32}/><span>ĐẾN HẾT BUỔI</span></div></section>
        <div className="hd-filters"><label><Building2 size={18}/><select aria-label="Sport Center" value={venue} onChange={e => setVenue(Number(e.target.value))}>{centers.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}</select></label><label><SlidersHorizontal size={17}/><select aria-label="Môn thể thao" value={sport} onChange={e => setSport(e.target.value)}><option value="all">Tất cả môn</option><option>Cầu lông</option><option>Pickleball</option></select></label><span className="hd-filter-chip"><Zap size={15}/> Vé vào chơi ngay</span></div>
        <div className="hd-section-title"><h2>Chỗ trống đang rao</h2><span>{visible.length} buổi · {center.address}</span></div>
        <div className="hd-offer-grid">{visible.map(s => <article className="hd-offer" key={s.id}><div className="hd-court-art"><div className="hd-court-lines"/><span className="hd-live-label"><span/> BUỔI ĐANG DIỄN RA</span><span className="hd-court-number">{s.court}</span></div><div className="hd-offer-content"><div className="hd-tags"><span>{s.sport}</span><span>Trình độ {s.band}</span></div><h2>Vào chơi ngay · Vé giờ lẻ</h2><p className="hd-location"><MapPin size={15}/>{centers.find(c => c.id === s.venue).name} · {s.court}</p><div className="hd-offer-facts"><span><Clock3 size={17}/>Buổi {clock(s.start)}–{clock(s.end)}</span><span><Users size={17}/>Còn {Math.min(offers[s.id].remaining, availableSeats(s))} chỗ mở bán</span></div><p className="hd-entry">Còn {durationLabel(s.end - DEMO_NOW)} · Kết thúc {clock(s.end)}</p>{s.band !== playerBand && <p className="hd-warning">Hồ sơ của bạn: {playerBand}. Cần duyệt ngoại lệ.</p>}<div className="hd-card-bottom"><strong>{money(offers[s.id].price)}<small>/ người / phần còn lại</small></strong><button className="hd-primary" onClick={() => inspect(s)}>Xem vé <ArrowRight size={17}/></button></div></div></article>)}</div>
        {!visible.length && <div className="hd-empty"><Ticket size={32}/><h2>Chưa có chỗ trống đang rao</h2><p>Thử chọn cơ sở hoặc môn khác.</p></div>}
        <p className="hd-caption">Chỉ mua thêm suất tham gia buổi social. Giá vé và số chỗ do cơ sở mở bán; vẫn áp dụng điều kiện trình độ.</p>
      </>}
      {view === 'detail' && <><button className="hd-back" onClick={() => navigate('browse')}><ArrowLeft size={17}/> Quay lại lịch social</button><div className="hd-heading"><span className="hd-eyebrow">{centers.find(c => c.id === session.venue).name.toUpperCase()} · {session.court.toUpperCase()}</span><h1>Vào sân ngay, chơi cùng mọi người.</h1></div><div className="hd-detail-grid"><section className="hd-panel"><div className="hd-court-art hd-detail-art"><div className="hd-court-lines"/><span className="hd-live-label"><span/> ĐANG DIỄN RA</span><span className="hd-court-number">{session.sport} · {session.court}</span></div><div className="hd-panel-body"><h2>Buổi social {clock(session.start)}–{clock(session.end)}</h2><div className="hd-info-grid"><div><span>Thời lượng vé</span><strong>Đến khi buổi chơi kết thúc</strong></div><div><span>Giờ hết hiệu lực</span><strong>{clock(session.end)} hôm nay</strong></div><div><span>Trình độ yêu cầu</span><strong>{session.band} · Hồ sơ của bạn: {playerBand}</strong></div><div><span>Chỗ đang mở bán</span><strong>{offer.remaining} / {session.capacity} suất của buổi</strong></div></div><h3>Vé này bao gồm</h3><ul className="hd-checklist"><li><Check size={17}/>Tham gia cùng nhóm đang chơi trong buổi social.</li><li><Check size={17}/>Vé hết hiệu lực cùng giờ kết thúc buổi, dù bạn vào sân muộn hơn.</li><li><Check size={17}/>Vào đúng cụm sân và sân con ghi trên vé.</li></ul></div></section><aside className="hd-checkout-box"><span className="hd-eyebrow">VÉ PHẦN THỜI GIAN CÒN LẠI</span><h2>{money(offer.price)}<small> / người</small></h2><p>Còn {durationLabel(session.end - DEMO_NOW)} tính từ {clock(DEMO_NOW)}. Vé kết thúc lúc {clock(session.end)}.</p><div className="hd-notice">Vào lúc {clock(DEMO_NOW)} → chơi đến {clock(session.end)}. Vào trễ hơn vẫn kết thúc lúc {clock(session.end)}.</div>{problem && <p className="hd-warning" role="alert">{problem}</p>}<button className="hd-primary" disabled={!!problem || order?.status === 'held' || order?.status === 'paid'} onClick={hold}>{order ? 'Bạn đã có vé / đơn trong bản mẫu' : 'Giữ 1 chỗ & thanh toán'}<ArrowRight size={18}/></button><p className="hd-caption">Giữ chỗ và thanh toán được mô phỏng trong bản thiết kế. Không phát sinh giao dịch thật.</p></aside></div></>}
      {view === 'checkout' && order && <section className="hd-single-panel"><span className="hd-eyebrow">GIỮ CHỖ THÀNH CÔNG · MÔ PHỎNG</span><h1>Hoàn tất vé vào chơi ngay</h1><div className="hd-receipt"><span>{centers.find(c => c.id === session.venue).name}</span><strong>{session.court} · {session.sport} · 1 người</strong><span>Vé kết thúc lúc {clock(session.end)}. Giá áp dụng cho phần thời gian còn lại, không phải một giờ tính từ lúc vào sân.</span><div><span>Thanh toán</span><strong>{money(order.price)}</strong></div></div><button className="hd-primary" onClick={pay}>Mô phỏng thanh toán thành công <ArrowRight size={18}/></button><button className="hd-back" onClick={cancelHold}>Hủy giữ chỗ, trả lại suất</button></section>}
      {view === 'ticket' && (order?.status === 'paid' ? <section className="hd-single-panel"><span className="hd-success-icon"><Check size={28}/></span><span className="hd-eyebrow">VÉ MINH HỌA · ĐÃ THANH TOÁN</span><h1>{checkedIn ? 'Bạn đã vào sân!' : 'Sẵn sàng ra sân'}</h1><div className="hd-ticket-code">TKSC · DEMO · 001</div><div className="hd-receipt"><strong>{centers.find(c => c.id === sessions.find(s => s.id === order.sessionId).venue).name}</strong><span>{sessions.find(s => s.id === order.sessionId).court} · Vé tham gia phần buổi còn lại</span><strong>{checkedIn ? `${clock(DEMO_NOW)}–${clock(sessions.find(s => s.id === order.sessionId).end)}` : 'Có hiệu lực đến giờ kết thúc buổi'}</strong><span>{checkedIn ? 'Hết hiệu lực sau giờ kết thúc ghi trên vé.' : 'Xuất trình vé cho Staff trước khi buổi kết thúc. Check-in muộn không kéo dài thời hạn vé.'}</span></div><button className="hd-primary" disabled={checkedIn} onClick={checkIn}>{checkedIn ? 'Đã xác nhận vào sân' : 'Mô phỏng Staff check-in'}</button></section> : <div className="hd-empty"><Ticket size={36}/><h1>Bạn chưa có vé giờ lẻ</h1><button className="hd-primary" onClick={() => navigate('browse')}>Tìm chỗ đang rao <ArrowRight size={18}/></button></div>)}
      {view === 'staff' && <><div className="hd-heading hd-heading-row"><div><span className="hd-eyebrow">VẬN HÀNH · TK SPORT CẦU GIẤY</span><h1>Còn chỗ? Mở thêm cuộc chơi.</h1><p>Rao suất còn trống của buổi đang diễn ra tại cơ sở được phân công.</p></div><span className="hd-staff-badge"><ShieldCheck size={17}/>Manager · Cầu Giấy</span></div><div className="hd-detail-grid"><section className="hd-panel"><div className="hd-panel-body"><h2>Buổi đang diễn ra</h2><div className="hd-staff-sessions">{staffSessions.map(s => <button key={s.id} className={formSession.id === s.id ? 'selected' : ''} onClick={() => {setSelected(s.id);setSeats(Math.max(1, availableSeats(s)));setMessage('');}}><div><strong>{s.court} · {s.sport}</strong><span>{clock(s.start)}–{clock(s.end)} · {s.band}</span></div><span className={availableSeats(s) ? 'hd-seat-badge' : 'hd-full-badge'}>{availableSeats(s) ? `${availableSeats(s)} chỗ có thể bán` : 'Đủ chỗ'}</span></button>)}</div><div className="hd-seat-breakdown"><div><strong>{formSession.playing}</strong><span>Đang chơi</span></div><div><strong>{formSession.confirmed}</strong><span>Vé đã trả tiền, chưa tới</span></div><div><strong>{formSession.holds}</strong><span>Đang giữ chỗ</span></div><div><strong>{availableSeats(formSession)}</strong><span>Có thể bán</span></div></div><div className="hd-notice">Người đã mua vé nhưng chưa đến vẫn chiếm suất. Không tự lấy suất đó để bán thêm.</div><h3 className="hd-block-heading">Đợt rao hiện tại</h3><div className="hd-published"><span>{offers[formSession.id]?.active ? 'Đang mở bán' : 'Chưa mở / đã dừng'} · còn {offers[formSession.id]?.remaining || 0} vé</span>{offers[formSession.id] && <button className="hd-secondary" onClick={() => setOffers(old => ({...old,[formSession.id]: {...old[formSession.id],active:!old[formSession.id].active}}))}>{offers[formSession.id].active ? <Pause size={16}/> : <Play size={16}/>} {offers[formSession.id].active ? 'Dừng bán' : 'Mở lại'}</button>}</div></div></section><section className="hd-checkout-box"><span className="hd-eyebrow">RAO CHỖ TRỐNG · {formSession.court}</span><h2>Rao phần buổi còn lại</h2><label>Số suất mở bán<input aria-label="Số suất mở bán" type="number" min="1" max={availableSeats(formSession)} value={seats} onChange={e => setSeats(e.target.value)}/></label><label>Giá phần còn lại / người (VNĐ)<input aria-label="Giá vé giờ lẻ" type="number" min="1000" step="1000" value={price} onChange={e => setPrice(e.target.value)}/></label><div className="hd-receipt"><span>Thời gian còn lại: {durationLabel(formSession.end - DEMO_NOW)}</span><span>Vé kết thúc cùng buổi: {clock(formSession.end)}</span><span>Trình độ: {formSession.band}, giữ nguyên điều kiện buổi</span></div>{formError && <p className="hd-warning" role="alert">{formError}</p>}<button className="hd-primary" disabled={!!formError} onClick={publish}><Plus size={18}/>Mở bán vé giờ lẻ</button>{message && <p role="status" className="hd-success">{message}</p>}<button className="hd-back" onClick={() => {setVenue(1);navigate('browse');}}>Xem phía Player <ArrowRight size={16}/></button></section></div></>}
    </main></div>
  </div>;
}
