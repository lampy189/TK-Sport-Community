import { ticketState as state, sessionTime as start } from './ticketState';
import { useState } from 'react';
import { QRCodeSVG } from 'qrcode.react';
import StatePanel from '../../components/ui/StatePanel';
const money = n => new Intl.NumberFormat('vi-VN',{style:'currency',currency:'VND'}).format(n);
const labels = {confirmed:'Sắp diễn ra',pending:'Chờ thanh toán',cancelled:'Đã hủy',expired:'Hết hạn',completed:'Đã kết thúc',checkedin:'Đã check-in',review_required:'Đang kiểm tra',invalidated:'Không còn hiệu lực',processing:'Đang xác nhận'};
export default function TicketList({tickets,now,busy,onRefresh,onPay,onCancel,onBrowse,initialFilter='all'}) {
  const [filter,setFilter]=useState(initialFilter);
  const [query,setQuery]=useState('');
  const [detail,setDetail]=useState(null);
  const [cancelId,setCancelId]=useState(null);
  const visible=tickets.filter(t=>(filter==='all'||(filter==='history'?['cancelled','expired','completed','invalidated','checkedin'].includes(state(t,now)):state(t,now)===filter))&&[t.ticketCode,t.sportName,t.courtName,t.venueName,t.holderName].join(' ').toLocaleLowerCase('vi').includes(query.toLocaleLowerCase('vi'))).sort((a,b)=>(start(b)||0)-(start(a)||0));
  return <>
    <div className="ticket-toolbar"><input aria-label="Tìm vé" placeholder="Tìm sân, người chơi, mã vé…" value={query} onChange={e=>setQuery(e.target.value)}/><button className="button button-secondary" disabled={busy} onClick={onRefresh}>Cập nhật</button>{onBrowse&&<button className="button button-primary" onClick={onBrowse}>Đặt buổi chơi</button>}</div>
    <div className="ticket-filters" aria-label="Lọc vé">{[['all','Tất cả'],['confirmed','Sắp diễn ra'],['pending','Chờ thanh toán'],['history','Lịch sử']].map(([key,label])=><button key={key} className="button" aria-pressed={filter===key} onClick={()=>setFilter(key)}>{label}</button>)}</div>
    <div className="ticket-list">{!visible.length&&<StatePanel title={tickets.length?'Không có vé phù hợp':'Bạn chưa có vé nào'} description="Chọn buổi chơi mới hoặc thay đổi bộ lọc để tìm vé."/>}
    {visible.map(t=>{const status=state(t,now); const open=detail===t.id; const canCancel=t.canManage!==false&&['pending','confirmed'].includes(status)&&(status==='pending'||start(t)-now>=43200000);
      return <article className="ticket-stub" key={t.id}><div className="ticket-main">
        <div className="ticket-top"><h4>{t.sportName||'Buổi chơi'} · {t.courtName||'Thông tin sân đang cập nhật'}</h4><span className={`payment-badge ${status}`}>{labels[status]}</span></div>
        <div className="ticket-meta">{t.venueName&&<span>{t.venueName}</span>}{t.sessionDate&&<span>{new Date(t.sessionDate+'T00:00:00+07:00').toLocaleDateString('vi-VN',{timeZone:'Asia/Ho_Chi_Minh'})}</span>}{t.startTime&&<span>{t.startTime.slice(0,5)} – {t.endTime?.slice(0,5)}</span>}{t.holderName&&<span>Người chơi: {t.holderName}</span>}</div>
        <strong className="ticket-price">{money(t.amountVnd)}</strong>
        {t.refundStatus&&<p className="ticket-refund">{({requested:'Đã yêu cầu hoàn tiền',processing:'Đang xử lý hoàn tiền',completed:'Đã hoàn tiền',failed:'Hoàn tiền chưa thành công'})[t.refundStatus]||'Đang kiểm tra hoàn tiền'} · {money(t.refundAmountVnd||0)}<br/><small>Hoàn về người thanh toán ban đầu.</small></p>}
        <div className="payment-actions"><button className="button button-secondary" aria-expanded={open} onClick={()=>setDetail(open?null:t.id)}>{open?'Thu gọn':'Chi tiết vé'}</button>{canCancel&&<button className="button ticket-cancel" disabled={busy} onClick={()=>setCancelId(t.id)}>Hủy vé</button>}</div>
        {open&&<div className="ticket-expanded">{status==='confirmed'&&t.ticketCode?<><QRCodeSVG value={t.ticketCode} size={148} title="Mã vé xuất trình tại sân"/><code className="ticket-code">{t.ticketCode}</code><p>Xuất trình mã vé khi đến sân.</p></>:<p>{labels[status]}. Vé chỉ sử dụng được khi đã xác nhận và còn hiệu lực.</p>}{status==='confirmed'&&<p>Hủy trước giờ chơi ít nhất 12 giờ để yêu cầu hoàn 100% tiền vé. Nhân viên sẽ xử lý hoàn tiền.</p>}{t.canManage===false&&<p>Người thanh toán quản lý thanh toán và hủy vé này.</p>}</div>}
        {cancelId===t.id&&<div className="ticket-confirm" role="region" aria-label="Xác nhận hủy vé"><strong>Hủy vé của {t.holderName||'bạn'}?</strong><p>{status==='confirmed'?`Chỗ sẽ được trả lại. Yêu cầu hoàn ${money(t.amountVnd)} sẽ gửi cho nhân viên, tiền chưa được hoàn ngay.`:'Chỗ giữ sẽ được trả lại. Bạn cần đặt mới nếu muốn tham gia.'} Các vé khác trong đơn được giữ nguyên.</p><div className="payment-actions"><button className="button button-secondary" disabled={busy} onClick={()=>setCancelId(null)}>Giữ vé</button><button className="button ticket-cancel" disabled={busy} onClick={async()=>{await onCancel(t.id);setCancelId(null);}}>Xác nhận hủy</button></div></div>}
      </div><div className="ticket-side">{status==='pending'&&t.canManage!==false?<button className="button button-primary" disabled={busy} onClick={()=>onPay(t)}>Thanh toán ngay</button>:<>{status==='confirmed' && t.ticketCode && <QRCodeSVG value={t.ticketCode} size={144} marginSize={4} title="QR check-in tại sân"/>}<span className="ticket-side-label">{status==='confirmed'?'Mã vé':'Trạng thái vé'}</span><strong className="ticket-code">{status==='confirmed'?t.ticketCode:labels[status]}</strong>{status==='confirmed'&&<button className="button button-secondary" onClick={()=>setDetail(open?null:t.id)}>Mở mã QR</button>}</>}</div></article>;
    })}</div>
  </>;
}
