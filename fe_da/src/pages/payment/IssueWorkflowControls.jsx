import AuditHistory from '../../components/audit/AuditHistory';
import { roleLabel } from '../../components/audit/auditPresentation';
import { useEffect,useState } from 'react';
import { incidents,issueLabels } from './incidentClient';
export default function IssueWorkflowControls({kind,id,role='staff',onUpdated}){
 const [data,setData]=useState(null),[reason,setReason]=useState(''),[assignee,setAssignee]=useState(''),[category,setCategory]=useState('other'),[busy,setBusy]=useState(false),[error,setError]=useState(''),[notice,setNotice]=useState('');
 useEffect(()=>{let live=true;incidents.detail(kind,id).then(d=>{if(live)setData(d);}).catch(e=>{if(live)setError(e.message);});return()=>{live=false;};},[kind,id]);
 async function run(action){if(busy)return;setBusy(true);setError('');setNotice('');try{await action();setData(await incidents.detail(kind,id));setReason('');setNotice('Đã lưu lịch sử xử lý.');await onUpdated?.();}catch(e){setError(e.message);}finally{setBusy(false);}}
 const valid=reason.trim().length>=10&&reason.trim().length<=1000&&!busy,manager=['manager','owner'].includes(role);
 const act=action=>run(()=>incidents.act(kind,id,{action,reason,expectedVersion:data.state.version,assigneeId:action==='claim'&&assignee?Number(assignee):null}));
 return <section className="aftercare-form" aria-label="Theo dõi xử lý sự cố"><h3>Người xử lý & việc còn chờ</h3>
 {error&&<p role="alert" className="payment-error">{error}</p>}{notice&&<p role="status">{notice}</p>}
 {data&&<><p><strong>{issueLabels[data.state.status]||data.state.status}</strong> · Người phụ trách: {data.state.assignedName||'Chưa phân công'}</p><p>Kết quả/việc chờ: {data.state.note||'Chưa có ghi nhận'}</p>
 <label>Căn cứ / kết quả / bước tiếp theo<textarea maxLength={1000} value={reason} onChange={e=>setReason(e.target.value)} disabled={busy}/></label>
 {manager&&<label>Phân công người xử lý<select value={assignee} onChange={e=>setAssignee(e.target.value)} disabled={busy}><option value="">Tự nhận xử lý</option>{data.people.map(p=><option key={p.id} value={p.id}>{p.name} · {roleLabel(p.role)}</option>)}</select></label>}
 <div className="workflow-actions">{data.state.status==='resolved'?manager&&<button className="button button-secondary" disabled={!valid} onClick={()=>act('reopen')}>Mở lại sự cố</button>:<>
 <button className="button button-primary" disabled={!valid} onClick={()=>act('claim')}>{manager&&assignee?'Phân công':'Nhận xử lý'}</button>
 <button className="button button-secondary" disabled={!valid||!data.state.assignedTo} onClick={()=>act('wait_bank')}>Chờ ngân hàng / đối soát</button>
 <button className="button button-secondary" disabled={!valid||!data.state.assignedTo} onClick={()=>act('wait_approval')}>Chờ phê duyệt</button>
 <button className="button button-secondary" disabled={!valid||!data.state.assignedTo||!data.resource.canResolve} onClick={()=>act('resolve')}>Ghi kết quả & đóng sự cố</button>
 </>}</div>
 {!data.resource.canResolve&&<p>Vui lòng hoàn tất nghiệp vụ đối soát/đổi/hoàn trước khi đóng sự cố.</p>}
 {role!=='owner'&&kind!=='operations'&&data.state.status!=='resolved'&&<><label>Loại ngoại lệ<select value={category} onChange={e=>setCategory(e.target.value)}><option value="payment_mismatch">Tiền thiếu/thừa, không thể kích hoạt vé</option><option value="funding">Nguồn tiền chưa phân bổ</option><option value="refund_funds">Lỗi chuyển tiền / thiếu quỹ hoàn</option><option value="court">Sự cố vận hành sân</option><option value="other">Ngoại lệ khác</option></select></label><button className="button button-secondary" disabled={!valid} onClick={()=>run(()=>incidents.request({kind,subjectId:id,category,reason}))}>Gửi yêu cầu phê duyệt ngoại lệ</button></>}
 <details><summary>Lịch sử xử lý ({data.history.length})</summary><AuditHistory items={data.history} people={data.people}/></details>
 </>}
 </section>;
}
