import ModuleIcon from "../ui/ModuleIcon";

export default function Navbar({ items, activeId, onNavigate }) {
  return (
    <nav className="site-nav" aria-label="Điều hướng chính">
      <div className="container nav-inner">
        <span className="nav-caption">KHÔNG GIAN CỦA BẠN</span>
        {items.map((item) => (
          <button
            key={item.id}
            type="button"
            aria-current={activeId === item.id ? "page" : undefined}
            className={activeId === item.id ? "nav-link is-active" : "nav-link"}
            onClick={() => onNavigate(item.id)}
          >
            <ModuleIcon label={item.label} />
            {item.label}
          </button>
        ))}
        <div className="nav-community">
          <span className="eyebrow">MEET. PLAY. CONNECT.</span>
          <strong>
            Mỗi buổi chơi,
            <br />
            thêm một kết nối.
          </strong>
          <span>Cầu lông & Pickleball</span>
        </div>
      </div>
    </nav>
  );
}
