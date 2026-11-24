import { useEffect, useState } from 'react';
import { api } from '../../api';
import { DEFAULT_TIME_SLOTS } from '../../config/sessionTimeSlots';
import { CalendarDays, ChevronLeft, ChevronRight } from 'lucide-react';
import '../../styles/sessions.css';

const weekdays = ['Thứ 2', 'Thứ 3', 'Thứ 4', 'Thứ 5', 'Thứ 6', 'Thứ 7', 'CN'];
const dateInVietnam = new Intl.DateTimeFormat('sv-SE', { timeZone: 'Asia/Ho_Chi_Minh' });
const labels = { ready: 'Sẵn sàng', completed: 'Đã kết thúc' };

function addDays(date, offset) {
  const parsed = new Date(`${date}T12:00:00Z`);
  parsed.setUTCDate(parsed.getUTCDate() + offset);
  return parsed.toISOString().slice(0, 10);
}

function mondayOf(date) {
  const day = new Date(`${date}T12:00:00Z`).getUTCDay();
  return addDays(date, -((day + 6) % 7));
}

function timeRange({ startTime, endTime }) {
  return `${startTime.slice(0, 5)}${endTime ? ` – ${endTime.slice(0, 5)}` : ""}`;
}

function shortDate(date) {
  const [, month, day] = date.split('-');
  return `${day}/${month}`;
}

export default function AssignedSessionsTimetable({ sessions, weekDate, onWeekDateChange, onSelect, busy, label = "Lịch điểm danh theo tuần", action = "Mở điểm danh", currentWeekLabel = "Tuần này" }) {
  const today = dateInVietnam.format(new Date());
  const currentWeek = mondayOf(today);
  const start = mondayOf(weekDate || today);
  const days = weekdays.map((label, index) => ({ label, date: addDays(start, index) }));
  const end = days.at(-1).date;
  const [timeSlots, setTimeSlots] = useState(DEFAULT_TIME_SLOTS);
  useEffect(() => {
    let active = true;
    api.getTimeSlots().then(slots => {
      if (active && slots.length) setTimeSlots(slots);
    }).catch(() => {
      // Keep the default grid if the configured time slots cannot be loaded.
    });
    return () => { active = false; };
  }, []);

  const weekSessions = sessions.filter(session => session.sessionDate >= start && session.sessionDate <= end);
  const rows = new Map(timeSlots.map(slot => [timeRange(slot), new Map()]));

  for (const session of weekSessions) {
    const matchingSlot = timeSlots.find(slot => slot.startTime.slice(0, 5) === session.startTime.slice(0, 5));
    const slot = timeRange({ ...session, endTime: session.endTime || matchingSlot?.endTime });
    if (!rows.has(slot)) rows.set(slot, new Map());
    const cells = rows.get(slot);
    if (!cells.has(session.sessionDate)) cells.set(session.sessionDate, []);
    cells.get(session.sessionDate).push(session);
  }

  return <div className="assigned-timetable">
    <div className="timetable-toolbar">
      <div className="timetable-week-input-row">
        <span className="timetable-week-badge">{shortDate(start)} – {shortDate(end)}/{end.slice(0, 4)}</span>
        <input type="date" aria-label="Chọn ngày để xem tuần" value={weekDate || today}
          onChange={event => { if (event.target.value) onWeekDateChange(event.target.value); }} />
      </div>
      <div className="timetable-nav-buttons">
        <button type="button" className="timetable-nav-btn" onClick={() => onWeekDateChange(addDays(start, -7))}>
          <ChevronLeft size={15} aria-hidden="true" /> Tuần trước
        </button>
        <button type="button" className={start === currentWeek ? 'timetable-nav-btn is-current' : 'timetable-nav-btn'}
          onClick={() => onWeekDateChange(today)}>
          <CalendarDays size={15} aria-hidden="true" /> {currentWeekLabel}
        </button>
        <button type="button" className="timetable-nav-btn" onClick={() => onWeekDateChange(addDays(start, 7))}>
          Tuần sau <ChevronRight size={15} aria-hidden="true" />
        </button>
      </div>
    </div>
    {weekSessions.length === 0 && <p role="status">Tuần đã chọn chưa có buổi được phân công.</p>}
    <div className="timetable-scroll">
      <table className="timetable-table" aria-label={label}>
        <thead><tr>
          <th scope="col" className="timetable-slot-col">Khung giờ</th>
          {days.map(({ label, date }) => <th key={date} scope="col" className={date === today ? 'timetable-day-col is-today' : 'timetable-day-col'}>
            <span className="day-title">{label}</span>
            <span className="day-subtitle">{shortDate(date)}</span>
          </th>)}
        </tr></thead>
        <tbody>{[...rows.keys()].sort().map(slot => <tr key={slot}>
          <th scope="row" className="timetable-slot-cell"><span className="slot-range">{slot}</span></th>
          {days.map(({ date }) => {
            const assigned = rows.get(slot).get(date) || [];
            const cards = <div className="timetable-cell-list">{assigned.map(session => {
              const badminton = session.sportName === 'Badminton';
              const sport = badminton ? 'Cầu lông' : session.sportName;
              const status = labels[session.conditionCode] || 'Chưa sẵn sàng';
              return <button key={session.sessionId} type="button" disabled={busy}
                className={`timetable-event ${badminton ? 'is-badminton' : 'is-pickleball'}`}
                aria-label={`${action} ${sport}, ${session.courtName}, ${shortDate(date)}, ${slot}`}
                title={session.conditionMessage}
                onClick={() => onSelect(session.sessionId)}>
                <strong className="timetable-event-title">{sport}</strong>
                <span className="timetable-event-meta">{session.courtName}</span>
                {session.conditionCode && <small>{status}</small>}
                {session.assignmentLabel && <small>{session.assignmentLabel}</small>}
              </button>;
            })}</div>;
            return <td key={date} className={date === today ? 'timetable-cell is-today-col' : 'timetable-cell'}>
              {assigned.length > 1 ? <details className="timetable-group">
                <summary>{assigned.length} buổi</summary>
                {cards}
              </details> : assigned.length === 1 ? cards : <span className="timetable-empty" aria-label="Trống">–</span>}
            </td>;
          })}
        </tr>)}</tbody>
      </table>
    </div>
  </div>;
}
