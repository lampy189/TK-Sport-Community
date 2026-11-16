import { useEffect, useState } from 'react';
import { workflowRequest } from '../workflowClient';
import './operations.css';
const freshVenue = { name: '', address: '', active: true };
const freshCourt = { name: '', venueId: '', sportId: '', active: true };
const historyLabels = { venue_created: 'Thêm cơ sở', venue_updated: 'Sửa cơ sở', court_created: 'Thêm sân', court_updated: 'Sửa sân', personnel_assigned: 'Đổi phân công cơ sở', future_assignment_transferred: 'Chuyển lịch cho người thay thế' };
function historyDetail(item, data) {
  let before, after;
  try { before = JSON.parse(item.oldValue || '{}'); after = JSON.parse(item.newValue || '{}'); } catch { return <p>Đã ghi nhận thay đổi #{item.id}.</p>; }
  const request = after.request || {};
  const venue = id => data.venues.find(v => v.id === Number(id))?.name || 'Chưa phân công';
  return <><p>{after.at && new Date(after.at).toLocaleString('vi-VN')}</p>
    {request.Name && <p>Tên: {before?.name || 'Thêm mới'} → {request.Name}</p>}
    {item.entityType === 'Personnel' && item.action === 'personnel_assigned' && <p>Cơ sở: {venue(before?.venue_id)} → {venue(request.VenueId)}</p>}
    {after.replacement && <p>Người thay thế: {data.people.find(p => p.id === after.replacement)?.name || `#${after.replacement}`} · buổi #{before.sessionId}</p>}
    {typeof request.Active === 'boolean' && <p>{request.Active ? 'Đang hoạt động' : 'Ngừng hoạt động'}</p>}
  </>;
}
export default function OperationsPage({ role = 'owner', initialTab = 'venues' }) {
  const [data, setData] = useState(null);
  const [tab, setTab] = useState(initialTab);
  const [editing, setEditing] = useState(null);
  const [form, setForm] = useState(null);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [busy, setBusy] = useState(false);
  const [revision, setRevision] = useState(0);
  const [drafts, setDrafts] = useState({});
  const owner = role === 'owner';
  useEffect(() => {
    let active = true;
    workflowRequest('/api/operations/setup').then(value => { if (active) setData(value); }).catch(e => { if (active) setError(e.message); });
    return () => { active = false; };
  }, [revision]);
  function edit(item = null) {
    setEditing(item?.id || null);
    setForm(item ? { ...item } : tab === 'venues' ? { ...freshVenue } : { ...freshCourt, venueId: data.venues.find(v => v.active)?.id || '', sportId: data.sports[0]?.id || '' });
    setError(''); setNotice('');
  }
  async function save(event) {
    event.preventDefault(); setBusy(true); setError(''); setNotice('');
    try {
      await workflowRequest(`/api/operations/${tab}${editing ? `/${editing}` : ''}`, editing ? 'PUT' : 'POST', tab === 'courts' ? { ...form, venueId: Number(form.venueId), sportId: Number(form.sportId) } : form);
      setForm(null); setRevision(value => value + 1); setNotice('Đã lưu thay đổi.');
    } catch (e) { setError(e.message); } finally { setBusy(false); }
  }
  async function assign(person) {
    const draft = drafts[person.id];
    if (!draft) return;
    setBusy(true); setError(''); setNotice('');
    try {
      await workflowRequest(`/api/operations/personnel/${person.id}`, 'PUT', { venueId: draft.venueId ? Number(draft.venueId) : null, replacementUserId: draft.replacementUserId ? Number(draft.replacementUserId) : null });
      setDrafts(value => { const next = { ...value }; delete next[person.id]; return next; });
      setRevision(value => value + 1); setNotice('Đã cập nhật phân công và lưu lịch sử.');
    } catch (e) { setError(e.message); } finally { setBusy(false); }
  }
  return <section className="ops-page"><h1>Cơ sở, sân và nhân sự</h1>
    <div className="ops-actions">{[['venues', 'Cơ sở'], ['courts', 'Sân'], ...(owner ? [['people', 'Nhân sự'], ['history', 'Lịch sử']] : [])].map(([id, label]) => <button key={id} aria-pressed={tab === id} disabled={busy} onClick={() => { setTab(id); setForm(null); }}>{label}</button>)}<button disabled={busy} onClick={() => setRevision(value => value + 1)}>Tải lại</button></div>
    {error && <p role="alert">{error}</p>}{notice && <p role="status">{notice}</p>}
    {!data ? !error && <p role="status">Đang tải dữ liệu…</p> : <>
      {['venues', 'courts'].includes(tab) && <>
        {(owner || tab === 'courts') && !form && <button onClick={() => edit()}>Thêm {tab === 'venues' ? 'cơ sở' : 'sân'}</button>}
        {form && <form className="ops-form" onSubmit={save}><h2>{editing ? 'Sửa' : 'Thêm'} {tab === 'venues' ? 'cơ sở' : 'sân'}</h2>
          <label>Tên<input required maxLength={255} value={form.name || ''} onChange={e => setForm({ ...form, name: e.target.value })} /></label>
          {tab === 'venues' ? <label>Địa chỉ<input maxLength={500} value={form.address || ''} onChange={e => setForm({ ...form, address: e.target.value })} /></label> : <>
            <label>Cơ sở<select required disabled={Boolean(editing)} value={form.venueId} onChange={e => setForm({ ...form, venueId: e.target.value })}><option value="">Chọn cơ sở</option>{data.venues.map(v => <option key={v.id} value={v.id}>{v.name}</option>)}</select></label>
            <label>Môn chơi<select required value={form.sportId || ''} onChange={e => setForm({ ...form, sportId: e.target.value })}><option value="">Chọn môn</option>{data.sports.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}</select></label>
          </>}
          <label className="ops-check"><input type="checkbox" checked={Boolean(form.active)} onChange={e => setForm({ ...form, active: e.target.checked })} />Đang hoạt động</label>
          <div className="ops-actions"><button type="submit" disabled={busy}>{busy ? 'Đang lưu…' : 'Lưu'}</button><button type="button" disabled={busy} onClick={() => setForm(null)}>Hủy</button></div>
        </form>}
        <div className="ops-grid">{data[tab].map(item => <article className="ops-card" key={item.id}><h2>{item.name}</h2>
          <p>{tab === 'venues' ? item.address || 'Chưa có địa chỉ' : `${data.venues.find(v => v.id === item.venueId)?.name || ''} · ${data.sports.find(s => s.id === item.sportId)?.name || ''}`}</p>
          <p>{item.active ? 'Đang hoạt động' : 'Ngừng hoạt động'}{tab === 'courts' && ` · ${item.futureSessions} lịch chưa kết thúc`}</p>
          {(owner || tab === 'courts') && <button disabled={busy} onClick={() => edit(item)}>Sửa</button>}
        </article>)}</div>{!data[tab].length && <p>Chưa có {tab === 'venues' ? 'cơ sở' : 'sân'}.</p>}
      </>}
      {tab === 'people' && <><p>Chuyển hoặc gỡ phân công chỉ hoàn tất khi lịch chưa kết thúc đã có người thay thế phù hợp.</p><div className="ops-table"><table><thead><tr><th>Nhân sự</th><th>Vai trò</th><th>Cơ sở mới</th><th>Người thay thế tại cơ sở cũ</th><th /></tr></thead><tbody>{data.people.map(person => {
        const draft = drafts[person.id] || { venueId: person.venueId || '', replacementUserId: '' };
        const update = change => setDrafts(value => ({ ...value, [person.id]: { ...draft, ...change } }));
        return <tr key={person.id}><td>{person.name}<small>{person.email}</small><small>{person.status}</small></td><td>{person.role}</td>
          <td><select aria-label={`Cơ sở của ${person.name}`} disabled={busy} value={draft.venueId} onChange={e => update({ venueId: e.target.value })}><option value="">Gỡ phân công</option>{data.venues.filter(v => v.active || v.id === person.venueId).map(v => <option key={v.id} value={v.id}>{v.name}</option>)}</select></td>
          <td><select aria-label={`Người thay thế ${person.name}`} disabled={busy} value={draft.replacementUserId} onChange={e => update({ replacementUserId: e.target.value })}><option value="">Chọn khi có lịch tương lai</option>{data.people.filter(p => p.id !== person.id && p.role === person.role && p.venueId === person.venueId && p.status?.toLowerCase() === 'active').map(p => <option key={p.id} value={p.id}>{p.name}</option>)}</select></td>
          <td><button disabled={busy || !drafts[person.id]} onClick={() => assign(person)}>Lưu phân công</button></td></tr>;
      })}</tbody></table></div>{!data.people.length && <p>Chưa có nhân sự.</p>}</>}
      {tab === 'history' && <>{!data.history.length && <p>Chưa có thay đổi.</p>}{data.history.map(item => <details className="ops-card" key={item.id}><summary>#{item.id} · {item.actor} · {historyLabels[item.action] || 'Cập nhật phân công'}</summary>{historyDetail(item, data)}</details>)}</>}
    </>}
  </section>;
}
