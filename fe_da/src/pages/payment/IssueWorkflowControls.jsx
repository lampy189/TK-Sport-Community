import { useEffect,useState } from 'react';
import { incidents,issueLabels } from './incidentClient';

const historyLabels = {
  operational_requested: 'Đã gửi yêu cầu',
  operational_approved: 'Đã chấp thuận phương án',
  operational_rejected: 'Đã từ chối phương án',
  operational_executed: 'Đã thực hiện phương án',
  operational_closed: 'Đã đóng hồ sơ',
  operational_return_verified: 'Đã xác minh hoàn tiền',
  return_payout: 'Đã ghi nhận chuyển tiền',
  return_transfer_reference: 'Đã lưu mã chuyển tiền',
  issue_open: 'Đã mở lại sự cố',
  issue_in_progress: 'Đã nhận hoặc phân công xử lý',
  issue_waiting_bank: 'Chờ ngân hàng / đối soát',
  issue_waiting_approval: 'Chờ phê duyệt',
  issue_resolved: 'Đã giải quyết',
};
const historyDate = new Intl.DateTimeFormat('vi-VN', {
  dateStyle: 'short', timeStyle: 'short', timeZone: 'Asia/Ho_Chi_Minh',
});

function IssueHistoryEntry({ entry }) {
  let snapshot;
  try {
    snapshot = JSON.parse(entry.newValue);
  } catch {
    snapshot = null;
  }
  const date = new Date(snapshot?.At ?? snapshot?.UpdatedAt ?? snapshot?.RequestedAt ?? entry.createdAt ?? '');
  const note = snapshot?.Note || snapshot?.Decision || snapshot?.Reason;
  const bankReference = snapshot?.BankReference ??
    (entry.action === 'return_transfer_reference' ? entry.newValue : null);

  return <li>
    <strong>{historyLabels[entry.action] || 'Đã cập nhật hồ sơ'}</strong>
    <small>{entry.actorId == null ? 'Hệ thống' : `Người thực hiện #${entry.actorId}`}
      {!Number.isNaN(date.getTime()) && <> · <time dateTime={date.toISOString()}>{historyDate.format(date)}</time></>}
    </small>
    {note && <p>{note}</p>}
    {snapshot?.AssignedName && <p>Người phụ trách: {snapshot.AssignedName}</p>}
    {bankReference && <p>Mã chuyển tiền: {bankReference}</p>}
  </li>;
}

export default function IssueWorkflowControls({kind,id,role='staff',onUpdated}){
 const [data,setData]=useState(null),[reason,setReason]=useState(''),[assignee,setAssignee]=useState(''),[category,setCategory]=useState('other'),[busy,setBusy]=useState(false),[error,setError]=useState(''),[notice,setNotice]=useState('');
 useEffect(()=>{let live=true;incidents.detail(kind,id).then(d=>{if(live)setData(d);}).catch(e=>{if(live)setError(e.message);});return()=>{live=false;};},[kind,id]);
 async function run(action){if(busy)return;setBusy(true);setError('');setNotice('');try{await action();setData(await incidents.detail(kind,id));setReason('');setNotice('Đã lưu lịch sử xử lý.');await onUpdated?.();}catch(e){setError(e.message);}finally{setBusy(false);}}
 const valid=reason.trim().length>=10&&reason.trim().length<=1000&&!busy,manager=['manager','owner'].includes(role);
 const act=action=>run(()=>incidents.act(kind,id,{action,reason,expectedVersion:data.state.version,assigneeId:action==='claim'&&assignee?Number(assignee):null}));
 return <section className="aftercare-form" aria-label="Theo dõi xử lý sự cố"><h3>Người xử lý & việc còn chờ</h3>
 {error&&<p role="alert" className="payment-error">{error}</p>}{notice&&<p role="status">{notice}</p>}
 {data&&<><p><strong>{issueLabels[data.state.status]||data.state.status}</strong> · Người phụ trách: {data.state.assignedName||'Chưa phân công'}</p><p>Kết quả/việc chờ: {data.state.note||'Chưa có ghi nhận'}</p>
 <label>Căn cứ / kết quả / bước tiếp theo<textarea minLength={10} maxLength={1000} placeholder="Nhập ít nhất 10 ký tự" value={reason} onChange={e=>setReason(e.target.value)} disabled={busy}/></label>
 {manager&&<label>Phân công người xử lý<select value={assignee} onChange={e=>setAssignee(e.target.value)} disabled={busy}><option value="">Tự nhận xử lý</option>{data.people.map(p=><option key={p.id} value={p.id}>{p.name} · {p.role}</option>)}</select></label>}
 <div className="workflow-actions">{data.state.status==='resolved'?manager&&<button className="button button-secondary" disabled={!valid} onClick={()=>act('reopen')}>Mở lại sự cố</button>:<>
 <button className="button button-primary" disabled={!valid} onClick={()=>act('claim')}>{manager&&assignee?'Phân công':'Nhận xử lý'}</button>
 <button className="button button-secondary" disabled={!valid||!data.state.assignedTo} onClick={()=>act('wait_bank')}>Chờ ngân hàng / đối soát</button>
 <button className="button button-secondary" disabled={!valid||!data.state.assignedTo} onClick={()=>act('wait_approval')}>Chờ phê duyệt</button>
 <button className="button button-secondary" disabled={!valid||!data.state.assignedTo||!data.resource.canResolve} onClick={()=>act('resolve')}>Ghi kết quả & đóng sự cố</button>
 </>}</div>
 {!data.resource.canResolve&&<p>{kind === 'operations' ? 'Bạn có thể đóng sự cố sau khi yêu cầu ngoại lệ được xử lý xong.' : 'Bạn có thể đóng sự cố sau khi hoàn tất xử lý giao dịch hoặc vé liên quan.'}</p>}
 {role!=='owner'&&kind!=='operations'&&data.state.status!=='resolved'&&<><label>Loại ngoại lệ<select value={category} onChange={e=>setCategory(e.target.value)}><option value="payment_mismatch">Tiền thiếu/thừa, không thể kích hoạt vé</option><option value="funding">Nguồn tiền chưa phân bổ</option><option value="refund_funds">Lỗi chuyển tiền / thiếu quỹ hoàn</option><option value="court">Sự cố vận hành sân</option><option value="other">Ngoại lệ khác</option></select></label><button className="button button-secondary" disabled={!valid} onClick={()=>run(()=>incidents.request({kind,subjectId:id,category,reason}))}>Gửi yêu cầu phê duyệt ngoại lệ</button></>}
 <details>
   <summary>Lịch sử xử lý ({data.history.length})</summary>
   {data.history.length > 0 ? <ol className="issue-workflow-history">
     {data.history.map(entry => <IssueHistoryEntry key={entry.id} entry={entry} />)}
   </ol> : <p>Chưa có lịch sử xử lý.</p>}
 </details>
 </>}
 </section>;
}
