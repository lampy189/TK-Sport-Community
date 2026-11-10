import { useEffect, useState } from 'react';
import { aftercare } from './aftercareClient';
import { OperationItem } from './TicketOperationCards';
import IssueHub from './IssueHub';
import './payment.css';

export default function TicketOperations({initialTab='reviews'}) {
 const [tab,setTab]=useState(initialTab),[items,setItems]=useState([]),[busy,setBusy]=useState(false),[loading,setLoading]=useState(true),[error,setError]=useState(''),[notice,setNotice]=useState('');
 useEffect(()=>{
  if(tab==='issues')return;
  let active=true;
  aftercare[tab]().then(data=>{if(active)setItems(data);})
   .catch(e=>{if(active)setError(e.message);}).finally(()=>{if(active)setLoading(false);});
  return()=>{active=false;};
 },[tab]);
 async function run(action){
  if(busy)return;
  setBusy(true);setError('');setNotice('');
  try{await action();setItems(await aftercare[tab]());setNotice('Đã xử lý thành công. Thông báo được gửi tới người liên quan.');}
  catch(e){setError(e.message);}finally{setBusy(false);}
 }
 return <section className="payment-workspace ticket-operations">
  <div className="payment-heading"><h1>{tab==='issues'?'Sự cố':'Xử lý vé & thanh toán'}</h1>{tab!=='issues'&&<button className="button button-secondary" disabled={busy} onClick={()=>run(async()=>{})}>Tải lại</button>}</div>
  <nav className="ticket-filters" aria-label="Chức năng xử lý vé">
   {[['issues','Sự cố'],['reviews','Thanh toán cần kiểm tra'],['sessions','Báo sự cố sân'],['changes','Yêu cầu đổi vé'],['refunds','Hoàn tiền']].map(([id,label])=>
    <button key={id} className="button" disabled={busy} aria-pressed={tab===id} onClick={()=>{if(id===tab)return;setTab(id);setItems([]);setLoading(true);setError('');setNotice('');}}>{label}</button>)}
  </nav>
  {tab==='issues'?<IssueHub/>:<>
   {error&&<p className="payment-error" role="alert">{error}</p>}
   {notice&&<p className="payment-notice" role="status">{notice}</p>}
   {loading?<p role="status">Đang tải…</p>:!items.length?<p>Không có mục cần xử lý.</p>:items.map(item=><OperationItem key={item.id} tab={tab} item={item} busy={busy} run={run}/>)}
  </>}
 </section>;
}
