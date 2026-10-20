import React, { useState } from 'react';
import { X, CheckCircle2, AlertCircle, ArrowRight, ShieldAlert, Sparkles } from 'lucide-react';
import { api } from '../api';

const PRESET_ACCOUNTS = [
  {
    email: 'player2@tksc.local',
    name: 'Trần Thị Bình (Player)',
    role: 'Player',
    tag: 'Đủ điều kiện (B2 Cầu lông / B1 Pickleball)',
    color: '#059669',
    desc: 'Thử đăng ký thành công cho cả Session 1 & Session 2'
  },
  {
    email: 'nhatlthe180545@fpt.edu.vn',
    name: 'Lê Trọng Nhật (Player)',
    role: 'Player',
    tag: 'Lệch Band Pickleball (Band B2)',
    color: '#D97706',
    desc: 'Thử đăng ký Session 2 (Yêu cầu B1) để thấy lỗi SKILL_INELIGIBLE'
  },
  {
    email: 'player_unrated@tksc.local',
    name: 'Người chơi mới (Chưa có hồ sơ)',
    role: 'Player',
    tag: 'Chưa làm Survey / Chưa có hồ sơ',
    color: '#DC2626',
    desc: 'Thử đăng ký để thấy lỗi SKILL_INELIGIBLE (chưa có hồ sơ môn này)'
  },
  {
    email: 'manager@tksc.local',
    name: 'Trần Văn Lâm (Manager)',
    role: 'Manager',
    tag: 'Tài khoản Quản lý (Non-Player)',
    color: '#6B7280',
    desc: 'Thử đăng ký để thấy lỗi 403 Forbidden (chỉ Player được đăng ký)'
  }
];

