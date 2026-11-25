import { PackageCheck, Truck } from "lucide-react";
import "./stock.css";

export default function GoodsHandoverPanel({ order, canDeliver, busy, onAction }) {
  const state = order.handoverState;
  if (!canDeliver && state !== "Delivered" && state !== "Received") return null;

  return (
    <section className="handover" aria-label="Giao nhận hàng">
      <h3>Giao nhận hàng</h3>
      {canDeliver ? (
        <button type="button" className="button button-primary" disabled={busy} onClick={() => onAction("deliver")}>
          <Truck size={16} aria-hidden="true" /> Đã giao đơn
        </button>
      ) : state === "Delivered" ? (
        <>
          <p className="stock-hint">Đã giao, chờ xác nhận nhận hàng.</p>
          <button type="button" className="button button-secondary" disabled={busy} onClick={() => onAction("receive")}>
            <PackageCheck size={16} aria-hidden="true" /> Xác nhận khách đã nhận hàng
          </button>
        </>
      ) : (
        <p className="stock-notice" role="status">
          Khách đã nhận hàng. Đơn hoàn tất.
        </p>
      )}
    </section>
  );
}
