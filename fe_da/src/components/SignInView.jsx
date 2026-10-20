import React, { useState } from 'react';
import { ArrowUpRight, ArrowRight, Mail, Lightbulb } from 'lucide-react';
import { api } from '../api';

const BADMINTON_TIPS = [
  "Khởi động kỹ cổ tay, khớp gối và cổ chân ít nhất 10 phút trước khi vào sân để phòng ngừa chấn thương dây chằng.",
  "Sau mỗi pha đánh cầu, hãy lập tức bật nhẹ một bước chân (Split step) và trở về vị trí trung tâm sân (Center court).",
  "Trong đánh đôi, người đứng trên lưới nên giơ mặt vợt ngang ngực để sẵn sàng phản tạt và chụp lưới bất ngờ.",
  "Cầm vợt thả lỏng theo kiểu bắt tay (Universal Grip); chỉ siết chặt ngón tay đúng vào tích tắc mặt vợt chạm cầu.",
  "Khi đối phương chuẩn bị đập cầu (Smash), hãy hạ thấp trọng tâm, hai chân mở rộng hơn vai và đưa vợt ra phía trước.",
  "Đừng lạm dụng smash liên tục; đan xen những quả chém cầu (Drop shot) sát lưới để ép đối thủ mất thăng bằng.",
  "Bộ chân (Footwork) chuẩn là bí quyết số một giúp bạn đón cầu sớm, vào đúng tư thế và phát lực trọn vẹn nhất.",
  "Giữ mắt quan sát chuyển động cổ tay và vai của đối phương khi chuẩn bị đánh để phán đoán hướng cầu sớm nửa nhịp.",
  "Giữ mặt vợt hơi úp khi thực hiện cú đè cầu (Drive) tầm trung để cầu bay sát mép lưới và cắm nhanh về cuối sân.",
  "Trong Pickleball, làm chủ khu vực Non-Volley Zone (Kitchen) bằng các cú Dinking là chìa khóa để giành quyền chủ động.",
  "Hãy thở đều và uống từng ngụm nước nhỏ có bù điện giải sau mỗi set đấu thay vì uống quá nhiều nước cùng một lúc.",
  "Khi giao cầu ngắn, tập trung vào điểm rơi sát vạch trên và giữ quỹ đạo cầu lướt sát mép lưới đối phương.",
  "Khi bị đối phương ép góc trái tay (Backhand), ưu tiên quả phông cầu bổng về góc xa để có thời gian lui về thủ.",
  "Sau khi kết thúc buổi chơi, dành 5–10 phút ép cơ tĩnh (Static Stretching) để cơ bắp phục hồi nhanh hơn.",
  "Chọn mức căng cước phù hợp với lực cổ tay: Người mới nên căng 20–22 lbs, người chơi phong trào nên chọn 22–24 lbs."
];

