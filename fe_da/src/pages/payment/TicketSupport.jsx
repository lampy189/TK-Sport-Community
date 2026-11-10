import { useEffect, useState } from 'react';
import { aftercare } from './aftercareClient';
import './payment.css';

export default function TicketSupport({ticketId, staff=false, initialOpen=false}) {
 const [open,setOpen]=useState(initialOpen),[data,setData]=useState(null),[content,setContent]=useState(''),[file,setFile]=useState(null),[busy,setBusy]=useState(false),[error,setError]=useState('');
 useEffect(()=>{
  if(!open)return;
  let active=true;
  const load=()=>aftercare.messages(ticketId).then(value=>{if(active)setData(value);}).catch(e=>{if(active)setError(e.message);});
  load(); const timer=setInterval(load,10000);
  return()=>{active=false;clearInterval(timer);};
 },[open,ticketId]);
 async function send(e){
  e.preventDefault();if(busy)return;const form=e.currentTarget;setBusy(true);setError('');
  try{
   let encoded=null;
   if(file)encoded=await new Promise((resolve,reject)=>{const reader=new FileReader();reader.onload=()=>resolve(String(reader.result).split(',')[1]);reader.onerror=()=>reject(new Error('Không đọc được ảnh.'));reader.readAsDataURL(file);});
   await aftercare.sendMessage(ticketId,content.trim(),encoded);
   setContent('');setFile(null);form.reset();
   setData(await aftercare.messages(ticketId));
  }catch(err){setError(err.message);}finally{setBusy(false);}
 }
 const imageUrl=id=>'/api/ticket-aftercare/tickets/'+ticketId+'/messages/'+id+'/image';
 return <section className="ticket-support"><button type="button" className="button button-secondary" aria-expanded={open} onClick={()=>setOpen(!open)}>{open?'Thu gọn trao đổi':staff?'Xem thông tin nhận tiền & trả lời':'Gửi QR / số tài khoản & trao đổi với Staff'}</button>
 {open&&<div className="aftercare-form"><h4>Trao đổi về vé #{ticketId}</h4><p>{staff?'Kiểm tra tài khoản nhận hoàn thuộc người thanh toán trước khi chuyển tiền. Trả lời tại đây; dùng nút xác nhận xử lý sau khi giao dịch thành công.':data?.canProvidePayout?'Bạn có thể gửi QR ngân hàng hoặc tên ngân hàng, số tài khoản và tên chủ tài khoản nhận hoàn.':'Tiền hoàn thuộc người thanh toán ban đầu. Nhờ người đó gửi tài khoản/QR nhận hoàn; bạn vẫn có thể trao đổi với Staff.'}</p><p>Không gửi mật khẩu hoặc OTP. Gửi tin nhắn không đồng nghĩa đã hoàn tiền.</p>
 {error&&<p className="payment-error" role="alert">{error}</p>}
 <div className="support-messages" aria-label="Lịch sử trao đổi">{!data?<p>Đang tải trao đổi…</p>:!data.messages.length?<p>Chưa có tin nhắn. Gửi thông tin để Staff hỗ trợ.</p>:data.messages.map(m=><article key={m.id} className={m.isStaff?'support-message staff':'support-message'}><strong>{m.isStaff?'Staff · ':''}{m.sender}</strong><time>{new Date(m.createdAt).toLocaleString('vi-VN')}</time>{m.content&&<p>{m.content}</p>}{m.hasImage&&<a href={imageUrl(m.id)} target="_blank" rel="noreferrer"><img loading="lazy" src={imageUrl(m.id)} alt="Ảnh đính kèm trong trao đổi vé"/></a>}</article>)}</div>
 <form onSubmit={send}><label>{staff?'Phản hồi cho người chơi':'Tin nhắn / thông tin tài khoản'}<textarea maxLength={2000} value={content} disabled={busy} onChange={e=>setContent(e.target.value)} placeholder={staff?'Đã nhận thông tin, dự kiến xử lý…':'Ngân hàng, số tài khoản, tên chủ tài khoản hoặc nội dung cần hỗ trợ…'}/></label><label>Đính kèm QR hoặc biên nhận (PNG/JPG, tối đa 2 MB)<input type="file" accept="image/png,image/jpeg" disabled={busy} onChange={e=>{const f=e.target.files?.[0];setError('');setFile(null);if(f&&(!['image/png','image/jpeg'].includes(f.type)||f.size>2097152)){setError('Chọn ảnh PNG/JPG tối đa 2 MB.');e.target.value='';return;}setFile(f||null);}}/></label><button className="button button-primary" disabled={busy||(!content.trim()&&!file)}>{busy?'Đang gửi…':staff?'Gửi phản hồi':'Gửi cho Staff'}</button></form></div>}
 </section>;
}
