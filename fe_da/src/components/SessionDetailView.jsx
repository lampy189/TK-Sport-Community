import React, { useState, useEffect } from 'react';
import { 
  ArrowLeft, Calendar, Clock, MapPin, Users, Shield, 
  AlertTriangle, CheckCircle2, Sparkles, XCircle, Info, Lock 
} from 'lucide-react';
import { api } from '../api';

function CountdownTimer({ expiresAt }) {
  const [secondsLeft, setSecondsLeft] = useState(() => calcSeconds(expiresAt));

  function calcSeconds(target) {
    if (!target) return 0;
    const diff = Math.floor((new Date(target).getTime() - Date.now()) / 1000);
    return diff > 0 ? diff : 0;
  }

  useEffect(() => {
    const timer = setInterval(() => {
      const remaining = calcSeconds(expiresAt);
      setSecondsLeft(remaining);
      if (remaining <= 0) clearInterval(timer);
    }, 1000);
    return () => clearInterval(timer);
  }, [expiresAt]);

  const mins = Math.floor(secondsLeft / 60);
  const secs = secondsLeft % 60;
  const timeFormatted = `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;

  return (
    <div style={{
      display: 'inline-flex',
      alignItems: 'center',
      gap: '5px',
      backgroundColor: '#CBEB72',
      color: '#143A2A',
      padding: '3px 10px',
      borderRadius: 'var(--radius-full)',
      fontWeight: '800',
      fontSize: '0.86rem',
      fontFamily: 'monospace'
    }}>
      <span>⏱️ {timeFormatted}</span>
    </div>
  );
}

export function SessionDetailView({ sessionId, user, onBack, onOpenLogin }) {
  const [details, setDetails] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  // Registration state & feedback
  const [registering, setRegistering] = useState(false);
  const [registrationFeedback, setRegistrationFeedback] = useState(null);

  const displayName = user?.fullName || 'Lan Anh';

  useEffect(() => {
    loadSessionDetails();
  }, [sessionId, user]);

  async function loadSessionDetails() {
    setLoading(true);
    setError(null);
    try {
      const data = await api.getSessionById(sessionId);
      setDetails(data);
    } catch (err) {
      setError(err.message || 'Không tìm thấy thông tin buổi chơi.');
    } finally {
      setLoading(false);
    }
  }

  async function handleRegister() {
    if (!user) {
      setRegistrationFeedback({
        type: 'NEED_LOGIN',
        message: 'Bạn đang xem với tư cách Khách. Vui lòng đăng nhập tài khoản Player để hệ thống kiểm tra điều kiện trình độ và tạo đơn giữ chỗ tạm thời.'
      });
      return;
    }

    setRegistering(true);
    setRegistrationFeedback(null);

    try {
      const result = await api.registerSession(sessionId);
      // Branch C: Record Pending Registration Success
      setRegistrationFeedback({
        type: 'SUCCESS_PENDING',
        data: result
      });
      loadSessionDetails();
    } catch (err) {
      // Branch A: Skill Ineligible or Branch B: Session Full or Other
      if (err.code === 'SKILL_INELIGIBLE') {
        setRegistrationFeedback({
          type: 'SKILL_INELIGIBLE',
          reason: err.reason,
          message: err.message
        });
      } else if (err.code === 'SESSION_FULL') {
        setRegistrationFeedback({
          type: 'SESSION_FULL',
          message: err.message || 'Buổi chơi đã đủ người.'
        });
        if (details) {
          setDetails({
            ...details,
            summary: {
              ...details.summary,
              availableSlots: 0,
              isFull: true
            }
          });
        }
      } else if (err.code === 'ALREADY_REGISTERED') {
        setRegistrationFeedback({
          type: 'ALREADY_REGISTERED',
          message: err.message || 'Bạn đã có vé hoặc đang giữ chỗ cho buổi chơi này.'
        });
      } else {
        setRegistrationFeedback({
          type: 'ERROR',
          message: err.message || 'Không thể đăng ký buổi chơi lúc này.'
        });
      }
    } finally {
      setRegistering(false);
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
    if (!s) return 'Buổi chơi';
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

  function formatDateTime(isoString) {
    if (!isoString) return '';
    const date = new Date(isoString);
    return date.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' }) + ' ' + 
           date.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' });
  }

  if (loading) {
    return (
      <div style={{ maxWidth: '1240px', margin: '0 auto', padding: '60px 24px', textAlign: 'center' }}>
        <p style={{ color: 'var(--color-text-muted)' }}>Đang tải chi tiết buổi chơi...</p>
      </div>
    );
  }

  if (error || !details) {
    return (
      <div style={{ maxWidth: '1240px', margin: '0 auto', padding: '40px 24px' }}>
        <button
          onClick={onBack}
          style={{
            display: 'inline-flex',
            alignItems: 'center',
            gap: '8px',
            color: 'var(--color-primary)',
            fontWeight: '700',
            marginBottom: '20px'
          }}
        >
          <ArrowLeft size={18} /> Quay lại danh sách
        </button>
        <div style={{
          padding: '30px',
          backgroundColor: 'var(--color-danger-bg)',
          borderRadius: 'var(--radius-lg)',
          color: 'var(--color-danger)'
        }}>
          {error || 'Buổi chơi không tồn tại.'}
        </div>
      </div>
    );
  }

  const { summary, publicParticipants = [] } = details;
  const availableSlots = summary.availableSlots ?? (summary.capacity - summary.occupiedPlaces);
  const isFull = availableSlots <= 0;
  const isEligible = summary.isEligibleSuggestion;

  return (
    <div style={{ maxWidth: '1240px', margin: '0 auto', padding: '24px 24px 80px' }}>
      {/* Breadcrumb / Back button */}
      <div style={{ marginBottom: '16px' }}>
        <button
          onClick={onBack}
          style={{
            display: 'inline-flex',
            alignItems: 'center',
            gap: '6px',
            color: 'var(--color-text-secondary)',
            fontWeight: '600',
            fontSize: '0.85rem'
          }}
        >
          <ArrowLeft size={16} /> Quay lại khám phá
        </button>
      </div>

      {/* Top Banner Category & Title (Figma Style) */}
      <div style={{ marginBottom: '28px' }}>
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
          fontFamily: "'Be Vietnam Pro', 'Plus Jakarta Sans', sans-serif",
          letterSpacing: '-0.02em',
          lineHeight: 1.2,
          marginBottom: '6px'
        }}>
          {getSessionTitle(summary)} — {summary.courtName}
        </h1>
        <p style={{
          fontSize: '0.95rem',
          color: 'var(--color-text-muted)'
        }}>
          Không gian của <strong style={{ color: 'var(--color-primary)' }}>{displayName}</strong> • Cầu lông & Pickleball
        </p>
      </div>

      {/* Main 2-Column Layout */}
      <div style={{
        display: 'grid',
        gridTemplateColumns: 'minmax(0, 1.8fr) minmax(320px, 1.15fr)',
        gap: '28px',
        alignItems: 'start'
      }}>
        {/* Left Column: Thông tin buổi chơi */}
        <div style={{
          backgroundColor: '#FFFFFF',
          borderRadius: 'var(--radius-lg)',
          padding: '32px',
          boxShadow: 'var(--shadow-sm)',
          border: '1px solid var(--color-border)'
        }}>
          <h2 style={{
            fontSize: '1.25rem',
            fontWeight: '800',
            color: 'var(--color-primary)',
            marginBottom: '16px'
          }}>
            Thông tin buổi chơi
          </h2>

          {/* Eligibility Badge Pill (Figma style) */}
          <div style={{ marginBottom: '20px' }}>
            {isEligible === true ? (
              <div style={{
                display: 'inline-flex',
                alignItems: 'center',
                gap: '6px',
                backgroundColor: '#E6F4D0',
                color: '#244D14',
                padding: '6px 16px',
                borderRadius: 'var(--radius-full)',
                fontSize: '0.82rem',
                fontWeight: '700'
              }}>
                <Sparkles size={14} /> Bạn phù hợp với buổi chơi này
              </div>
            ) : isEligible === false ? (
              <div style={{
                display: 'inline-flex',
                alignItems: 'center',
                gap: '6px',
                backgroundColor: '#FEF3C7',
                color: '#92400E',
                padding: '6px 16px',
                borderRadius: 'var(--radius-full)',
                fontSize: '0.82rem',
                fontWeight: '700'
              }}>
                <AlertTriangle size={14} /> Chưa phù hợp trình độ: {summary.ineligibilityReason || 'Lệch band kỹ năng'}
              </div>
            ) : (
              <div style={{
                display: 'inline-flex',
                alignItems: 'center',
                gap: '6px',
                backgroundColor: '#F3F4F6',
                color: '#4B5563',
                padding: '6px 16px',
                borderRadius: 'var(--radius-full)',
                fontSize: '0.82rem',
                fontWeight: '600'
              }}>
                Đăng nhập để xem đối chiếu trình độ
              </div>
            )}
          </div>

          {/* Clean Key-Value Rows (Figma layout) */}
          <div style={{ display: 'flex', flexDirection: 'column', gap: '14px', marginBottom: '28px' }}>
            <div style={{ display: 'grid', gridTemplateColumns: '180px 1fr', fontSize: '0.9rem' }}>
              <span style={{ color: 'var(--color-text-muted)' }}>Môn thể thao</span>
              <strong style={{ color: 'var(--color-primary)' }}>
                {summary.sportName === 'Badminton' ? 'Cầu lông' : 'Pickleball'}
              </strong>
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '180px 1fr', fontSize: '0.9rem' }}>
              <span style={{ color: 'var(--color-text-muted)' }}>Địa điểm</span>
              <strong style={{ color: 'var(--color-primary)' }}>
                {summary.courtName} - {summary.venueName}
              </strong>
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '180px 1fr', fontSize: '0.9rem' }}>
              <span style={{ color: 'var(--color-text-muted)' }}>Thời gian</span>
              <strong style={{ color: 'var(--color-primary)' }}>
                {formatSessionDate(summary.sessionDate)} • {summary.timeLabel || `${summary.startTime} - ${summary.endTime}`}
              </strong>
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '180px 1fr', fontSize: '0.9rem' }}>
              <span style={{ color: 'var(--color-text-muted)' }}>Trình độ yêu cầu</span>
              <strong style={{ color: 'var(--color-primary)' }}>
                {summary.band ? `Band ${summary.band}` : 'Tự do / Mọi trình độ'}
              </strong>
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '180px 1fr', fontSize: '0.9rem' }}>
              <span style={{ color: 'var(--color-text-muted)' }}>Phụ trách buổi chơi</span>
              <strong style={{ color: 'var(--color-primary)' }}>
                Trọng tài Nguyễn Trọng Tài (TKSC)
              </strong>
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '180px 1fr', fontSize: '0.9rem' }}>
              <span style={{ color: 'var(--color-text-muted)' }}>Số người đã xác nhận</span>
              <strong style={{ color: 'var(--color-primary)' }}>
                {summary.occupiedPlaces} / {summary.capacity} người
              </strong>
            </div>
          </div>

          {/* Section: Người chơi đã tham gia */}
          <div style={{ paddingTop: '20px', borderTop: '1px solid var(--color-border)' }}>
            <h3 style={{ fontSize: '1rem', fontWeight: '800', color: 'var(--color-primary)', marginBottom: '12px' }}>
              Người chơi đã tham gia
            </h3>

            {publicParticipants.length === 0 ? (
              <p style={{ fontSize: '0.85rem', color: 'var(--color-text-muted)', marginBottom: '14px' }}>
                Chưa có người chơi nào tham gia buổi này.
              </p>
            ) : (
              <div style={{ display: 'flex', flexWrap: 'wrap', gap: '8px', marginBottom: '14px' }}>
                {publicParticipants.map((p, idx) => (
                  <div
                    key={p.ticketId || idx}
                    style={{
                      display: 'inline-flex',
                      alignItems: 'center',
                      gap: '6px',
                      padding: '5px 12px',
                      backgroundColor: 'var(--color-bg)',
                      borderRadius: 'var(--radius-full)',
                      fontSize: '0.84rem',
                      fontWeight: '600',
                      color: 'var(--color-text-primary)'
                    }}
                  >
                    <span>{p.fullName}</span>
                    {p.currentBand && (
                      <span style={{ fontSize: '0.72rem', color: 'var(--color-text-muted)' }}>
                        ({p.currentBand})
                      </span>
                    )}
                  </div>
                ))}
              </div>
            )}

            <p style={{
              fontSize: '0.8rem',
              color: 'var(--color-text-muted)',
              fontStyle: 'italic',
              lineHeight: 1.4
            }}>
              * Có mặt trước giờ chơi để đối chiếu vé. Mang theo vợt và trang phục thể thao.
            </p>
          </div>
        </div>

        {/* Right Column: Đăng ký tham gia (Figma Card) */}
        <div style={{
          backgroundColor: '#FFFFFF',
          borderRadius: 'var(--radius-lg)',
          padding: '28px 24px',
          boxShadow: 'var(--shadow-sm)',
          border: '1px solid var(--color-border)',
          position: 'sticky',
          top: '86px'
        }}>
          <h2 style={{
            fontSize: '1.25rem',
            fontWeight: '800',
            color: 'var(--color-primary)',
            marginBottom: '10px'
          }}>
            Đăng ký tham gia
          </h2>

          <div style={{
            fontSize: '1.85rem',
            fontWeight: '800',
            color: 'var(--color-primary)',
            fontFamily: "'Be Vietnam Pro', 'Plus Jakarta Sans', sans-serif",
            marginBottom: '12px'
          }}>
            {formatPrice(summary.priceVnd)} <span style={{ fontSize: '0.95rem', fontWeight: '500', color: 'var(--color-text-muted)' }}>/ người</span>
          </div>

          {/* Remaining spots pill (Figma style) */}
          <div style={{ marginBottom: '14px' }}>
            <span style={{
              display: 'inline-block',
              backgroundColor: isFull ? '#FEE2E2' : '#E6F4D0',
              color: isFull ? '#B91C1C' : '#2D5B18',
              fontSize: '0.78rem',
              fontWeight: '700',
              padding: '4px 12px',
              borderRadius: 'var(--radius-full)'
            }}>
              {isFull ? 'Đã hết chỗ' : `còn ${availableSlots} chỗ`}
            </span>
          </div>

          <p style={{ fontSize: '0.78rem', color: 'var(--color-text-muted)', marginBottom: '20px', lineHeight: 1.4 }}>
            Hệ thống kiểm tra trình độ và chỗ trống trước khi giữ chỗ.
          </p>

          {/* Main Action Button */}
          <button
            disabled={registering || isFull || registrationFeedback?.type === 'SUCCESS_PENDING'}
            onClick={handleRegister}
            style={{
              width: '100%',
              padding: '13px',
              borderRadius: 'var(--radius-md)',
              backgroundColor: isFull || registrationFeedback?.type === 'SUCCESS_PENDING'
                ? '#94A3B8'
                : 'var(--color-primary)',
              color: '#FFFFFF',
              fontSize: '0.95rem',
              fontWeight: '700',
              cursor: isFull || registrationFeedback?.type === 'SUCCESS_PENDING' ? 'not-allowed' : 'pointer',
              marginBottom: '10px',
              transition: 'background-color 0.15s'
            }}
          >
            {registering ? 'Đang xử lý...' : 'Đăng ký buổi chơi'}
          </button>

          {/* Secondary Button: Xin ngoại lệ trình độ */}
          <button
            onClick={() => alert("Chức năng nộp đơn Xin ngoại lệ trình độ sẽ gửi tới Trọng tài duyệt.")}
            style={{
              width: '100%',
              padding: '11px',
              borderRadius: 'var(--radius-md)',
              backgroundColor: '#FFFFFF',
              color: 'var(--color-text-secondary)',
              border: '1px solid #D1D5DB',
              fontSize: '0.88rem',
              fontWeight: '600',
              cursor: 'pointer',
              marginBottom: '12px'
            }}
          >
            Xin ngoại lệ trình độ
          </button>

          <p style={{ fontSize: '0.75rem', color: 'var(--color-text-muted)', textAlign: 'center' }}>
            Ngoại lệ chỉ áp dụng cho buổi chơi được duyệt.
          </p>

          {/* ======================================================== */}
          {/* THREE CONDITIONAL FEEDBACK BRANCHES (UC-07)               */}
          {/* ======================================================== */}

          {registrationFeedback && (
            <div style={{ marginTop: '20px' }}>
              {/* Branch Guest: Need Login */}
              {registrationFeedback.type === 'NEED_LOGIN' && (
                <div style={{
                  padding: '16px',
                  backgroundColor: '#EFF6FF',
                  border: '1.5px solid #93C5FD',
                  borderRadius: 'var(--radius-md)',
                  animation: 'fadeIn 0.2s ease-out'
                }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '8px', color: '#1D4ED8', marginBottom: '8px' }}>
                    <Users size={18} />
                    <strong style={{ fontSize: '0.94rem' }}>Khách cần đăng nhập</strong>
                  </div>
                  <p style={{ fontSize: '0.84rem', color: '#1E40AF', marginBottom: '12px', lineHeight: 1.45 }}>
                    {registrationFeedback.message}
                  </p>
                  <button
                    onClick={onOpenLogin}
                    style={{
                      width: '100%',
                      padding: '10px',
                      backgroundColor: 'var(--color-primary)',
                      color: '#FFFFFF',
                      fontSize: '0.86rem',
                      fontWeight: '700',
                      borderRadius: 'var(--radius-md)',
                      border: 'none',
                      cursor: 'pointer'
                    }}
                  >
                    Đăng nhập tài khoản Player
                  </button>
                </div>
              )}

              {/* Branch A: Display Skill Ineligibility Reason (Trường hợp 1) */}
              {registrationFeedback.type === 'SKILL_INELIGIBLE' && (
                <div style={{
                  padding: '18px',
                  backgroundColor: '#FEF2F2',
                  border: '1.5px solid #F87171',
                  borderRadius: 'var(--radius-md)',
                  animation: 'fadeIn 0.2s ease-out'
                }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '8px', color: '#B91C1C', marginBottom: '8px' }}>
                    <XCircle size={20} />
                    <strong style={{ fontSize: '0.96rem' }}>Không đủ điều kiện trình độ</strong>
                  </div>
                  <div style={{
                    fontSize: '0.86rem',
                    color: '#1F2937',
                    backgroundColor: '#FFFFFF',
                    padding: '10px 12px',
                    borderRadius: 'var(--radius-sm)',
                    border: '1px solid #FECACA',
                    lineHeight: 1.45,
                    marginBottom: '10px'
                  }}>
                    {registrationFeedback.reason === 'chưa làm survey' && (
                      <span><strong>Lý do:</strong> Bạn chưa làm bài khảo sát trình độ cho môn thể thao này. Vui lòng làm khảo sát trước khi đăng ký.</span>
                    )}
                    {registrationFeedback.reason === 'chưa có hồ sơ môn này' && (
                      <span><strong>Lý do:</strong> Bạn chưa có hồ sơ trình độ cho môn {summary.sportName === 'Badminton' ? 'Cầu lông' : 'Pickleball'}.</span>
                    )}
                    {registrationFeedback.reason === 'lệch band kỹ năng' && (
                      <span><strong>Lý do:</strong> Trình độ của bạn chưa khớp với yêu cầu của buổi chơi (Yêu cầu: <strong>Band {summary.band}</strong>).</span>
                    )}
                    {!['chưa làm survey', 'chưa có hồ sơ môn này', 'lệch band kỹ năng'].includes(registrationFeedback.reason) && (
                      <span><strong>Lý do:</strong> {registrationFeedback.message}</span>
                    )}
                  </div>
                  <div style={{ fontSize: '0.78rem', color: '#6B7280' }}>
                    💡 Bạn có thể bấm <strong>"Xin ngoại lệ trình độ"</strong> ở trên để gửi yêu cầu tới Trọng tài duyệt.
                  </div>
                </div>
              )}

              {/* Branch B: Display Session Full Notice (Trường hợp 2) */}
              {registrationFeedback.type === 'SESSION_FULL' && (
                <div style={{
                  padding: '18px',
                  backgroundColor: '#FFFBEB',
                  border: '1.5px solid #FBBF24',
                  borderRadius: 'var(--radius-md)',
                  animation: 'fadeIn 0.2s ease-out'
                }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '8px', color: '#B45309', marginBottom: '8px' }}>
                    <AlertTriangle size={20} />
                    <strong style={{ fontSize: '0.96rem' }}>Buổi chơi đã đầy chỗ</strong>
                  </div>
                  <p style={{
                    fontSize: '0.86rem',
                    color: '#78350F',
                    backgroundColor: '#FFFFFF',
                    padding: '10px 12px',
                    borderRadius: 'var(--radius-sm)',
                    border: '1px solid #FDE68A',
                    lineHeight: 1.45,
                    marginBottom: '8px'
                  }}>
                    {registrationFeedback.message || 'Buổi chơi đã đủ số lượng người đăng ký (hết chỗ trống). Nút đăng ký đã được khóa.'}
                  </p>
                  <p style={{ fontSize: '0.78rem', color: '#6B7280' }}>
                    Vui lòng quay lại màn hình Khám phá để chọn các buổi chơi khác còn chỗ.
                  </p>
                </div>
              )}

              {/* Already registered notice */}
              {registrationFeedback.type === 'ALREADY_REGISTERED' && (
                <div style={{
                  padding: '16px',
                  backgroundColor: '#EFF6FF',
                  border: '1px solid #BFDBFE',
                  borderRadius: 'var(--radius-md)',
                  color: '#1E40AF',
                  fontSize: '0.85rem'
                }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '6px', fontWeight: '700', marginBottom: '4px' }}>
                    <Info size={18} /> Đã có thông tin đăng ký
                  </div>
                  <p>{registrationFeedback.message}</p>
                </div>
              )}

              {/* Branch C: Record Pending Registration (Trường hợp 3: Thành công!) */}
              {registrationFeedback.type === 'SUCCESS_PENDING' && (
                <div style={{
                  padding: '20px',
                  backgroundColor: '#143A2A',
                  border: '2px solid #CBEB72',
                  borderRadius: 'var(--radius-md)',
                  color: '#FFFFFF',
                  boxShadow: '0 4px 14px rgba(20, 58, 42, 0.25)',
                  animation: 'fadeIn 0.25s ease-out'
                }}>
                  <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '12px' }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '8px', color: '#CBEB72' }}>
                      <CheckCircle2 size={22} />
                      <strong style={{ fontSize: '1.05rem' }}>
                        Giữ chỗ tạm thời thành công!
                      </strong>
                    </div>
                    <span style={{
                      backgroundColor: '#CBEB72',
                      color: '#143A2A',
                      fontSize: '0.72rem',
                      fontWeight: '800',
                      padding: '2px 8px',
                      borderRadius: 'var(--radius-full)'
                    }}>
                      PENDING
                    </span>
                  </div>

                  {/* Countdown Timer Box */}
                  <div style={{
                    backgroundColor: 'rgba(203, 235, 114, 0.12)',
                    border: '1px dashed #CBEB72',
                    borderRadius: 'var(--radius-sm)',
                    padding: '10px 12px',
                    marginBottom: '14px',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'space-between'
                  }}>
                    <span style={{ fontSize: '0.82rem', color: '#D1E0D7' }}>Thời gian giữ chỗ còn lại:</span>
                    <CountdownTimer expiresAt={registrationFeedback.data.expiresAt} />
                  </div>

                  <div style={{
                    backgroundColor: 'rgba(255, 255, 255, 0.08)',
                    borderRadius: 'var(--radius-sm)',
                    padding: '12px 14px',
                    display: 'flex',
                    flexDirection: 'column',
                    gap: '8px',
                    fontSize: '0.84rem'
                  }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                      <span style={{ color: '#D1E0D7' }}>Mã đơn giữ chỗ:</span>
                      <strong style={{ fontFamily: 'monospace', color: '#CBEB72', fontSize: '0.92rem' }}>
                        {registrationFeedback.data.orderCode}
                      </strong>
                    </div>

                    <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                      <span style={{ color: '#D1E0D7' }}>Mã vé tạm:</span>
                      <strong style={{ fontFamily: 'monospace' }}>
                        {registrationFeedback.data.ticketCode}
                      </strong>
                    </div>

                    <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                      <span style={{ color: '#D1E0D7' }}>Số tiền giữ chỗ:</span>
                      <strong style={{ fontSize: '0.95rem' }}>{formatPrice(registrationFeedback.data.amountVnd)}</strong>
                    </div>

                    <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                      <span style={{ color: '#D1E0D7' }}>Hạn giữ chỗ:</span>
                      <strong style={{ color: '#CBEB72' }}>
                        {formatDateTime(registrationFeedback.data.expiresAt)}
                      </strong>
                    </div>
                  </div>

                  <div style={{
                    marginTop: '12px',
                    padding: '8px 10px',
                    backgroundColor: 'rgba(255, 255, 255, 0.05)',
                    borderRadius: '4px',
                    fontSize: '0.76rem',
                    color: '#D1E0D7',
                    lineHeight: 1.45,
                    borderLeft: '3px solid #CBEB72'
                  }}>
                    📌 <strong>Bàn giao Người 2:</strong> Đơn đã lưu với trạng thái <em>Pending</em>. Quá trình đăng ký của Người 1 hoàn tất, dừng tại đây để Người 2 tiếp nhận xử lý Thanh toán.
                  </div>
                </div>
              )}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
