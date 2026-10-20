import { useId } from "react";

export default function SectionLayout({
  title,
  description,
  action,
  children,
  className = "",
}) {
  const titleId = useId();
  return (
    <section
      className={`section-layout ${className}`}
      aria-labelledby={titleId}
    >
      <div className="section-heading">
        <div>
          <h2 id={titleId}>{title}</h2>
          {description && <p>{description}</p>}
        </div>
        {action}
      </div>
      {children}
    </section>
  );
}
