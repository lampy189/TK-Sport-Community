import React, { useState, useEffect, useCallback } from 'react';
import {
  Search,
  Users,
  UserPlus,
  UserCheck,
  MessageSquare,
  CalendarPlus,
  ArrowUpRight,
  Check,
  X,
  Send
} from 'lucide-react';
import { api } from '../api';
import { PublicPlayerProfileModal } from './PublicPlayerProfileModal';
import { openFloatingChatWith } from './FloatingChatBubble';
import {
  ChatMessageContent,
  PickSessionToInviteModal,
  parseSessionInviteMessage
} from './SessionInviteCard';
import '../styles/community.css';

function getSportItem(sportProfiles, code) {
  const found = (sportProfiles || []).find((s) => s.sportCode?.toLowerCase() === code.toLowerCase());
  return (
    found || {
      sportCode: code,
      sportName: code === 'badminton' ? 'Cầu lông' : 'Pickleball',
      currentBand: 'Unrated',
      currentScore: null,
      ratingStatus: 'Unrated'
    }
  );
}

function getStatusPillClass(status) {
  if (!status) return 'unrated';
  const s = status.toLowerCase();
  if (s === 'confirmed' || s === 'rated') return 'confirmed';
  if (s === 'provisional') return 'provisional';
  return 'unrated';
}

function getStatusShortLabel(status) {
  if (!status) return 'Chưa chấm';
  const s = status.toLowerCase();
  if (s === 'confirmed' || s === 'rated') return 'Chính thức';
  if (s === 'provisional') return 'Tạm tính';
  return 'Chưa chấm';
}

