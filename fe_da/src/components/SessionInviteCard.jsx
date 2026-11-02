import React, { useState, useEffect } from 'react';
import {
  CalendarPlus,
  CalendarDays,
  Clock,
  MapPin,
  ArrowUpRight,
  UserPlus,
  Send,
  Check,
  X
} from 'lucide-react';
import { api } from '../api';
import { openFloatingChatWith } from './FloatingChatBubble';

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

export function buildSessionInviteMessage(session, customNote = '') {
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
    const cleanText = content.slice(0, startIdx).trim();
    return { text: cleanText, session: parsed };
  } catch {
    return { text: content, session: null };
  }
}

export function ChatMessageContent({ content, isMine }) {
  const { text, session } = parseSessionInviteMessage(content);

  if (!session) {
    return <>{content}</>;
  }

  const isBadminton = session.sport === 'Cầu lông';
  const formattedPrice = session.price
    ? new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(session.price)
    : '';

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
      {text && <div>{text}</div>}
      <div
        style={{
          background: '#ffffff',
          color: '#14261c',
          borderRadius: '10px',
          padding: '11px 12px',
          border: '1px solid #d5e3da',
          boxShadow: '0 2px 6px rgba(16, 36, 26, 0.06)',
          display: 'flex',
          flexDirection: 'column',
          gap: '6px',
          minWidth: '210px'
        }}
      >
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: '6px' }}>
          <span
            style={{
              fontSize: '11px',
              fontWeight: 700,
              textTransform: 'uppercase',
              letterSpacing: '0.04em',
              color: isBadminton ? '#1b6f50' : '#b85c2b',
              display: 'inline-flex',
              alignItems: 'center',
              gap: '5px'
            }}
          >
            <span className={`comm-dot ${isBadminton ? 'badminton' : 'pickleball'}`} />
            Lời mời đặt sân · {session.sport}
          </span>
          <span
            style={{
              fontSize: '10.5px',
              fontWeight: 700,
              background: '#eaf5ef',
              color: '#12513a',
              padding: '2px 7px',
              borderRadius: '999px'
            }}
          >
            {session.band}
          </span>
        </div>

        <div style={{ fontSize: '13.5px', fontWeight: 700, color: '#14261c' }}>
          {session.court} {session.venue ? `· ${session.venue}` : ''}
        </div>

        <div style={{ fontSize: '12px', color: '#4d5d53', display: 'flex', flexDirection: 'column', gap: '3px' }}>
          <span style={{ display: 'inline-flex', alignItems: 'center', gap: '5px' }}>
            <CalendarDays size={12} /> Ngày: <strong>{session.date}</strong>
          </span>
          <span style={{ display: 'inline-flex', alignItems: 'center', gap: '5px' }}>
            <Clock size={12} /> Giờ: <strong>{session.time}</strong>
          </span>
          {formattedPrice && (
            <span style={{ color: '#1b6f50', fontWeight: 700, marginTop: '2px' }}>
              Vé: {formattedPrice}/người
            </span>
          )}
        </div>

        <button
          type="button"
          className="comm-btn comm-btn-primary"
          style={{ width: '100%', justifyContent: 'center', marginTop: '4px', padding: '7px 10px', fontSize: '12.5px' }}
          onClick={() => openSessionDetailById(session.id)}
        >
          <span>Xem &amp; Đặt sân ngay</span>
          <ArrowUpRight size={13} />
        </button>
      </div>
    </div>
  );
}

/**
 * Modal dùng trong trang Chi tiết buổi chơi (SessionDetailView):
 * Cho phép chọn bạn bè để gửi lời mời đặt sân buổi chơi hiện tại.
 */
