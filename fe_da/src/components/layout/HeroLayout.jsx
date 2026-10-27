export default function HeroLayout({
  eyebrow,
  title,
  description,
  action,
  aside,
}) {
  return (
    <section className="hero-layout" aria-labelledby="hero-title">
      <div className="hero-copy">
        <span className="eyebrow">{eyebrow}</span>
        <h1 id="hero-title">{title}</h1>
        <p>{description}</p>
        {action}
        <div className="hero-sports">
          <span>Cầu lông</span>
          <i aria-hidden="true" />
          <span>Pickleball</span>
          <span className="hero-sports-note">Một cộng đồng. Cùng đam mê.</span>
        </div>
      </div>
      <div className="hero-aside">
        {aside || (
          <div className="court-art" aria-hidden="true">
            <div className="court-lines" />
            <span className="court-ball" />
            <span className="court-caption">
              <b>YOUR COURT. YOUR COMMUNITY.</b>
              <span>Sẵn sàng cho lần ra sân tiếp theo.</span>
            </span>
          </div>
        )}
      </div>
    </section>
  );
}
