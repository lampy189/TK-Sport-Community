import React, { useState, useEffect } from 'react';
import { Calendar, Clock, MapPin, Users, Sparkles, Filter, ChevronRight, CheckCircle2, AlertTriangle, ArrowRight, ChevronDown } from 'lucide-react';
import { api } from '../api';

export function BrowseSessionsView({ user, onSelectSession }) {
  const [sessions, setSessions] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  // Filters state
  const [selectedSport, setSelectedSport] = useState('ALL');
  const [selectedDate, setSelectedDate] = useState('');
  const [selectedSlot, setSelectedSlot] = useState('ALL');
  const [filterEligibleOnly, setFilterEligibleOnly] = useState(false);

  const displayName = user?.fullName || 'Lan Anh';

  useEffect(() => {
    loadSessions();
  }, [user, selectedSport, selectedDate, selectedSlot]);

  async function loadSessions(retryCount = 0) {
    setLoading(true);
    setError(null);
    try {
      const params = {};
      if (selectedSport !== 'ALL') {
        params.sport = selectedSport;
      }
      if (selectedDate) {
        params.date = selectedDate;
      }
      if (selectedSlot !== 'ALL') {
        params.timeSlotId = selectedSlot;
      }
      const data = await api.getSessions(params);
      setSessions(data);
    } catch (err) {
      console.error('loadSessions error:', err);
      if (retryCount < 2) {
        setTimeout(() => loadSessions(retryCount + 1), 800);
        return;
      }
      setError('Không thể tải danh sách buổi chơi. Vui lòng kiểm tra kết nối.');
    } finally {
      setLoading(false);
    }
  }

  function formatPrice(val) {
    return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(val);
  }

  function formatSessionDate(dateStr) {
    if (!dateStr) return '';
    try {
      const parts = dateStr.split('-');
      if (parts.length === 3) {
        const year = parseInt(parts[0], 10);
        const month = parseInt(parts[1], 10);
        const day = parseInt(parts[2], 10);
        if (year === 2026 && month === 10 && day === 20) return 'Hôm nay, 20/10';
        if (year === 2026 && month === 10 && day === 21) return 'Ngày mai, 21/10';
        return `${day.toString().padStart(2, '0')}/${month.toString().padStart(2, '0')}/${year}`;
      }
    } catch (_) {}
    return dateStr;
  }

  function getSessionTitle(s) {
    if (s.sportName === 'Badminton') {
      if (s.timeLabel?.includes('18:00') && s.band === 'B2') return 'Cầu lông sau giờ làm';
      if (s.timeLabel?.includes('20:00')) return 'Cầu lông phong trào tối';
      if (s.band === 'B3') return 'Cầu lông nâng cao';
      return 'Giao lưu cầu lông';
    } else {
      if (s.timeLabel?.includes('08:00')) return 'Pickleball cuối tuần';
      if (!s.band) return 'Pickleball giao lưu mở';
      return 'Giao lưu Pickleball';
    }
  }

  const displayedSessions = filterEligibleOnly 
    ? sessions.filter(s => s.isEligibleSuggestion === true)
    : sessions;

  return (
    <div style={{ maxWidth: '1240px', margin: '0 auto', padding: '28px 24px 80px' }}>
      {/* Top Title & Subtitle (Figma Exact) */}
      <div style={{ marginBottom: '20px' }}>
        <div style={{
          fontSize: '0.74rem',
          fontWeight: '700',
          textTransform: 'uppercase',
          letterSpacing: '0.06em',
          color: 'var(--color-text-muted)',
          marginBottom: '6px'
        }}>
          TK SPORT COMMUNITY / PLAYER
        </div>
        <h1 style={{
          fontSize: '2.4rem',
          fontWeight: '800',
          color: 'var(--color-primary)',
          fontFamily: 'Outfit, sans-serif',
          letterSpacing: '-0.02em',
          lineHeight: 1.2,
          marginBottom: '6px'
        }}>
          Khám phá buổi chơi
        </h1>
        <p style={{
          fontSize: '0.95rem',
          color: 'var(--color-text-muted)'
        }}>
          Không gian của <strong style={{ color: 'var(--color-primary)' }}>{displayName}</strong> • Cầu lông & Pickleball
        </p>
      </div>

      {/* Hero Banner (Figma Exact: "Một buổi chơi hay đang chờ bạn") */}
      <div style={{
        backgroundColor: '#143A2A',
        borderRadius: 'var(--radius-lg)',
        padding: '24px 32px',
        color: '#FFFFFF',
        marginBottom: '24px',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        flexWrap: 'wrap',
        gap: '20px',
        boxShadow: 'var(--shadow-sm)'
      }}>
        <div>
          <h2 style={{ fontSize: '1.45rem', fontWeight: '800', fontFamily: 'Outfit, sans-serif', marginBottom: '6px' }}>
            Một buổi chơi hay đang chờ bạn.
          </h2>
          <p style={{ fontSize: '0.88rem', color: '#D1E0D7' }}>
            Khám phá các buổi chơi phù hợp với trình độ hiện tại của bạn.
          </p>
        </div>

        <button
          onClick={() => alert("Chức năng Hoàn thiện hồ sơ sẽ mở giao diện cập nhật hồ sơ kỹ năng.")}
          style={{
            padding: '10px 22px',
            backgroundColor: 'var(--color-accent)',
            color: 'var(--color-primary)',
            borderRadius: 'var(--radius-full)',
            fontWeight: '800',
            fontSize: '0.88rem',
            display: 'flex',
            alignItems: 'center',
            gap: '6px',
            boxShadow: '0 2px 8px rgba(203, 235, 114, 0.3)',
            cursor: 'pointer'
          }}
        >
          Hoàn thiện hồ sơ <ArrowRight size={15} />
        </button>
      </div>

      {/* Filter Control Bar (Figma Exact Layout) */}
      <div style={{
        backgroundColor: '#FFFFFF',
        borderRadius: 'var(--radius-md)',
        padding: '14px 20px',
        marginBottom: '28px',
        boxShadow: '0 1px 3px rgba(0,0,0,0.04)',
        border: '1px solid #E5E7EB',
        display: 'flex',
        flexWrap: 'wrap',
        alignItems: 'center',
        justifyContent: 'space-between',
        gap: '12px'
      }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '10px', flexWrap: 'wrap', flex: 1 }}>
          {/* Phù hợp với tôi dropdown/toggle */}
          <button
            onClick={() => setFilterEligibleOnly(!filterEligibleOnly)}
            style={{
              padding: '8px 16px',
              borderRadius: 'var(--radius-md)',
              fontSize: '0.85rem',
              fontWeight: '600',
              backgroundColor: filterEligibleOnly ? '#E6F4D0' : '#FFFFFF',
              color: filterEligibleOnly ? '#143A2A' : '#4B5563',
              border: filterEligibleOnly ? '1.5px solid #CBEB72' : '1px solid #D1D5DB',
              display: 'flex',
              alignItems: 'center',
              gap: '6px',
              cursor: 'pointer'
            }}
          >
            <span>Phù hợp với tôi</span>
            <ChevronDown size={14} color="#6B7280" />
          </button>

          {/* Môn thể thao dropdown */}
          <div style={{ position: 'relative' }}>
            <select
              value={selectedSport}
              onChange={(e) => setSelectedSport(e.target.value)}
              style={{
                appearance: 'none',
                padding: '8px 32px 8px 14px',
                borderRadius: 'var(--radius-md)',
                border: '1px solid #D1D5DB',
                fontSize: '0.85rem',
                fontWeight: '600',
                color: '#374151',
                backgroundColor: '#FFFFFF',
                outline: 'none',
                cursor: 'pointer'
              }}
            >
              <option value="ALL">Tất cả môn</option>
              <option value="Badminton">Cầu lông</option>
              <option value="Pickleball">Pickleball</option>
            </select>
            <ChevronDown size={14} color="#6B7280" style={{ position: 'absolute', right: '10px', top: '11px', pointerEvents: 'none' }} />
          </div>

          {/* Date Picker */}
          <input
            type="date"
            value={selectedDate}
            onChange={(e) => setSelectedDate(e.target.value)}
            style={{
              padding: '7px 12px',
              borderRadius: 'var(--radius-md)',
              border: '1px solid #D1D5DB',
              fontSize: '0.85rem',
              color: '#374151',
              backgroundColor: '#FFFFFF',
              outline: 'none'
            }}
          />

          {/* Khung giờ dropdown */}
          <div style={{ position: 'relative' }}>
            <select
              value={selectedSlot}
              onChange={(e) => setSelectedSlot(e.target.value)}
              style={{
                appearance: 'none',
                padding: '8px 32px 8px 14px',
                borderRadius: 'var(--radius-md)',
                border: '1px solid #D1D5DB',
                fontSize: '0.85rem',
                fontWeight: '600',
                color: '#374151',
                backgroundColor: '#FFFFFF',
                outline: 'none',
                cursor: 'pointer'
              }}
            >
              <option value="ALL">Tất cả khung giờ</option>
              <option value="1">08:00 - 10:00 (Sáng)</option>
              <option value="2">10:00 - 12:00 (Trưa)</option>
              <option value="3">14:00 - 16:00 (Chiều)</option>
              <option value="4">16:00 - 18:00 (Chiều)</option>
              <option value="5">18:00 - 20:00 (Tối)</option>
              <option value="6">20:00 - 22:00 (Tối)</option>
            </select>
            <ChevronDown size={14} color="#6B7280" style={{ position: 'absolute', right: '10px', top: '11px', pointerEvents: 'none' }} />
          </div>
        </div>

        {/* Nút Tìm buổi chơi */}
        <button
          onClick={loadSessions}
          style={{
            padding: '9px 24px',
            borderRadius: 'var(--radius-md)',
            backgroundColor: 'var(--color-primary)',
            color: '#FFFFFF',
            fontSize: '0.88rem',
            fontWeight: '700',
            cursor: 'pointer'
          }}
        >
          Tìm buổi chơi
        </button>
      </div>

      {/* Main Sessions Grid (Figma 3-column layout) */}
      {loading ? (
        <div style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fill, minmax(360px, 1fr))',
          gap: '24px'
        }}>
          {[1, 2, 3].map((i) => (
            <div key={i} style={{
              height: '240px',
              backgroundColor: '#FFFFFF',
              borderRadius: 'var(--radius-lg)',
              padding: '24px',
              border: '1px solid #E5E7EB'
            }} />
          ))}
        </div>
      ) : error ? (
        <div style={{
          padding: '30px',
          backgroundColor: 'var(--color-danger-bg)',
          borderRadius: 'var(--radius-lg)',
          color: 'var(--color-danger)',
          textAlign: 'center',
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          gap: '12px'
        }}>
          <div>{error}</div>
          <button
            onClick={() => loadSessions(0)}
            style={{
              padding: '8px 22px',
              borderRadius: 'var(--radius-full)',
              backgroundColor: 'var(--color-primary)',
              color: '#FFFFFF',
              fontWeight: '700',
              fontSize: '0.85rem',
              cursor: 'pointer',
              border: 'none',
              boxShadow: '0 2px 6px rgba(0,0,0,0.1)'
            }}
          >
            Tải lại danh sách
          </button>
        </div>
      ) : displayedSessions.length === 0 ? (
        <div style={{
          backgroundColor: '#FFFFFF',
          borderRadius: 'var(--radius-lg)',
          padding: '60px 24px',
          textAlign: 'center',
          border: '1px solid #E5E7EB'
        }}>
          <h3 style={{ fontSize: '1.2rem', fontWeight: '700', color: 'var(--color-primary)', marginBottom: '8px' }}>
            Không tìm thấy buổi chơi nào
          </h3>
          <p style={{ color: 'var(--color-text-muted)', fontSize: '0.9rem', marginBottom: '16px' }}>
            Thử tắt bộ lọc hoặc chọn khung giờ khác.
          </p>
          <button
            onClick={() => { setSelectedSport('ALL'); setSelectedDate(''); setSelectedSlot('ALL'); setFilterEligibleOnly(false); }}
            style={{
              padding: '8px 20px',
              borderRadius: 'var(--radius-full)',
              backgroundColor: 'var(--color-primary)',
              color: '#FFFFFF',
              fontWeight: '600'
            }}
          >
            Xóa bộ lọc
          </button>
        </div>
      ) : (
        <div style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fill, minmax(360px, 1fr))',
          gap: '24px',
          marginBottom: '32px'
        }}>
          {displayedSessions.map((s) => {
            const isEligible = s.isEligibleSuggestion;
            const availableSlots = s.availableSlots ?? (s.capacity - s.occupiedPlaces);
            const isFull = availableSlots <= 0;

            const sessionTitle = getSessionTitle(s);
            const bandLabel = s.band === 'B1' ? 'Mới chơi / B1' : (s.band === 'B2' ? 'Trung bình' : (s.band || 'Tự do'));

            return (
              <div
                key={s.id}
                onClick={() => onSelectSession(s.id)}
                style={{
                  backgroundColor: '#FFFFFF',
                  borderRadius: 'var(--radius-lg)',
                  border: isEligible
                    ? '1.5px solid #CBEB72'
                    : '1px solid #E5E7EB',
                  padding: '24px',
                  boxShadow: '0 1px 3px rgba(0,0,0,0.03)',
                  cursor: 'pointer',
                  display: 'flex',
                  flexDirection: 'column',
                  justifyContent: 'space-between',
                  transition: 'all 0.15s ease'
                }}
                onMouseEnter={(e) => {
                  e.currentTarget.style.transform = 'translateY(-2px)';
                  e.currentTarget.style.boxShadow = '0 6px 12px rgba(0,0,0,0.06)';
                }}
                onMouseLeave={(e) => {
                  e.currentTarget.style.transform = 'translateY(0)';
                  e.currentTarget.style.boxShadow = '0 1px 3px rgba(0,0,0,0.03)';
                }}
              >
                <div>
                  {/* Title & Pill Row */}
                  <div style={{ marginBottom: '10px' }}>
                    <h3 style={{
                      fontSize: '1.25rem',
                      fontWeight: '800',
                      color: 'var(--color-primary)',
                      fontFamily: 'Outfit, sans-serif',
                      marginBottom: '8px'
                    }}>
                      {sessionTitle}
                    </h3>

                    {/* Remaining Slots Pill (Figma Exact: "còn 4 chỗ" in light green) */}
                    <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                      <span style={{
                        backgroundColor: isFull ? '#FEE2E2' : '#E6F4D0',
                        color: isFull ? '#B91C1C' : '#2D5B18',
                        fontSize: '0.78rem',
                        fontWeight: '700',
                        padding: '3px 12px',
                        borderRadius: 'var(--radius-full)'
                      }}>
                        {isFull ? 'Đã hết chỗ' : `còn ${availableSlots} chỗ`}
                      </span>

                      {isEligible === true && (
                        <span style={{
                          backgroundColor: '#EDF7D2',
                          color: '#2B5718',
                          fontSize: '0.74rem',
                          fontWeight: '700',
                          padding: '3px 10px',
                          borderRadius: 'var(--radius-full)'
                        }}>
                          ✨ Phù hợp
                        </span>
                      )}
                    </div>
                  </div>

                  {/* Info list (Figma Exact: Address, Date/Time, Band) */}
                  <div style={{
                    fontSize: '0.85rem',
                    color: '#4B5563',
                    display: 'flex',
                    flexDirection: 'column',
                    gap: '4px',
                    margin: '16px 0 20px'
                  }}>
                    <div>
                      {s.courtName} - {s.venueName}
                    </div>
                    <div>
                      {formatSessionDate(s.sessionDate)} • {s.timeLabel || `${s.startTime} - ${s.endTime}`}
                    </div>
                    <div>
                      Trình độ: <strong style={{ color: 'var(--color-primary)' }}>{bandLabel}</strong>
                    </div>
                  </div>
                </div>

                {/* Bottom: Price + Lime Button (Figma Exact) */}
                <div>
                  <div style={{
                    fontSize: '1.25rem',
                    fontWeight: '800',
                    color: 'var(--color-primary)',
                    marginBottom: '12px'
                  }}>
                    {formatPrice(s.priceVnd)} <span style={{ fontSize: '0.85rem', fontWeight: '500', color: 'var(--color-text-muted)' }}>/ người</span>
                  </div>

                  {/* Button with Lime #CBEB72 Background (Figma Screen 01 Exact) */}
                  <button
                    style={{
                      width: '100%',
                      padding: '11px',
                      borderRadius: 'var(--radius-md)',
                      backgroundColor: 'var(--color-accent)',
                      color: 'var(--color-primary)',
                      fontWeight: '800',
                      fontSize: '0.88rem',
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      gap: '6px',
                      cursor: 'pointer',
                      border: 'none'
                    }}
                  >
                    Xem buổi chơi <ArrowRight size={15} />
                  </button>
                </div>
              </div>
            );
          })}
        </div>
      )}

      {/* Bottom Recommendation Bar (Figma Screen 01 Exact) */}
      <div style={{
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        flexWrap: 'wrap',
        gap: '12px',
        padding: '14px 20px',
        backgroundColor: '#FFFFFF',
        borderRadius: 'var(--radius-md)',
        border: '1px solid #E5E7EB',
        fontSize: '0.85rem',
        color: '#4B5563'
      }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
          <span style={{ fontWeight: '600' }}>Gợi ý cho bạn:</span>
          <span style={{
            backgroundColor: '#F3F4F6',
            padding: '4px 12px',
            borderRadius: 'var(--radius-full)',
            fontWeight: '700',
            color: 'var(--color-primary)'
          }}>
            TOP 3 • Cầu lông • Mọi khung giờ
          </span>
        </div>

        <div style={{
          display: 'flex',
          alignItems: 'center',
          gap: '6px',
          fontWeight: '600',
          cursor: 'pointer'
        }}>
          <span>Xem theo ngày</span>
          <ChevronDown size={14} color="#6B7280" />
        </div>
      </div>
    </div>
  );
}
