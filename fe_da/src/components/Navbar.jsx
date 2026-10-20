import React, { useState } from 'react';
import { ArrowUpRight, User, LogOut, ChevronDown } from 'lucide-react';

export function Navbar({ user, onOpenLogin, onLogout, onNavigate, currentView }) {
  const [profileDropdownOpen, setProfileDropdownOpen] = useState(false);
  const displayName = user?.fullName || 'Lan Anh';

  return (
    <header style={{
      backgroundColor: '#FFFFFF',
      borderBottom: '1px solid #E5E7EB',
      position: 'sticky',
      top: 0,
      zIndex: 100,
      boxShadow: '0 1px 2px rgba(0,0,0,0.03)'
    }}>
      <div style={{
        maxWidth: '1240px',
        margin: '0 auto',
        padding: '0 24px',
        height: '66px',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between'
      }}>
        {/* Left: Brand Logo */}
        <div 
          onClick={() => onNavigate('browse')}
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: '5px',
            cursor: 'pointer',
            userSelect: 'none'
          }}
        >
          <div style={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            color: 'var(--color-primary)',
            fontWeight: '900',
            fontSize: '1.25rem'
          }}>
            <ArrowUpRight size={22} strokeWidth={2.8} />
          </div>
          <span style={{
            fontSize: '1.35rem',
            fontWeight: '800',
            letterSpacing: '-0.02em',
            color: 'var(--color-primary)',
            fontFamily: "'Be Vietnam Pro', 'Plus Jakarta Sans', sans-serif"
          }}>
            TKSC
          </span>
        </div>

        {/* Center: Navigation Menu Pills (Figma exact styling) */}
        <nav style={{
          display: 'flex',
          alignItems: 'center',
          gap: '8px'
        }}>
          {[
            { id: 'browse', label: 'Khám phá' },
            { id: 'tickets', label: 'Vé & lịch chơi' },
            { id: 'friends', label: 'Bạn bè' },
            { id: 'messages', label: 'Tin nhắn' },
          ].map((item) => {
            const isActive = currentView === item.id;
            return (
              <button
                key={item.id}
                onClick={() => onNavigate(item.id)}
                style={{
                  padding: '7px 18px',
                  borderRadius: 'var(--radius-full)',
                  fontSize: '0.86rem',
                  fontWeight: isActive ? '700' : '500',
                  color: isActive ? '#143A2A' : '#4B5563',
                  backgroundColor: isActive ? '#E6F4D0' : '#FFFFFF',
                  border: isActive ? '1.5px solid #CBEB72' : '1px solid #E5E7EB',
                  boxShadow: isActive ? '0 1px 3px rgba(203, 235, 114, 0.4)' : 'none',
                  transition: 'all 0.15s ease',
                  cursor: 'pointer'
                }}
              >
                {item.label}
              </button>
            );
          })}
        </nav>

        {/* Right: Notifications & Profile Pill */}
        <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
          {/* Notification Pill */}
          <div style={{
            display: 'flex',
            alignItems: 'center',
            padding: '7px 18px',
            borderRadius: 'var(--radius-full)',
            backgroundColor: '#E6F4D0',
            color: '#143A2A',
            fontSize: '0.86rem',
            fontWeight: '600',
            cursor: 'pointer',
            border: '1px solid #D7EBA6'
          }}>
            Thông báo • 3
          </div>

          {/* Profile Name Pill or Login Button */}
          {user ? (
            <div style={{ position: 'relative' }}>
              <button
                onClick={() => setProfileDropdownOpen(!profileDropdownOpen)}
                style={{
                  display: 'flex',
                  alignItems: 'center',
                  gap: '8px',
                  padding: '6px 16px',
                  borderRadius: 'var(--radius-full)',
                  backgroundColor: '#FFFFFF',
                  border: '1px solid #D1D5DB',
                  fontSize: '0.88rem',
                  fontWeight: '600',
                  color: 'var(--color-primary)',
                  boxShadow: '0 1px 2px rgba(0,0,0,0.04)',
                  cursor: 'pointer'
                }}
              >
                <div style={{
                  width: '24px',
                  height: '24px',
                  borderRadius: '50%',
                  backgroundColor: 'var(--color-primary)',
                  color: 'var(--color-accent)',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  fontSize: '0.75rem',
                  fontWeight: '700'
                }}>
                  {displayName.charAt(0).toUpperCase()}
                </div>
                <span>{displayName}</span>
                <ChevronDown size={14} color="#6B7280" />
              </button>

            {/* Profile Dropdown */}
            {profileDropdownOpen && user && (
              <div 
                style={{
                  position: 'absolute',
                  right: 0,
                  top: '46px',
                  width: '240px',
                  backgroundColor: '#FFFFFF',
                  borderRadius: 'var(--radius-lg)',
                  boxShadow: 'var(--shadow-xl)',
                  border: '1px solid var(--color-border)',
                  padding: '12px',
                  zIndex: 200,
                  animation: 'fadeIn 0.15s ease-out'
                }}
              >
                <div style={{ padding: '8px 10px', borderBottom: '1px solid #F1F5F9', marginBottom: '8px' }}>
                  <div style={{ fontWeight: '700', color: 'var(--color-primary)', fontSize: '0.92rem' }}>
                    {user.fullName || 'Người chơi'}
                  </div>
                  <div style={{ fontSize: '0.78rem', color: '#6B7280' }}>
                    {user.email}
                  </div>
                  <div style={{
                    display: 'inline-block',
                    marginTop: '4px',
                    fontSize: '0.68rem',
                    fontWeight: '700',
                    backgroundColor: '#E6F4D0',
                    color: '#2B5718',
                    padding: '2px 8px',
                    borderRadius: '4px'
                  }}>
                    {user.role}
                  </div>
                </div>

                <button
                  onClick={() => {
                    setProfileDropdownOpen(false);
                    onNavigate?.('profile');
                  }}
                  style={{
                    width: '100%',
                    padding: '8px 10px',
                    borderRadius: 'var(--radius-md)',
                    display: 'flex',
                    alignItems: 'center',
                    gap: '8px',
                    fontSize: '0.85rem',
                    color: 'var(--color-text-secondary)',
                    fontWeight: '600',
                    textAlign: 'left'
                  }}
                  onMouseEnter={(e) => e.currentTarget.style.backgroundColor = '#F8FAFC'}
                  onMouseLeave={(e) => e.currentTarget.style.backgroundColor = 'transparent'}
                >
                  <User size={16} /> Hồ sơ
                </button>

                <button
                  onClick={() => {
                    setProfileDropdownOpen(false);
                    onLogout();
                  }}
                  style={{
                    width: '100%',
                    padding: '8px 10px',
                    borderRadius: 'var(--radius-md)',
                    display: 'flex',
                    alignItems: 'center',
                    gap: '8px',
                    fontSize: '0.85rem',
                    color: 'var(--color-danger)',
                    fontWeight: '600',
                    textAlign: 'left',
                    marginTop: '4px'
                  }}
                  onMouseEnter={(e) => e.currentTarget.style.backgroundColor = '#FEF2F2'}
                  onMouseLeave={(e) => e.currentTarget.style.backgroundColor = 'transparent'}
                >
                  <LogOut size={16} /> Đăng xuất
                </button>
              </div>
            )}
          </div>
        ) : (
          <button
            onClick={() => onNavigate('signin')}
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: '6px',
              padding: '7px 18px',
              borderRadius: 'var(--radius-full)',
              backgroundColor: 'var(--color-primary)',
              color: '#FFFFFF',
              fontSize: '0.86rem',
              fontWeight: '700',
              cursor: 'pointer',
              border: 'none',
              boxShadow: '0 2px 6px rgba(0,0,0,0.1)'
            }}
          >
            <User size={15} /> Đăng nhập
          </button>
        )}
        </div>
      </div>
    </header>
  );
}
