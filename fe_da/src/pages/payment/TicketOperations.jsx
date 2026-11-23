import PaginatedList from '../../components/ui/PaginatedList';
import { useEffect, useState } from 'react';
import { aftercare } from './aftercareClient';
import { OperationItem } from './TicketOperationCards';
import IssueHub from './IssueHub';
import PaymentRecoveryPage from './PaymentRecoveryPage';
import './payment.css';

export default function TicketOperations({initialTab='reviews',role='staff'}) {
 const [statusFilter,setStatusFilter]=useState('all');
 const managerTabs = initialTab === 'issues' ? [['issues','Sự cố'],['sessions','Báo sự cố sân']] : initialTab === 'changes' ? [['changes','Yêu cầu đổi vé'],['refunds','Hoàn tiền']] : [];
 const [tab,setTab]=useState(initialTab),[items,setItems]=useState([]),[busy,setBusy]=useState(false),[loading,setLoading]=useState(true),[error,setError]=useState(''),[notice,setNotice]=useState('');
 useEffect(()=>{
  if(tab==='issues'||tab==='reviews')return;
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
 const displayed = role === 'manager' && statusFilter !== 'all' ? items.filter(item => item.status === statusFilter) : items;
 const statuses = [...new Set(items.map(item => item.status).filter(Boolean))];
 const statusLabels = {requested:'Chờ duyệt',processing:'Đang xử lý',completed:'Hoàn tất',rejected:'Từ chối',failed:'Thất bại',pending:'Chờ xử lý',Open:'Đang mở',Published:'Đang mở',suspended:'Tạm ngưng',cancelled:'Đã hủy'};
 return <section className="payment-workspace ticket-operations">
  <div className="payment-heading"><h1>{role==='manager' ? (tab==='reviews'?'Đối soát thanh toán':tab==='issues'||tab==='sessions'?'Sự cố':'Đổi & hoàn vé') : (tab==='reviews'?'Đối soát thanh toán vé':tab==='issues'?'Sự cố':'Xử lý vé & thanh toán')}</h1>{role!=='manager'&&tab!=='issues'&&<button className="button button-secondary" disabled={busy} onClick={()=>run(async()=>{})}>Tải lại</button>}</div>
  {(role !== 'manager' || managerTabs.length > 0) && <nav className="ticket-filters" aria-label="Chức năng xử lý vé">
   {(role === 'manager' ? managerTabs : [['issues','Sự cố'],['reviews','Thanh toán cần kiểm tra'],['sessions','Báo sự cố sân'],['changes','Yêu cầu đổi vé'],['refunds','Hoàn tiền']]).map(([id,label])=>
    <button key={id} className="button" disabled={busy} aria-pressed={tab===id} onClick={()=>{if(id===tab)return;setTab(id);setStatusFilter('all');setItems([]);setLoading(true);setError('');setNotice('');}}>{label}</button>)}
  </nav>}
  {tab==='reviews'?<PaymentRecoveryPage role={role}/>:tab==='issues'?<IssueHub role={role}/>:<>
   {error&&<p className="payment-error" role="alert">{error}</p>}
   {notice&&<p className="payment-notice" role="status">{notice}</p>}
   {role === 'manager' && <label className="manager-filter">Trạng thái {tab === 'refunds' ? 'hoàn tiền' : tab === 'changes' ? 'đổi vé' : 'buổi chơi'}<select value={statusFilter} onChange={e=>setStatusFilter(e.target.value)}><option value="all">Tất cả</option>{statuses.map(status=><option key={status} value={status}>{statusLabels[status]||status}</option>)}</select></label>}
   {loading?<p role="status">Đang tải…</p>:!displayed.length?<p>Không có mục cần xử lý.</p>:<PaginatedList items={displayed}>{item=><OperationItem key={item.id} role={role} tab={tab} item={item} busy={busy} run={run}/>}</PaginatedList>}
  </>}
 </section>;
}
