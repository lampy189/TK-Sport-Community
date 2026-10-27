import { ArrowUpRight, LogIn, LogOut } from "lucide-react";

export default function Header({
  roleLabel,
  accountName,
  onLogout,
  onLogin,
  onHome,
}) {
  return (
    <header className="site-header">
      <div className="container header-inner">
        <a
          className="brand"
          href="#home"
          onClick={onHome}
          aria-label="TK Sport Community - Trang chủ"
        >
          <span className="brand-mark">
            <ArrowUpRight size={27} strokeWidth={2.2} />
          </span>
          <span>
            TK SPORT<small>COMMUNITY</small>
          </span>
        </a>
        <span className="header-tagline">Cùng đam mê. Cùng ra sân.</span>
        <div className="account">
          <span className="avatar" aria-hidden="true">
            {accountName
              ? accountName
                  .trim()
                  .split(/\s+/)
                  .slice(-2)
                  .map((part) => part[0])
                  .join("")
              : "TK"}
          </span>
          <span>
            {accountName || "Chào mừng bạn"}
            <small>{roleLabel}</small>
          </span>
          {onLogout && (
            <button className="account-action" onClick={onLogout}>
              <LogOut size={16} /> <span>Đăng xuất</span>
            </button>
          )}
          {onLogin && (
            <button className="account-action" onClick={onLogin}>
              <LogIn size={16} /> <span>Đăng nhập</span>
            </button>
          )}
        </div>
      </div>
    </header>
  );
}
