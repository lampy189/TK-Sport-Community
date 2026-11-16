const SESSION_TAG_PREFIX = '[TKSC_SESSION:';
const SESSION_TAG_SUFFIX = ']';

export function openSessionDetailById(sessionId) {
  if (!sessionId) return;
  window.dispatchEvent(new CustomEvent('tksc:close-chat'));
  window.dispatchEvent(
    new CustomEvent('tksc:open-session', {
      detail: { sessionId: Number(sessionId), ts: Date.now() }
    })
  );
}

export function buildSessionInviteMessage(session, customNote = '', invitationId = null) {
  const sportVi =
    session.sportName === 'Badminton' || session.sportCode === 'badminton'
      ? 'Cầu lông'
      : 'Pickleball';
  const timeRange =
    session.timeLabel ||
    `${(session.startTime || '').slice(0, 5)} - ${(session.endTime || '').slice(0, 5)}`;
  const bandLabel = session.band ? `Band ${session.band}` : 'Tự do';
  const payload = {
    id: session.id,
    invitationId,
    sport: sportVi,
    court: session.courtName || 'Sân TKSC',
    venue: session.venueName || '',
    date: session.sessionDate || '',
    time: timeRange,
    band: bandLabel,
    price: session.priceVnd || 0
  };
  const intro =
    customNote.trim() ||
    `Mình rủ bạn cùng đặt sân buổi ${sportVi} ngày ${session.sessionDate} (${timeRange}) tại ${session.courtName}!`;
  return `${intro} ${SESSION_TAG_PREFIX}${JSON.stringify(payload)}${SESSION_TAG_SUFFIX}`;
}

export function parseSessionInviteMessage(content) {
  if (!content || typeof content !== 'string') {
    return { text: content || '', session: null };
  }
  const startIdx = content.indexOf(SESSION_TAG_PREFIX);
  if (startIdx === -1) {
    return { text: content, session: null };
  }
  const endIdx = content.lastIndexOf(SESSION_TAG_SUFFIX);
  if (endIdx === -1 || endIdx <= startIdx) {
    return { text: content, session: null };
  }
  const rawJson = content.slice(startIdx + SESSION_TAG_PREFIX.length, endIdx);
  try {
    const parsed = JSON.parse(rawJson);
    if (!parsed || Array.isArray(parsed) || !Number.isSafeInteger(Number(parsed.id)) || Number(parsed.id) <= 0
      || ['sport', 'court', 'date', 'time', 'band'].some(key => typeof parsed[key] !== 'string')
      || (parsed.venue != null && typeof parsed.venue !== 'string')
      || typeof parsed.price !== 'number' || !Number.isFinite(parsed.price) || parsed.price < 0
      || (parsed.invitationId != null && (!Number.isSafeInteger(Number(parsed.invitationId)) || Number(parsed.invitationId) <= 0))) {
      return { text: content, session: null };
    }
    const cleanText = content.slice(0, startIdx).trim();
    return { text: cleanText, session: parsed };
  } catch {
    return { text: content, session: null };
  }
}


export function openFloatingChatWith(friendObj) { window.dispatchEvent(new CustomEvent('tksc:open-chat', { detail: friendObj })); }
