import { useEffect, useState } from "react";
import { ArrowUpRight, ArrowUp, Menu, X } from "lucide-react";
import "../../styles/guest.css";

export default function GuestLayout({
  isOverview,
  onHome,
  onBrowse,
  onLogin,
  children,
}) {
  const [menuOpen, setMenuOpen] = useState(false);
  const [section, setSection] = useState(null);

  useEffect(() => {
    if (menuOpen) document.querySelector("#guest-navigation a")?.focus();
  }, [menuOpen]);

  useEffect(() => {
    if (!isOverview || !section) return;
    const target = document.getElementById(section);
    target?.scrollIntoView({ block: "start" });
    target?.focus({ preventScroll: true });
  }, [isOverview, section]);

  function home(event) {
    event.preventDefault();
    setMenuOpen(false);
    setSection(null);
    onHome();
  }
  function browse() {
    setMenuOpen(false);
    setSection(null);
    onBrowse();
  }
  function jump(event, id) {
    setMenuOpen(false);
    if (isOverview) return;
    event.preventDefault();
    setSection(id);
    onHome();
  }

  return (
    <div className="guest-site" id="home">
      <a className="skip-link" href="#main-content">
        Bỏ qua điều hướng
      </a>
      <header
        className="guest-header"
        onKeyDown={(event) => {
          if (event.key === "Escape" && menuOpen) {
            setMenuOpen(false);
            document.getElementById("guest-menu-toggle")?.focus();
          }
        }}
      >
        <div className="guest-container guest-header-inner">
          <a
            className="guest-brand"
            href="/guest/home"
            onClick={home}
            aria-label="TK Sport Community — Trang chủ"
          >
            <span className="guest-brand-mark">
              <ArrowUpRight size={27} strokeWidth={2.1} />
            </span>
            <span>
              TK SPORT<small>COMMUNITY</small>
            </span>
          </a>
          <nav
            className={`guest-nav${menuOpen ? " is-open" : ""}`}
            id="guest-navigation"
            aria-label="Điều hướng chính"
          >
            <a
              href="/guest/home"
              onClick={home}
              aria-current={isOverview ? "page" : undefined}
            >
              Trang chủ
            </a>
            <a
              href="#guest-sports"
              onClick={(event) => jump(event, "guest-sports")}
            >
              Bộ môn
            </a>
            <a
              href="#guest-community"
              onClick={(event) => jump(event, "guest-community")}
            >
              Cộng đồng
            </a>
            <button
              onClick={browse}
              aria-current={!isOverview ? "page" : undefined}
            >
              Lịch social
            </button>
          </nav>
          <div className="guest-header-actions">
            <button className="guest-login" onClick={onLogin}>
              Đăng nhập <ArrowUpRight size={16} aria-hidden="true" />
            </button>
            <button
              id="guest-menu-toggle"
              className="guest-menu-toggle"
              aria-label={menuOpen ? "Đóng menu" : "Mở menu"}
              aria-expanded={menuOpen}
              aria-controls="guest-navigation"
              onClick={() => setMenuOpen(!menuOpen)}
            >
              {menuOpen ? <X size={22} /> : <Menu size={22} />}
            </button>
          </div>
        </div>
      </header>
      <main
        id="main-content"
        className={
          isOverview ? "guest-main" : "guest-container guest-workspace"
        }
        tabIndex={-1}
      >
        {children}
      </main>
      <footer className="guest-footer">
        <div className="guest-container">
          <div className="guest-footer-top">
            <div className="guest-footer-identity">
              <a className="guest-brand" href="/guest/home" onClick={home}>
                <span className="guest-brand-mark">
                  <ArrowUpRight size={27} />
                </span>
                <span>
                  TK SPORT<small>COMMUNITY</small>
                </span>
              </a>
              <p>Cùng đam mê. Cùng ra sân.</p>
            </div>
            <a className="guest-top-link" href="#home">
              Về đầu trang <ArrowUp size={16} aria-hidden="true" />
            </a>
          </div>
          <div className="guest-footer-bottom">
            <span>Cầu lông & Pickleball · TK Sport Community</span>
            <nav aria-label="Cổng đăng nhập">
              <a href="/staff/login">Đội ngũ vận hành</a>
              <a href="/admin/login">Quản trị hệ thống</a>
            </nav>
          </div>
        </div>
      </footer>
    </div>
  );
}
