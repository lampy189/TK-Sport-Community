import { useState } from 'react';
import { aftercare } from './aftercareClient';
const money=n=>new Intl.NumberFormat('vi-VN',{style:'currency',currency:'VND'}).format(n);
export default function TicketIssueActions({ticket,onRefresh}) {
 const [options,setOptions]=useState(null),[selected,setSelected]=useState(''),[confirm,setConfirm]=useState(''),[error,setError]=useState(''),[busy,setBusy]=useState(false),[notice,setNotice]=useState('');
 const target=options?.find(o=>String(o.id)===selected);
 async function run(action){if(busy)return;setBusy(true);setError('');try{await action();}catch(e){setError(e.message);}finally{setBusy(false);}}
 return <div className="ticket-issue" role="region" aria-label="Sự cố buổi chơi">
  <strong>Buổi chơi {ticket.sessionStatus==='cancelled'?'đã hủy':'đang tạm ngưng'} do sự cố sân</strong>
  {ticket.venueIssue&&<p>{ticket.venueIssue}</p>}
  <p>QR của buổi này không thể check-in. Bạn có thể đổi buổi hoặc yêu cầu hoàn tiền về người thanh toán.</p>
  {error&&<p role="alert" className="payment-error">{error}</p>}{notice&&<p role="status">{notice}</p>}
  {ticket.canResolveIssue && ticket.status==='confirmed' && !ticket.checkedIn && !ticket.refundStatus && (ticket.pendingChangeId?<>
   <p>Đang chờ Staff xử lý yêu cầu đổi vé. Chỗ ở buổi mới chỉ được xác nhận khi xử lý thành công.</p>
   <button className="button button-secondary" disabled={busy} onClick={()=>run(async()=>{await aftercare.cancelChange(ticket.pendingChangeId);await onRefresh();setNotice('Đã rút yêu cầu đổi vé.');})}>Rút yêu cầu đổi</button>
  </>:<>
   <div className="payment-actions"><button className="button button-secondary" disabled={busy} onClick={()=>run(async()=>{setOptions(await aftercare.options(ticket.id));setConfirm('change');})}>Đổi sang buổi khác</button><button className="button ticket-cancel" disabled={busy} onClick={()=>setConfirm('refund')}>Yêu cầu hoàn tiền</button></div>
   {confirm==='change'&&<div className="aftercare-form"><label>Buổi chơi còn chỗ<select value={selected} onChange={e=>setSelected(e.target.value)}><option value="">Chọn buổi mới</option>{options?.map(o=><option key={o.id} value={o.id}>{o.sessionDate} · {o.startTime.slice(0,5)}–{o.endTime.slice(0,5)} · {o.court} · {money(o.priceVnd)} · còn {o.available} chỗ</option>)}</select></label>{options?.length===0&&<p>Chưa có buổi phù hợp còn chỗ, cùng môn và đúng trình độ.</p>}{target&&<p>{target.differenceVnd>0?`Cần trả thêm ${money(target.differenceVnd)}. Staff sẽ đối chiếu khoản thu thêm trước khi đổi.`:target.differenceVnd<0?`Được yêu cầu hoàn chênh lệch ${money(-target.differenceVnd)} sau khi đổi.`:'Không phát sinh chênh lệch giá.'} Yêu cầu chưa giữ chỗ; Staff kiểm tra lại trước khi xác nhận.</p>}<button className="button button-primary" disabled={busy||!target} onClick={()=>run(async()=>{await aftercare.change(ticket.id,Number(selected));setConfirm('');await onRefresh();setNotice('Đã gửi yêu cầu đổi vé.');})}>Gửi yêu cầu đổi vé</button></div>}
   {confirm==='refund'&&<div className="aftercare-form"><p>Xác nhận yêu cầu hoàn {money(ticket.amountVnd)}? Vé sẽ bị hủy ngay; Staff xác nhận riêng sau khi chuyển tiền.</p><div className="payment-actions"><button className="button button-secondary" disabled={busy} onClick={()=>setConfirm('')}>Quay lại</button><button className="button ticket-cancel" disabled={busy} onClick={()=>run(async()=>{await aftercare.refund(ticket.id);setConfirm('');await onRefresh();})}>Xác nhận yêu cầu hoàn</button></div></div>}
  </>)}
 </div>;
}
