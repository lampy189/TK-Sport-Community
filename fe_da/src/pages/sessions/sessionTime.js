// Venue schedules are always in Vietnam time, independent of the browser zone.
export function venueToday(now = Date.now()) {
  return new Intl.DateTimeFormat('en-CA', { timeZone: 'Asia/Ho_Chi_Minh', year: 'numeric', month: '2-digit', day: '2-digit' }).format(now);
}

export function sessionTime(date, time) {
  return Date.parse(`${date}T${String(time).slice(0, 8)}+07:00`);
}

export function hasEnded(session, now = Date.now()) {
  return sessionTime(session.sessionDate, session.endTime) <= now || String(session.status ?? session.sessionStatus).toLowerCase() === 'completed';
}

export function canCheckIn(session, now = Date.now()) {
  const status = String(session?.sessionStatus ?? session?.status ?? '').toLowerCase();
  return !!session && (status ? ['open', 'published', 'closed'].includes(status) : session.conditionCode === 'ready') && !hasEnded(session, now)
    && sessionTime(session.sessionDate, session.startTime) <= now;
}
