import { useEffect, useState } from 'react';
import { aftercare } from './aftercareClient';
import './payment.css';

export default function TicketSupport({ticketId, staff=false, initialOpen=false}) {
 const [report,setReport]=useState(false),[category,setCategory]=useState('court'),[notice,setNotice]=useState(''),[open,setOpen]=useState(initialOpen),[data,setData]=useState(null),[content,setContent]=useState(''),[file,setFile]=useState(null),[busy,setBusy]=useState(false),[error,setError]=useState('');
 useEffect(()=>{
  if(!open)return;
  let active=true;
  const load=()=>aftercare.messages(ticketId).then(value=>{if(active)setData(value);}).catch(e=>{if(active)setError(e.message);});
  load(); const timer=setInterval(load,10000);
  return()=>{active=false;clearInterval(timer);};
 },[open,ticketId]);
 async function send(e){
  e.preventDefault();if(busy||(report&&content.trim().length<10))return;const form=e.currentTarget;setBusy(true);setError('');
  try{
   let encoded=null;
   if(file)encoded=await new Promise((resolve,reject)=>{const reader=new FileReader();reader.onload=()=>resolve(String(reader.result).split(',')[1]);reader.onerror=()=>reject(new Error('Không đọc được ảnh.'));reader.readAsDataURL(file);});
   if(report)await aftercare.report(ticketId,category,content.trim(),encoded);
   else await aftercare.sendMessage(ticketId,content.trim(),encoded);
   setNotice(report?'Đã gửi báo cáo sự cố. Staff/Quản lý sẽ phản hồi trong hội thoại này.':'Đã gửi tin nhắn.');setReport(false);
   setContent('');setFile(null);form.reset();
   setData(await aftercare.messages(ticketId));
  }catch(err){setError(err.message);}finally{setBusy(false);}
 }
 const imageUrl=id=>'/api/ticket-aftercare/tickets/'+ticketId+'/messages/'+id+'/image';
 return <section className="ticket-support"><button type="button" className="button button-secondary" aria-expanded={open} onClick={()=>setOpen(!open)}>{open?'Thu gọn trao đổi':staff?'Xem báo cáo & trả lời':'Trao đổi hỗ trợ vé'}</button>
 {!staff&&<button type="button" className="button button-secondary" onClick={()=>{setOpen(true);setReport(true);setNotice('');}}>Báo cáo sự cố</button>}
 {open&&<div className="aftercare-form"><h4>Trao đổi về vé #{ticketId}</h4><p>{staff?'Đọc báo cáo và phản hồi cho người chơi tại đây. Với hoàn tiền, kiểm tra tài khoản nhận thuộc người thanh toán và xác nhận xử lý sau khi giao dịch thành công.':data?.canProvidePayout?'Bạn có thể gửi QR ngân hàng hoặc tên ngân hàng, số tài khoản và tên chủ tài khoản nhận hoàn.':'Tiền hoàn thuộc người thanh toán ban đầu. Nhờ người đó gửi tài khoản/QR nhận hoàn; bạn vẫn có thể trao đổi với Staff.'}</p><p>Không gửi mật khẩu hoặc OTP. Gửi tin nhắn không đồng nghĩa đã hoàn tiền.</p>
 {notice&&<p role="status">{notice}</p>}
 {error&&<p className="payment-error" role="alert">{error}</p>}
 <div className="support-messages" aria-label="Lịch sử trao đổi">{!data?<p>Đang tải trao đổi…</p>:!data.messages.length?<p>Chưa có tin nhắn. Gửi thông tin để Staff hỗ trợ.</p>:data.messages.map(m=><article key={m.id} className={m.isStaff?'support-message staff':'support-message'}><strong>{m.isStaff?'Staff · ':''}{m.sender}</strong><time>{new Date(m.createdAt).toLocaleString('vi-VN')}</time>{m.content&&<p>{m.content}</p>}{m.hasImage&&<a href={imageUrl(m.id)} target="_blank" rel="noreferrer"><img loading="lazy" src={imageUrl(m.id)} alt="Ảnh đính kèm trong trao đổi vé"/></a>}</article>)}</div>
 <form onSubmit={send}>{report&&<><h4>Báo cáo sự cố</h4><label>Loại sự cố<select value={category} disabled={busy} onChange={e=>setCategory(e.target.value)}><option value="court">Sân hỏng</option><option value="flood">Sân ngập</option><option value="payment">Thanh toán lỗi</option><option value="refund">Chậm hoàn / thiếu quỹ hoàn</option><option value="other">Sự cố khác</option></select></label><p>Mô tả 10–1800 ký tự; có thể đính kèm ảnh. Báo cáo không tự hủy vé hoặc xác nhận hoàn tiền.</p></>}<label>{staff?'Phản hồi cho người chơi':'Tin nhắn / thông tin tài khoản'}<textarea required={report} minLength={report?10:undefined} maxLength={report?1800:2000} value={content} disabled={busy} onChange={e=>setContent(e.target.value)} placeholder={staff?'Đã nhận thông tin, dự kiến xử lý…':'Ngân hàng, số tài khoản, tên chủ tài khoản hoặc nội dung cần hỗ trợ…'}/></label><label>Đính kèm QR hoặc biên nhận (PNG/JPG, tối đa 2 MB)<input type="file" accept="image/png,image/jpeg" disabled={busy} onChange={e=>{const f=e.target.files?.[0];setError('');setFile(null);if(f&&(!['image/png','image/jpeg'].includes(f.type)||f.size>2097152)){setError('Chọn ảnh PNG/JPG tối đa 2 MB.');e.target.value='';return;}setFile(f||null);}}/></label><button className="button button-primary" disabled={busy||(report?content.trim().length<10:(!content.trim()&&!file))}>{busy?'Đang gửi…':staff?'Gửi phản hồi':report?'Gửi báo cáo sự cố':'Gửi cho Staff'}</button>{report&&<button type="button" className="button button-secondary" disabled={busy} onClick={()=>setReport(false)}>Hủy báo cáo</button>}</form></div>}
 </section>;
}
