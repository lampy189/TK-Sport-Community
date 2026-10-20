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
      </div>
      <div className="hero-aside">
        {aside || (
          <div className="court-art" aria-hidden="true">
            <div className="court-lines" />
            <span className="court-ball" />
            <span className="court-caption">MEET · PLAY · CONNECT</span>
          </div>
        )}
      </div>
    </section>
  );
}
