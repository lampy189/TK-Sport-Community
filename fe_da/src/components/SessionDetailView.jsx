import React, { useState, useEffect } from 'react';
import { 
  ArrowLeft, Calendar, Clock, MapPin, Users, Shield, 
  AlertTriangle, CheckCircle2, Sparkles, XCircle, Info, Lock 
} from 'lucide-react';
import { api } from '../api';

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
      onOpenLogin();
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
          fontFamily: 'Outfit, sans-serif',
          letterSpacing: '-0.02em',
          lineHeight: 1.2,
          marginBottom: '6px'
        }}>
          {summary.sportName === 'Badminton' ? 'Cầu lông sau giờ làm' : 'Giao lưu Pickleball'} — {summary.courtName}
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
                {summary.sessionDate} • {summary.timeLabel || `${summary.startTime} - ${summary.endTime}`}
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
            fontFamily: 'Outfit, sans-serif',
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
          {/* THREE CONDITIONAL FEEDBACK BRANCHES (Người 1 requirements) */}
          {/* ======================================================== */}

          {registrationFeedback && (
            <div style={{ marginTop: '20px' }}>
              {/* Branch A: Display Skill Ineligibility Reason */}
              {registrationFeedback.type === 'SKILL_INELIGIBLE' && (
                <div style={{
                  padding: '16px',
                  backgroundColor: 'var(--color-danger-bg)',
                  border: '1.5px solid var(--color-danger-border)',
                  borderRadius: 'var(--radius-md)',
                  animation: 'fadeIn 0.2s ease-out'
                }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '6px', color: 'var(--color-danger)', marginBottom: '6px' }}>
                    <XCircle size={18} />
                    <strong style={{ fontSize: '0.9rem' }}>Không đủ điều kiện kỹ năng</strong>
                  </div>
                  <div style={{ fontSize: '0.78rem', color: '#7F1D1D', marginBottom: '6px' }}>
                    Mã lỗi: <strong style={{ fontFamily: 'monospace' }}>SKILL_INELIGIBLE</strong>
                  </div>
                  <div style={{
                    fontSize: '0.84rem',
                    color: 'var(--color-text-primary)',
                    backgroundColor: '#FFFFFF',
                    padding: '8px 10px',
                    borderRadius: 'var(--radius-sm)',
                    border: '1px solid #FECACA',
                    fontWeight: '600'
                  }}>
                    Lý do: {registrationFeedback.reason === 'chưa làm survey' && 'Người chơi chưa làm khảo sát kỹ năng.'}
                    {registrationFeedback.reason === 'chưa có hồ sơ môn này' && 'Chưa có hồ sơ kỹ năng cho môn thể thao này.'}
                    {registrationFeedback.reason === 'lệch band kỹ năng' && `Trình độ không khớp với yêu cầu của buổi chơi (Yêu cầu Band ${summary.band}).`}
                    {!['chưa làm survey', 'chưa có hồ sơ môn này', 'lệch band kỹ năng'].includes(registrationFeedback.reason) && registrationFeedback.message}
                  </div>
                </div>
              )}

              {/* Branch B: Display Session Full Notice */}
              {registrationFeedback.type === 'SESSION_FULL' && (
                <div style={{
                  padding: '16px',
                  backgroundColor: 'var(--color-warning-bg)',
                  border: '1.5px solid var(--color-warning-border)',
                  borderRadius: 'var(--radius-md)',
                  animation: 'fadeIn 0.2s ease-out'
                }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '6px', color: 'var(--color-warning)', marginBottom: '6px' }}>
                    <AlertTriangle size={18} />
                    <strong style={{ fontSize: '0.9rem' }}>Buổi chơi đã đủ người</strong>
                  </div>
                  <p style={{ fontSize: '0.82rem', color: '#92400E' }}>
                    {registrationFeedback.message || 'Buổi chơi đã đủ người. Vui lòng chọn buổi chơi khác.'}
                  </p>
                </div>
              )}

              {/* Already registered warning */}
              {registrationFeedback.type === 'ALREADY_REGISTERED' && (
                <div style={{
                  padding: '14px',
                  backgroundColor: '#EFF6FF',
                  border: '1px solid #BFDBFE',
                  borderRadius: 'var(--radius-md)',
                  color: '#1E40AF',
                  fontSize: '0.85rem'
                }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '6px', fontWeight: '700', marginBottom: '4px' }}>
                    <Info size={16} /> Đã đăng ký
                  </div>
                  <p>{registrationFeedback.message}</p>
                </div>
              )}

              {/* Branch C: Record Pending Registration (Success!) */}
              {registrationFeedback.type === 'SUCCESS_PENDING' && (
                <div style={{
                  padding: '18px',
                  backgroundColor: 'var(--color-primary)',
                  border: '2px solid var(--color-accent)',
                  borderRadius: 'var(--radius-md)',
                  color: '#FFFFFF',
                  boxShadow: 'var(--shadow-md)',
                  animation: 'fadeIn 0.25s ease-out'
                }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '8px', color: 'var(--color-accent)', marginBottom: '8px' }}>
                    <CheckCircle2 size={20} />
                    <strong style={{ fontSize: '0.98rem' }}>
                      Giữ chỗ tạm thời thành công!
                    </strong>
                  </div>

                  <div style={{
                    backgroundColor: 'rgba(255, 255, 255, 0.08)',
                    borderRadius: 'var(--radius-sm)',
                    padding: '12px',
                    display: 'flex',
                    flexDirection: 'column',
                    gap: '6px',
                    fontSize: '0.82rem'
                  }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                      <span style={{ color: '#D1E0D7' }}>Mã đơn:</span>
                      <strong style={{ fontFamily: 'monospace', color: 'var(--color-accent)' }}>
                        {registrationFeedback.data.orderCode}
                      </strong>
                    </div>

                    <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                      <span style={{ color: '#D1E0D7' }}>Mã vé:</span>
                      <strong style={{ fontFamily: 'monospace' }}>
                        {registrationFeedback.data.ticketCode}
                      </strong>
                    </div>

                    <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                      <span style={{ color: '#D1E0D7' }}>Số tiền:</span>
                      <strong>{formatPrice(registrationFeedback.data.amountVnd)}</strong>
                    </div>

                    <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                      <span style={{ color: '#D1E0D7' }}>Hạn giữ chỗ:</span>
                      <strong style={{ color: 'var(--color-accent)' }}>
                        {formatDateTime(registrationFeedback.data.expiresAt)}
                      </strong>
                    </div>
                  </div>

                  <p style={{
                    fontSize: '0.74rem',
                    color: '#D1E0D7',
                    marginTop: '10px',
                    lineHeight: 1.4
                  }}>
                    ⏱️ Suất chơi đang được giữ tạm thời trong 15 phút.
                  </p>
                </div>
              )}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
