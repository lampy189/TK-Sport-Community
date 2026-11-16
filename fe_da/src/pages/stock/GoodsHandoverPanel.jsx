import { useState } from "react";
import { CheckCircle2, PackageCheck, Truck, XCircle } from "lucide-react";
import { workflowRequest } from "../workflowClient";
import "./stock.css";

const root = "/api/v1/commerce/goods-orders";
const time = (value) =>
  new Date(value).toLocaleTimeString("vi-VN", { timeZone: "Asia/Ho_Chi_Minh", hour: "2-digit", minute: "2-digit" });
const steps = [
  { key: "Delivered", label: "Giao hàng", icon: Truck },
  { key: "Received", label: "Khách nhận hàng", icon: PackageCheck },
  { key: "Completed", label: "Trừ tồn kho", icon: CheckCircle2 },
];

// UC-31: last three cells of SWL 3 — Delivery -> Receive order -> Update Stock.
// The order holds its goods from creation; an unpaid order can be cancelled (its hold goes back to stock).
export default function GoodsHandoverPanel({ order, onUpdated }) {
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  // Carts from the store start as "Pending"; older rows may be null.
  const state = order.handoverState === "Pending" ? null : order.handoverState;
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
      {order.status === "Cancelled" || order.status === "Expired" ? (
        <p className="stock-hint">
          {order.status === "Cancelled" ? "Đơn đã hủy" : "Đơn đã hết hạn giữ hàng"}; hàng đã trả lại kho.
          {order.paymentState === "NeedsReview" ? " Có khoản tiền về muộn cần đối soát." : ""}
        </p>
      ) : !order.invoice ? (
        <>
          <p className="stock-hint">
            Chỉ giao hàng sau khi đơn đã thanh toán và xuất hóa đơn.
            {order.reservationState === "Held" && order.paymentState !== "Paid" && order.holdExpiresAt
              ? ` Hàng đang được giữ đến ${time(order.holdExpiresAt)}; quá hạn chưa thanh toán sẽ tự trả lại kho.`
              : ""}
          </p>
          {order.canCancel && (
            <button className="button button-secondary" disabled={busy} onClick={() => run("cancel")}>
              <XCircle size={16} aria-hidden="true" /> {busy ? "Đang hủy…" : "Hủy đơn, trả hàng về kho"}
            </button>
          )}
        </>
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
