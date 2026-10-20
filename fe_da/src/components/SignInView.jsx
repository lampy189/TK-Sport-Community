import React, { useState } from 'react';
import { 
  ArrowUpRight, LogIn, Sparkles, CheckCircle2, AlertTriangle, 
  ArrowRight, UserCheck, ShieldCheck, Mail, Lock, User
} from 'lucide-react';
import { api } from '../api';

const PRESET_ACCOUNTS = [
  {
    email: 'player2@tksc.local',
    name: 'Lan Anh / Bình (Player)',
    role: 'Player',
    badge: 'Đủ điều kiện B2 Cầu lông / B1 Pickleball',
    color: '#059669',
    badgeBg: '#E6F4D0',
    desc: 'Dùng để test Kịch bản 3: Đủ điều kiện và Giữ chỗ tạm thời thành công (Pending).'
  },
  {
    email: 'nhatlthe180545@fpt.edu.vn',
    name: 'Lê Trọng Nhật (Player)',
    role: 'Player',
    badge: 'Lệch Band Pickleball (Band B2)',
    color: '#D97706',
    badgeBg: '#FEF3C7',
    desc: 'Dùng để test Kịch bản 1: Lệch band kỹ năng ➔ Báo lỗi SKILL_INELIGIBLE.'
  },
  {
    email: 'player_unrated@tksc.local',
    name: 'Người chơi mới (Chưa có hồ sơ)',
    role: 'Player',
    badge: 'Chưa làm Survey / Chưa có Band',
    color: '#DC2626',
    badgeBg: '#FEE2E2',
    desc: 'Dùng để test Kịch bản 1: Chưa làm khảo sát ➔ Báo lỗi SKILL_INELIGIBLE.'
  },
  {
    email: 'manager@tksc.local',
    name: 'Trần Văn Lâm (Manager)',
    role: 'Manager',
    badge: 'Tài khoản Quản lý',
    color: '#4B5563',
    badgeBg: '#F3F4F6',
    desc: 'Thử đăng ký để xác nhận quy tắc chỉ Player mới được đặt chỗ (403 Forbidden).'
  }
];

