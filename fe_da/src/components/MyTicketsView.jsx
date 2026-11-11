import React, { useState } from 'react';
import { Ticket, Calendar, Clock, MapPin, CheckCircle2, ArrowRight, Shield, QrCode } from 'lucide-react';

export function MyTicketsView({ user, onNavigate }) {
  const displayName = user?.fullName || 'Lan Anh';

  // Sample or recent tickets for display
  const [selectedTicket, setSelectedTicket] = useState(null);

  const sampleTickets = [
    {
      id: 1,
      ticketCode: 'TKT-1026-0081',
      orderCode: 'SBO-261020-4821',
      sessionTitle: 'Cầu lông sau giờ làm',
      sport: 'Cầu lông',
      court: 'Sân An Nhiên - Bình Thạnh',
      date: '19/10/2026',
      time: '18:00 - 20:00',
      priceVnd: 80000,
      status: 'Đã xác nhận',
      referee: 'Trọng tài Nguyễn Trọng Tài'
    }
  ];

  function formatPrice(val) {
    return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(val);
  }

  return (
    <div style={{ maxWidth: '1240px', margin: '0 auto', padding: '28px 24px 80px' }}>
      {/* Top Title & Subtitle (Figma Exact) */}
      <div style={{ marginBottom: '24px' }}>
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
          Vé & lịch chơi của tôi
        </h1>
        <p style={{
          fontSize: '0.95rem',
          color: 'var(--color-text-muted)'
        }}>
          Không gian của <strong style={{ color: 'var(--color-primary)' }}>{displayName}</strong> • Cầu lông & Pickleball
        </p>
      </div>

      {/* Main 2-Column Layout (Figma Screen 05 Layout) */}
      <div style={{
        display: 'grid',
        gridTemplateColumns: 'minmax(0, 1.8fr) minmax(320px, 1.2fr)',
        gap: '28px',
        alignItems: 'start'
      }}>
        {/* Left Column: Vé của tôi */}
        <div style={{
          backgroundColor: '#FFFFFF',
          borderRadius: 'var(--radius-lg)',
          padding: '28px 32px',
          border: '1px solid #E5E7EB',
          boxShadow: '0 1px 3px rgba(0,0,0,0.03)'
        }}>
          <h2 style={{ fontSize: '1.25rem', fontWeight: '800', color: 'var(--color-primary)', marginBottom: '16px' }}>
            Vé của tôi
          </h2>

          <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
            {sampleTickets.map((t) => (
              <div
                key={t.id}
                onClick={() => setSelectedTicket(t)}
                style={{
                  padding: '20px',
                  backgroundColor: '#FFFFFF',
                  borderRadius: 'var(--radius-md)',
                  border: '1.5px solid #E6F4D0',
                  boxShadow: '0 1px 3px rgba(0,0,0,0.04)',
                  cursor: 'pointer',
                  transition: 'all 0.15s ease'
                }}
                onMouseEnter={(e) => {
                  e.currentTarget.style.borderColor = 'var(--color-primary)';
                  e.currentTarget.style.transform = 'translateY(-1px)';
                }}
                onMouseLeave={(e) => {
                  e.currentTarget.style.borderColor = '#E6F4D0';
                  e.currentTarget.style.transform = 'translateY(0)';
                }}
              >
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '8px' }}>
                  <h3 style={{ fontSize: '1.15rem', fontWeight: '800', color: 'var(--color-primary)' }}>
                    {t.sessionTitle}
                  </h3>
                  <span style={{
                    backgroundColor: '#E6F4D0',
                    color: '#2B5718',
                    fontSize: '0.78rem',
                    fontWeight: '700',
                    padding: '3px 12px',
                    borderRadius: 'var(--radius-full)'
                  }}>
                    {t.status}
                  </span>
                </div>

                <div style={{ fontSize: '0.85rem', color: '#4B5563', marginBottom: '14px', lineHeight: 1.5 }}>
                  <div>📍 {t.court}</div>
                  <div>📅 {t.date} • {t.time}</div>
                </div>

                <div style={{
                  display: 'flex',
                  justifyContent: 'space-between',
                  alignItems: 'center',
                  paddingTop: '12px',
                  borderTop: '1px solid #F1F5F9'
                }}>
                  <span style={{ fontFamily: 'monospace', fontWeight: '700', color: 'var(--color-text-muted)' }}>
                    {t.ticketCode}
                  </span>
                  <button
                    style={{
                      padding: '6px 14px',
                      backgroundColor: 'var(--color-accent)',
                      color: 'var(--color-primary)',
                      borderRadius: 'var(--radius-full)',
                      fontWeight: '700',
                      fontSize: '0.82rem',
                      display: 'flex',
                      alignItems: 'center',
                      gap: '4px'
                    }}
                  >
                    Xem chi tiết vé <ArrowRight size={13} />
                  </button>
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Right Column: Lịch sử thanh toán & Chi tiết vé (Figma Screen 06) */}
        <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
          {/* Card: Lịch sử thanh toán */}
          <div style={{
            backgroundColor: '#FFFFFF',
            borderRadius: 'var(--radius-lg)',
            padding: '24px',
            border: '1px solid #E5E7EB',
            boxShadow: '0 1px 3px rgba(0,0,0,0.03)'
          }}>
            <h3 style={{ fontSize: '1.1rem', fontWeight: '800', color: 'var(--color-primary)', marginBottom: '14px' }}>
              Lịch sử thanh toán
            </h3>

            <div style={{
              backgroundColor: '#F9FAFB',
              borderRadius: 'var(--radius-md)',
              padding: '16px',
              fontSize: '0.85rem'
            }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '6px' }}>
                <span style={{ color: 'var(--color-text-muted)' }}>Đơn hàng gần nhất:</span>
                <strong style={{ color: 'var(--color-primary)' }}>80.000đ</strong>
              </div>
              <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '6px' }}>
                <span style={{ color: 'var(--color-text-muted)' }}>Mã thanh toán:</span>
                <span style={{ fontFamily: 'monospace' }}>TKSC-1026-0081</span>
              </div>
              <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                <span style={{ color: 'var(--color-text-muted)' }}>Trạng thái:</span>
                <span style={{ color: '#059669', fontWeight: '700' }}>✓ Đã thanh toán</span>
              </div>
            </div>
          </div>

          {/* Card: Chi tiết vé tham gia buổi chơi (Figma Screen 06 Exact) */}
          <div style={{
            backgroundColor: '#FFFFFF',
            borderRadius: 'var(--radius-lg)',
            padding: '24px',
            border: '2px solid #CBEB72',
            boxShadow: '0 4px 6px -1px rgba(0,0,0,0.05)'
          }}>
            <h3 style={{ fontSize: '1.1rem', fontWeight: '800', color: 'var(--color-primary)', marginBottom: '12px' }}>
              Vé tham gia buổi chơi
            </h3>

            <div style={{
              backgroundColor: 'var(--color-primary)',
              borderRadius: 'var(--radius-md)',
              padding: '20px',
              color: '#FFFFFF',
              textAlign: 'center',
              marginBottom: '16px'
            }}>
              <div style={{ fontSize: '0.78rem', color: '#D1E0D7', textTransform: 'uppercase', letterSpacing: '0.05em' }}>
                MÃ VÉ CHÍNH THỨC
              </div>
              <div style={{
                fontSize: '1.6rem',
                fontWeight: '900',
                letterSpacing: '0.04em',
                fontFamily: 'monospace',
                color: 'var(--color-accent)',
                margin: '6px 0 10px'
              }}>
                TKT-1026-0081
              </div>
              <div style={{ fontSize: '0.85rem', color: '#D1E0D7' }}>
                Người tham gia: <strong style={{ color: '#FFFFFF' }}>{displayName}</strong>
              </div>
            </div>

            <div style={{
              fontSize: '0.8rem',
              color: 'var(--color-text-secondary)',
              lineHeight: 1.5,
              padding: '12px',
              backgroundColor: '#F8FAFC',
              borderRadius: 'var(--radius-md)'
            }}>
              <strong>Trước khi ra sân:</strong>
              <ul style={{ paddingLeft: '18px', marginTop: '6px' }}>
                <li>Có mặt trước giờ chơi 15 phút</li>
                <li>Chuẩn bị vợt và trang phục thể thao</li>
                <li>Xuất trình mã vé cho trọng tài trực sân</li>
              </ul>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
