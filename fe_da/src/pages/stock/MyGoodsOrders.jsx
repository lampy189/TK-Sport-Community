import { useEffect, useState } from "react";
import { PackageCheck } from "lucide-react";
import SectionLayout from "../../components/layout/SectionLayout";
import { workflowRequest } from "../workflowClient";
import "./stock.css";

const root = "/api/v1/commerce/my-goods-orders";
const money = (value) =>
  new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND" }).format(value);

function orderState(order) {
  if (order.handoverState === "Received") return { label: "Đã nhận hàng", className: "badge-lime" };
  if (order.handoverState === "Delivered") return { label: "Đã giao, chờ bạn xác nhận", className: "stock-badge-warning" };
  if (order.paymentState === "Paid") return { label: "Đã thanh toán, chờ giao", className: "badge-gray" };
  return { label: "Chờ thanh toán", className: "badge-gray" };
}

// UC-31 (SWL 3 "Receive order"): the buyer confirms receipt; the system then deducts stock.
export default function MyGoodsOrders() {
  const [orders, setOrders] = useState(null);
  const [error, setError] = useState("");
  const [busyId, setBusyId] = useState(null);

  useEffect(() => {
    let active = true;
    workflowRequest(root)
      .then((list) => active && setOrders(list))
      .catch((e) => {
        if (!active) return;
        setOrders([]);
        setError(e.message);
      });
    return () => {
      active = false;
    };
  }, []);

  async function receive(id) {
    setBusyId(id);
    setError("");
    try {
      const updated = await workflowRequest(`${root}/${id}/receive`, "POST");
      setOrders((list) => list.map((o) => (o.id === updated.id ? updated : o)));
    } catch (e) {
      setError(e.message);
    } finally {
      setBusyId(null);
    }
  }

  if (!orders) return null;
  return (
    <SectionLayout title="Đơn hàng của tôi" description="Theo dõi đơn đã đặt và xác nhận khi nhận được hàng." className="stock-page my-orders">
      {error && (
        <p className="stock-error" role="alert">
          {error}
        </p>
      )}
      {orders.length === 0 ? (
        <p className="stock-hint">Bạn chưa có đơn hàng nào.</p>
      ) : (
        <ul className="my-order-list">
          {orders.map((order) => {
            const state = orderState(order);
            return (
              <li key={order.id}>
                <div className="my-order-head">
                  <strong>{order.orderCode}</strong>
                  <span className={state.className}>{state.label}</span>
                </div>
                <p>{order.lines.map((l) => `${l.quantity} × ${l.name}`).join(", ")}</p>
                <div className="my-order-foot">
                  <strong>{money(order.totalAmount)}</strong>
                  {order.handoverState === "Delivered" && (
                    <button className="button button-primary" disabled={busyId === order.id} onClick={() => receive(order.id)}>
                      <PackageCheck size={16} aria-hidden="true" />
                      {busyId === order.id ? "Đang xác nhận…" : "Nhận hàng"}
                    </button>
                  )}
                </div>
              </li>
            );
          })}
        </ul>
      )}
    </SectionLayout>
  );
}
