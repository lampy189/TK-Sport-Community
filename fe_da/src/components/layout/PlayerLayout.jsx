import { useEffect, useRef, useState } from "react";
import { ArrowUpRight, LogOut, Menu, X } from "lucide-react";
import "../../styles/player-home.css";

export default function PlayerLayout({
  navigation,
  activeId,
  onNavigate,
  onLogout,
  accountName,
  children,
}) {
  const [expanded, setExpanded] = useState(false);
  const menu = useRef(null);
  const toggle = useRef(null);
  useEffect(() => {
    if (expanded) menu.current?.querySelector("button")?.focus();
  }, [expanded]);
  function navigate(id) {
    setExpanded(false);
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
          if (event.key === "Escape" && expanded) {
            setExpanded(false);
            toggle.current?.focus();
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
            {accountName && (
              <span className="player-account-name" title={accountName}>
                {accountName}
              </span>
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
