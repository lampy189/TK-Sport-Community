import PaginatedList from '../../components/ui/PaginatedList';
import { useEffect, useState } from 'react';
import SectionLayout from '../../components/layout/SectionLayout';
import { workflowRequest } from '../workflowClient';
import '../workflow.css';
import './session-management.css';

export default function ShiftsPage({ role, user }) {
  const manager = role === 'manager';
  const [rows, setRows] = useState([]);
  const [people, setPeople] = useState([]);
  const [selected, setSelected] = useState(null);
  const [ids, setIds] = useState({ refereeIds: [], staffIds: [] });
  const [date, setDate] = useState('');
  const [filter, setFilter] = useState('upcoming');
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [now, setNow] = useState(() => Date.now());
  useEffect(() => {
    const timer = window.setInterval(() => setNow(Date.now()), 30000);
    return () => window.clearInterval(timer);
  }, []);
  const root = '/api/sessions/manage';
  async function load() {
    const [sessions, options] = await Promise.all([
      workflowRequest(manager ? root : '/api/shifts/mine'),
      manager ? workflowRequest(`${root}/options`) : Promise.resolve({ personnel: [] }),
    ]);
    setRows(sessions); setPeople(options.personnel);
  }
  useEffect(() => {
    let active = true;
    Promise.all([workflowRequest(manager ? root : '/api/shifts/mine'), manager ? workflowRequest(`${root}/options`) : Promise.resolve({ personnel: [] })])
      .then(([sessions, options]) => { if (active) { setRows(sessions); setPeople(options.personnel); } })
      .catch(e => { if (active) setError(e.message); })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [manager]);
  const future = row => new Date(`${row.sessionDate}T${row.startTime}+07:00`).getTime() > now;
  const available = row => future(row) && !['cancelled', 'canceled', 'completed', 'ended'].includes(row.status.toLowerCase());
  const mine = row => (role === 'referee' ? row.referees : row.staff).find(p => String(p.id) === String(user.id));
  const visible = rows.filter(row => (!date || row.sessionDate === date) && (filter === 'all' || (available(row) && (filter !== 'pending' || !mine(row)?.accepted))))
    .sort((a, b) => `${a.sessionDate}${a.startTime}`.localeCompare(`${b.sessionDate}${b.startTime}`));
  function choose(row) {
    setSelected(row.id); setIds({ refereeIds: row.referees.map(p => p.id), staffIds: row.staff.map(p => p.id) });
    setNotice(''); setError('');
  }
  async function act(row) {
    setBusy(true); setError(''); setNotice('');
    try {
      await workflowRequest(manager ? `${root}/${row.id}/assignments` : `/api/shifts/${row.id}/accept`, manager ? 'PUT' : 'POST', manager ? ids : {});
      await load(); setSelected(null); setNotice(manager ? 'Đã lưu phân công.' : 'Đã nhận ca.');
    } catch (e) { setError(e.message); }
    finally { setBusy(false); }
  }
  return <SectionLayout title={manager ? 'Chia ca' : 'Ca của tôi'} description={manager ? 'Phân công theo buổi chơi và theo dõi nhận ca.' : 'Xem lịch được phân công và xác nhận nhận ca.'}>
    <div className="workflow manager-sessions" aria-busy={loading || busy}>
      <div className="workflow-actions">
        <label>Ngày <input type="date" value={date} onChange={e => setDate(e.target.value)} /></label>
        <label>Hiển thị <select value={filter} onChange={e => setFilter(e.target.value)}><option value="upcoming">Ca sắp tới</option>{!manager && <option value="pending">Chờ nhận</option>}<option value="all">Tất cả</option></select></label>
        <button className="button button-secondary" disabled={busy || loading} onClick={async () => { setLoading(true); setError(''); try { await load(); setSelected(null); } catch (e) { setError(e.message); } finally { setLoading(false); } }}>Tải lại</button>
      </div>
      {error && <p className="workflow-error" role="alert">{error}</p>}
      {notice && <p role="status">{notice}</p>}
      {loading ? <p>Đang tải ca…</p> : visible.length === 0 ? <p>Không có ca phù hợp.</p> : <PaginatedList items={visible}>{row => <section key={row.id} className="manager-session-form">
        <h3>{row.sportName} · {row.courtName}</h3>
        <p>{row.sessionDate.split('-').reverse().join('/')} · {row.startTime.slice(0, 5)}–{row.endTime.slice(0, 5)} (giờ Việt Nam)</p>
        {!available(row) && <p>Ca đã bắt đầu, đã kết thúc hoặc đã hủy.</p>}
        {manager ? <>
          <p>Trọng tài: {row.referees.length ? row.referees.map(p => `${p.name} (${p.accepted ? 'Đã nhận' : 'Chờ nhận'})`).join(', ') : 'Chưa phân công'}</p>
          <p>Staff: {row.staff.length ? row.staff.map(p => `${p.name} (${p.accepted ? 'Đã nhận' : 'Chờ nhận'})`).join(', ') : 'Chưa phân công'}</p>
          {selected === row.id ? <>
            {['Referee', 'Staff'].map(kind => { const key = kind === 'Referee' ? 'refereeIds' : 'staffIds'; return <fieldset className="manager-session-people" key={kind} disabled={busy}>
              <legend>{kind === 'Referee' ? 'Trọng tài' : 'Staff'}</legend>
              {people.filter(p => p.role === kind).map(p => <label key={p.id}><input type="checkbox" checked={ids[key].includes(p.id)} onChange={e => setIds(current => ({ ...current, [key]: e.target.checked ? [...current[key], p.id] : current[key].filter(id => id !== p.id) }))} />{p.name}</label>)}
            </fieldset>; })}
            <div className="workflow-actions"><button className="button button-primary" disabled={busy || !available(row)} onClick={() => act(row)}>Lưu phân công</button><button className="button button-secondary" disabled={busy} onClick={() => setSelected(null)}>Hủy</button></div>
          </> : <button className="button button-secondary" disabled={busy || !available(row)} onClick={() => choose(row)}>Chia ca</button>}
        </> : <><p>{mine(row)?.accepted ? 'Đã nhận ca' : 'Chưa nhận ca'}</p><button className="button button-primary" disabled={busy || !available(row) || mine(row)?.accepted} onClick={() => act(row)}>{mine(row)?.accepted ? 'Đã nhận' : 'Nhận ca'}</button></>}
      </section>}</PaginatedList>}
    </div>
  </SectionLayout>;
}
