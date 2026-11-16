import { useState, useEffect, useCallback } from 'react';
import {
  UserPlus,
  Check,
  MessageSquare,
  X,
  RefreshCw,
  Send
} from 'lucide-react';
import { api } from '../api';
import { openFloatingChatWith } from './sessionInviteMessage';
import { ChatMessageContent } from './SessionInviteCard';
import '../styles/community.css';

function formatGender(g) {
  if (!g) return 'Chưa cập nhật';
  const low = g.toLowerCase();
  if (low === 'male' || low === 'nam') return 'Nam';
  if (low === 'female' || low === 'nữ' || low === 'nu') return 'Nữ';
  return 'Khác';
}

function formatStatusBadge(status) {
  if (!status) return 'Chưa chấm';
  const s = status.toLowerCase();
  if (s === 'confirmed' || s === 'rated') return 'Chính thức';
  if (s === 'provisional') return 'Tạm tính';
  return 'Chưa chấm';
}

function formatStatusLabel(status) {
  if (!status) return 'Chưa chấm';
  const s = status.toLowerCase();
  if (s === 'confirmed' || s === 'rated') return 'Chính thức (Trọng tài xác nhận)';
  if (s === 'provisional') return 'Tạm tính (Tự đánh giá)';
  return 'Chưa đánh giá';
}

function getStatusClass(status) {
  if (!status) return 'unrated';
  const s = status.toLowerCase();
  if (s === 'confirmed' || s === 'rated') return 'confirmed';
  if (s === 'provisional') return 'provisional';
  return 'unrated';
}