export function DevLoginModal({ isOpen, onClose, onLoginSuccess }) {
  const [customEmail, setCustomEmail] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  if (!isOpen) return null;

  async function handleLogin(email) {
    if (!email) return;
    setLoading(true);
    setError(null);
    try {
      const account = await api.devLogin(email);
      onLoginSuccess(account);
      onClose();
    } catch (err) {
      setError(err.message || err.code || 'Đăng nhập không thành công. Hãy chắc chắn Backend đang chạy.');
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="modal-overlay">
      <div className="modal-content" style={{ maxWidth: '540px' }}>
        {/* Header */}
        <div style={{
          padding: '20px 24px',
          borderBottom: '1px solid var(--color-border)',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          backgroundColor: '#FFFFFF'
        }}>
          <div>
            <h3 style={{ fontSize: '1.25rem', fontWeight: '800', color: 'var(--color-primary)' }}>
              Đăng nhập tài khoản
            </h3>
            <p style={{ fontSize: '0.82rem', color: 'var(--color-text-secondary)', marginTop: '2px' }}>
              Chọn tài khoản có sẵn để test nhanh các kịch bản của Người 1
            </p>
          </div>
          <button
            onClick={onClose}
            style={{
              padding: '6px',
              borderRadius: '50%',
              color: 'var(--color-text-muted)',
              display: 'flex'
            }}
          >
            <X size={20} />
          </button>
        </div>

        {/* Body */}
        <div style={{ padding: '24px', maxHeight: '70vh', overflowY: 'auto' }}>
          {error && (
            <div style={{
              padding: '12px 16px',
              backgroundColor: 'var(--color-danger-bg)',
              border: '1px solid var(--color-danger-border)',
              borderRadius: 'var(--radius-md)',
              color: 'var(--color-danger)',
              fontSize: '0.85rem',
              marginBottom: '18px',
              display: 'flex',
              alignItems: 'center',
              gap: '8px'
            }}>
              <AlertCircle size={16} />
              <span>{error}</span>
            </div>
          )}

          {/* Presets List */}
          <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
            <label style={{ fontSize: '0.78rem', fontWeight: '700', textTransform: 'uppercase', letterSpacing: '0.05em', color: 'var(--color-text-muted)' }}>
              Danh sách tài khoản kiểm thử (Presets):
            </label>

            {PRESET_ACCOUNTS.map((acc) => (
              <div
                key={acc.email}
                onClick={() => !loading && handleLogin(acc.email)}
                style={{
                  padding: '12px 14px',
                  backgroundColor: '#FFFFFF',
                  border: '1.5px solid var(--color-border)',
                  borderRadius: 'var(--radius-lg)',
                  cursor: loading ? 'not-allowed' : 'pointer',
                  transition: 'all 0.15s ease',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'space-between',
                  boxShadow: 'var(--shadow-sm)'
                }}
                onMouseEnter={(e) => {
                  e.currentTarget.style.borderColor = 'var(--color-primary)';
                  e.currentTarget.style.transform = 'translateY(-1px)';
                }}
                onMouseLeave={(e) => {
                  e.currentTarget.style.borderColor = 'var(--color-border)';
                  e.currentTarget.style.transform = 'translateY(0)';
                }}
              >
                <div style={{ display: 'flex', flexDirection: 'column', gap: '2px' }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                    <span style={{ fontSize: '0.92rem', fontWeight: '700', color: 'var(--color-primary)' }}>
                      {acc.name}
                    </span>
                  </div>
                  <span style={{ fontSize: '0.78rem', color: acc.color, fontWeight: '600' }}>
                    • {acc.tag}
                  </span>
                  <span style={{ fontSize: '0.74rem', color: 'var(--color-text-muted)' }}>
                    {acc.desc}
                  </span>
                </div>

                <div style={{
                  backgroundColor: 'var(--color-bg)',
                  padding: '6px 12px',
                  borderRadius: 'var(--radius-full)',
                  fontSize: '0.78rem',
                  fontWeight: '700',
                  color: 'var(--color-primary)',
                  display: 'flex',
                  alignItems: 'center',
                  gap: '4px'
                }}>
                  Chọn <ArrowRight size={13} />
                </div>
              </div>
            ))}
          </div>

          {/* Custom Email Input */}
          <div style={{ marginTop: '20px', paddingTop: '18px', borderTop: '1px solid var(--color-border)' }}>
            <label style={{ display: 'block', fontSize: '0.8rem', fontWeight: '700', color: 'var(--color-text-secondary)', marginBottom: '8px' }}>
              Hoặc nhập Email tài khoản khác:
            </label>
            <div style={{ display: 'flex', gap: '8px' }}>
              <input
                type="email"
                placeholder="VD: player1@tksc.local"
                value={customEmail}
                onChange={(e) => setCustomEmail(e.target.value)}
                onKeyDown={(e) => e.key === 'Enter' && handleLogin(customEmail)}
                style={{
                  flex: 1,
                  padding: '10px 14px',
                  borderRadius: 'var(--radius-md)',
                  border: '1.5px solid var(--color-border)',
                  fontSize: '0.88rem',
                  outline: 'none'
                }}
              />
              <button
                disabled={loading || !customEmail.trim()}
                onClick={() => handleLogin(customEmail.trim())}
                style={{
                  padding: '10px 20px',
                  backgroundColor: 'var(--color-primary)',
                  color: '#FFFFFF',
                  fontWeight: '700',
                  fontSize: '0.88rem',
                  borderRadius: 'var(--radius-md)',
                  opacity: loading || !customEmail.trim() ? 0.6 : 1
                }}
              >
                {loading ? 'Đang vào...' : 'Vào'}
              </button>
            </div>
          </div>

          {/* Google Auth Divider */}
          <div style={{ marginTop: '20px', paddingTop: '16px', borderTop: '1px solid var(--color-border)', textAlign: 'center' }}>
            <button
              onClick={() => api.googleLogin()}
              style={{
                width: '100%',
                padding: '10px',
                borderRadius: 'var(--radius-md)',
                border: '1px solid var(--color-border)',
                backgroundColor: '#FFFFFF',
                fontSize: '0.88rem',
                fontWeight: '600',
                color: 'var(--color-text-primary)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                gap: '8px'
              }}
            >
              <svg width="18" height="18" viewBox="0 0 24 24">
                <path fill="#4285F4" d="M23.745 12.27c0-.7-.06-1.4-.19-2.07H12v4.51h6.6c-.29 1.52-1.14 2.8-2.4 3.65v3h3.88c2.27-2.09 3.665-5.17 3.665-9.09z"/>
                <path fill="#34A853" d="M12 24c3.24 0 5.95-1.08 7.93-2.91l-3.88-3c-1.08.72-2.45 1.16-4.05 1.16-3.12 0-5.77-2.1-6.72-4.93H1.26v3.09C3.27 21.41 7.35 24 12 24z"/>
                <path fill="#FBBC05" d="M5.28 14.32c-.25-.72-.38-1.49-.38-2.32s.13-1.6.38-2.32V6.59H1.26C.46 8.19 0 9.99 0 12s.46 3.81 1.26 5.41l4.02-3.09z"/>
                <path fill="#EA4335" d="M12 4.75c1.77 0 3.35.61 4.6 1.8l3.42-3.42C17.95 1.19 15.24 0 12 0 7.35 0 3.27 2.59 1.26 6.59l4.02 3.09c.95-2.83 3.6-4.93 6.72-4.93z"/>
              </svg>
              Đăng nhập bằng tài khoản Google thật
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
