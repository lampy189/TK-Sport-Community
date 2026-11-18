import { useEffect, useId } from "react";
import { X } from "lucide-react";

// Modal used by every stock screen: Escape or a click outside closes it.
export default function StockDialog({ title, onClose, children }) {
  const titleId = useId();
  useEffect(() => {
    const onKey = (e) => e.key === "Escape" && onClose();
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [onClose]);
  return (
    <div
      className="modal-overlay"
      onMouseDown={(e) => e.target === e.currentTarget && onClose()}
    >
      <div
        className="modal-content stock-dialog"
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
      >
        <div className="stock-dialog-head">
          <h3 id={titleId}>{title}</h3>
          <button className="stock-icon-button" aria-label="Đóng" onClick={onClose}>
            <X size={18} />
          </button>
        </div>
        {children}
      </div>
    </div>
  );
}
