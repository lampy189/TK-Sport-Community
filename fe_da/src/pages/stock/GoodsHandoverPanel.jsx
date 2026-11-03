import { useState } from "react";
import { CheckCircle2, PackageCheck, Truck } from "lucide-react";
import { workflowRequest } from "../workflowClient";
import "./stock.css";

const root = "/api/v1/commerce/goods-orders";
const steps = [
  { key: "Delivered", label: "Giao hàng", icon: Truck },
  { key: "Received", label: "Khách nhận hàng", icon: PackageCheck },
  { key: "Completed", label: "Trừ tồn kho", icon: CheckCircle2 },
];

// UC-31: last three cells of SWL 3 — Delivery -> Receive order -> Update Stock.
export default function GoodsHandoverPanel({ order, onUpdated }) {
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const state = order.handoverState;
  const reached = {
    Delivered: state === "Delivered" || state === "Received",
    Received: state === "Received",
    Completed: state === "Received" && order.status === "Completed",
  };

  async function run(action) {
    setBusy(true);
    setError("");
    try {
      onUpdated(await workflowRequest(`${root}/${order.id}/${action}`, "POST"));
    } catch (e) {
      setError(e.message);
    } finally {
      setBusy(false);
    }
  }

  return (
    <section className="handover" aria-label="Giao nhận hàng">
      <h3>Giao nhận hàng</h3>
      <ol className="handover-steps">
        {steps.map(({ key, label, icon: Icon }) => (
          <li key={key} className={reached[key] ? "is-done" : ""}>
            <Icon size={16} aria-hidden="true" />
            {label}
          </li>
        ))}
      </ol>
      {!order.invoice ? (
        <p className="stock-hint">Chỉ giao hàng sau khi đơn đã thanh toán và xuất hóa đơn.</p>
      ) : !state ? (
        <button className="button button-primary" disabled={busy} onClick={() => run("deliver")}>
          <Truck size={16} aria-hidden="true" /> {busy ? "Đang ghi nhận…" : "Giao hàng"}
        </button>
      ) : state === "Delivered" ? (
        <>
          <p className="stock-hint">
            Xác nhận khi khách đã cầm hàng. Hệ thống trừ tồn kho đúng một lần theo hóa đơn.
          </p>
          <button className="button button-primary" disabled={busy} onClick={() => run("receive")}>
            <PackageCheck size={16} aria-hidden="true" /> {busy ? "Đang chốt đơn…" : "Xác nhận khách đã nhận hàng"}
          </button>
        </>
      ) : (
        <p className="stock-notice" role="status">
          Khách đã nhận hàng, tồn kho đã được trừ. Đơn hoàn tất.
        </p>
      )}
      {error && (
        <p className="stock-error" role="alert">
          {error}
        </p>
      )}
    </section>
  );
}
