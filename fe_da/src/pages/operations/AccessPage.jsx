import { matchesText } from '../../components/ui/managerUiState';
import { accessDecisionActions, accessTargets } from './ownerAccessModel';
import PaginatedList from '../../components/ui/PaginatedList';
import { useEffect, useState } from 'react';
import { workflowRequest } from '../workflowClient';
import './operations.css';
import './business-console.css';
const parse = text => { try { return JSON.parse(text); } catch { return {}; } };
const statuses = { pending: 'Chờ phê duyệt', approved: 'Đã duyệt, chờ triển khai', rejected: 'Đã từ chối', provisioned: 'Chờ người nhận kiểm tra', verified: 'Đã kiểm tra, chờ thu hồi quyền cũ', executed: 'Hoàn tất' };
export default function AccessPage({ role, user }) {
  const [data, setData] = useState(null);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState('');
  const [revision, setRevision] = useState(0);
  const [reasons, setReasons] = useState({});
  const [form, setForm] = useState({ targetUserId: '', role: role === 'manager' ? 'Staff' : 'Manager', venueId: '', action: 'grant', reason: '', outgoingUserId: '' });
  const id = Number(user?.id);
  const [accountQuery,setAccountQuery]=useState(''),[requestQuery,setRequestQuery]=useState(''),[requestStatus,setRequestStatus]=useState('all');
  useEffect(() => {
    let active = true;
    workflowRequest('/api/operations/access').then(value => { if (active) { setData(value); setError(''); } }).catch(e => { if (active) setError(e.message); });
    return () => { active = false; };
  }, [revision]);
  async function run(path, body) {
    if(busy)return;setBusy(true); setError(''); setNotice('');
    try { await workflowRequest(`/api/operations/access${path}`, 'POST', body); setRevision(value => value + 1); setNotice('Đã lưu kết quả.'); }
    catch (e) { setError(e.message); } finally { setBusy(false); }
  }
  function submit(e) {
    e.preventDefault();
    void run('', { ...form, targetUserId: Number(form.targetUserId), venueId: form.role === 'SystemAdmin' ? null : Number(form.venueId), outgoingUserId: form.action === 'handover' ? Number(form.outgoingUserId) : null });
  }
  const requests = data?.requests.filter(item => (requestStatus === 'all' || item.status === requestStatus) && matchesText(`${item.target} ${item.requester} ${item.id}`, requestQuery)) || [];
  return <section className="ops-page"><h1>Bổ nhiệm và quyền truy cập</h1>
    <p>Phê duyệt độc lập trước khi cấp hoặc thu hồi quyền. Người nhận bàn giao dùng tài khoản riêng.</p>
    {error && <p role="alert">{error}</p>}{notice && <p role="status">{notice}</p>}
    {(role !== 'manager' || error) && <button disabled={busy} onClick={() => setRevision(value => value + 1)}>Tải lại</button>}
    {!data ? !error && <p role="status">Đang tải yêu cầu…</p> : <>
      <details className="business-section" open={role === 'manager' || undefined}><summary>Đề xuất bổ nhiệm / thay đổi quyền</summary><form className="ops-form" onSubmit={submit}><h2>Đề xuất</h2><p>Tài khoản nhận quyền cần đăng nhập Google trước để có tài khoản riêng trong hệ thống.</p>
        <label>Tìm tài khoản<input type="search" value={accountQuery} onChange={e=>{setAccountQuery(e.target.value);setForm({...form,targetUserId:''});}} placeholder="Tên hoặc email"/></label>
        <label>Tài khoản<select aria-label="Tài khoản" disabled={busy} required value={form.targetUserId} onChange={e => setForm({ ...form, targetUserId: e.target.value })}><option value="">Chọn tài khoản</option>{accessTargets(data.users,accountQuery).map(u => <option key={u.id} value={u.id}>{u.name} · {u.email} · {u.role}</option>)}</select></label>
        <label>Vai trò<select aria-label="Vai trò" disabled={busy} value={form.role} onChange={e => setForm({ ...form, role: e.target.value, targetUserId:'', outgoingUserId:'', action: 'grant' })}>{(role === 'manager' ? ['Staff', 'Referee'] : ['Manager', 'Staff', 'Referee', 'SystemAdmin']).map(value => <option key={value} value={value}>{role==='manager'?({Staff:'Nhân viên',Referee:'Trọng tài'})[value]||value:value}</option>)}</select></label>
        {form.role !== 'SystemAdmin' && <label>Cơ sở<select aria-label="Cơ sở" disabled={busy} required value={form.venueId} onChange={e => setForm({ ...form, venueId: e.target.value })}><option value="">Chọn cơ sở</option>{data.venues.map(v => <option key={v.id} value={v.id}>{v.name}</option>)}</select></label>}
        <label>Hình thức<select aria-label="Hình thức" disabled={busy} value={form.action} onChange={e => setForm({ ...form, action: e.target.value })}><option value="grant">Cấp quyền / bổ nhiệm</option><option value="revoke">Thu hồi truy cập</option>{form.role === 'SystemAdmin' && <option value="handover">Bàn giao quản trị</option>}</select></label>
        {form.action === 'handover' && <label>Tài khoản bàn giao<select aria-label="Tài khoản bàn giao" disabled={busy} required value={form.outgoingUserId} onChange={e => setForm({ ...form, outgoingUserId: e.target.value })}><option value="">Chọn quản trị viên hiện tại</option>{data.users.filter(u => u.role === 'SystemAdmin' && u.id !== Number(form.targetUserId)).map(u => <option key={u.id} value={u.id}>{u.name} · {u.email}</option>)}</select></label>}
        <label>Lý do<textarea required maxLength={2000} value={form.reason} onChange={e => setForm({ ...form, reason: e.target.value })} /></label><button type="submit" disabled={busy}>{busy ? 'Đang lưu…' : 'Gửi đề xuất'}</button>
      </form></details>
      <h2>Yêu cầu</h2><div className="ops-actions owner-list-filters"><label>{role === 'manager' ? 'Tìm người / mã yêu cầu' : 'Tìm yêu cầu'}<input type="search" value={requestQuery} onChange={e=>setRequestQuery(e.target.value)} placeholder="Người nhận hoặc người đề xuất"/></label><label>Trạng thái yêu cầu<select value={requestStatus} onChange={e=>setRequestStatus(e.target.value)}><option value="all">Tất cả</option>{Object.entries(statuses).map(([key,label])=><option key={key} value={key}>{label}</option>)}</select></label></div>{!requests.length && <p>Không có yêu cầu phù hợp.</p>}
      <div className="ops-grid">{<PaginatedList key={`${requestQuery}-${requestStatus}`} label="Phân trang yêu cầu nhân sự" items={requests}>{item => {
        const payload = parse(item.payload); const request = payload.Request || {};
        const actions=accessDecisionActions(item,id,role,request.Role);
        const canDecide=actions.length>0;
        return <article className="ops-card" key={item.id}><h2>#{item.id} · {item.target}</h2><p>{request.Role} · {({ grant: 'Cấp quyền', revoke: 'Thu hồi', handover: 'Bàn giao' })[request.Action]}</p><strong>{statuses[item.status] || item.status}</strong><p>Người đề xuất: {item.requester}</p><p>{request.Reason}</p><p>{payload.At && new Date(payload.At).toLocaleString('vi-VN')}</p>
          {canDecide && <><label>Lý do quyết định<textarea required maxLength={2000} value={reasons[item.id] || ''} onChange={e => setReasons({ ...reasons, [item.id]: e.target.value })} /></label><div className="ops-actions">{actions.includes('approve')&&<button disabled={busy || !reasons[item.id]?.trim()} onClick={() => run(`/${item.id}/decision`, { approve: true, reason: reasons[item.id] })}>Duyệt</button>}<button disabled={busy || !reasons[item.id]?.trim()} onClick={() => run(`/${item.id}/decision`, { approve: false, reason: reasons[item.id] })}>{item.status==='approved'?'Thu hồi phê duyệt':'Từ chối'}</button></div></>}
          {role === 'system-admin' && ['approved', 'verified'].includes(item.status) && <button disabled={busy} onClick={() => run(`/${item.id}/execute`)}>{item.status === 'verified' ? 'Hoàn tất và thu hồi truy cập cũ' : 'Triển khai quyền đã duyệt'}</button>}
          {role === 'system-admin' && item.status === 'provisioned' && request.TargetUserId === id && <><p>Xác nhận sau khi đã kiểm tra quyền cấu hình và hỗ trợ bằng tài khoản của bạn.</p><button disabled={busy} onClick={() => run(`/${item.id}/verify`)}>Tôi đã kiểm tra quyền truy cập</button></>}
        </article>;
      }}</PaginatedList>}</div>
    </>}
  </section>;
}
