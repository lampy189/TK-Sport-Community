export default function Header({ roleLabel, accountName }) {
  return (
    <header className="site-header">
      <div className="container header-inner">
        <a
          className="brand"
          href="#home"
          aria-label="TK Sport Community - Trang chủ"
        >
          <span className="brand-mark">
            TK<span>↗</span>
          </span>
          <span>
            TK SPORT<small>COMMUNITY</small>
          </span>
        </a>
        <span className="header-tagline">Cùng đam mê. Cùng ra sân.</span>
        <div className="account">
          <span className="avatar" aria-hidden="true">
            TK
          </span>
          <span>
            {accountName || "Không gian trải nghiệm"}
            <small>{roleLabel}</small>
          </span>
        </div>
      </div>
    </header>
  );
}