export function SignInView({ onLoginSuccess, onContinueAsGuest }) {
  const [customEmail, setCustomEmail] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

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
      maxWidth: '920px',
      margin: '40px auto 80px',
      padding: '0 24px',
      fontFamily: "'Be Vietnam Pro', 'Plus Jakarta Sans', sans-serif"
    }}>
      {/* Header Info */}
      <div style={{ textAlign: 'center', marginBottom: '32px' }}>
        <div style={{
          display: 'inline-flex',
          alignItems: 'center',
          gap: '6px',
          padding: '4px 14px',
          borderRadius: 'var(--radius-full)',
          backgroundColor: '#E6F4D0',
          color: '#143A2A',
          fontSize: '0.78rem',
          fontWeight: '700',
          letterSpacing: '0.04em',
          marginBottom: '12px'
        }}>
          <Sparkles size={14} /> UC-02 • ĐĂNG NHẬP HỆ THỐNG TKSC
        </div>

        <h1 style={{
          fontSize: '2.4rem',
          fontWeight: '800',
          color: 'var(--color-primary)',
          letterSpacing: '-0.02em',
          marginBottom: '8px'
        }}>
          Chào mừng đến với TKSC
        </h1>

        <p style={{ fontSize: '0.96rem', color: 'var(--color-text-secondary)', maxWidth: '560px', margin: '0 auto' }}>
          Đăng nhập để tìm buổi chơi đúng trình độ, nhận đề xuất thông minh và gửi yêu cầu giữ chỗ tạm thời.
        </p>
      </div>

      {/* Main Grid: Sign in Box & Presets */}
      <div style={{
        display: 'grid',
        gridTemplateColumns: 'minmax(0, 1.2fr) minmax(0, 1fr)',
        gap: '28px',
        alignItems: 'stretch'
      }}>
        {/* Left Column: Official Login Options */}
        <div style={{
          backgroundColor: '#FFFFFF',
          borderRadius: 'var(--radius-lg)',
          padding: '32px',
          border: '1px solid #E5E7EB',
          boxShadow: '0 1px 3px rgba(0,0,0,0.04)',
          display: 'flex',
          flexDirection: 'column',
          justifyContent: 'space-between'
        }}>
          <div>
            <h2 style={{ fontSize: '1.25rem', fontWeight: '800', color: 'var(--color-primary)', marginBottom: '8px' }}>
              Màn hình Sign In
            </h2>
            <p style={{ fontSize: '0.84rem', color: 'var(--color-text-muted)', marginBottom: '24px' }}>
              Chọn phương thức xác thực Google OAuth hoặc tài khoản hệ thống.
            </p>

            {error && (
              <div style={{
                padding: '12px 16px',
                backgroundColor: 'var(--color-danger-bg)',
                border: '1px solid var(--color-danger-border)',
                borderRadius: 'var(--radius-md)',
                color: 'var(--color-danger)',
                fontSize: '0.84rem',
                marginBottom: '18px'
              }}>
                {error}
              </div>
            )}

            {/* Google OAuth Button */}
            <button
              onClick={() => api.googleLogin()}
              style={{
                width: '100%',
                padding: '13px',
                borderRadius: 'var(--radius-md)',
                border: '1.5px solid #D1D5DB',
                backgroundColor: '#FFFFFF',
                fontSize: '0.92rem',
                fontWeight: '700',
                color: '#374151',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                gap: '10px',
                cursor: 'pointer',
                transition: 'all 0.15s ease',
                marginBottom: '20px'
              }}
              onMouseEnter={(e) => {
                e.currentTarget.style.backgroundColor = '#F9FAFB';
                e.currentTarget.style.borderColor = '#9CA3AF';
              }}
              onMouseLeave={(e) => {
                e.currentTarget.style.backgroundColor = '#FFFFFF';
                e.currentTarget.style.borderColor = '#D1D5DB';
              }}
            >
              <svg width="20" height="20" viewBox="0 0 24 24">
                <path fill="#4285F4" d="M23.745 12.27c0-.7-.06-1.4-.19-2.07H12v4.51h6.6c-.29 1.52-1.14 2.8-2.4 3.65v3h3.88c2.27-2.09 3.665-5.17 3.665-9.09z"/>
                <path fill="#34A853" d="M12 24c3.24 0 5.95-1.08 7.93-2.91l-3.88-3c-1.08.72-2.45 1.16-4.05 1.16-3.12 0-5.77-2.1-6.72-4.93H1.26v3.09C3.27 21.41 7.35 24 12 24z"/>
                <path fill="#FBBC05" d="M5.28 14.32c-.25-.72-.38-1.49-.38-2.32s.13-1.6.38-2.32V6.59H1.26C.46 8.19 0 9.99 0 12s.46 3.81 1.26 5.41l4.02-3.09z"/>
                <path fill="#EA4335" d="M12 4.75c1.77 0 3.35.61 4.6 1.8l3.42-3.42C17.95 1.19 15.24 0 12 0 7.35 0 3.27 2.59 1.26 6.59l4.02 3.09c.95-2.83 3.6-4.93 6.72-4.93z"/>
              </svg>
              <span>Đăng nhập với Google</span>
            </button>

            {/* Divider */}
            <div style={{
              display: 'flex',
              alignItems: 'center',
              gap: '12px',
              margin: '20px 0',
              color: '#9CA3AF',
              fontSize: '0.8rem'
            }}>
              <div style={{ flex: 1, height: '1px', backgroundColor: '#E5E7EB' }} />
              <span>HOẶC ĐĂNG NHẬP BẰNG EMAIL</span>
              <div style={{ flex: 1, height: '1px', backgroundColor: '#E5E7EB' }} />
            </div>

            {/* Email Input */}
            <div style={{ marginBottom: '14px' }}>
              <label style={{ display: 'block', fontSize: '0.82rem', fontWeight: '700', color: '#374151', marginBottom: '6px' }}>
                Địa chỉ Email
              </label>
              <div style={{ position: 'relative' }}>
                <Mail size={16} color="#9CA3AF" style={{ position: 'absolute', left: '12px', top: '13px' }} />
                <input
                  type="email"
                  placeholder="nhập email người chơi (VD: player2@tksc.local)"
                  value={customEmail}
                  onChange={(e) => setCustomEmail(e.target.value)}
                  onKeyDown={(e) => e.key === 'Enter' && handleLogin(customEmail.trim())}
                  style={{
                    width: '100%',
                    padding: '11px 12px 11px 38px',
                    borderRadius: 'var(--radius-md)',
                    border: '1.5px solid #D1D5DB',
                    fontSize: '0.88rem',
                    outline: 'none'
                  }}
                />
              </div>
            </div>

            <button
              disabled={loading || !customEmail.trim()}
              onClick={() => handleLogin(customEmail.trim())}
              style={{
                width: '100%',
                padding: '12px',
                borderRadius: 'var(--radius-md)',
                backgroundColor: 'var(--color-primary)',
                color: '#FFFFFF',
                fontSize: '0.9rem',
                fontWeight: '700',
                cursor: loading || !customEmail.trim() ? 'not-allowed' : 'pointer',
                opacity: loading || !customEmail.trim() ? 0.6 : 1,
                border: 'none',
                transition: 'background-color 0.15s'
              }}
            >
              {loading ? 'Đang xác thực...' : 'Đăng nhập vào hệ thống'}
            </button>
          </div>

          {/* Continue as guest */}
          <div style={{ marginTop: '24px', paddingTop: '18px', borderTop: '1px solid #E5E7EB', textAlign: 'center' }}>
            <button
              onClick={onContinueAsGuest}
              style={{
                background: 'none',
                border: 'none',
                color: 'var(--color-text-secondary)',
                fontSize: '0.85rem',
                fontWeight: '600',
                cursor: 'pointer',
                display: 'inline-flex',
                alignItems: 'center',
                gap: '4px'
              }}
            >
              Tiếp tục xem lịch chơi với tư cách khách <ArrowRight size={14} />
            </button>
          </div>
        </div>

        {/* Right Column: Fast Demo Presets */}
        <div style={{
          backgroundColor: '#F8FAFC',
          borderRadius: 'var(--radius-lg)',
          padding: '28px',
          border: '1.5px dashed #CBD5E1',
          display: 'flex',
          flexDirection: 'column',
          justifyContent: 'space-between'
        }}>
          <div>
            <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '8px' }}>
              <UserCheck size={18} color="var(--color-primary)" />
              <h3 style={{ fontSize: '1.1rem', fontWeight: '800', color: 'var(--color-primary)' }}>
                Tài khoản kiểm thử nhanh (UC-02)
              </h3>
            </div>
            <p style={{ fontSize: '0.8rem', color: '#64748B', marginBottom: '18px' }}>
              Chọn tài khoản người chơi mẫu để kiểm thử ngay 3 kịch bản UC-07:
            </p>

            <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
              {PRESET_ACCOUNTS.map((acc) => (
                <div
                  key={acc.email}
                  onClick={() => !loading && handleLogin(acc.email)}
                  style={{
                    backgroundColor: '#FFFFFF',
                    borderRadius: 'var(--radius-md)',
                    padding: '12px 14px',
                    border: '1px solid #E2E8F0',
                    cursor: loading ? 'not-allowed' : 'pointer',
                    boxShadow: '0 1px 2px rgba(0,0,0,0.03)',
                    transition: 'all 0.15s ease'
                  }}
                  onMouseEnter={(e) => {
                    e.currentTarget.style.borderColor = 'var(--color-primary)';
                    e.currentTarget.style.transform = 'translateY(-1px)';
                  }}
                  onMouseLeave={(e) => {
                    e.currentTarget.style.borderColor = '#E2E8F0';
                    e.currentTarget.style.transform = 'translateY(0)';
                  }}
                >
                  <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '4px' }}>
                    <strong style={{ fontSize: '0.88rem', color: 'var(--color-primary)' }}>
                      {acc.name}
                    </strong>
                    <span style={{
                      fontSize: '0.72rem',
                      fontWeight: '700',
                      padding: '2px 8px',
                      borderRadius: 'var(--radius-full)',
                      backgroundColor: acc.badgeBg,
                      color: acc.color
                    }}>
                      {acc.badge}
                    </span>
                  </div>
                  <p style={{ fontSize: '0.74rem', color: '#64748B', lineHeight: 1.35 }}>
                    {acc.desc}
                  </p>
                </div>
              ))}
            </div>
          </div>

          <div style={{ marginTop: '20px', fontSize: '0.74rem', color: '#94A3B8', textAlign: 'center' }}>
            Sau khi đăng nhập, hệ thống tự động lưu session cookie và gọi <code style={{ color: '#0F172A' }}>/api/auth/me</code>.
          </div>
        </div>
      </div>
    </div>
  );
}
