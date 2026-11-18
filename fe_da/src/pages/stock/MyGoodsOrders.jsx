import PaginatedList from '../../components/ui/PaginatedList';
import { useEffect, useState } from "react";
import { PackageCheck } from "lucide-react";
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

// Receipt confirmation follows staff handover; stock was posted at delivery.
export default function MyGoodsOrders() {
  const [orders, setOrders] = useState(null);
  const [error, setError] = useState("");
  const [busyId, setBusyId] = useState(null);
  const [filter, setFilter] = useState('all');
  const [query, setQuery] = useState('');
  const [detail, setDetail] = useState(null);
  async function inspect(id) {
    setBusyId(id); setError(''); setDetail(null);
    try { setDetail(await workflowRequest(root+'/'+id)); }
    catch(e) { setError(e.message); } finally { setBusyId(null); }
  }

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

  if (!orders) return <p role="status">Đang tải đơn hàng…</p>;
  const visible=orders.filter(o=>(filter==='all'||[o.status,o.paymentState,o.handoverState].includes(filter))&&[o.orderCode,...o.lines.map(l=>l.name)].join(' ').toLocaleLowerCase('vi').includes(query.toLocaleLowerCase('vi')));
  return (
    <SectionLayout title="Đơn hàng của tôi" description="Theo dõi đơn đã đặt và xác nhận khi nhận được hàng." className="stock-page my-orders">
      {error && (
        <p className="stock-error" role="alert">
          {error}
        </p>
      )}
      <div className="workflow-actions">
        <input aria-label="Tìm đơn hàng" placeholder="Mã đơn, sản phẩm…" value={query} onChange={e=>setQuery(e.target.value)}/>
        <select aria-label="Trạng thái đơn hàng" value={filter} onChange={e=>setFilter(e.target.value)}><option value="all">Tất cả</option><option value="Paid">Đã thanh toán</option><option value="Received">Đã nhận hàng</option><option value="Cancelled">Đã hủy</option><option value="Expired">Hết hạn</option><option value="NeedsReview">Cần đối soát</option></select>
        <button className="button button-secondary" disabled={busyId!==null} onClick={async()=>{setBusyId(0);setError('');try{setOrders(await workflowRequest(root));}catch(e){setError(e.message);}finally{setBusyId(null);}}}>Cập nhật</button>
      </div>
      {visible.length === 0 ? (
        <p className="stock-hint">{orders.length ? 'Không có đơn hàng phù hợp.' : 'Bạn chưa có đơn hàng nào.'}</p>
      ) : (
        <ul className="my-order-list">
          {<PaginatedList items={visible} listItem>{(order) => {
            const state = orderState(order);
            return (
              <li key={order.id} id={`goods-order-${order.id}`} ref={node => { if (node && String(order.id) === new URLSearchParams(window.location.search).get('goodsOrderId')) node.scrollIntoView({ block: 'center' }); }}>
                <div className="my-order-head">
                  <strong>{order.orderCode}</strong>
                  <span className={state.className}>{state.label}</span>
                </div>
                <p>{order.lines.map((l) => `${l.quantity} × ${l.name}`).join(", ")}</p>
                <button className="button button-secondary" disabled={busyId!==null} onClick={()=>inspect(order.id)}>Chi tiết / chứng từ</button>
                {detail?.id===order.id && <section aria-label="Chi tiết đơn hàng">
                  <p>Người mua: {detail.customerName || 'Chưa ghi nhận'} · Phương thức: {detail.paymentMethod || 'Chưa ghi nhận'}</p>
                  {detail.referenceCode && <p>Mã thanh toán: <code>{detail.referenceCode}</code></p>}
                  {detail.invoice ? <div><h4>Hóa đơn {detail.invoice.code}</h4><p>{new Date(detail.invoice.issuedAt).toLocaleString('vi-VN',{timeZone:'Asia/Ho_Chi_Minh'})} · {money(detail.invoice.totalAmount)}</p></div>:<p>Chưa xuất hóa đơn.</p>}
                  <p>{orderState(detail).label}</p>
                  {!!detail.history?.length && <ol aria-label="Diễn biến đơn hàng">{detail.history.map(h=><li key={h.sequence}>{h.label}</li>)}</ol>}
                </section>}
                <div className="my-order-foot">
                  <strong>{money(order.totalAmount)}</strong>
                  {order.canCancel && (
                    <button className="button button-secondary" disabled={busyId === order.id} onClick={() => cancel(order.id)}>
                      {busyId === order.id ? "Đang hủy…" : "Hủy đơn"}
                    </button>
                  )}
                  {order.handoverState === "Delivered" && (
                    <button className="button button-primary" disabled={busyId === order.id} onClick={() => receive(order.id)}>
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
    </SectionLayout>
  );
}
