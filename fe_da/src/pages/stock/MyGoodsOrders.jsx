import PaginatedList from '../../components/ui/PaginatedList';
import { useEffect, useRef, useState } from "react";
import { PackageCheck } from "lucide-react";
import { QRCodeSVG } from "qrcode.react";
import SectionLayout from "../../components/layout/SectionLayout";
import { workflowRequest } from "../workflowClient";
import "./stock.css";

const root = "/api/v1/commerce/my-goods-orders";
const money = (value) =>
  new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND" }).format(value);

function orderState(order) {
  const terminal = {Cancelled:'Đã hủy',Expired:'Hết hạn',Failed:'Thất bại',NeedsReview:'Cần đối soát'};
  if (terminal[order.status] || terminal[order.paymentState]) return {label:terminal[order.status] || terminal[order.paymentState],className:'badge-gray'};
  if (order.handoverState === "Received") return { label: "Đã nhận hàng", className: "badge-lime" };
  if (order.handoverState === "Delivered") return { label: "Đã giao, chờ bạn xác nhận", className: "stock-badge-warning" };
  if (order.paymentState === "Paid") return { label: "Đã thanh toán, chờ giao", className: "badge-gray" };
  return { label: "Chờ thanh toán", className: "badge-gray" };
}

const closed = new Set(['Cancelled', 'Expired', 'Failed', 'NeedsReview']);
function payable(order) {
  if (!order || closed.has(order.status) || closed.has(order.paymentState)) return false;
  if (order.paymentState === 'Paid' || order.handoverState === 'Delivered' || order.handoverState === 'Received') return false;
  return true;
}

// Receipt confirmation follows staff handover; stock was posted at delivery.
export default function MyGoodsOrders({ embedded = false, refreshKey = 0, highlightId = null }) {
  const [orders, setOrders] = useState(null);
  const [error, setError] = useState("");
  const [busyId, setBusyId] = useState(null);
  const [query, setQuery] = useState('');
  const [detail, setDetail] = useState(null);
  const scrolled = useRef("");

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
  }, [refreshKey]);

  async function cancel(id) {
    setBusyId(id);
    setError("");
    try {
      const updated = await workflowRequest(`${root}/${id}/cancel`, "POST");
      setOrders((list) => list.map((o) => (o.id === updated.id ? updated : o)));
    } catch (e) {
      setError(e.message);
    } finally {
      setBusyId(null);
    }
  }

  function remember(updated) {
    setDetail(updated);
    setOrders((list) => list.map((o) => (o.id === updated.id ? { ...o, ...updated } : o)));
  }

  async function pay(id) {
    setBusyId(id);
    setError("");
    try { remember(await workflowRequest(`${root}/${id}/payment-instructions`, "POST")); }
    catch (e) { setError(e.message); }
    finally { setBusyId(null); }
  }

  async function refreshPayment(id) {
    setBusyId(id);
    setError("");
    try { remember(await workflowRequest(`${root}/${id}/refresh`, "POST")); }
    catch (e) { setError(e.message); }
    finally { setBusyId(null); }
  }

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

  const focusId = String(highlightId || new URLSearchParams(window.location.search).get('goodsOrderId') || '');
  useEffect(() => {
    if (!orders || !focusId) return;
    const mark = `${refreshKey}:${focusId}`;
    if (scrolled.current === mark) return;
    const node = document.getElementById(`goods-order-${focusId}`);
    if (!node) return;
    scrolled.current = mark;
    node.scrollIntoView({ block: 'nearest' });
  }, [orders, focusId, refreshKey]);

  if (!orders) return <p role="status">Đang tải đơn hàng…</p>;
  const visible=orders.filter(o=>[o.orderCode,...o.lines.map(l=>l.name)].join(' ').toLocaleLowerCase('vi').includes(query.toLocaleLowerCase('vi')));
  const body = (
    <>
      {error && (
        <p className="stock-error" role="alert">
          {error}
        </p>
      )}
      <div className="workflow-actions store-order-tools">
        <input aria-label="Tìm đơn hàng" placeholder="Mã đơn, sản phẩm…" value={query} onChange={e=>setQuery(e.target.value)}/>
        <button type="button" className="button button-secondary" disabled={busyId!==null} onClick={async()=>{setBusyId(0);setError('');try{setOrders(await workflowRequest(root));}catch(e){setError(e.message);}finally{setBusyId(null);}}}>Cập nhật</button>
      </div>
      {visible.length === 0 ? (
        <p className="stock-hint">{orders.length ? 'Không có đơn hàng phù hợp.' : 'Bạn chưa có đơn hàng nào.'}</p>
      ) : (
        <ul className="my-order-list">
          {<PaginatedList items={visible} listItem pageSize={embedded ? 5 : undefined}>{(order) => {
            const state = orderState(order);
            const open = detail?.id === order.id;
            return (
              <li key={order.id} id={`goods-order-${order.id}`} className={String(order.id) === focusId ? 'is-current' : undefined}>
                <div className="my-order-head">
                  <strong>{order.orderCode}</strong>
                  <span className={state.className}>{state.label}</span>
                </div>
                <p>{order.lines.map((l) => `${l.quantity} × ${l.name}`).join(", ")}</p>
                {open && (detail.checkoutUrl || detail.invoice) && <section aria-label="Chứng từ thanh toán">
                  {detail.invoice && <div><h4>Hóa đơn {detail.invoice.code}</h4><p>{new Date(detail.invoice.issuedAt).toLocaleString('vi-VN',{timeZone:'Asia/Ho_Chi_Minh'})} · {money(detail.invoice.totalAmount)}</p></div>}
                  {detail.checkoutUrl && <div className="store-pay-box">
                    <QRCodeSVG value={detail.checkoutUrl} size={132} title="Mã thanh toán đơn hàng" />
                    <a href={detail.checkoutUrl} target="_blank" rel="noreferrer">Mở trang thanh toán</a>
                    {detail.expiresAt && <p>Hạn thanh toán: {new Date(detail.expiresAt).toLocaleString('vi-VN', { timeZone: 'Asia/Ho_Chi_Minh' })}</p>}
                  </div>}
                </section>}
                <div className="my-order-foot">
                  <strong>{money(order.totalAmount)}</strong>
                  {payable(order) && (
                    <button type="button" className="button button-primary" disabled={busyId !== null} onClick={() => pay(order.id)}>
                      Thanh toán
                    </button>
                  )}
                  {open && detail.paymentMethod === 'payos' && detail.paymentState !== 'Paid' && (
                    <button type="button" className="button button-secondary" disabled={busyId !== null} onClick={() => refreshPayment(order.id)}>Kiểm tra chuyển khoản</button>
                  )}
                  {order.canCancel && (
                    <button type="button" className="button button-secondary" disabled={busyId === order.id} onClick={() => cancel(order.id)}>
                      {busyId === order.id ? "Đang hủy…" : "Hủy đơn"}
                    </button>
                  )}
                  {order.handoverState === "Delivered" && (
                    <button type="button" className="button button-primary" disabled={busyId === order.id} onClick={() => receive(order.id)}>
                      <PackageCheck size={16} aria-hidden="true" />
                      {busyId === order.id ? "Đang xác nhận…" : "Nhận hàng"}
                    </button>
                  )}
                </div>
              </li>
            );
          }}</PaginatedList>}
        </ul>
      )}
    </>
  );
  if (embedded) return <div className="store-orders">{body}</div>;
  return (
    <SectionLayout title="Đơn hàng của tôi" description="Theo dõi đơn đã đặt và xác nhận khi nhận được hàng." className="stock-page my-orders">
      {body}
    </SectionLayout>
  );
}
