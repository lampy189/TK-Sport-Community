export default function Navbar({ items, activeId, onNavigate }) {
  return (
    <nav className="site-nav" aria-label="Điều hướng chính">
      <div className="container nav-inner">
        {items.map((item) => (
          <button
            key={item.id}
            type="button"
            aria-current={activeId === item.id ? "page" : undefined}
            className={activeId === item.id ? "nav-link is-active" : "nav-link"}
            onClick={() => onNavigate(item.id)}
          >
            {item.label}
          </button>
        ))}
      </div>
    </nav>
  );
}
