import './issue-hub.css';
import { useEffect, useState } from 'react';
import { aftercare } from './aftercareClient';
import { buildIssues } from './issueHubModel';
import { OperationItem } from './TicketOperationCards';
import TicketSupport from './TicketSupport';

export default function IssueHub() {
 const [rows,setRows]=useState([]),[selected,setSelected]=useState(null),[loading,setLoading]=useState(true),[busy,setBusy]=useState(false),[error,setError]=useState(''),[notice,setNotice]=useState('');
 useEffect(()=>{
  let active=true;
  load().then(result=>{if(active){setRows(result.rows);setError(result.error);}})
   .finally(()=>{if(active)setLoading(false);});
  return()=>{active=false;};
 },[]);
 async function load(){
  const kinds=['reviews','sessions','refunds','inbox'];
  const results=await Promise.allSettled(kinds.map(kind=>aftercare[kind==='sessions'?'incidents':kind]()));
  const data={},errors=[];
  results.forEach((result,i)=>{if(result.status==='fulfilled')data[kinds[i]]=result.value;else errors.push(result.reason.message);});
  return {rows:buildIssues(data),error:errors.length?`Một số danh sách chưa tải được: ${errors.join('; ')}`:''};
 }
 async function refresh(action){
  if(busy)return;
  setBusy(true);setError('');setNotice('');
  try{
   if(action)await action();
   const result=await load();setRows(result.rows);setError(result.error);
   if(action)setNotice('Đã lưu kết quả xử lý.');
  }catch(e){setError(e.message);}finally{setBusy(false);}
 }
 const current=rows.find(row=>row.key===selected);
 return <section className="issue-hub" aria-label="Quản lý sự cố tập trung">
  <div className="payment-heading"><p>Thanh toán, tình trạng sân, hoàn tiền tồn và báo cáo người chơi.</p><button className="button button-secondary" disabled={busy} onClick={()=>refresh()}>Tải lại sự cố</button></div>
  {error&&<p className="payment-error" role="alert">{error}</p>}
  {notice&&<p role="status">{notice}</p>}
  {loading?<p role="status">Đang tải sự cố…</p>:<div className="issue-hub-layout">
   <div className="issue-hub-list" aria-label="Danh sách sự cố">
    {!rows.length&&<p>Không có sự cố cần xử lý.</p>}
    {rows.map(row=><button key={row.key} className="issue-row" aria-pressed={selected===row.key} onClick={()=>{setSelected(row.key);setNotice('');}}>
     <span className="payment-badge">{row.type} · {row.status}</span><strong>{row.title}</strong><span>{row.summary}</span>
    </button>)}
   </div>
   <section className="issue-hub-detail" aria-label="Chi tiết sự cố" aria-live="polite">
    {current?<><h2>{current.title}</h2><p>{current.type} · {current.status}</p>
     {current.kind==='inbox'?<TicketSupport key={current.key} ticketId={current.item.ticketId} staff initialOpen/>:
      <OperationItem key={current.key} tab={current.kind} item={current.item} busy={busy} run={refresh}/>}
    </>:<p>Chọn một sự cố trong danh sách để xem chi tiết và phản hồi.</p>}
   </section>
  </div>}
 </section>;
}
