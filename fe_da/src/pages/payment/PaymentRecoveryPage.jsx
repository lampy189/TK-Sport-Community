import { useEffect, useState } from 'react';
import { workflowRequest } from '../workflowClient';
import '../workflow.css';
import './recovery.css';
import { historyDetails, recoveryLabels } from './recoveryModel';
const root='/api/v1/commerce/payment-recovery';
const money=n=>`${Number(n).toLocaleString('vi-VN')} ₫`;
const labels={pending:'Chưa xác nhận',failed:'Thất bại / đã đóng',review_required:'Cần đối soát',paid:'Đã thu tiền · có khoản cần kiểm tra'};
export default function PaymentRecoveryPage(){
 const [rows,setRows]=useState([]),[selected,setSelected]=useState(null),[evidence,setEvidence]=useState(''),[bank,setBank]=useState('');
 const [busy,setBusy]=useState(false),[error,setError]=useState(''),[notice,setNotice]=useState(''),[loading,setLoading]=useState(true);
 useEffect(()=>{let live=true;workflowRequest(root).then(data=>{if(live)setRows(data);}).catch(e=>{if(live)setError(e.message);}).finally(()=>{if(live)setLoading(false);});return()=>{live=false;};},[]);
 const current=rows.find(r=>r.id===selected);
 async function run(action){
  if(busy)return;setBusy(true);setError('');setNotice('');
  try{
   if(action)await workflowRequest(`${root}/${selected}/${action}`,'POST',{evidence:evidence.trim(),bankTransactionId:bank?Number(bank):null});
   setRows(await workflowRequest(root));
   if(action)setNotice('Đã ghi nhận kết quả. Kiểm tra trạng thái và lịch sử bên dưới.');
  }catch(e){setError(e.message);}finally{setBusy(false);}
 }
 const canAct=!busy&&evidence.trim().length>=10&&evidence.trim().length<=1000;
 return <section className="workflow payment-recovery" aria-label="Xử lý lại giao dịch">
  <div className="workflow-actions"><h2>Đối soát vé & hàng hóa</h2><button className="button button-secondary" disabled={busy} onClick={()=>run()}>Tải lại</button></div>
  {error&&<p role="alert" className="workflow-error">{error}</p>}{notice&&<p role="status">{notice}</p>}
  {loading?<p role="status">Đang tải giao dịch…</p>:<div className="recovery-grid">
   <div className="recovery-list" aria-label="Giao dịch cần kiểm tra">{!rows.length&&<p>Không có giao dịch cần kiểm tra.</p>}
    {rows.map(row=><button key={row.id} disabled={busy} aria-pressed={selected===row.id} onClick={()=>{setSelected(row.id);setEvidence('');setBank('');setNotice('');}}>
      <strong>{row.kind==='goods'?'Hàng hóa':'Vé'} · {row.reference}</strong><span>{money(row.amountVnd)} · {labels[row.status]||row.status}</span>
    </button>)}
   </div>
   <div aria-live="polite">{current?<>
    <h3>{current.reference}</h3><p>Đơn #{current.orderId} · {labels[current.status]||current.status}</p>
    <p>Số tiền cần thu: <strong>{money(current.amountVnd)}</strong></p>
    <label>Giao dịch ngân hàng<select value={bank} onChange={e=>setBank(e.target.value)} disabled={busy}>
     <option value="">Chọn chứng từ để duyệt</option>{current.transactions.map(t=><option key={t.id} value={t.id}>{t.reference} · {money(t.amountVnd)} · {recoveryLabels[t.status]||t.status}</option>)}
    </select></label>
    {!current.transactions.length&&<p>Chưa có chứng từ ngân hàng được xác minh.</p>}
    <label>Nội dung / bằng chứng<textarea rows={4} minLength={10} maxLength={1000} value={evidence} onChange={e=>setEvidence(e.target.value)} disabled={busy} placeholder="Ghi căn cứ xử lý, mã đối soát hoặc đường dẫn bằng chứng (10–1000 ký tự)"/></label>
    <div className="workflow-actions">
     <button className="button button-primary" disabled={!canAct} onClick={()=>run('recheck')}>Kiểm tra lại payOS</button>
     <button className="button button-secondary" disabled={!canAct||!bank||current.status!=='review_required'} onClick={()=>run('approve')}>Duyệt khoản đủ tiền</button>
     <button className="button button-secondary" disabled={!canAct||current.status==='paid'||current.transactions.length>0} onClick={()=>run('close-unpaid')}>Đóng khi chưa thu tiền</button>
     <button className="button button-secondary" disabled={!canAct} onClick={()=>run('note')}>Lưu ghi chú</button>
    </div>
    <p>Khoản thiếu/thừa hoặc chưa rõ kết quả cần tiếp tục đối soát. Không tự cấp vé hay bàn giao hàng.</p>
    <h4>Lịch sử và nguyên nhân</h4>{!current.history.length&&<p>Chưa có lịch sử xử lý. Dùng kiểm tra lại payOS để xác minh.</p>}
    {current.history.map((h,i)=>{const detail=historyDetails(h);return <article className="recovery-history" key={i}>
     <strong>{detail.title}</strong><p>Người xử lý: {h.actorId??'Hệ thống'}{detail.at&&<> · {new Date(detail.at).toLocaleString('vi-VN',{timeZone:'Asia/Ho_Chi_Minh'})}</>}</p>
     {detail.evidence&&<p>{detail.evidence}</p>}{detail.reason&&<p>{detail.reason}</p>}{detail.status&&<p>{detail.status}</p>}
     {detail.reference&&<p>Mã ngân hàng: {detail.reference}</p>}{detail.received!=null&&<p>Thực nhận: {money(detail.received)} · Cần thu: {money(detail.expected)}</p>}
    </article>;})}
   </>:<p>Chọn một giao dịch để xem nguyên nhân và xử lý.</p>}</div>
  </div>}
 </section>;
}
