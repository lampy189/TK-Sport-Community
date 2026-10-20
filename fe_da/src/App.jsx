import React, { useState, useEffect } from 'react';
import { Navbar } from './components/Navbar';
import { BrowseSessionsView } from './components/BrowseSessionsView';
import { SessionDetailView } from './components/SessionDetailView';
import { MyTicketsView } from './components/MyTicketsView';
import { DevLoginModal } from './components/DevLoginModal';
import { api } from './api';

export default function App() {
  const [user, setUser] = useState(null);
  const [loadingUser, setLoadingUser] = useState(true);
  const [currentView, setCurrentView] = useState('browse'); // 'browse' | 'detail' | 'tickets'
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
    } catch {
      setUser(null);
    } finally {
      setLoadingUser(false);
    }
  }

  async function handleLogout() {
    await api.logout();
    setUser(null);
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

  const playerName = user?.fullName || 'Lan Anh';

  return (
    <div style={{ minHeight: '100vh', display: 'flex', flexDirection: 'column', backgroundColor: 'var(--color-bg)' }}>
      {/* Top Navigation Bar (Figma Exact) */}
      <Navbar
        user={user}
        currentView={currentView}
        onNavigate={(view) => {
          if (view === 'browse') handleBackToBrowse();
          if (view === 'tickets') {
            setCurrentView('tickets');
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
            Không gian dành cho người chơi: <strong style={{ color: 'var(--color-primary)' }}>{playerName}</strong>
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
