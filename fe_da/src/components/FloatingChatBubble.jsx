import { parseSessionInviteMessage } from './sessionInviteMessage';
import { useState, useEffect, useCallback, useRef } from 'react';
import { MessageCircle, Send, ChevronLeft, Minus, Eye, CalendarPlus } from 'lucide-react';
import { api } from '../api';
import { PublicPlayerProfileModal } from './PublicPlayerProfileModal';
import {
  ChatMessageContent,
  PickSessionToInviteModal
} from './SessionInviteCard';
import '../styles/community.css';



export default function FloatingChatBubble({ onOpenCommunity }) {
  const [isOpen, setIsOpen] = useState(false);
  const [friends, setFriends] = useState([]);
  const [activeFriend, setActiveFriend] = useState(null);
  const [messages, setMessages] = useState([]);
  const [loadingMsgs, setLoadingMsgs] = useState(false);
  const [draft, setDraft] = useState('');
  const [sending, setSending] = useState(false);
  const [publicModalPlayerId, setPublicModalPlayerId] = useState(null);
  const [showPickSessionModal, setShowPickSessionModal] = useState(false);
  const messagesEndRef = useRef(null);
  const conversationRequest = useRef(0);
  const [error, setError] = useState('');
  useEffect(() => {
    if (isOpen) window.dispatchEvent(new Event('tksc:close-assistant'));
  }, [isOpen]);

  const loadFriends = useCallback(async () => {
    try {
      const list = await api.getFriends();
      const arr = Array.isArray(list) ? list : [];
      setFriends(arr);
      setActiveFriend((prev) => {
        if (!prev) return null;
        return arr.find((f) => f.friendPlayerId === prev.friendPlayerId) || null;
      });
    } catch (e) {
      setError(e.message || 'Chưa tải được danh sách bạn bè.');
    }
  }, []);

  useEffect(() => {
    const initial = setTimeout(() => void loadFriends(), 0);
    const timer = setInterval(loadFriends, 8000);
    return () => { clearTimeout(initial); clearInterval(timer); };
  }, [loadFriends]);

  // Listen for global "open chat with player" trigger
  useEffect(() => {
    const handler = (e) => {
      const p = e.detail;
      if (!p) {
        setIsOpen(true);
        return;
      }
      const friendPlayerId = p.friendPlayerId || p.playerId || p.otherPlayerId;
      if (friendPlayerId) {
        setActiveFriend({
          friendPlayerId,
          fullName: p.fullName || p.otherFullName || 'Người chơi',
          avatarUrl: p.avatarUrl || p.otherAvatarUrl || null
        });
      }
      setIsOpen(true);
      loadFriends();
    };
    const closeHandler = () => setIsOpen(false);
    window.addEventListener('tksc:open-chat', handler);
    window.addEventListener('tksc:close-chat', closeHandler);
    return () => {
      window.removeEventListener('tksc:open-chat', handler);
      window.removeEventListener('tksc:close-chat', closeHandler);
    };
  }, [loadFriends]);

  const loadConversation = useCallback(async (friendPlayerId, silent = false) => {
    if (!friendPlayerId) return;
    const generation = ++conversationRequest.current;
    if (!silent) setLoadingMsgs(true);
    try {
      const list = await api.getConversation(friendPlayerId, 60);
      if (generation !== conversationRequest.current) return;
      setMessages(Array.isArray(list) ? list : []);
      setError('');
      await api.markMessagesAsRead(friendPlayerId);
      setFriends((prev) =>
        prev.map((f) => (f.friendPlayerId === friendPlayerId ? { ...f, unreadCount: 0 } : f))
      );
    } catch (e) {
      if (generation === conversationRequest.current) setError(e.message || 'Chưa tải được tin nhắn.');
    } finally {
      if (generation === conversationRequest.current) setLoadingMsgs(false);
    }
  }, []);

  useEffect(() => {
    if (isOpen && activeFriend?.friendPlayerId) {
      const initial = setTimeout(() => void loadConversation(activeFriend.friendPlayerId), 0);
      const timer = setInterval(() => {
        loadConversation(activeFriend.friendPlayerId, true);
      }, 4500);
      return () => { clearTimeout(initial); clearInterval(timer); ++conversationRequest.current; };
    }
  }, [isOpen, activeFriend?.friendPlayerId, loadConversation]);

  useEffect(() => {
    if (isOpen && activeFriend) {
      messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
    }
  }, [messages.length, isOpen, activeFriend]);

  const handleSend = async (e) => {
    e.preventDefault();
    if (!activeFriend?.friendPlayerId || !draft.trim() || sending) return;
    setSending(true);
    setError('');
    try {
      const created = await api.sendMessage(activeFriend.friendPlayerId, draft.trim());
      setMessages((prev) => [...prev, created]);
      setDraft('');
      setFriends((prev) =>
        prev.map((f) =>
          f.friendPlayerId === activeFriend.friendPlayerId
            ? { ...f, lastMessagePreview: created.content, lastMessageAt: created.createdAt }
            : f
        )
      );
    } catch (e) {
      setError(e.message || 'Chưa gửi được tin nhắn. Nội dung vẫn được giữ để bạn thử lại.');
    } finally {
      setSending(false);
    }
  };

  const totalUnread = friends.reduce((sum, f) => sum + (f.unreadCount || 0), 0);
  const chatHeads = friends.slice(0, 3);

  return (
    <>
      <div className="mess-floating-root" aria-label="Bong bóng tin nhắn nhanh">
        {/* Popup Chat Box when expanded */}
        {isOpen && (
          <div className="mess-popup-window">
            {error && <div role="alert" style={{ padding: 12 }}>{error}<button type="button" onClick={() => activeFriend ? loadConversation(activeFriend.friendPlayerId) : loadFriends()}>Thử lại</button></div>}
            {!activeFriend ? (
              <>
                <div className="mess-popup-header">
                  <div className="mess-popup-header-title">
                    <MessageCircle size={18} />
                    <span>Tin nhắn ({friends.length})</span>
                  </div>
                  <div style={{ display: 'flex', gap: '6px' }}>
                    {onOpenCommunity && (
                      <button
                        type="button"
                        className="mess-popup-icon-btn"
                        title="Mở trang Cộng đồng"
                        onClick={() => {
                          setIsOpen(false);
                          onOpenCommunity();
                        }}
                      >
                        ↗
                      </button>
                    )}
                    <button
                      type="button"
                      className="mess-popup-icon-btn"
                      title="Thu gọn"
                      onClick={() => setIsOpen(false)}
                    >
                      <Minus size={15} />
                    </button>
                  </div>
                </div>

                <div className="comm-chat-friend-list" style={{ background: '#ffffff' }}>
                  {friends.length === 0 ? (
                    <div style={{ padding: '32px 18px', textAlign: 'center', color: '#475569', fontSize: '13.5px' }}>
                      <p style={{ margin: '0 0 12px' }}>
                        Bạn chưa có người bạn nào trong danh sách Tin nhắn.
                      </p>
                      {onOpenCommunity && (
                        <button
                          type="button"
                          className="comm-btn comm-btn-primary"
                          onClick={() => {
                            setIsOpen(false);
                            onOpenCommunity();
                          }}
                        >
                          Kết bạn trong Cộng đồng
                        </button>
                      )}
                    </div>
                  ) : (
                    friends.map((fr) => (
                      <div
                        key={fr.friendPlayerId}
                        className="comm-chat-friend-item"
                        onClick={() => setActiveFriend(fr)}
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
                            {fr.unreadCount > 0 && (
                              <span className="comm-badge-count">{fr.unreadCount}</span>
                            )}
                          </div>
                          <div className="comm-chat-friend-preview">
                            {fr.lastMessagePreview
                              ? parseSessionInviteMessage(fr.lastMessagePreview).text || 'Đã gửi lời mời đặt sân'
                              : 'Bấm để nhắn tin'}
                          </div>
                        </div>
                      </div>
                    ))
                  )}
                </div>
              </>
            ) : (
              <>
                <div className="mess-popup-header">
                  <div className="mess-popup-header-title">
                    <button
                      type="button"
                      className="mess-popup-icon-btn"
                      title="Danh sách bạn bè"
                      onClick={() => setActiveFriend(null)}
                    >
                      <ChevronLeft size={16} />
                    </button>
                    <div
                      style={{
                        display: 'flex',
                        alignItems: 'center',
                        gap: '8px',
                        cursor: 'pointer',
                        minWidth: 0
                      }}
                      onClick={() => setPublicModalPlayerId(activeFriend.friendPlayerId)}
                      title="Xem Hồ sơ công khai"
                    >
                      <div className="comm-avatar-sm" style={{ width: '30px', height: '30px', fontSize: '12px' }}>
                        {activeFriend.avatarUrl ? (
                          <img src={activeFriend.avatarUrl} alt={activeFriend.fullName} />
                        ) : (
                          (activeFriend.fullName || 'P').charAt(0).toUpperCase()
                        )}
                      </div>
                      <span style={{ whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
                        {activeFriend.fullName}
                      </span>
                    </div>
                  </div>
                  <div style={{ display: 'flex', gap: '6px' }}>
                    <button
                      type="button"
                      className="mess-popup-icon-btn"
                      title="Rủ đặt sân"
                      onClick={() => setShowPickSessionModal(true)}
                    >
                      <CalendarPlus size={14} />
                    </button>
                    <button
                      type="button"
                      className="mess-popup-icon-btn"
                      title="Xem Hồ sơ công khai"
                      onClick={() => setPublicModalPlayerId(activeFriend.friendPlayerId)}
                    >
                      <Eye size={14} />
                    </button>
                    <button
                      type="button"
                      className="mess-popup-icon-btn"
                      title="Thu gọn thành bong bóng"
                      onClick={() => setIsOpen(false)}
                    >
                      <Minus size={15} />
                    </button>
                  </div>
                </div>

                <div className="comm-chat-messages" style={{ padding: '14px' }}>
                  {loadingMsgs ? (
                    <div style={{ textAlign: 'center', color: '#64748b', fontSize: '13px', padding: '20px' }}>
                      Đang tải tin nhắn...
                    </div>
                  ) : messages.length === 0 ? (
                    <div style={{ textAlign: 'center', color: '#64748b', fontSize: '13px', padding: '24px 12px' }}>
                      Gửi lời chào tới <strong>{activeFriend.fullName}</strong> để hẹn lịch ra sân!
                    </div>
                  ) : (
                    messages.map((m) => (
                      <div key={m.id} className={`comm-msg-row ${m.isMine ? 'mine' : 'theirs'}`}>
                        <div className="comm-msg-bubble" style={{ fontSize: '13.5px', padding: '8px 12px' }}>
                          <ChatMessageContent content={m.content} isMine={m.isMine} />
                        </div>
                        <span className="comm-msg-time">
                          {new Date(m.createdAt).toLocaleTimeString('vi-VN', {
                            hour: '2-digit',
                            minute: '2-digit'
                          })}
                        </span>
                      </div>
                    ))
                  )}
                  <div ref={messagesEndRef} />
                </div>

                <form className="comm-chat-composer" onSubmit={handleSend}>
                  <button
                    type="button"
                    className="comm-btn comm-btn-outline"
                    style={{ padding: '7px 9px' }}
                    title="Chọn buổi chơi để rủ đặt sân"
                    onClick={() => setShowPickSessionModal(true)}
                  >
                    <CalendarPlus size={14} />
                  </button>
                  <input
                    type="text"
                    className="comm-chat-input"
                    placeholder="Nhập tin nhắn..."
                    value={draft}
                    onChange={(e) => setDraft(e.target.value)}
                  />
                  <button
                    type="submit"
                    className="comm-btn comm-btn-primary"
                    style={{ borderRadius: '999px', padding: '8px 12px' }}
                    disabled={sending || !draft.trim()}
                  >
                    <Send size={15} />
                  </button>
                </form>
              </>
            )}
          </div>
        )}

        {/* Right Dock Column: Friend Chat Head Bubbles + Main Round Chat Bubble */}
        <div className="mess-dock-col">
          {!isOpen &&
            chatHeads.map((fr) => (
              <button
                key={fr.friendPlayerId}
                type="button"
                className="mess-head-avatar"
                title={`Nhắn tin với ${fr.fullName}`}
                onClick={() => {
                  setActiveFriend(fr);
                  setIsOpen(true);
                }}
              >
                {fr.avatarUrl ? (
                  <img src={fr.avatarUrl} alt={fr.fullName} />
                ) : (
                  (fr.fullName || 'P').charAt(0).toUpperCase()
                )}
                {fr.unreadCount > 0 && (
                  <span className="mess-unread-badge">{fr.unreadCount}</span>
                )}
              </button>
            ))}

          <button
            type="button"
            className="mess-main-fab"
            title="Tin nhắn"
            onClick={() => setIsOpen((v) => !v)}
          >
            <MessageCircle size={26} />
            {totalUnread > 0 && <span className="mess-unread-badge">{totalUnread}</span>}
          </button>
        </div>
      </div>

      {publicModalPlayerId && (
        <PublicPlayerProfileModal
          playerId={publicModalPlayerId}
          onClose={() => setPublicModalPlayerId(null)}
          onFriendshipChanged={loadFriends}
        />
      )}

      {showPickSessionModal && activeFriend && (
        <PickSessionToInviteModal
          friendName={activeFriend.fullName}
          friendPlayerId={activeFriend.friendPlayerId}
          onClose={() => setShowPickSessionModal(false)}
          onSelectSessionToSend={async (msgContent) => {
            const created = await api.sendMessage(activeFriend.friendPlayerId, msgContent);
            setMessages((prev) => [...prev, created]);
            setFriends((prev) =>
              prev.map((f) =>
                f.friendPlayerId === activeFriend.friendPlayerId
                  ? { ...f, lastMessagePreview: created.content, lastMessageAt: created.createdAt }
                  : f
              )
            );
          }}
        />
      )}
    </>
  );
}

