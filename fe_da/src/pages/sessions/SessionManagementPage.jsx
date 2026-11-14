import { useEffect, useMemo, useState } from 'react';
import { CalendarPlus, CircleCheck, RefreshCw, Save, UserRound } from 'lucide-react';
import SectionLayout from '../../components/layout/SectionLayout';
import { workflowRequest } from '../workflowClient';
import '../workflow.css';
import './session-management.css';

const root = '/api/sessions/manage';
const blank = () => ({ sportId: '', courtId: '', timeSlotId: '', sessionDate: new Date().toLocaleDateString('en-CA'), band: '', capacity: '8', priceVnd: '0' });
const money = value => `${Number(value || 0).toLocaleString('vi-VN')} ₫`;
const statusNames = { Draft: 'Nháp', Open: 'Đang mở', Published: 'Đang mở', Closed: 'Đã đóng', cancelled: 'Đã hủy', completed: 'Đã kết thúc' };

export default function SessionManagementPage() {
  const [options, setOptions] = useState({ sports: [], courts: [], timeSlots: [], personnel: [] });
  const [sessions, setSessions] = useState([]);
  const [form, setForm] = useState(blank);
  const [selectedId, setSelectedId] = useState(null);
  const [refereeIds, setRefereeIds] = useState([]);
  const [staffIds, setStaffIds] = useState([]);
  const [busy, setBusy] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');

  async function reload() {
    setError('');
    const [available, rows] = await Promise.all([workflowRequest(`${root}/options`), workflowRequest(root)]);
    setOptions(available);
    setSessions(rows);
  }

  useEffect(() => {
    let active = true;
    Promise.all([workflowRequest(`${root}/options`), workflowRequest(root)])
      .then(([available, rows]) => {
        if (!active) return;
        setOptions(available);
        setSessions(rows);
      })
      .catch(e => { if (active) setError(e.message); })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, []);

  const courts = useMemo(() => options.courts.filter(court => !form.sportId || !court.sportId || court.sportId === Number(form.sportId)), [options.courts, form.sportId]);
  const occupied = sessions.find(session => session.id === selectedId)?.occupiedPlaces ?? 0;

  function choose(session) {
    setSelectedId(session.id);
    setForm({ sportId: String(session.sportId), courtId: String(session.courtId), timeSlotId: String(session.timeSlotId), sessionDate: session.sessionDate, band: session.band ?? '', capacity: String(session.capacity), priceVnd: String(session.priceVnd) });
    setRefereeIds(session.referees.map(person => String(person.id)));
    setStaffIds(session.staff.map(person => String(person.id)));
    setError('');
    setNotice('');
  }

  function createNew() {
    setSelectedId(null);
    setForm({ ...blank(), sportId: String(options.sports[0]?.id ?? ''), courtId: String(courts[0]?.id ?? ''), timeSlotId: String(options.timeSlots[0]?.id ?? '') });
    setRefereeIds([]);
    setStaffIds([]);
    setError('');
    setNotice('');
  }

  function toggle(setter, current, id) {
    setter(current.includes(id) ? current.filter(value => value !== id) : [...current, id]);
  }

  async function save(event) {
    event.preventDefault();
    setBusy(true); setError(''); setNotice('');
    try {
      const body = { ...form, sportId: Number(form.sportId), courtId: Number(form.courtId), timeSlotId: Number(form.timeSlotId), capacity: Number(form.capacity), priceVnd: Number(form.priceVnd), band: form.band || null };
      const saved = await workflowRequest(selectedId ? `${root}/${selectedId}` : root, selectedId ? 'PUT' : 'POST', body);
      const id = saved.id;
      const assigned = await workflowRequest(`${root}/${id}/assignments`, 'PUT', { refereeIds: refereeIds.map(Number), staffIds: staffIds.map(Number) });
      await reload();
      choose(assigned);
      setNotice(selectedId ? 'Đã lưu buổi chơi và phân công.' : 'Đã tạo buổi nháp và lưu phân công.');
    } catch (e) { setError(e.message); }
    finally { setBusy(false); }
  }

  async function changeStatus(action) {
    if (!selectedId) return;
    setBusy(true); setError(''); setNotice('');
    try {
      const updated = await workflowRequest(`${root}/${selectedId}/${action}`, 'POST', {});
      await reload();
      choose(updated);
      setNotice(action === 'open' ? 'Đã mở đăng ký. Player có thể tìm và đăng ký buổi này.' : 'Đã đóng đăng ký; vé hiện có vẫn được giữ nguyên.');
    } catch (e) { setError(e.message); }
    finally { setBusy(false); }
  }

  const selected = sessions.find(session => session.id === selectedId);
  return <SectionLayout title="Quản lý buổi chơi" description="Lập lịch, phân công nhân sự và điều khiển đăng ký tại cơ sở của bạn.">
    <div className="workflow manager-sessions" aria-busy={busy || loading}>
      <div className="workflow-actions">
        <button className="button button-primary" type="button" disabled={busy || loading} onClick={createNew}><CalendarPlus size={17} />Tạo buổi</button>
        <button className="button button-secondary" type="button" disabled={busy || loading} onClick={() => reload().catch(e => setError(e.message))}><RefreshCw size={17} />Tải lại</button>
      </div>
      {error && <p className="workflow-error" role="alert">{error}</p>}
      {notice && <p className="manager-session-notice" role="status"><CircleCheck size={17} />{notice}</p>}
      <div className="manager-session-layout">
        <section className="manager-session-list" aria-label="Lịch buổi chơi">
          <h3>Lịch tại cơ sở</h3>
          {loading ? <p>Đang tải lịch…</p> : sessions.length === 0 ? <p>Chưa có buổi chơi.</p> : sessions.map(session => (
            <button type="button" key={session.id} className={`manager-session-row ${selectedId === session.id ? 'is-selected' : ''}`} onClick={() => choose(session)} aria-pressed={selectedId === session.id}>
              <span className="manager-session-row-top"><strong>{session.sportName} · {session.courtName}</strong><span>{statusNames[session.status] || session.status}</span></span>
              <span>{session.sessionDate} · {session.startTime.slice(0, 5)}–{session.endTime.slice(0, 5)}</span>
              <small>{session.occupiedPlaces}/{session.capacity} chỗ · {money(session.priceVnd)} · {session.referees.length} Referee · {session.staff.length} Staff</small>
            </button>
          ))}
        </section>
        <form className="manager-session-form" onSubmit={save}>
          <div className="manager-session-form-heading"><div><h3>{selectedId ? `Buổi #${selectedId}` : 'Thông tin buổi'}</h3><p>{selected ? statusNames[selected.status] || selected.status : 'Buổi mới được lưu ở trạng thái nháp.'}</p></div></div>
          <div className="manager-session-fields">
            <label>Môn<select required value={form.sportId} onChange={event => setForm(current => ({ ...current, sportId: event.target.value, courtId: '' }))} disabled={busy}><option value="">Chọn môn</option>{options.sports.map(sport => <option key={sport.id} value={sport.id}>{sport.name}</option>)}</select></label>
            <label>Sân<select required value={form.courtId} onChange={event => setForm(current => ({ ...current, courtId: event.target.value }))} disabled={busy}><option value="">Chọn sân</option>{courts.map(court => <option key={court.id} value={court.id}>{court.name}</option>)}</select></label>
            <label>Ngày<input type="date" required value={form.sessionDate} onChange={event => setForm(current => ({ ...current, sessionDate: event.target.value }))} disabled={busy} /></label>
            <label>Khung giờ<select required value={form.timeSlotId} onChange={event => setForm(current => ({ ...current, timeSlotId: event.target.value }))} disabled={busy}><option value="">Chọn giờ</option>{options.timeSlots.map(slot => <option key={slot.id} value={slot.id}>{slot.label}</option>)}</select></label>
            <label>Sức chứa<input type="number" min="1" max="8" required value={form.capacity} onChange={event => setForm(current => ({ ...current, capacity: event.target.value }))} disabled={busy || (occupied > 0 && Number(form.capacity) < occupied)} /></label>
            <label>Giá vé (VND)<input type="number" min="0" step="1000" required value={form.priceVnd} onChange={event => setForm(current => ({ ...current, priceVnd: event.target.value }))} disabled={busy || occupied > 0} /></label>
            <label>Điều kiện trình độ<select value={form.band} onChange={event => setForm(current => ({ ...current, band: event.target.value }))} disabled={busy || occupied > 0}><option value="">Không giới hạn</option>{['B1', 'B2', 'B3', 'B4', 'B5', 'P1', 'P2', 'P3', 'P4', 'P5'].map(band => <option key={band} value={band}>{band}</option>)}</select></label>
          </div>
          {occupied > 0 && <p className="manager-session-lock">Buổi có {occupied} vé đang giữ hoặc đã xác nhận. Môn, sân, giờ, giá và trình độ được khóa.</p>}
          <fieldset className="manager-session-people" disabled={busy}>
            <legend><UserRound size={17} />Referee <span>Bắt buộc ít nhất một người để mở đăng ký</span></legend>
            {options.personnel.filter(person => person.role.toLowerCase() === 'referee').map(person => <label key={person.id}><input type="checkbox" checked={refereeIds.includes(String(person.id))} onChange={() => toggle(setRefereeIds, refereeIds, String(person.id))} />{person.name}</label>)}
          </fieldset>
          <fieldset className="manager-session-people" disabled={busy}>
            <legend><UserRound size={17} />Staff <span>Tùy chọn</span></legend>
            {options.personnel.filter(person => person.role.toLowerCase() === 'staff').map(person => <label key={person.id}><input type="checkbox" checked={staffIds.includes(String(person.id))} onChange={() => toggle(setStaffIds, staffIds, String(person.id))} />{person.name}</label>)}
          </fieldset>
          <div className="workflow-actions manager-session-controls">
            <button className="button button-primary" disabled={busy || loading || options.sports.length === 0 || options.courts.length === 0 || options.timeSlots.length === 0}><Save size={17} />{busy ? 'Đang lưu…' : 'Lưu buổi & phân công'}</button>
            {selected && selected.status !== 'Open' && selected.status !== 'Published' && <button className="button button-secondary" type="button" disabled={busy || refereeIds.length === 0} onClick={() => changeStatus('open')}>Mở đăng ký</button>}
            {selected && (selected.status === 'Open' || selected.status === 'Published') && <button className="button button-secondary" type="button" disabled={busy} onClick={() => changeStatus('close')}>Đóng đăng ký</button>}
          </div>
        </form>
      </div>
    </div>
  </SectionLayout>;
}