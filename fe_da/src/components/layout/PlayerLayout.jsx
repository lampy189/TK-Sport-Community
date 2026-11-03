import NotificationBell from "../NotificationBell";
import FloatingChatBubble from "../FloatingChatBubble";
import { useEffect, useRef, useState } from "react";
import { ArrowUpRight, ChevronDown, LogOut, Menu, User, Award, X } from "lucide-react";
import { api } from "../../api";
import "../../styles/player-home.css";
import "../../styles/community.css";

export default function PlayerLayout({
  navigation,
  activeId,
  onNavigate,
  onLogout,
  accountName,
  children,
}) {
  const [expanded, setExpanded] = useState(false);
  const [userMenuOpen, setUserMenuOpen] = useState(false);
  const [headerProfile, setHeaderProfile] = useState(null);
  const menu = useRef(null);
  const toggle = useRef(null);
  const userMenuRef = useRef(null);

  useEffect(() => {
    if (!onLogout) return;
    api.getMyProfile().then((p) => p && setHeaderProfile(p)).catch(() => {});
    const onUpdated = (e) => {
      if (e.detail) setHeaderProfile(e.detail);
    };
    window.addEventListener("tksc:profile-updated", onUpdated);
    return () => window.removeEventListener("tksc:profile-updated", onUpdated);
  }, [onLogout]);

  useEffect(() => {
    if (expanded) menu.current?.querySelector("button")?.focus();
  }, [expanded]);

  useEffect(() => {
    function handleOutsideClick(e) {
      if (userMenuRef.current && !userMenuRef.current.contains(e.target)) {
        setUserMenuOpen(false);
      }
    }
    document.addEventListener("mousedown", handleOutsideClick);
    return () => document.removeEventListener("mousedown", handleOutsideClick);
  }, []);

  function navigate(id) {
    setExpanded(false);
    setUserMenuOpen(false);
    onNavigate(id);
  }

  return (
    <div className="player-site" id="home">
      <a className="skip-link" href="#main-content">
        Bỏ qua điều hướng
      </a>
      <header
        className="player-header"
        onKeyDown={(event) => {
          if (event.key === "Escape") {
            if (expanded) {
              setExpanded(false);
              toggle.current?.focus();
            }
            if (userMenuOpen) {
              setUserMenuOpen(false);
            }
          }
        }}
      >
        <div className="player-container player-header-top">
          <a
            href="/player/home"
            className="player-brand"
            aria-label="TK Sport Community — Trang chủ"
            onClick={(event) => {
              event.preventDefault();
              navigate("overview");
            }}
          >
            <span className="player-brand-mark">
              <ArrowUpRight size={27} aria-hidden="true" />
            </span>
            <span>
              TK SPORT<small>COMMUNITY</small>
            </span>
          </a>
          <div className="player-account">
            {onLogout && <NotificationBell onTickets={() => navigate("Vé của tôi")} />}
            {accountName && (
              <div className="player-user-dropdown-wrap" ref={userMenuRef}>
                <button
                  type="button"
                  className={`player-user-trigger ${userMenuOpen ? "is-open" : ""}`}
                  aria-expanded={userMenuOpen}
                  title="Bấm để mở Hồ sơ cá nhân & Hồ sơ kỹ năng"
                  onClick={() => setUserMenuOpen((v) => !v)}
                >
                  <span className="player-user-trigger-avatar">
                    {headerProfile?.avatarUrl ? (
                      <img src={headerProfile.avatarUrl} alt={headerProfile.fullName || accountName} />
                    ) : (
                      (headerProfile?.fullName || accountName).charAt(0).toUpperCase()
                    )}
                  </span>
                  <span style={{ maxWidth: "180px", overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>
                    {headerProfile?.fullName || accountName}
                  </span>
                  <ChevronDown size={15} />
                </button>

                {userMenuOpen && (
                  <div className="player-user-menu" role="menu">
                    <div className="player-user-menu-header">
                      <strong>{accountName}</strong>
                      <span>Tài khoản Người chơi</span>
                    </div>
                    <button
                      type="button"
                      role="menuitem"
                      className={`player-user-menu-item ${activeId === "Hồ sơ cá nhân" ? "is-active" : ""}`}
                      onClick={() => navigate("Hồ sơ cá nhân")}
                    >
                      <span className="player-user-menu-icon">
                        <User size={16} />
                      </span>
                      <span>Hồ sơ cá nhân</span>
                    </button>
                    <button
                      type="button"
                      role="menuitem"
                      className={`player-user-menu-item ${activeId === "Hồ sơ kỹ năng" ? "is-active" : ""}`}
                      onClick={() => navigate("Hồ sơ kỹ năng")}
                    >
                      <span className="player-user-menu-icon">
                        <Award size={16} />
                      </span>
                      <span>Hồ sơ kỹ năng</span>
                    </button>
                  </div>
                )}
              </div>
            )}
            {onLogout && (
              <button className="player-logout" onClick={onLogout}>
                <LogOut size={17} aria-hidden="true" />
                <span>Đăng xuất</span>
              </button>
            )}
            <button
              ref={toggle}
              className="player-menu-toggle"
              aria-label={expanded ? "Đóng menu" : "Mở menu"}
              aria-expanded={expanded}
              aria-controls="player-navigation"
              onClick={() => setExpanded(!expanded)}
            >
              {expanded ? <X size={22} /> : <Menu size={22} />}
            </button>
          </div>
        </div>
        <nav
          ref={menu}
          id="player-navigation"
          className={`player-navigation${expanded ? " is-open" : ""}`}
          aria-label="Điều hướng chính"
        >
          <div className="player-container">
            {navigation.map((item) => (
              <button
                key={item.id}
                aria-current={activeId === item.id ? "page" : undefined}
                onClick={() => navigate(item.id)}
              >
                {item.label}
              </button>
            ))}
          </div>
        </nav>
      </header>
      <main id="main-content" tabIndex={-1}>
        {children}
      </main>
      {onLogout && (
        <FloatingChatBubble onOpenCommunity={() => navigate("Cộng đồng")} />
      )}
      <footer className="player-footer">
        <div className="player-container">
          <strong>
            TK SPORT <span>COMMUNITY</span>
          </strong>
          <p>Cùng đam mê. Cùng ra sân.</p>
          <a href="#home">Về đầu trang ↑</a>
        </div>
      </footer>
    </div>
  );
}