export function InviteFriendsToSessionModal({ sessionSummary, onClose }) {
  const [friends, setFriends] = useState([]);
  const [loading, setLoading] = useState(true);
  const [sendingId, setSendingId] = useState(null);
  const [sentIds, setSentIds] = useState([]);
  const [customNote, setCustomNote] = useState('');
  const [error, setError] = useState(null);

  useEffect(() => {
    let active = true;
    api
      .getFriends()
      .then((res) => {
        if (active) setFriends(Array.isArray(res) ? res : []);
      })
      .catch(() => {
        if (active) setError('Không thể tải danh sách bạn bè.');
      })
      .finally(() => {
        if (active) setLoading(false);
      });
    return () => {
      active = false;
    };
  }, []);

  if (!sessionSummary) return null;

  const targetSportCode =
    sessionSummary.sportName === 'Badminton' ? 'badminton' : 'pickleball';

  const handleInviteFriend = async (friend) => {
    setSendingId(friend.friendPlayerId);
    setError(null);
    try {
      const msgContent = buildSessionInviteMessage(sessionSummary, customNote);
      await api.sendMessage(friend.friendPlayerId, msgContent);
      setSentIds((prev) => [...prev, friend.friendPlayerId]);
      openFloatingChatWith(friend);
    } catch (err) {
      setError(err.message || 'Không thể gửi lời mời đặt sân.');
    } finally {
      setSendingId(null);
    }
  };

  return (
    <div className="comm-modal-backdrop" onClick={onClose}>
      <div
        className="comm-modal-card"
        style={{ maxWidth: '540px' }}
        onClick={(e) => e.stopPropagation()}
      >
        <div className="comm-modal-header">
          <div>
            <span className="comm-eyebrow">RỦ BẠN BÈ CÙNG RA SÂN</span>
            <h2 style={{ margin: '4px 0 0', fontSize: '19px', fontWeight: 700, color: '#14261c' }}>
              Mời bạn bè đặt sân cùng buổi này
            </h2>
            <p style={{ margin: '4px 0 0', fontSize: '13px', color: '#4d5d53' }}>
              {sessionSummary.sportName === 'Badminton' ? 'Cầu lông' : 'Pickleball'} ·{' '}
              {sessionSummary.courtName} · Ngày {sessionSummary.sessionDate} (
              {sessionSummary.timeLabel || `${sessionSummary.startTime} - ${sessionSummary.endTime}`})
            </p>
          </div>
          <button type="button" className="comm-modal-close" onClick={onClose} title="Đóng">
            <X size={16} />
          </button>
        </div>

        <div className="comm-modal-body">
          {error && (
            <div
              style={{
                padding: '10px 14px',
                borderRadius: '10px',
                background: '#fef2f2',
                color: '#b91c1c',
                fontSize: '13px',
                fontWeight: 600
              }}
            >
              {error}
            </div>
          )}

          <div>
            <label
              style={{
                display: 'block',
                fontSize: '12px',
                fontWeight: 700,
                color: '#4d5d53',
                marginBottom: '6px'
              }}
            >
              Lời nhắn kèm theo (tùy chọn)
            </label>
            <input
              type="text"
              className="comm-form-input"
              style={{ width: '100%' }}
              placeholder="Ví dụ: Tối nay ra sân làm vài set cầu lông với mình nhé!"
              value={customNote}
              onChange={(e) => setCustomNote(e.target.value)}
            />
          </div>

          <div>
            <div
              style={{
                fontSize: '12.5px',
                fontWeight: 700,
                color: '#14261c',
                marginBottom: '10px'
              }}
            >
              Chọn bạn bè để gửi thẻ mời đặt sân ({friends.length})
            </div>

            {loading ? (
              <div style={{ padding: '24px', textAlign: 'center', color: '#4d5d53', fontSize: '13px' }}>
                Đang tải danh sách bạn bè...
              </div>
            ) : friends.length === 0 ? (
              <div
                style={{
                  padding: '20px',
                  borderRadius: '10px',
                  background: '#fafcfa',
                  border: '1px solid #e1e8e3',
                  color: '#4d5d53',
                  fontSize: '13.5px',
                  textAlign: 'center'
                }}
              >
                Bạn chưa có người bạn nào trong danh sách Bạn bè. Hãy kết bạn trong mục Cộng đồng hoặc bấm vào người chơi ở danh sách Cùng ra sân.
              </div>
            ) : (
              <div style={{ display: 'flex', flexDirection: 'column', gap: '8px', maxHeight: '300px', overflowY: 'auto' }}>
                {friends.map((fr) => {
                  const sp = (fr.sportProfiles || []).find(
                    (s) => s.sportCode?.toLowerCase() === targetSportCode
                  );
                  const friendBand = sp?.currentBand && sp.currentBand !== 'Unrated' ? `Band ${sp.currentBand}` : 'Chưa chấm Band';
                  const isSent = sentIds.includes(fr.friendPlayerId);

                  return (
                    <div
                      key={fr.friendPlayerId}
                      style={{
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'space-between',
                        gap: '12px',
                        padding: '10px 14px',
                        borderRadius: '10px',
                        border: '1px solid #e1e8e3',
                        background: '#fafcfa'
                      }}
                    >
                      <div style={{ display: 'flex', alignItems: 'center', gap: '10px', minWidth: 0 }}>
                        <div className="comm-avatar-sm">
                          {fr.avatarUrl ? (
                            <img src={fr.avatarUrl} alt={fr.fullName} />
                          ) : (
                            (fr.fullName || 'P').charAt(0).toUpperCase()
                          )}
                        </div>
                        <div style={{ minWidth: 0 }}>
                          <div style={{ fontWeight: 700, fontSize: '14px', color: '#14261c' }}>
                            {fr.fullName}
                          </div>
                          <div style={{ fontSize: '12px', color: '#4d5d53' }}>
                            Trình độ {sessionSummary.sportName === 'Badminton' ? 'Cầu lông' : 'Pickleball'}:{' '}
                            <strong style={{ color: '#1b6f50' }}>{friendBand}</strong>
                          </div>
                        </div>
                      </div>

                      <button
                        type="button"
                        className={`comm-btn ${isSent ? 'comm-btn-outline' : 'comm-btn-primary'}`}
                        disabled={sendingId === fr.friendPlayerId || isSent}
                        onClick={() => handleInviteFriend(fr)}
                      >
                        {isSent ? (
                          <>
                            <Check size={14} />
                            <span>Đã gửi lời mời</span>
                          </>
                        ) : (
                          <>
                            <Send size={14} />
                            <span>{sendingId === fr.friendPlayerId ? 'Đang gửi...' : 'Mời đặt sân'}</span>
                          </>
                        )}
                      </button>
                    </div>
                  );
                })}
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}

/**
 * Modal dùng ngay trong khung Tin nhắn:
 * Cho phép chọn một buổi chơi đang mở để gửi lời mời đặt sân cho người bạn đang chat.
 */
export function PickSessionToInviteModal({ friendName, onSelectSessionToSend, onClose }) {
  const [sessions, setSessions] = useState([]);
  const [loading, setLoading] = useState(true);
  const [sendingId, setSendingId] = useState(null);

  useEffect(() => {
    let active = true;
    api
      .getSessions({})
      .then((list) => {
        if (!active) return;
        const arr = Array.isArray(list) ? list : [];
        const openOnes = arr.filter((s) => {
          const avail = s.availableSlots ?? s.capacity - s.occupiedPlaces;
          return avail > 0;
        });
        setSessions(openOnes.slice(0, 20));
      })
      .catch(() => {})
      .finally(() => {
        if (active) setLoading(false);
      });
    return () => {
      active = false;
    };
  }, []);

  const handlePick = async (s) => {
    setSendingId(s.id);
    try {
      const msgText = buildSessionInviteMessage(s);
      await onSelectSessionToSend(msgText);
      onClose();
    } finally {
      setSendingId(null);
    }
  };

  return (
    <div className="comm-modal-backdrop" onClick={onClose}>
      <div
        className="comm-modal-card"
        style={{ maxWidth: '560px' }}
        onClick={(e) => e.stopPropagation()}
      >
        <div className="comm-modal-header">
          <div>
            <span className="comm-eyebrow">HẸN LỊCH RA SÂN</span>
            <h2 style={{ margin: '4px 0 0', fontSize: '18.5px', fontWeight: 700, color: '#14261c' }}>
              Rủ {friendName} cùng đặt sân
            </h2>
            <p style={{ margin: '4px 0 0', fontSize: '13px', color: '#4d5d53' }}>
              Chọn một buổi chơi đang còn chỗ trống để gửi thẻ mời đặt sân vào cuộc trò chuyện.
            </p>
          </div>
          <button type="button" className="comm-modal-close" onClick={onClose} title="Đóng">
            <X size={16} />
          </button>
        </div>

        <div className="comm-modal-body">
          {loading ? (
            <div style={{ padding: '24px', textAlign: 'center', color: '#4d5d53', fontSize: '13px' }}>
              Đang tải danh sách buổi chơi đang mở...
            </div>
          ) : sessions.length === 0 ? (
            <div style={{ padding: '20px', textAlign: 'center', color: '#4d5d53', fontSize: '13.5px' }}>
              Hiện chưa có buổi chơi nào đang mở.
            </div>
          ) : (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '8px', maxHeight: '340px', overflowY: 'auto' }}>
              {sessions.map((s) => {
                const isBadminton = s.sportName === 'Badminton';
                const avail = s.availableSlots ?? s.capacity - s.occupiedPlaces;
                const timeRange = s.timeLabel || `${(s.startTime || '').slice(0, 5)} - ${(s.endTime || '').slice(0, 5)}`;
                return (
                  <div
                    key={s.id}
                    style={{
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'space-between',
                      gap: '12px',
                      padding: '12px 14px',
                      borderRadius: '10px',
                      border: '1px solid #e1e8e3',
                      background: '#fafcfa'
                    }}
                  >
                    <div style={{ minWidth: 0 }}>
                      <div style={{ display: 'flex', alignItems: 'center', gap: '6px', marginBottom: '3px' }}>
                        <span className={`comm-dot ${isBadminton ? 'badminton' : 'pickleball'}`} />
                        <strong style={{ fontSize: '13.5px', color: '#14261c' }}>
                          {isBadminton ? 'Cầu lông' : 'Pickleball'} · {s.courtName}
                        </strong>
                        <span className="comm-status-pill confirmed">
                          {s.band ? `Band ${s.band}` : 'Tự do'}
                        </span>
                      </div>
                      <div style={{ fontSize: '12px', color: '#4d5d53' }}>
                        Ngày {s.sessionDate} • {timeRange} • Còn {avail}/{s.capacity} chỗ
                      </div>
                    </div>

                    <button
                      type="button"
                      className="comm-btn comm-btn-primary"
                      disabled={sendingId === s.id}
                      onClick={() => handlePick(s)}
                    >
                      <Send size={13} />
                      <span>{sendingId === s.id ? 'Đang gửi...' : 'Gửi lời mời'}</span>
                    </button>
                  </div>
                );
              })}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