export function SignInView({ onLoginSuccess, onContinueAsGuest }) {
  const [customEmail, setCustomEmail] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [currentTipIndex, setCurrentTipIndex] = useState(() => Math.floor(Math.random() * BADMINTON_TIPS.length));
  const [tipAnimating, setTipAnimating] = useState(false);

  function handleNextTip() {
    setTipAnimating(true);
    setTimeout(() => {
      let nextIndex;
      do {
        nextIndex = Math.floor(Math.random() * BADMINTON_TIPS.length);
      } while (nextIndex === currentTipIndex && BADMINTON_TIPS.length > 1);
      setCurrentTipIndex(nextIndex);
      setTipAnimating(false);
    }, 150);
  }

  async function handleLogin(email) {
    if (!email) return;
    setLoading(true);
    setError(null);
    try {
      const account = await api.devLogin(email);
      if (onLoginSuccess) {
        onLoginSuccess(account);
      }
    } catch (err) {
      setError(err.message || 'Đăng nhập không thành công. Hãy chắc chắn Backend đang chạy.');
    } finally {
      setLoading(false);
    }
  }

  return (
    <div style={{
      minHeight: '100vh',
      backgroundColor: '#F8FAFC',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      padding: '32px 16px',
      fontFamily: "'Be Vietnam Pro', 'Plus Jakarta Sans', sans-serif"
    }}>
      {/* Main Split Container (Fibery Style) */}
      <div style={{
        width: '100%',
        maxWidth: '1040px',
        backgroundColor: '#FFFFFF',
        borderRadius: '24px',
        boxShadow: '0 20px 45px -12px rgba(15, 23, 42, 0.08), 0 1px 3px rgba(0,0,0,0.04)',
        border: '1px solid #E2E8F0',
        display: 'grid',
        gridTemplateColumns: 'repeat(auto-fit, minmax(340px, 1fr))',
        overflow: 'hidden'
      }}>
        {/* Left Column: Login Form */}
        <div style={{
          padding: '48px 44px',
          display: 'flex',
          flexDirection: 'column',
          justifyContent: 'space-between'
        }}>
          <div>
            {/* Brand Logo */}
            <div style={{
              display: 'inline-flex',
              alignItems: 'center',
              gap: '6px',
              marginBottom: '36px',
              cursor: 'pointer'
            }} onClick={onContinueAsGuest}>
              <div style={{
                width: '32px',
                height: '32px',
                borderRadius: '8px',
                backgroundColor: '#143A2A',
                color: '#D3F082',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center'
              }}>
                <ArrowUpRight size={20} strokeWidth={2.8} />
              </div>
              <span style={{
                fontSize: '1.4rem',
                fontWeight: '900',
                letterSpacing: '-0.03em',
                color: '#143A2A'
              }}>
                TKSC
              </span>
            </div>

            {/* Title */}
            <h1 style={{
              fontSize: '1.85rem',
              fontWeight: '800',
              color: '#0F172A',
              letterSpacing: '-0.025em',
              marginBottom: '28px'
            }}>
              Login to TKSC
            </h1>

            {error && (
              <div style={{
                padding: '12px 16px',
                backgroundColor: 'var(--color-danger-bg)',
                border: '1px solid var(--color-danger-border)',
                borderRadius: 'var(--radius-md)',
                color: 'var(--color-danger)',
                fontSize: '0.85rem',
                marginBottom: '20px'
              }}>
                {error}
              </div>
            )}

            {/* Social / SSO Buttons */}
            <div style={{ display: 'flex', flexDirection: 'column', gap: '10px', marginBottom: '24px' }}>
              {/* Continue with Google */}
              <button
                type="button"
                onClick={() => api.googleLogin()}
                style={{
                  width: '100%',
                  padding: '11px 16px',
                  borderRadius: '10px',
                  border: '1px solid #E2E8F0',
                  backgroundColor: '#FFFFFF',
                  fontSize: '0.9rem',
                  fontWeight: '600',
                  color: '#334155',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  gap: '10px',
                  cursor: 'pointer',
                  transition: 'all 0.15s ease',
                  boxShadow: '0 1px 2px rgba(0,0,0,0.03)'
                }}
                onMouseEnter={(e) => {
                  e.currentTarget.style.backgroundColor = '#F8FAFC';
                  e.currentTarget.style.borderColor = '#CBD5E1';
                }}
                onMouseLeave={(e) => {
                  e.currentTarget.style.backgroundColor = '#FFFFFF';
                  e.currentTarget.style.borderColor = '#E2E8F0';
                }}
              >
                <svg width="18" height="18" viewBox="0 0 24 24">
                  <path fill="#4285F4" d="M23.745 12.27c0-.7-.06-1.4-.19-2.07H12v4.51h6.6c-.29 1.52-1.14 2.8-2.4 3.65v3h3.88c2.27-2.09 3.665-5.17 3.665-9.09z"/>
                  <path fill="#34A853" d="M12 24c3.24 0 5.95-1.08 7.93-2.91l-3.88-3c-1.08.72-2.45 1.16-4.05 1.16-3.12 0-5.77-2.1-6.72-4.93H1.26v3.09C3.27 21.41 7.35 24 12 24z"/>
                  <path fill="#FBBC05" d="M5.28 14.32c-.25-.72-.38-1.49-.38-2.32s.13-1.6.38-2.32V6.59H1.26C.46 8.19 0 9.99 0 12s.46 3.81 1.26 5.41l4.02-3.09z"/>
                  <path fill="#EA4335" d="M12 4.75c1.77 0 3.35.61 4.6 1.8l3.42-3.42C17.95 1.19 15.24 0 12 0 7.35 0 3.27 2.59 1.26 6.59l4.02 3.09c.95-2.83 3.6-4.93 6.72-4.93z"/>
                </svg>
                <span>Continue with Google</span>
              </button>

              {/* Continue with Email (also triggers Google OAuth as requested) */}
              <button
                type="button"
                onClick={() => api.googleLogin()}
                style={{
                  width: '100%',
                  padding: '11px 16px',
                  borderRadius: '10px',
                  border: '1px solid #E2E8F0',
                  backgroundColor: '#FFFFFF',
                  fontSize: '0.9rem',
                  fontWeight: '600',
                  color: '#334155',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  gap: '10px',
                  cursor: 'pointer',
                  transition: 'all 0.15s ease',
                  boxShadow: '0 1px 2px rgba(0,0,0,0.03)'
                }}
                onMouseEnter={(e) => {
                  e.currentTarget.style.backgroundColor = '#F8FAFC';
                  e.currentTarget.style.borderColor = '#CBD5E1';
                }}
                onMouseLeave={(e) => {
                  e.currentTarget.style.backgroundColor = '#FFFFFF';
                  e.currentTarget.style.borderColor = '#E2E8F0';
                }}
              >
                <Mail size={18} color="#475569" />
                <span>Continue with Email</span>
              </button>
            </div>

            {/* Divider */}
            <div style={{
              display: 'flex',
              alignItems: 'center',
              gap: '12px',
              margin: '22px 0',
              color: '#94A3B8',
              fontSize: '0.78rem',
              fontWeight: '700',
              letterSpacing: '0.04em'
            }}>
              <div style={{ flex: 1, height: '1px', backgroundColor: '#E2E8F0' }} />
              <span>HOẶC NHẬP EMAIL TRỰC TIẾP</span>
              <div style={{ flex: 1, height: '1px', backgroundColor: '#E2E8F0' }} />
            </div>

            {/* Work Email text input */}
            <div style={{ marginBottom: '14px' }}>
              <label style={{
                display: 'block',
                fontSize: '0.82rem',
                fontWeight: '700',
                color: '#334155',
                marginBottom: '6px'
              }}>
                Work Email
              </label>
              <div style={{ position: 'relative' }}>
                <Mail size={16} color="#94A3B8" style={{ position: 'absolute', left: '14px', top: '13px' }} />
                <input
                  type="email"
                  placeholder="nhập email (VD: player2@tksc.local)"
                  value={customEmail}
                  onChange={(e) => setCustomEmail(e.target.value)}
                  onKeyDown={(e) => e.key === 'Enter' && handleLogin(customEmail.trim())}
                  style={{
                    width: '100%',
                    padding: '11px 12px 11px 40px',
                    borderRadius: '10px',
                    border: '1.5px solid #CBD5E1',
                    fontSize: '0.88rem',
                    outline: 'none',
                    transition: 'border-color 0.15s ease'
                  }}
                  onFocus={(e) => e.target.style.borderColor = '#143A2A'}
                  onBlur={(e) => e.target.style.borderColor = '#CBD5E1'}
                />
              </div>
            </div>

            {/* Submit / Continue Button */}
            <button
              type="button"
              disabled={loading || !customEmail.trim()}
              onClick={() => handleLogin(customEmail.trim())}
              style={{
                width: '100%',
                padding: '12px',
                borderRadius: '10px',
                backgroundColor: '#143A2A',
                color: '#FFFFFF',
                fontSize: '0.92rem',
                fontWeight: '700',
                cursor: loading || !customEmail.trim() ? 'not-allowed' : 'pointer',
                opacity: loading || !customEmail.trim() ? 0.65 : 1,
                border: 'none',
                transition: 'background-color 0.15s ease'
              }}
              onMouseEnter={(e) => {
                if (!loading && customEmail.trim()) e.currentTarget.style.backgroundColor = '#1E4E38';
              }}
              onMouseLeave={(e) => {
                e.currentTarget.style.backgroundColor = '#143A2A';
              }}
            >
              {loading ? 'Đang xác thực...' : 'Đăng nhập vào hệ thống'}
            </button>
          </div>

          {/* Bottom Footer inside Left Column */}
          <div style={{ marginTop: '32px', paddingTop: '20px', borderTop: '1px solid #F1F5F9', textAlign: 'center' }}>
            <button
              type="button"
              onClick={onContinueAsGuest}
              style={{
                background: 'none',
                border: 'none',
                color: '#64748B',
                fontSize: '0.86rem',
                fontWeight: '600',
                cursor: 'pointer',
                display: 'inline-flex',
                alignItems: 'center',
                gap: '6px',
                transition: 'color 0.15s'
              }}
              onMouseEnter={(e) => e.currentTarget.style.color = '#143A2A'}
              onMouseLeave={(e) => e.currentTarget.style.color = '#64748B'}
            >
              Tiếp tục xem lịch chơi với tư cách khách <ArrowRight size={14} />
            </button>
          </div>
        </div>

        {/* Right Column: Green Gradient with Interactive Badminton Tips */}
        <div style={{
          position: 'relative',
          background: 'linear-gradient(145deg, #0F2D20 0%, #164832 40%, #1E5C41 75%, #297A57 100%)',
          padding: '40px',
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          justifyContent: 'center',
          overflow: 'hidden'
        }}>
          {/* Subtle Decorative Gradient Glows & Abstract Sport Shapes */}
          <div style={{
            position: 'absolute',
            top: '-60px',
            right: '-60px',
            width: '260px',
            height: '260px',
            borderRadius: '50%',
            background: 'radial-gradient(circle, rgba(211, 240, 130, 0.22) 0%, rgba(211, 240, 130, 0) 70%)',
            pointerEvents: 'none'
          }} />

          <div style={{
            position: 'absolute',
            bottom: '-80px',
            left: '-60px',
            width: '280px',
            height: '280px',
            borderRadius: '50%',
            background: 'radial-gradient(circle, rgba(167, 243, 208, 0.18) 0%, rgba(167, 243, 208, 0) 70%)',
            pointerEvents: 'none'
          }} />

          {/* SVG Shuttlecock Watermark in background */}
          <svg
            style={{
              position: 'absolute',
              bottom: '10px',
              right: '15px',
              opacity: 0.08,
              pointerEvents: 'none'
            }}
            width="220"
            height="220"
            viewBox="0 0 100 100"
            fill="none"
            stroke="#FFFFFF"
            strokeWidth="2"
          >
            <path d="M50 15 L25 65 L75 65 Z" />
            <path d="M35 65 Q50 90 65 65" />
            <line x1="33" y1="48" x2="67" y2="48" />
            <line x1="41" y1="32" x2="59" y2="32" />
          </svg>

          {/* Floating Interactive Badminton Tip Card (Fibery style) */}
          <div
            onClick={handleNextTip}
            title="Bấm để xem mẹo ngẫu nhiên khác"
            style={{
              position: 'relative',
              zIndex: 2,
              width: '100%',
              maxWidth: '360px',
              backgroundColor: 'rgba(255, 255, 255, 0.96)',
              backdropFilter: 'blur(12px)',
              borderRadius: '20px',
              padding: '24px 26px',
              boxShadow: '0 20px 40px rgba(0, 0, 0, 0.16), 0 1px 3px rgba(0,0,0,0.06)',
              border: '1px solid rgba(255, 255, 255, 0.8)',
              cursor: 'pointer',
              userSelect: 'none',
              transform: tipAnimating ? 'scale(0.97)' : 'scale(1)',
              opacity: tipAnimating ? 0.7 : 1,
              transition: 'all 0.18s ease'
            }}
            onMouseEnter={(e) => {
              e.currentTarget.style.transform = 'translateY(-3px)';
              e.currentTarget.style.boxShadow = '0 24px 48px rgba(0, 0, 0, 0.2)';
            }}
            onMouseLeave={(e) => {
              e.currentTarget.style.transform = 'translateY(0)';
              e.currentTarget.style.boxShadow = '0 20px 40px rgba(0, 0, 0, 0.16)';
            }}
          >
            {/* Tip Card Header */}
            <div style={{
              display: 'flex',
              alignItems: 'center',
              gap: '8px',
              marginBottom: '14px'
            }}>
              <div style={{
                width: '28px',
                height: '28px',
                borderRadius: '50%',
                backgroundColor: '#E6F4D0',
                color: '#143A2A',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center'
              }}>
                <Lightbulb size={16} />
              </div>
              <strong style={{ fontSize: '0.92rem', color: '#0F172A', fontWeight: '800' }}>
                Bạn có biết?
              </strong>
            </div>

            {/* Tip Content */}
            <p style={{
              fontSize: '0.88rem',
              lineHeight: 1.55,
              color: '#334155',
              fontWeight: '500',
              margin: 0
            }}>
              "{BADMINTON_TIPS[currentTipIndex]}"
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}
