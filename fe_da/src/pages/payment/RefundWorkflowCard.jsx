import AuditHistory from '../../components/audit/AuditHistory';
import { useEffect, useState } from 'react';
import { aftercare } from './aftercareClient';
import TicketSupport from './TicketSupport';
const refundLabels={requested:'Chờ duyệt',processing:'Chờ chuyển / xác minh',failed:'Chuyển tiền lỗi',completed:'Đã hoàn tất',rejected:'Đã từ chối'};
const decode=value=>{try{return JSON.parse(value);}catch{return null;}};
export default function RefundWorkflowCard({item,role='staff',busy,run}){
 const [reason,setReason]=useState(''),[bank,setBank]=useState(''),[document,setDocument]=useState(''),[paidAt,setPaidAt]=useState(''),[checked,setChecked]=useState(false),[history,setHistory]=useState([]),[error,setError]=useState('');
 const payout=decode(item.payout),decision=decode(item.decision),status=item.status||'requested',canDecide=['manager','owner'].includes(role.toLowerCase());
 useEffect(()=>{let live=true;aftercare.refundHistory(item.id).then(x=>{if(live)setHistory(x);}).catch(e=>{if(live)setError(e.message);});return()=>{live=false;};},[item.id,item.status,item.payout]);
 const needsDecision=status==='requested'||(['processing','failed'].includes(status)&&!decision&&!payout);
 const valid=reason.trim().length>=10&&reason.trim().length<=1000&&!busy;
 const transferValid=checked&&document.trim().length>=10&&bank.trim().length>=6&&bank.trim().length<=100&&paidAt&&new Date(paidAt)<=new Date();
 function act(action){return run(action);}
 return <article className="payment-card"><h4>Hoàn tiền vé #{item.ticketId}</h4><p><strong>{refundLabels[status]||status}</strong> · {item.amountVnd?.toLocaleString('vi-VN')} ₫ → {item.recipient} ({item.email})</p><p>Giao dịch gốc: {item.referenceCode}</p><TicketSupport ticketId={item.ticketId} staff/>
 {error&&<p role="alert">{error}</p>}
 {!['completed','rejected'].includes(status)&&<><label>Lý do / căn cứ xử lý<textarea value={reason} maxLength={1000} onChange={e=>setReason(e.target.value)} disabled={busy}/></label>
 {needsDecision&&<>{canDecide?<div className="workflow-actions"><button className="button button-primary" disabled={!valid} onClick={()=>act(()=>aftercare.decideRefund(item.id,{approve:true,amountVnd:item.amountVnd,recipientId:item.recipientId,reason}))}>Duyệt khoản hoàn</button><button className="button button-secondary" disabled={!valid} onClick={()=>act(()=>aftercare.decideRefund(item.id,{approve:false,amountVnd:item.amountVnd,recipientId:item.recipientId,reason}))}>Từ chối khoản hoàn</button></div>:<p>Chờ Manager/Owner duyệt đúng số tiền và người nhận.</p>}</>}
 {!needsDecision&&['processing','failed'].includes(status)&&<>{!payout?<><p>Quyết định: {decision?.Reason||'Đã duyệt'}. Chỉ ghi chứng từ sau khi thực sự chuyển tiền.</p><label>Mã giao dịch ngân hàng<input maxLength={100} value={bank} onChange={e=>setBank(e.target.value)} disabled={busy}/></label><label>Chứng từ tiền ra<textarea maxLength={1000} value={document} onChange={e=>setDocument(e.target.value)} disabled={busy}/></label><label>Thời điểm chuyển tiền<input type="datetime-local" value={paidAt} onChange={e=>setPaidAt(e.target.value)} disabled={busy}/></label><label><input type="checkbox" checked={checked} onChange={e=>setChecked(e.target.checked)} disabled={busy}/>Tôi đã chuyển đủ tiền về đúng người trả gốc và đối chiếu chứng từ.</label><button className="button button-primary" disabled={!transferValid||busy} onClick={()=>act(()=>aftercare.refundPayout(item.id,{amountVnd:item.amountVnd,recipientId:item.recipientId,bankReference:bank,document,paidAt:new Date(paidAt).toISOString()}))}>Lưu chứng từ tiền ra</button></>:<><p>Chứng từ đã lưu: {payout.BankReference} · {payout.AmountVnd?.toLocaleString('vi-VN')} ₫. Người ghi #{payout.RecordedBy}. Không chuyển lại.</p><p>{payout.Document}</p><label><input type="checkbox" checked={checked} onChange={e=>setChecked(e.target.checked)}/>Tôi là người kiểm tra độc lập và đã xác minh đúng giao dịch, số tiền, người nhận.</label><button className="button button-primary" disabled={!valid||!checked} onClick={()=>act(()=>aftercare.completeRefund(item.id,reason,payout.BankReference))}>Xác minh và hoàn tất</button></>}
 <button className="button button-secondary" disabled={!valid} onClick={()=>act(()=>aftercare.failRefund(item.id,reason))}>Ghi nhận lỗi / thiếu quỹ hoàn</button></>}
 </>}
 <details><summary>Lịch sử xử lý ({history.length})</summary><AuditHistory items={history}/></details>
 </article>;
}
