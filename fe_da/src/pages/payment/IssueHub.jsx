import PaymentRecoveryPage from './PaymentRecoveryPage';
import OperationalExceptions from './OperationalExceptions';
import IssueWorkflowControls from './IssueWorkflowControls';
import { incidents,issueLabels } from './incidentClient';
import './issue-hub.css';
import { useEffect, useState } from 'react';
import { aftercare } from './aftercareClient';
import { buildIssues } from './issueHubModel';
import { OperationItem } from './TicketOperationCards';
import TicketSupport from './TicketSupport';

export default function IssueHub({role='staff'}) {
 const [rows,setRows]=useState([]),[selected,setSelected]=useState(null),[loading,setLoading]=useState(true),[busy,setBusy]=useState(false),[error,setError]=useState(''),[notice,setNotice]=useState(''),[states,setStates]=useState([]),[filter,setFilter]=useState('active');
 useEffect(()=>{
  let active=true;
  load().then(result=>{if(active){setRows(result.rows);setStates(result.states);setError(result.error);}})
   .finally(()=>{if(active)setLoading(false);});
  return()=>{active=false;};
 },[]);
 async function load(){
  const kinds=['reviews','sessions','refunds','inbox'];
  const results=await Promise.allSettled([...kinds.map(kind=>aftercare[kind==='sessions'?'incidents':kind]()),incidents.states(),incidents.cases()]);
  const data={},errors=[];
  results.slice(0,4).forEach((result,i)=>{if(result.status==='fulfilled'&&Array.isArray(result.value))data[kinds[i]]=result.value;else errors.push(result.reason?.message||'Danh sách không hợp lệ.');});
  const stateResult=results[4],caseResult=results[5];
  const states=stateResult.status==='fulfilled'&&Array.isArray(stateResult.value)?stateResult.value:[];
  const operations=caseResult.status==='fulfilled'&&Array.isArray(caseResult.value)?caseResult.value:[];
  if(stateResult.status==='rejected')errors.push(stateResult.reason.message);
  if(caseResult.status==='rejected')errors.push(caseResult.reason.message);
  return {states,rows:[...buildIssues(data),...operations.map(o=>({key:`operations-${o.item.id}`,kind:'operations',item:o.item,title:`Ngoại lệ #${o.item.id} · ${o.resource.title}`,type:'Ngoại lệ vận hành',summary:o.item.reason,status:({submitted:'Chờ duyệt',approved:'Đã duyệt, chờ xử lý',executed:'Đã thực hiện, chờ đóng hồ sơ',rejected:'Đã từ chối',closed:'Đã hoàn tất'})[o.item.status]}))],error:errors.length?`Một số danh sách chưa tải được: ${errors.join('; ')}`:''};
 }
 async function refresh(action){
  if(busy)return;
  setBusy(true);setError('');setNotice('');
  try{
   if(action)await action();
   const result=await load();setRows(result.rows);setStates(result.states);setError(result.error);
   if(action)setNotice('Đã lưu kết quả xử lý.');
  }catch(e){setError(e.message);}finally{setBusy(false);}
 }
 const current=rows.find(row=>row.key===selected);
 const displayed=rows.filter(row=>{const closed=states.find(s=>s.key===row.key)?.state.status==='resolved'||['completed','rejected','closed'].includes(row.item.status);return filter==='all'||(filter==='closed'?closed:!closed);});
 return <section className="issue-hub" aria-label="Quản lý sự cố tập trung">
  <div className="payment-heading"><p>Thanh toán, tình trạng sân, hoàn tiền tồn và báo cáo người chơi.</p><button className="button button-secondary" disabled={busy} onClick={()=>refresh()}>Tải lại sự cố</button></div>
  <label>Bộ lọc sự cố<select value={filter} onChange={e=>setFilter(e.target.value)}><option value="active">Còn việc chờ</option><option value="closed">Đã đóng / kết thúc</option><option value="all">Tất cả & lịch sử</option></select></label>
  {error&&<p className="payment-error" role="alert">{error}</p>}
  {notice&&<p role="status">{notice}</p>}
  {loading?<p role="status">Đang tải sự cố…</p>:<div className="issue-hub-layout">
   <div className="issue-hub-list" aria-label="Danh sách sự cố">
    {!displayed.length&&<p>Không có sự cố cần xử lý.</p>}
    {displayed.map(row=><button key={row.key} className="issue-row" aria-pressed={selected===row.key} onClick={()=>{setSelected(row.key);setNotice('');}}>
     <span className="payment-badge">{row.type} · {row.status}</span><strong>{row.title}</strong><span>{row.summary}</span><span>{issueLabels[states.find(s=>s.key===row.key)?.state.status]||'Chưa nhận xử lý'} · {states.find(s=>s.key===row.key)?.state.assignedName||'Chưa phân công'}</span>
    </button>)}
   </div>
   <section className="issue-hub-detail" aria-label="Chi tiết sự cố" aria-live="polite">
    {current?<><h2>{current.title}</h2>{current.kind!=='reviews'&&<IssueWorkflowControls key={'flow-'+current.key} kind={current.kind} id={current.item.id} role={role} onUpdated={()=>refresh()}/>}<p>{current.type} · {current.status}</p>
     {current.kind==='reviews'?<PaymentRecoveryPage key={current.item.id} role={role} initialPaymentId={current.item.id}/>:current.kind==='operations'?<OperationalExceptions key={current.key} role={role} initialCaseId={current.item.id}/>:current.kind==='inbox'?<TicketSupport key={current.key} ticketId={current.item.ticketId} staff initialOpen/>:
      <OperationItem key={current.key} role={role} tab={current.kind} item={current.item} busy={busy} run={refresh}/>}
    </>:<p>Chọn một sự cố trong danh sách để xem chi tiết và phản hồi.</p>}
   </section>
  </div>}
 </section>;
}
