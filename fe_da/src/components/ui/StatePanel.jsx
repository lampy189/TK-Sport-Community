import { CircleAlert, SearchX, LoaderCircle } from "lucide-react";

export default function StatePanel({
  kind = "empty",
  title,
  description,
  action,
}) {
  const Icon =
    kind === "error"
      ? CircleAlert
      : kind === "loading"
        ? LoaderCircle
        : SearchX;
  return (
    <div
      className={`state-panel state-${kind}`}
      role={kind === "error" ? "alert" : "status"}
    >
      <span className="state-icon">
        <Icon size={28} aria-hidden="true" />
      </span>
      <h2>{title}</h2>
      {description && <p>{description}</p>}
      {action}
    </div>
  );
}
