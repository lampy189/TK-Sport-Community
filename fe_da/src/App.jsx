import React, { useState, useEffect } from 'react';
import { Navbar } from './components/Navbar';
import { BrowseSessionsView } from './components/BrowseSessionsView';
import { SessionDetailView } from './components/SessionDetailView';
import { MyTicketsView } from './components/MyTicketsView';
import { SignInView } from './components/SignInView';
import { DevLoginModal } from './components/DevLoginModal';
import { api } from './api';

export default function App() {
  const [user, setUser] = useState(null);
  const [loadingUser, setLoadingUser] = useState(true);
  const [currentView, setCurrentView] = useState('signin'); // 'signin' | 'browse' | 'detail' | 'tickets'
  const [selectedSessionId, setSelectedSessionId] = useState(null);
  const [isLoginModalOpen, setIsLoginModalOpen] = useState(false);

  useEffect(() => {
    checkCurrentUser();
  }, []);

  async function checkCurrentUser() {
    setLoadingUser(true);
    try {
      const me = await api.getMe();
      setUser(me);
      if (!me) {
        setCurrentView('signin');
      } else {
        setCurrentView('browse');
      }
    } catch {
      setUser(null);
      setCurrentView('signin');
    } finally {
      setLoadingUser(false);
    }
  }

  async function handleLogout() {
    await api.logout();
    setUser(null);
    setCurrentView('signin');
  }

  function handleSelectSession(id) {
    setSelectedSessionId(id);
    setCurrentView('detail');
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  function handleBackToBrowse() {
    setCurrentView('browse');
    setSelectedSessionId(null);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  // 1. Minimal Splash Loader while checking authentication
  if (loadingUser) {
    return (
      <div style={{
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        backgroundColor: '#F8FAFC',
        fontFamily: "'Be Vietnam Pro', 'Plus Jakarta Sans', sans-serif"
      }}>
        <div style={{ textAlign: 'center' }}>
          <div style={{
            fontSize: '2rem',
            fontWeight: '900',
            color: '#143A2A',
            letterSpacing: '-0.03em',
            marginBottom: '8px'
          }}>
            ↗ TKSC
          </div>
          <div style={{ color: '#64748B', fontSize: '0.88rem' }}>Đang kết nối hệ thống...</div>
        </div>
      </div>
    );
  }

  // 2. Standalone Login View (Fibery-style, separated from main app frame)
  if (currentView === 'signin') {
    return (
      <SignInView
        onLoginSuccess={(account) => {
          setUser(account);
          setCurrentView('browse');
        }}
        onContinueAsGuest={handleBackToBrowse}
      />
    );
  }

  // 3. Main Application Frame (Browse / Detail / Tickets)
  return (
    <div style={{ minHeight: '100vh', display: 'flex', flexDirection: 'column', backgroundColor: 'var(--color-bg)' }}>
      {/* Top Navigation Bar */}
      <Navbar
        user={user}
        currentView={currentView}
        onNavigate={(view) => {
          if (view === 'browse') handleBackToBrowse();
          else if (view === 'tickets') {
            setCurrentView('tickets');
            setSelectedSessionId(null);
          } else if (view === 'signin') {
            setCurrentView('signin');
            setSelectedSessionId(null);
          }
        }}
        onOpenLogin={() => setIsLoginModalOpen(true)}
        onLogout={handleLogout}
      />

      {/* Main Content Area */}
      <main style={{ flex: 1 }}>
        {currentView === 'browse' && (
          <BrowseSessionsView
            user={user}
            onSelectSession={handleSelectSession}
            onNavigate={(view) => {
              if (view === 'tickets') setCurrentView('tickets');
            }}
          />
        )}

        {currentView === 'detail' && selectedSessionId && (
          <SessionDetailView
            sessionId={selectedSessionId}
            user={user}
            onBack={handleBackToBrowse}
            onOpenLogin={() => setIsLoginModalOpen(true)}
          />
        )}

        {currentView === 'tickets' && (
          <MyTicketsView
            user={user}
            onNavigate={(view) => {
              if (view === 'browse') handleBackToBrowse();
            }}
          />
        )}
      </main>

      {/* Footer */}
      <footer style={{
        backgroundColor: '#FFFFFF',
        borderTop: '1px solid #E5E7EB',
        padding: '24px',
        fontSize: '0.82rem',
        color: 'var(--color-text-muted)'
      }}>
        <div style={{ maxWidth: '1240px', margin: '0 auto', display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '10px' }}>
          <div>
            <strong style={{ color: 'var(--color-primary)' }}>↗ TKSC — TK Sport Community</strong> © 2026. All rights reserved.
          </div>
          <div>
            {user ? (
              <>Không gian dành cho người chơi: <strong style={{ color: 'var(--color-primary)' }}>{user.fullName}</strong></>
            ) : (
              <span style={{ color: '#6B7280' }}>Đang xem lịch chơi với tư cách <strong>Khách</strong> (chưa đăng nhập)</span>
            )}
          </div>
        </div>
      </footer>

      {/* Quick Test Login Modal */}
      <DevLoginModal
        isOpen={isLoginModalOpen}
        onClose={() => setIsLoginModalOpen(false)}
        onLoginSuccess={(account) => {
          setUser(account);
          checkCurrentUser();
        }}
      />
    </div>
  );
}