export function CommunityView({ user }) {
  // 'discover' | 'suggestions' | 'friends' | 'requests' | 'chat'
  const [activeSubTab, setActiveSubTab] = useState('discover');

  const [searchQuery, setSearchQuery] = useState('');
  const [sportFilter, setSportFilter] = useState('ALL');
  const [bandFilter, setBandFilter] = useState('ALL');
  const [members, setMembers] = useState([]);
  const [suggestions, setSuggestions] = useState([]);
  const [friends, setFriends] = useState([]);
  const [requests, setRequests] = useState([]);
  const [loadingData, setLoadingData] = useState(true);
  const [actionBusyId, setActionBusyId] = useState(null);
  const [feedback, setFeedback] = useState(null);

  const [selectedPublicPlayerId, setSelectedPublicPlayerId] = useState(null);

  const [activeChatFriend, setActiveChatFriend] = useState(null);
  const [conversation, setConversation] = useState([]);
  const [loadingChat, setLoadingChat] = useState(false);
  const [messageText, setMessageText] = useState('');
  const [sendingMessage, setSendingMessage] = useState(false);
  const [showPickSessionModal, setShowPickSessionModal] = useState(false);

  const loadCommunityAll = useCallback(
    async (queryOverride) => {
      if (!user) {
        setLoadingData(false);
        return;
      }
      setLoadingData(true);
      try {
        const q = queryOverride !== undefined ? queryOverride : searchQuery;
        const [membersRes, suggestionsRes, friendsRes, reqRes] = await Promise.all([
          api.searchCommunityMembers(q, 50),
          api.getFriendSuggestions().catch(() => []),
          api.getFriends(),
          api.getFriendRequests()
        ]);
        setMembers(Array.isArray(membersRes) ? membersRes : []);
        setSuggestions(Array.isArray(suggestionsRes) ? suggestionsRes : []);
        const friendList = Array.isArray(friendsRes) ? friendsRes : [];
        setFriends(friendList);
        setRequests(Array.isArray(reqRes) ? reqRes : []);

        setActiveChatFriend((prev) => {
          if (prev) {
            const updated = friendList.find((f) => f.friendPlayerId === prev.friendPlayerId);
            return updated || prev;
          }
          return friendList[0] || null;
        });
      } catch (err) {
        setFeedback({ type: 'error', text: err.message || 'Không thể tải dữ liệu cộng đồng.' });
      } finally {
        setLoadingData(false);
      }
    },
    [user, searchQuery]
  );

  useEffect(() => {
    loadCommunityAll('');
  }, []);

  const loadActiveConversation = useCallback(async (friendPlayerId, silent = false) => {
    if (!friendPlayerId) return;
    if (!silent) setLoadingChat(true);
    try {
      const msgs = await api.getConversation(friendPlayerId, 80);
      setConversation(Array.isArray(msgs) ? msgs : []);
      await api.markMessagesAsRead(friendPlayerId);
      setFriends((prev) =>
        prev.map((f) => (f.friendPlayerId === friendPlayerId ? { ...f, unreadCount: 0 } : f))
      );
    } catch {
      // ignore
    } finally {
      if (!silent) setLoadingChat(false);
    }
  }, []);

  useEffect(() => {
    if (activeSubTab === 'chat' && activeChatFriend?.friendPlayerId) {
      loadActiveConversation(activeChatFriend.friendPlayerId);
      const timer = setInterval(() => {
        loadActiveConversation(activeChatFriend.friendPlayerId, true);
      }, 5000);
      return () => clearInterval(timer);
    }
  }, [activeSubTab, activeChatFriend?.friendPlayerId, loadActiveConversation]);

  const handleSendFriendRequest = async (targetPlayerId) => {
    setActionBusyId(targetPlayerId);
    setFeedback(null);
    try {
      await api.sendFriendRequest(targetPlayerId);
      setFeedback({ type: 'success', text: 'Đã gửi lời mời kết bạn.' });
      await loadCommunityAll();
    } catch (err) {
      setFeedback({ type: 'error', text: err.message || 'Không thể gửi lời mời kết bạn.' });
    } finally {
      setActionBusyId(null);
    }
  };

  const handleRespondRequest = async (friendshipId, accept) => {
    setActionBusyId(friendshipId);
    setFeedback(null);
    try {
      await api.respondFriendRequest(friendshipId, accept);
      setFeedback({
        type: 'success',
        text: accept ? 'Đã chấp nhận lời mời kết bạn.' : 'Đã từ chối lời mời kết bạn.'
      });
      await loadCommunityAll();
    } catch (err) {
      setFeedback({ type: 'error', text: err.message || 'Thao tác thất bại.' });
    } finally {
      setActionBusyId(null);
    }
  };

  const handleRemoveFriend = async (friendPlayerId) => {
    setActionBusyId(friendPlayerId);
    setFeedback(null);
    try {
      await api.removeFriend(friendPlayerId);
      setFeedback({ type: 'info', text: 'Đã cập nhật danh sách bạn bè.' });
      await loadCommunityAll();
    } catch (err) {
      setFeedback({ type: 'error', text: err.message || 'Không thể hủy kết bạn.' });
    } finally {
      setActionBusyId(null);
    }
  };

  const handleOpenChatWithPlayer = (playerObj) => {
    openFloatingChatWith(playerObj);
    const friendPlayerId = playerObj.friendPlayerId || playerObj.playerId || playerObj.otherPlayerId;
    const existing = friends.find((f) => f.friendPlayerId === friendPlayerId);
    setActiveChatFriend(
      existing || {
        friendPlayerId,
        fullName: playerObj.fullName || playerObj.otherFullName || 'Người chơi',
        avatarUrl: playerObj.avatarUrl || playerObj.otherAvatarUrl || null,
        sportProfiles: playerObj.sportProfiles || []
      }
    );
  };

  const handleSendChatMessage = async (e) => {
    e.preventDefault();
    if (!activeChatFriend?.friendPlayerId || !messageText.trim() || sendingMessage) return;
    setSendingMessage(true);
    try {
      const msg = await api.sendMessage(activeChatFriend.friendPlayerId, messageText.trim());
      setConversation((prev) => [...prev, msg]);
      setMessageText('');
      setFriends((prev) =>
        prev.map((f) =>
          f.friendPlayerId === activeChatFriend.friendPlayerId
            ? { ...f, lastMessagePreview: msg.content, lastMessageAt: msg.createdAt }
            : f
        )
      );
    } catch (err) {
      setFeedback({ type: 'error', text: err.message || 'Không thể gửi tin nhắn.' });
    } finally {
      setSendingMessage(false);
    }
  };

  if (!user) {
    return (
      <div className="comm-container">
        <div className="comm-light-hero" style={{ justifyContent: 'center', textAlign: 'center' }}>
          <div>
            <h2>Cộng đồng Người chơi TKSC</h2>
            <p>
              Vui lòng đăng nhập tài khoản Player để xem Hồ sơ công khai của người chơi khác, kết bạn và nhắn tin.
            </p>
          </div>
        </div>
      </div>
    );
  }

  const incomingRequests = requests.filter((r) => r.isIncoming);
  const outgoingRequests = requests.filter((r) => !r.isIncoming);
  const totalUnreadMessages = friends.reduce((sum, f) => sum + (f.unreadCount || 0), 0);

  const filteredMembers = members.filter((m) => {
    if (sportFilter !== 'ALL') {
      const sp = getSportItem(m.sportProfiles, sportFilter);
      if (!sp.currentBand || sp.currentBand === 'Unrated') return false;
      if (bandFilter !== 'ALL' && sp.currentBand !== bandFilter) return false;
    } else if (bandFilter !== 'ALL') {
      const hasBand = (m.sportProfiles || []).some((s) => s.currentBand === bandFilter);
      if (!hasBand) return false;
    }
    return true;
  });

  return (
    <div className="comm-container">
      {/* Editorial Header */}
      <header className="comm-light-hero">
        <div>
          <span className="comm-eyebrow">CẦU LÔNG &amp; PICKLEBALL DIRECTORY</span>
          <h1>
            Cộng đồng Người chơi<span>.</span>
          </h1>
          <p>
            Tra cứu trình độ Band Cầu lông &amp; Pickleball của các thành viên trong câu lạc bộ, kết nối bạn tập và hẹn lịch ra sân.
          </p>
        </div>

        <div className="comm-segmented-tabs" role="tablist">
          <button
            type="button"
            role="tab"
            aria-selected={activeSubTab === 'discover'}
            className={`comm-tab-btn ${activeSubTab === 'discover' ? 'active' : ''}`}
            onClick={() => setActiveSubTab('discover')}
          >
            <Search size={15} />
            <span>Thành viên ({members.length})</span>
          </button>

          <button
            type="button"
            role="tab"
            aria-selected={activeSubTab === 'suggestions'}
            className={`comm-tab-btn ${activeSubTab === 'suggestions' ? 'active' : ''}`}
            onClick={() => setActiveSubTab('suggestions')}
          >
            <UserPlus size={15} />
            <span>Gợi ý kết bạn ({suggestions.length})</span>
          </button>

          <button
            type="button"
            role="tab"
            aria-selected={activeSubTab === 'friends'}
            className={`comm-tab-btn ${activeSubTab === 'friends' ? 'active' : ''}`}
            onClick={() => setActiveSubTab('friends')}
          >
            <Users size={15} />
            <span>Bạn bè ({friends.length})</span>
          </button>

          <button
            type="button"
            role="tab"
            aria-selected={activeSubTab === 'requests'}
            className={`comm-tab-btn ${activeSubTab === 'requests' ? 'active' : ''}`}
            onClick={() => setActiveSubTab('requests')}
          >
            <UserCheck size={15} />
            <span>Lời mời kết bạn</span>
            {incomingRequests.length > 0 ? (
              <span className="comm-badge-count">{incomingRequests.length}</span>
            ) : (
              <span>(0)</span>
            )}
          </button>

          <button
            type="button"
            role="tab"
            aria-selected={activeSubTab === 'chat'}
            className={`comm-tab-btn ${activeSubTab === 'chat' ? 'active' : ''}`}
            onClick={() => setActiveSubTab('chat')}
          >
            <MessageSquare size={15} />
            <span>Tin nhắn</span>
            {totalUnreadMessages > 0 && (
              <span className="comm-badge-count">{totalUnreadMessages}</span>
            )}
          </button>
        </div>
      </header>

      {feedback && (
        <div
          style={{
            padding: '14px 18px',
            borderRadius: '14px',
            fontSize: '15px',
            fontWeight: 600,
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
            background: feedback.type === 'error' ? '#fef2f2' : '#f0f9f4',
            color: feedback.type === 'error' ? '#b91c1c' : '#14553d',
            border: `1px solid ${feedback.type === 'error' ? '#fecaca' : '#cce8d8'}`
          }}
        >
          <span>{feedback.text}</span>
          <button
            type="button"
            onClick={() => setFeedback(null)}
            style={{ background: 'transparent', border: 'none', cursor: 'pointer', color: 'inherit' }}
          >
            <X size={16} />
          </button>
        </div>
      )}

      {/* TAB 1: DISCOVER & SEARCH PLAYERS */}
      {activeSubTab === 'discover' && (
        <>
          <form
            className="comm-filter-bar"
            onSubmit={(e) => {
              e.preventDefault();
              loadCommunityAll(searchQuery);
            }}
          >
            <div className="comm-search-wrap">
              <Search size={16} />
              <input
                type="text"
                className="comm-search-input"
                placeholder="Tìm thành viên theo tên..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
              />
            </div>
            <select
              className="comm-select"
              value={sportFilter}
              onChange={(e) => setSportFilter(e.target.value)}
            >
              <option value="ALL">Tất cả bộ môn</option>
              <option value="badminton">Cầu lông</option>
              <option value="pickleball">Pickleball</option>
            </select>
            <select
              className="comm-select"
              value={bandFilter}
              onChange={(e) => setBandFilter(e.target.value)}
            >
              <option value="ALL">Mọi mức Band</option>
              <option value="B1">Band B1 · Mới chơi</option>
              <option value="B2">Band B2 · Cơ bản</option>
              <option value="B3">Band B3 · Trung bình</option>
              <option value="B4">Band B4 · Trung khá</option>
              <option value="B5">Band B5 · Nâng cao</option>
            </select>
            <button type="submit" className="comm-btn comm-btn-primary">
              Lọc kết quả
            </button>
          </form>

          {loadingData ? (
            <div style={{ padding: '44px', textAlign: 'center', color: '#55685d', fontWeight: 600 }}>
              Đang tải danh sách thành viên...
            </div>
          ) : filteredMembers.length === 0 ? (
            <div
              style={{
                padding: '44px',
                textAlign: 'center',
                background: '#fff',
                borderRadius: '16px',
                border: '1px solid #e1e8e3',
                color: '#55685d'
              }}
            >
              Không tìm thấy người chơi phù hợp với bộ lọc hiện tại.
            </div>
          ) : (
            <div className="comm-members-grid">
              {filteredMembers.map((member) => {
                const bm = getSportItem(member.sportProfiles, 'badminton');
                const pk = getSportItem(member.sportProfiles, 'pickleball');
                return (
                  <article key={member.playerId} className="comm-member-card">
                    <div className="comm-member-top">
                      <div
                        className="comm-clickable-identity"
                        onClick={() => setSelectedPublicPlayerId(member.playerId)}
                      >
                        <div className="comm-avatar-md">
                          {member.avatarUrl ? (
                            <img src={member.avatarUrl} alt={member.fullName} />
                          ) : (
                            (member.fullName || 'P').charAt(0).toUpperCase()
                          )}
                        </div>
                        <div style={{ minWidth: 0 }}>
                          <h3 className="comm-member-name">{member.fullName}</h3>
                          <div className="comm-member-sub">
                            {member.gender === 'Male' ? 'Nam' : member.gender === 'Female' ? 'Nữ' : 'Thành viên CLB'}
                          </div>
                        </div>
                      </div>

                      {member.friendshipStatus === 'Accepted' && (
                        <span className="comm-status-pill confirmed">Bạn bè</span>
                      )}
                    </div>

                    {member.bio && <p className="comm-member-bio">{member.bio}</p>}

                    {/* Dual Sport Metrics */}
                    <div
                      className="comm-sport-DualGrid"
                      onClick={() => setSelectedPublicPlayerId(member.playerId)}
                      style={{ cursor: 'pointer' }}
                    >
                      <div className="comm-sport-mini-card">
                        <div className="comm-sport-mini-title">
                          <span className="comm-sport-label-dot">
                            <span className="comm-dot" />
                            Cầu lông
                          </span>
                          <span className={`comm-status-pill ${getStatusPillClass(bm.ratingStatus)}`}>
                            {getStatusShortLabel(bm.ratingStatus)}
                          </span>
                        </div>
                        <div className="comm-sport-mini-metrics">
                          <span className="comm-sport-mini-band">
                            {bm.currentBand && bm.currentBand !== 'Unrated' ? `Band ${bm.currentBand}` : '—'}
                          </span>
                          <span className="comm-sport-mini-score">
                            {bm.currentScore != null ? `${bm.currentScore}đ` : 'Chưa chấm'}
                          </span>
                        </div>
                      </div>

                      <div className="comm-sport-mini-card is-pickleball">
                        <div className="comm-sport-mini-title">
                          <span className="comm-sport-label-dot">
                            <span className="comm-dot is-pickleball" />
                            Pickleball
                          </span>
                          <span className={`comm-status-pill ${getStatusPillClass(pk.ratingStatus)}`}>
                            {getStatusShortLabel(pk.ratingStatus)}
                          </span>
                        </div>
                        <div className="comm-sport-mini-metrics">
                          <span className="comm-sport-mini-band">
                            {pk.currentBand && pk.currentBand !== 'Unrated' ? `Band ${pk.currentBand}` : '—'}
                          </span>
                          <span className="comm-sport-mini-score">
                            {pk.currentScore != null ? `${pk.currentScore}đ` : 'Chưa chấm'}
                          </span>
                        </div>
                      </div>
                    </div>

                    <div className="comm-member-actions">
                      <button
                        type="button"
                        className="comm-btn comm-btn-ghost"
                        onClick={() => setSelectedPublicPlayerId(member.playerId)}
                      >
                        <span>Xem hồ sơ</span>
                        <ArrowUpRight size={14} />
                      </button>

                      {member.friendshipStatus === 'None' && (
                        <button
                          type="button"
                          className="comm-btn comm-btn-outline"
                          disabled={actionBusyId === member.playerId}
                          onClick={() => handleSendFriendRequest(member.playerId)}
                        >
                          <UserPlus size={14} />
                          <span>Kết bạn</span>
                        </button>
                      )}

                      {member.friendshipStatus === 'PendingSent' && (
                        <button
                          type="button"
                          className="comm-btn comm-btn-outline"
                          disabled={actionBusyId === member.playerId}
                          onClick={() => handleRemoveFriend(member.playerId)}
                        >
                          Đã gửi lời mời
                        </button>
                      )}

                      {member.friendshipStatus === 'PendingReceived' && (
                        <button
                          type="button"
                          className="comm-btn comm-btn-primary"
                          onClick={() => setActiveSubTab('requests')}
                        >
                          <UserCheck size={14} />
                          <span>Xem lời mời kết bạn</span>
                        </button>
                      )}

                      {member.friendshipStatus === 'Accepted' && (
                        <button
                          type="button"
                          className="comm-btn comm-btn-primary"
                          onClick={() => handleOpenChatWithPlayer(member)}
                        >
                          <MessageSquare size={14} />
                          <span>Nhắn tin</span>
                        </button>
                      )}
                    </div>
                  </article>
                );
              })}
            </div>
          )}
        </>
      )}

      {/* TAB 2: SUGGESTED FRIENDS (PLAYED IN SAME SLOT) */}
      {activeSubTab === 'suggestions' && (
        <div style={{ background: '#fff', borderRadius: '18px', padding: '26px 30px', border: '1px solid #e1e8e3' }}>
          <div style={{ marginBottom: '20px' }}>
            <h3 style={{ margin: '0 0 6px', fontSize: '19px', fontWeight: 700, color: '#14261c' }}>
              Gợi ý kết bạn từ các buổi chơi cùng sân ({suggestions.length})
            </h3>
            <p style={{ margin: 0, fontSize: '15px', color: '#4d5d53' }}>
              Những người chơi đã từng đăng ký hoặc đánh cùng khung giờ / buổi chơi (slot) với bạn.
            </p>
          </div>

          {loadingData ? (
            <div style={{ padding: '36px', textAlign: 'center', color: '#55685d', fontWeight: 600, fontSize: '15px' }}>
              Đang tải gợi ý kết bạn...
            </div>
          ) : suggestions.length === 0 ? (
            <div style={{ color: '#55685d', fontSize: '15px', padding: '18px 0' }}>
              Chưa có gợi ý kết bạn mới. Khi bạn đăng ký hoặc tham gia các buổi chơi có thành viên khác cùng slot, hệ thống sẽ tự động gợi ý tại đây.
            </div>
          ) : (
            <div className="comm-members-grid">
              {suggestions.map((sg) => {
                const bm = getSportItem(sg.sportProfiles, 'badminton');
                const pk = getSportItem(sg.sportProfiles, 'pickleball');
                return (
                  <article key={sg.playerId} className="comm-member-card">
                    <div className="comm-member-top">
                      <div
                        className="comm-clickable-identity"
                        onClick={() => setSelectedPublicPlayerId(sg.playerId)}
                      >
                        <div className="comm-avatar-md">
                          {sg.avatarUrl ? (
                            <img src={sg.avatarUrl} alt={sg.fullName} />
                          ) : (
                            (sg.fullName || 'P').charAt(0).toUpperCase()
                          )}
                        </div>
                        <div style={{ minWidth: 0 }}>
                          <h3 className="comm-member-name">{sg.fullName}</h3>
                          <div className="comm-member-sub">
                            {sg.gender === 'Male' ? 'Nam' : sg.gender === 'Female' ? 'Nữ' : 'Thành viên CLB'}
                          </div>
                        </div>
                      </div>

                      <span className="comm-status-pill confirmed">
                        Cùng ra sân {sg.sharedSessionsCount} buổi
                      </span>
                    </div>

                    {sg.latestSharedSessionLabel && (
                      <div
                        style={{
                          padding: '10px 14px',
                          borderRadius: '10px',
                          background: '#f4f9f6',
                          border: '1px solid #d5e8dd',
                          fontSize: '14px',
                          color: '#12513a',
                          fontWeight: 500
                        }}
                      >
                        Gần nhất: <strong>{sg.latestSharedSessionLabel}</strong>
                      </div>
                    )}

                    {/* Dual Sport Metrics */}
                    <div
                      className="comm-sport-DualGrid"
                      onClick={() => setSelectedPublicPlayerId(sg.playerId)}
                      style={{ cursor: 'pointer' }}
                    >
                      <div className="comm-sport-mini-card">
                        <div className="comm-sport-mini-title">
                          <span className="comm-sport-label-dot">
                            <span className="comm-dot" />
                            Cầu lông
                          </span>
                          <span className={`comm-status-pill ${getStatusPillClass(bm.ratingStatus)}`}>
                            {getStatusShortLabel(bm.ratingStatus)}
                          </span>
                        </div>
                        <div className="comm-sport-mini-metrics">
                          <span className="comm-sport-mini-band">
                            {bm.currentBand && bm.currentBand !== 'Unrated' ? `Band ${bm.currentBand}` : '—'}
                          </span>
                          <span className="comm-sport-mini-score">
                            {bm.currentScore != null ? `${bm.currentScore}đ` : 'Chưa chấm'}
                          </span>
                        </div>
                      </div>

                      <div className="comm-sport-mini-card is-pickleball">
                        <div className="comm-sport-mini-title">
                          <span className="comm-sport-label-dot">
                            <span className="comm-dot is-pickleball" />
                            Pickleball
                          </span>
                          <span className={`comm-status-pill ${getStatusPillClass(pk.ratingStatus)}`}>
                            {getStatusShortLabel(pk.ratingStatus)}
                          </span>
                        </div>
                        <div className="comm-sport-mini-metrics">
                          <span className="comm-sport-mini-band">
                            {pk.currentBand && pk.currentBand !== 'Unrated' ? `Band ${pk.currentBand}` : '—'}
                          </span>
                          <span className="comm-sport-mini-score">
                            {pk.currentScore != null ? `${pk.currentScore}đ` : 'Chưa chấm'}
                          </span>
                        </div>
                      </div>
                    </div>

                    <div className="comm-member-actions">
                      <button
                        type="button"
                        className="comm-btn comm-btn-ghost"
                        onClick={() => setSelectedPublicPlayerId(sg.playerId)}
                      >
                        <span>Xem hồ sơ</span>
                        <ArrowUpRight size={14} />
                      </button>

                      {sg.friendshipStatus === 'None' && (
                        <button
                          type="button"
                          className="comm-btn comm-btn-primary"
                          disabled={actionBusyId === sg.playerId}
                          onClick={() => handleSendFriendRequest(sg.playerId)}
                        >
                          <UserPlus size={14} />
                          <span>Kết bạn</span>
                        </button>
                      )}

                      {sg.friendshipStatus === 'PendingSent' && (
                        <button
                          type="button"
                          className="comm-btn comm-btn-outline"
                          disabled={actionBusyId === sg.playerId}
                          onClick={() => handleRemoveFriend(sg.playerId)}
                        >
                          Đã gửi lời mời
                        </button>
                      )}

                      {sg.friendshipStatus === 'PendingReceived' && (
                        <button
                          type="button"
                          className="comm-btn comm-btn-primary"
                          onClick={() => setActiveSubTab('requests')}
                        >
                          <UserCheck size={14} />
                          <span>Xem lời mời kết bạn</span>
                        </button>
                      )}
                    </div>
                  </article>
                );
              })}
            </div>
          )}
        </div>
      )}

      {/* TAB 3: FRIENDS LIST ONLY */}
      {activeSubTab === 'friends' && (
        <div style={{ background: '#fff', borderRadius: '18px', padding: '26px 30px', border: '1px solid #e1e8e3' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px', gap: '14px', flexWrap: 'wrap' }}>
            <h3 style={{ margin: 0, fontSize: '19px', fontWeight: 700, color: '#14261c' }}>
              Danh sách bạn bè ({friends.length})
            </h3>
            {incomingRequests.length > 0 && (
              <button
                type="button"
                className="comm-btn comm-btn-outline"
                onClick={() => setActiveSubTab('requests')}
              >
                <UserCheck size={15} />
                <span>Bạn có {incomingRequests.length} lời mời kết bạn đang chờ</span>
              </button>
            )}
          </div>

          {friends.length === 0 ? (
            <div style={{ color: '#55685d', fontSize: '15px', padding: '18px 0' }}>
              Bạn chưa có người bạn nào trong danh sách. Hãy chuyển sang tab Thành viên để tìm bạn cùng trình độ.
            </div>
          ) : (
            <div className="comm-members-grid">
              {friends.map((fr) => {
                const bm = getSportItem(fr.sportProfiles, 'badminton');
                const pk = getSportItem(fr.sportProfiles, 'pickleball');
                return (
                  <div key={fr.friendshipId} className="comm-member-card">
                    <div className="comm-member-top">
                      <div
                        className="comm-clickable-identity"
                        onClick={() => setSelectedPublicPlayerId(fr.friendPlayerId)}
                      >
                        <div className="comm-avatar-md">
                          {fr.avatarUrl ? (
                            <img src={fr.avatarUrl} alt={fr.fullName} />
                          ) : (
                            (fr.fullName || 'P').charAt(0).toUpperCase()
                          )}
                        </div>
                        <div>
                          <h4 className="comm-member-name">{fr.fullName}</h4>
                          <div className="comm-member-sub">
                            Cầu lông: <strong>{bm.currentBand || '—'}</strong> · Pickleball:{' '}
                            <strong>{pk.currentBand || '—'}</strong>
                          </div>
                        </div>
                      </div>
                      <button
                        type="button"
                        className="comm-btn comm-btn-ghost"
                        onClick={() => setSelectedPublicPlayerId(fr.friendPlayerId)}
                      >
                        Hồ sơ <ArrowUpRight size={14} />
                      </button>
                    </div>
                    {fr.bio && <p className="comm-member-bio">{fr.bio}</p>}
                    <div className="comm-member-actions">
                      <button
                        type="button"
                        className="comm-btn comm-btn-danger-ghost"
                        disabled={actionBusyId === fr.friendPlayerId}
                        onClick={() => handleRemoveFriend(fr.friendPlayerId)}
                      >
                        Hủy kết bạn
                      </button>
                      <button
                        type="button"
                        className="comm-btn comm-btn-primary"
                        onClick={() => handleOpenChatWithPlayer(fr)}
                      >
                        <MessageSquare size={14} />
                        <span>Nhắn tin {fr.unreadCount > 0 ? `(${fr.unreadCount})` : ''}</span>
                      </button>
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </div>
      )}

      {/* TAB 3: FRIEND REQUESTS (DEDICATED ACCEPT / DECLINE TAB) */}
      {activeSubTab === 'requests' && (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
          <div style={{ background: '#fff', borderRadius: '18px', padding: '26px 30px', border: '1px solid #e1e8e3' }}>
            <h3 style={{ margin: '0 0 16px', fontSize: '19px', fontWeight: 700, color: '#14261c' }}>
              Lời mời kết bạn nhận được ({incomingRequests.length})
            </h3>
            {incomingRequests.length === 0 ? (
              <div style={{ color: '#55685d', fontSize: '15px' }}>
                Hiện không có lời mời kết bạn nào đang chờ bạn xác nhận.
              </div>
            ) : (
              <div className="comm-members-grid">
                {incomingRequests.map((req) => {
                  const bm = getSportItem(req.sportProfiles, 'badminton');
                  const pk = getSportItem(req.sportProfiles, 'pickleball');
                  return (
                    <div key={req.friendshipId} className="comm-member-card">
                      <div className="comm-member-top">
                        <div
                          className="comm-clickable-identity"
                          onClick={() => setSelectedPublicPlayerId(req.otherPlayerId)}
                        >
                          <div className="comm-avatar-md">
                            {req.otherAvatarUrl ? (
                              <img src={req.otherAvatarUrl} alt={req.otherFullName} />
                            ) : (
                              (req.otherFullName || 'P').charAt(0).toUpperCase()
                            )}
                          </div>
                          <div>
                            <h4 className="comm-member-name">{req.otherFullName}</h4>
                            <div className="comm-member-sub">
                              Cầu lông: <strong>{bm.currentBand || '—'}</strong> · Pickleball:{' '}
                              <strong>{pk.currentBand || '—'}</strong>
                            </div>
                          </div>
                        </div>
                        <button
                          type="button"
                          className="comm-btn comm-btn-ghost"
                          onClick={() => setSelectedPublicPlayerId(req.otherPlayerId)}
                        >
                          Hồ sơ <ArrowUpRight size={14} />
                        </button>
                      </div>

                      <div className="comm-member-actions">
                        <button
                          type="button"
                          className="comm-btn comm-btn-danger-ghost"
                          disabled={actionBusyId === req.friendshipId}
                          onClick={() => handleRespondRequest(req.friendshipId, false)}
                        >
                          <X size={14} />
                          <span>Từ chối</span>
                        </button>
                        <button
                          type="button"
                          className="comm-btn comm-btn-success"
                          disabled={actionBusyId === req.friendshipId}
                          onClick={() => handleRespondRequest(req.friendshipId, true)}
                        >
                          <Check size={14} />
                          <span>Chấp nhận</span>
                        </button>
                      </div>
                    </div>
                  );
                })}
              </div>
            )}
          </div>

          <div style={{ background: '#fff', borderRadius: '18px', padding: '26px 30px', border: '1px solid #e1e8e3' }}>
            <h3 style={{ margin: '0 0 16px', fontSize: '19px', fontWeight: 700, color: '#14261c' }}>
              Lời mời kết bạn đã gửi ({outgoingRequests.length})
            </h3>
            {outgoingRequests.length === 0 ? (
              <div style={{ color: '#55685d', fontSize: '15px' }}>
                Bạn chưa gửi lời mời kết bạn nào đang chờ phản hồi.
              </div>
            ) : (
              <div className="comm-members-grid">
                {outgoingRequests.map((req) => (
                  <div key={req.friendshipId} className="comm-member-card">
                    <div className="comm-member-top">
                      <div
                        className="comm-clickable-identity"
                        onClick={() => setSelectedPublicPlayerId(req.otherPlayerId)}
                      >
                        <div className="comm-avatar-md">
                          {req.otherAvatarUrl ? (
                            <img src={req.otherAvatarUrl} alt={req.otherFullName} />
                          ) : (
                            (req.otherFullName || 'P').charAt(0).toUpperCase()
                          )}
                        </div>
                        <div>
                          <h4 className="comm-member-name">{req.otherFullName}</h4>
                          <div className="comm-member-sub">Đang chờ người chơi xác nhận</div>
                        </div>
                      </div>
                    </div>
                    <div className="comm-member-actions">
                      <button
                        type="button"
                        className="comm-btn comm-btn-ghost"
                        onClick={() => setSelectedPublicPlayerId(req.otherPlayerId)}
                      >
                        Xem hồ sơ <ArrowUpRight size={14} />
                      </button>
                      <button
                        type="button"
                        className="comm-btn comm-btn-danger-ghost"
                        disabled={actionBusyId === req.otherPlayerId}
                        onClick={() => handleRemoveFriend(req.otherPlayerId)}
                      >
                        Hủy lời mời
                      </button>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      )}

      {/* TAB 4: CHAT MESSAGING */}
      {activeSubTab === 'chat' && (
        <div className="comm-chat-layout">
          <div className="comm-chat-sidebar">
            <div className="comm-chat-sidebar-head">Hội thoại ({friends.length})</div>
            <div className="comm-chat-friend-list">
              {friends.length === 0 ? (
                <div style={{ padding: '20px 16px', fontSize: '13px', color: '#55685d' }}>
                  Kết bạn với người chơi khác để bắt đầu nhắn tin.
                </div>
              ) : (
                friends.map((fr) => (
                  <div
                    key={fr.friendPlayerId}
                    className={`comm-chat-friend-item ${
                      activeChatFriend?.friendPlayerId === fr.friendPlayerId ? 'active' : ''
                    }`}
                    onClick={() => setActiveChatFriend(fr)}
                  >
                    <div className="comm-avatar-sm">
                      {fr.avatarUrl ? (
                        <img src={fr.avatarUrl} alt={fr.fullName} />
                      ) : (
                        (fr.fullName || 'P').charAt(0).toUpperCase()
                      )}
                    </div>
                    <div className="comm-chat-friend-info">
                      <div className="comm-chat-friend-name">
                        <span>{fr.fullName}</span>
                        {fr.unreadCount > 0 && <span className="comm-badge-count">{fr.unreadCount}</span>}
                      </div>
                      <div className="comm-chat-friend-preview">
                        {fr.lastMessagePreview
                          ? parseSessionInviteMessage(fr.lastMessagePreview).text || 'Đã gửi lời mời đặt sân'
                          : 'Bắt đầu cuộc trò chuyện'}
                      </div>
                    </div>
                  </div>
                ))
              )}
            </div>
          </div>

          <div className="comm-chat-main">
            {!activeChatFriend ? (
              <div
                style={{
                  flex: 1,
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  color: '#55685d',
                  padding: '24px',
                  textAlign: 'center'
                }}
              >
                Chọn một người bạn ở danh sách bên trái để nhắn tin hẹn lịch ra sân.
              </div>
            ) : (
              <>
                <div className="comm-chat-header">
                  <div
                    className="comm-clickable-identity"
                    onClick={() => setSelectedPublicPlayerId(activeChatFriend.friendPlayerId)}
                  >
                    <div className="comm-avatar-sm">
                      {activeChatFriend.avatarUrl ? (
                        <img src={activeChatFriend.avatarUrl} alt={activeChatFriend.fullName} />
                      ) : (
                        (activeChatFriend.fullName || 'P').charAt(0).toUpperCase()
                      )}
                    </div>
                    <div>
                      <div style={{ fontWeight: 700, color: '#14261c', fontSize: '16.5px' }}>
                        {activeChatFriend.fullName}
                      </div>
                      <div style={{ fontSize: '13.5px', color: '#55685d' }}>
                        Bấm để xem Hồ sơ công khai
                      </div>
                    </div>
                  </div>
                  <div style={{ display: 'flex', gap: '8px', alignItems: 'center' }}>
                    <button
                      type="button"
                      className="comm-btn comm-btn-outline"
                      onClick={() => setShowPickSessionModal(true)}
                    >
                      <CalendarPlus size={14} />
                      <span>Rủ đặt sân</span>
                    </button>
                    <button
                      type="button"
                      className="comm-btn comm-btn-outline"
                      onClick={() => setSelectedPublicPlayerId(activeChatFriend.friendPlayerId)}
                    >
                      <span>Hồ sơ</span>
                      <ArrowUpRight size={14} />
                    </button>
                  </div>
                </div>

                <div className="comm-chat-messages">
                  {loadingChat ? (
                    <div style={{ textAlign: 'center', color: '#55685d', padding: '24px', fontSize: '13px' }}>
                      Đang tải cuộc trò chuyện...
                    </div>
                  ) : conversation.length === 0 ? (
                    <div style={{ textAlign: 'center', color: '#55685d', padding: '32px', fontSize: '13px' }}>
                      Chưa có tin nhắn nào với <strong>{activeChatFriend.fullName}</strong>.
                    </div>
                  ) : (
                    conversation.map((m) => (
                      <div key={m.id} className={`comm-msg-row ${m.isMine ? 'mine' : 'theirs'}`}>
                        <div className="comm-msg-bubble">
                          <ChatMessageContent content={m.content} isMine={m.isMine} />
                        </div>
                        <span className="comm-msg-time">
                          {new Date(m.createdAt).toLocaleString('vi-VN', {
                            hour: '2-digit',
                            minute: '2-digit',
                            day: '2-digit',
                            month: '2-digit'
                          })}
                        </span>
                      </div>
                    ))
                  )}
                </div>

                <form className="comm-chat-composer" onSubmit={handleSendChatMessage}>
                  <button
                    type="button"
                    className="comm-btn comm-btn-outline"
                    title="Chọn buổi chơi để rủ đặt sân"
                    onClick={() => setShowPickSessionModal(true)}
                  >
                    <CalendarPlus size={14} />
                  </button>
                  <input
                    type="text"
                    className="comm-chat-input"
                    placeholder={`Nhắn tin cho ${activeChatFriend.fullName}...`}
                    value={messageText}
                    onChange={(e) => setMessageText(e.target.value)}
                  />
                  <button
                    type="submit"
                    className="comm-btn comm-btn-primary"
                    disabled={sendingMessage || !messageText.trim()}
                  >
                    <Send size={14} />
                    <span>Gửi</span>
                  </button>
                </form>
              </>
            )}
          </div>
        </div>
      )}

      {showPickSessionModal && activeChatFriend && (
        <PickSessionToInviteModal
          friendName={activeChatFriend.fullName}
          onClose={() => setShowPickSessionModal(false)}
          onSelectSessionToSend={async (msgContent) => {
            const msg = await api.sendMessage(activeChatFriend.friendPlayerId, msgContent);
            setConversation((prev) => [...prev, msg]);
            setFriends((prev) =>
              prev.map((f) =>
                f.friendPlayerId === activeChatFriend.friendPlayerId
                  ? { ...f, lastMessagePreview: msg.content, lastMessageAt: msg.createdAt }
                  : f
              )
            );
          }}
        />
      )}

      {selectedPublicPlayerId && (
        <PublicPlayerProfileModal
          playerId={selectedPublicPlayerId}
          onClose={() => setSelectedPublicPlayerId(null)}
          onOpenFullChat={(profileData) => {
            setSelectedPublicPlayerId(null);
            handleOpenChatWithPlayer(profileData);
          }}
          onFriendshipChanged={() => loadCommunityAll()}
        />
      )}
    </div>
  );
}