export function PublicPlayerProfileModal({
  playerId,
  onClose,
  onFriendshipChanged
}) {
  const [profile, setProfile] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [actionBusy, setActionBusy] = useState(false);
  const [actionBanner, setActionBanner] = useState(null);

  // Embedded direct chat state when friends
  const [showInlineChat, setShowInlineChat] = useState(false);
  const [messages, setMessages] = useState([]);
  const [chatLoading, setChatLoading] = useState(false);
  const [draftMsg, setDraftMsg] = useState('');
  const [sendingMsg, setSendingMsg] = useState(false);

  const loadPublicProfile = useCallback(async () => {
    if (!playerId) return;
    setLoading(true);
    setError(null);
    try {
      const data = await api.getPublicPlayerProfile(playerId);
      setProfile(data);
    } catch (err) {
      setError(err.message || 'Không thể tải hồ sơ công khai của người chơi.');
    } finally {
      setLoading(false);
    }
  }, [playerId]);

  useEffect(() => {
    const initial = setTimeout(() => void loadPublicProfile(), 0);
    return () => clearTimeout(initial);
  }, [loadPublicProfile]);

  const loadInlineChat = useCallback(async () => {
    if (!playerId) return;
    setChatLoading(true);
    try {
      const list = await api.getConversation(playerId, 40);
      setMessages(Array.isArray(list) ? list : []);
      await api.markMessagesAsRead(playerId);
    } catch (e) {
      setActionBanner({ type: 'error', text: e.message || 'Chưa tải được tin nhắn. Vui lòng thử lại.' });
    } finally {
      setChatLoading(false);
    }
  }, [playerId]);

  useEffect(() => {
    if (showInlineChat && profile?.friendshipStatus === 'Accepted') {
      const initial = setTimeout(() => void loadInlineChat(), 0);
      return () => clearTimeout(initial);
    }
  }, [showInlineChat, profile?.friendshipStatus, loadInlineChat]);

  const handleSendFriendRequest = async () => {
    setActionBusy(true);
    setActionBanner(null);
    try {
      await api.sendFriendRequest(playerId);
      setActionBanner({ type: 'success', text: 'Đã gửi lời mời kết bạn thành công.' });
      await loadPublicProfile();
      if (onFriendshipChanged) onFriendshipChanged();
    } catch (err) {
      setActionBanner({ type: 'error', text: err.message || 'Không thể gửi lời mời kết bạn.' });
    } finally {
      setActionBusy(false);
    }
  };

  const handleRespondRequest = async (accept) => {
    if (!profile?.friendshipId) return;
    setActionBusy(true);
    setActionBanner(null);
    try {
      await api.respondFriendRequest(profile.friendshipId, accept);
      setActionBanner({
        type: 'success',
        text: accept ? 'Đã chấp nhận lời mời kết bạn. Giờ hai bạn có thể nhắn tin.' : 'Đã từ chối lời mời kết bạn.'
      });
      await loadPublicProfile();
      if (accept) setShowInlineChat(true);
      if (onFriendshipChanged) onFriendshipChanged();
    } catch (err) {
      setActionBanner({ type: 'error', text: err.message || 'Thao tác thất bại.' });
    } finally {
      setActionBusy(false);
    }
  };

  const handleRemoveOrCancel = async () => {
    setActionBusy(true);
    setActionBanner(null);
    try {
      await api.removeFriend(playerId);
      setShowInlineChat(false);
      setActionBanner({ type: 'info', text: 'Đã cập nhật trạng thái kết bạn.' });
      await loadPublicProfile();
      if (onFriendshipChanged) onFriendshipChanged();
    } catch (err) {
      setActionBanner({ type: 'error', text: err.message || 'Không thể hủy kết bạn.' });
    } finally {
      setActionBusy(false);
    }
  };

  const handleSendDirectMessage = async (e) => {
    e.preventDefault();
    if (!draftMsg.trim() || sendingMsg) return;
    setSendingMsg(true);
    try {
      const created = await api.sendMessage(playerId, draftMsg.trim());
      setMessages((prev) => [...prev, created]);
      setDraftMsg('');
    } catch (err) {
      setActionBanner({ type: 'error', text: err.message || 'Không thể gửi tin nhắn.' });
    } finally {
      setSendingMsg(false);
    }
  };

  if (!playerId) return null;

  const sports = profile?.sportProfiles || [];
  const badminton = sports.find((s) => s.sportCode?.toLowerCase() === 'badminton') || {
    sportCode: 'badminton',
    sportName: 'Cầu lông (Badminton)',
    currentBand: 'Unrated',
    currentScore: null,
    ratingStatus: 'Unrated'
  };
  const pickleball = sports.find((s) => s.sportCode?.toLowerCase() === 'pickleball') || {
    sportCode: 'pickleball',
    sportName: 'Pickleball',
    currentBand: 'Unrated',
    currentScore: null,
    ratingStatus: 'Unrated'
  };

  return (
    <div className="comm-modal-backdrop" onClick={onClose}>
      <div className="comm-modal-card" onClick={(e) => e.stopPropagation()}>
        <div className="comm-modal-header">
          <div style={{ display: 'flex', alignItems: 'center', gap: '16px' }}>
            <div className="comm-avatar-lg">
              {profile?.avatarUrl ? (
                <img src={profile.avatarUrl} alt={profile.fullName} />
              ) : (
                (profile?.fullName || 'P').charAt(0).toUpperCase()
              )}
            </div>
            <div>
              <div style={{ display: 'flex', alignItems: 'center', gap: '8px', flexWrap: 'wrap' }}>
                <h2 style={{ margin: 0, fontSize: '20px', fontWeight: 700, color: '#14261c' }}>
                  {loading ? 'Đang tải hồ sơ...' : profile?.fullName || 'Người chơi TKSC'}
                </h2>
                {profile?.isMe && (
                  <span className="comm-status-pill confirmed">
                    Hồ sơ của bạn
                  </span>
                )}
                {profile?.friendshipStatus === 'Accepted' && (
                  <span className="comm-status-pill confirmed" style={{ display: 'inline-flex', alignItems: 'center', gap: '4px' }}>
                    <Check size={12} /> Bạn bè
                  </span>
                )}
              </div>
              {profile && (
                <div style={{ fontSize: '13px', color: '#4d5d53', marginTop: '4px', display: 'flex', gap: '14px', flexWrap: 'wrap' }}>
                  <span>Giới tính: <strong style={{ color: '#14261c' }}>{formatGender(profile.gender)}</strong></span>
                  {profile.dateOfBirth && <span>Ngày sinh: <strong style={{ color: '#14261c' }}>{profile.dateOfBirth}</strong></span>}
                  <span>Mã Player: <strong style={{ color: '#14261c' }}>#{profile.playerId}</strong></span>
                </div>
              )}
              {profile?.bio && (
                <p style={{ margin: '8px 0 0', fontSize: '13.5px', color: '#4d5d53', lineHeight: 1.5 }}>
                  “{profile.bio}”
                </p>
              )}
            </div>
          </div>
          <button type="button" className="comm-modal-close" onClick={onClose} title="Đóng">
            <X size={16} />
          </button>
        </div>

        <div className="comm-modal-body">
          {error && (
            <div style={{ padding: '12px 16px', borderRadius: '10px', background: '#fef2f2', color: '#b91c1c', fontSize: '13.5px', fontWeight: 600 }}>
              {error}
            </div>
          )}

          {actionBanner && (
            <div
              style={{
                padding: '10px 14px',
                borderRadius: '10px',
                fontSize: '13.5px',
                fontWeight: 600,
                background: actionBanner.type === 'error' ? '#fef2f2' : '#eaf5ef',
                color: actionBanner.type === 'error' ? '#b91c1c' : '#12513a',
                border: `1px solid ${actionBanner.type === 'error' ? '#fecaca' : '#c6e2d3'}`
              }}
            >
              {actionBanner.text}
            </div>
          )}

          {!loading && profile && (
            <>
              {/* Friendship & Chat Action Bar (UC-25c / SWL 1) */}
              {!profile.isMe && (
                <div
                  style={{
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'space-between',
                    gap: '12px',
                    flexWrap: 'wrap',
                    padding: '14px 16px',
                    background: '#fafcfa',
                    borderRadius: '12px',
                    border: '1px solid #e1e8e3'
                  }}
                >
                  <div style={{ fontSize: '13.5px', color: '#14261c', fontWeight: 500 }}>
                    {profile.friendshipStatus === 'None' && 'Kết bạn để nhắn tin và hẹn lịch cùng ra sân.'}
                    {profile.friendshipStatus === 'PendingSent' && 'Bạn đã gửi lời mời kết bạn. Đang chờ đối phương phản hồi.'}
                    {profile.friendshipStatus === 'PendingReceived' && `${profile.fullName} đã gửi lời mời kết bạn cho bạn.`}
                    {profile.friendshipStatus === 'Accepted' && `Bạn và ${profile.fullName} đã là bạn bè trong cộng đồng TKSC.`}
                  </div>

                  <div style={{ display: 'flex', gap: '8px', flexWrap: 'wrap' }}>
                    {profile.friendshipStatus === 'None' && (
                      <button
                        type="button"
                        className="comm-btn comm-btn-primary"
                        disabled={actionBusy}
                        onClick={handleSendFriendRequest}
                      >
                        <UserPlus size={14} /> Gửi lời mời kết bạn
                      </button>
                    )}

                    {profile.friendshipStatus === 'PendingSent' && (
                      <button
                        type="button"
                        className="comm-btn comm-btn-outline"
                        disabled={actionBusy}
                        onClick={handleRemoveOrCancel}
                      >
                        Đã gửi lời mời · Hủy
                      </button>
                    )}

                    {profile.friendshipStatus === 'PendingReceived' && (
                      <>
                        <button
                          type="button"
                          className="comm-btn comm-btn-success"
                          disabled={actionBusy}
                          onClick={() => handleRespondRequest(true)}
                        >
                          <Check size={14} /> Chấp nhận kết bạn
                        </button>
                        <button
                          type="button"
                          className="comm-btn comm-btn-danger-ghost"
                          disabled={actionBusy}
                          onClick={() => handleRespondRequest(false)}
                        >
                          Từ chối
                        </button>
                      </>
                    )}

                    {profile.friendshipStatus === 'Accepted' && (
                      <>
                        <button
                          type="button"
                          className="comm-btn comm-btn-primary"
                          onClick={() => {
                            onClose();
                            openFloatingChatWith(profile);
                          }}
                        >
                          <MessageSquare size={14} /> Nhắn tin
                        </button>
                        <button
                          type="button"
                          className="comm-btn comm-btn-outline"
                          onClick={() => setShowInlineChat((v) => !v)}
                        >
                          {showInlineChat ? 'Ẩn khung chat' : 'Chat ngay trên hồ sơ'}
                        </button>
                        <button
                          type="button"
                          className="comm-btn comm-btn-danger-ghost"
                          disabled={actionBusy}
                          onClick={handleRemoveOrCancel}
                        >
                          Hủy kết bạn
                        </button>
                      </>
                    )}
                  </div>
                </div>
              )}

              {/* Embedded Chat Drawer when Friends */}
              {showInlineChat && profile.friendshipStatus === 'Accepted' && (
                <div className="comm-inline-chat-drawer">
                  <div
                    style={{
                      padding: '10px 16px',
                      background: '#1b6f50',
                      color: '#fff',
                      fontSize: '13.5px',
                      fontWeight: 600,
                      display: 'flex',
                      justifyContent: 'space-between',
                      alignItems: 'center'
                    }}
                  >
                    <span style={{ display: 'inline-flex', alignItems: 'center', gap: '8px' }}>
                      <MessageSquare size={15} /> Tin nhắn với {profile.fullName}
                    </span>
                    <button
                      type="button"
                      onClick={loadInlineChat}
                      style={{
                        background: 'transparent',
                        border: 'none',
                        color: '#eaf5ef',
                        cursor: 'pointer',
                        fontSize: '12px',
                        display: 'inline-flex',
                        alignItems: 'center',
                        gap: '4px'
                      }}
                    >
                      <RefreshCw size={13} /> Làm mới
                    </button>
                  </div>
                  <div style={{ maxHeight: '220px', overflowY: 'auto', padding: '14px', display: 'flex', flexDirection: 'column', gap: '8px' }}>
                    {chatLoading ? (
                      <div style={{ fontSize: '13px', color: '#4d5d53', textAlign: 'center', padding: '16px' }}>
                        Đang tải tin nhắn...
                      </div>
                    ) : messages.length === 0 ? (
                      <div style={{ fontSize: '13px', color: '#4d5d53', textAlign: 'center', padding: '16px' }}>
                        Chưa có tin nhắn nào. Hãy gửi lời chào đầu tiên để hẹn lịch ra sân.
                      </div>
                    ) : (
                      messages.map((m) => (
                        <div key={m.id} className={`comm-msg-row ${m.isMine ? 'mine' : 'theirs'}`}>
                          <div className="comm-msg-bubble">
                            <ChatMessageContent content={m.content} isMine={m.isMine} />
                          </div>
                          <span className="comm-msg-time">
                            {new Date(m.createdAt).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })}
                          </span>
                        </div>
                      ))
                    )}
                  </div>
                  <form className="comm-chat-composer" onSubmit={handleSendDirectMessage}>
                    <input
                      type="text"
                      className="comm-chat-input"
                      placeholder={`Nhắn tin cho ${profile.fullName}...`}
                      value={draftMsg}
                      onChange={(e) => setDraftMsg(e.target.value)}
                    />
                    <button type="submit" className="comm-btn comm-btn-primary" disabled={sendingMsg || !draftMsg.trim()}>
                      <Send size={14} /> {sendingMsg ? 'Đang gửi...' : 'Gửi'}
                    </button>
                  </form>
                </div>
              )}

              {/* Both Sports Skill Cards (UC-05: Cầu lông & Pickleball) */}
              <div>
                <h3 style={{ margin: '0 0 12px', fontSize: '14.5px', fontWeight: 700, color: '#14261c' }}>
                  Hồ sơ Kỹ năng Thể thao (Cầu lông & Pickleball)
                </h3>
                <div className="comm-sport-DualGrid">
                  {[
                    { dotClass: 'badminton', label: 'Cầu lông (Badminton)', data: badminton },
                    { dotClass: 'pickleball', label: 'Pickleball', data: pickleball }
                  ].map(({ dotClass, label, data }) => (
                    <div
                      key={data.sportCode}
                      style={{
                        background: '#fafcfa',
                        border: '1px solid #e1e8e3',
                        borderRadius: '12px',
                        padding: '16px',
                        display: 'flex',
                        flexDirection: 'column',
                        gap: '8px'
                      }}
                    >
                      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: '8px' }}>
                        <span style={{ fontSize: '14px', fontWeight: 700, color: '#14261c', display: 'inline-flex', alignItems: 'center', gap: '7px' }}>
                          <span className={`comm-dot ${dotClass}`} />
                          {label}
                        </span>
                        <span className={`comm-status-pill ${getStatusClass(data.ratingStatus)}`}>
                          {formatStatusBadge(data.ratingStatus)}
                        </span>
                      </div>

                      <div style={{ display: 'flex', alignItems: 'baseline', gap: '12px', marginTop: '4px' }}>
                        <div>
                          <div style={{ fontSize: '11px', color: '#708076', textTransform: 'uppercase', fontWeight: 700 }}>
                            Mức Band
                          </div>
                          <div style={{ fontSize: '21px', fontWeight: 800, color: '#14261c' }}>
                            {data.currentBand && data.currentBand !== 'Unrated' ? `Band ${data.currentBand}` : 'Chưa xếp hạng'}
                          </div>
                        </div>
                        <div style={{ marginLeft: 'auto', textAlign: 'right' }}>
                          <div style={{ fontSize: '11px', color: '#708076', textTransform: 'uppercase', fontWeight: 700 }}>
                            Điểm kỹ năng
                          </div>
                          <div style={{ fontSize: '18px', fontWeight: 800, color: '#1b6f50' }}>
                            {data.currentScore !== null && data.currentScore !== undefined ? `${data.currentScore}/100` : '--/100'}
                          </div>
                        </div>
                      </div>

                      <div style={{ fontSize: '12px', color: '#4d5d53' }}>
                        Trạng thái: <strong style={{ color: '#14261c' }}>{formatStatusLabel(data.ratingStatus)}</strong>
                      </div>
                    </div>
                  ))}
                </div>
              </div>

              {/* Public Rating History */}
              <div>
                <h3 style={{ margin: '0 0 10px', fontSize: '14.5px', fontWeight: 700, color: '#14261c' }}>
                  Lịch sử cập nhật Band & Điểm kỹ năng gần đây
                </h3>
                {(!profile.recentRatingHistory || profile.recentRatingHistory.length === 0) ? (
                  <div style={{ padding: '14px', borderRadius: '10px', background: '#fafcfa', color: '#4d5d53', fontSize: '13px', border: '1px solid #e1e8e3' }}>
                    Chưa có lịch sử chấm điểm công khai nào.
                  </div>
                ) : (
                  <div className="comm-history-list">
                    {profile.recentRatingHistory.map((h) => (
                      <div key={h.id} className="comm-history-item">
                        <div>
                          <div style={{ fontWeight: 700, color: '#14261c', display: 'inline-flex', alignItems: 'center', gap: '7px' }}>
                            <span
                              className={`comm-dot ${
                                h.sportCode?.toLowerCase() === 'badminton' ? 'badminton' : 'pickleball'
                              }`}
                            />
                            {h.sportCode?.toLowerCase() === 'badminton' ? 'Cầu lông' : 'Pickleball'} ·{' '}
                            {h.assessmentType === 'OnCourt' ? 'Trọng tài chấm trên sân' : 'Tự đánh giá trực tuyến'}
                          </div>
                          <div style={{ fontSize: '12px', color: '#4d5d53', marginTop: '2px' }}>
                            {h.courtName ? `Sân: ${h.courtName}` : 'Khảo sát trực tuyến'}
                            {h.sessionDate ? ` • Ngày: ${h.sessionDate}` : ''}
                          </div>
                        </div>
                        <div style={{ textAlign: 'right', fontWeight: 700, color: '#1b6f50' }}>
                          <div>
                            {h.oldBand ? `${h.oldBand} → ` : ''}
                            Band {h.newBand || '--'}
                          </div>
                          <div style={{ fontSize: '12px', color: '#4d5d53' }}>
                            {h.oldScore !== null && h.oldScore !== undefined ? `${h.oldScore} → ` : ''}
                            {h.newScore ?? '--'} điểm
                          </div>
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            </>
          )}
        </div>
      </div>
    </div>
  );
}
