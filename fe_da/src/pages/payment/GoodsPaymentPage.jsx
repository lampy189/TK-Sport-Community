import { useEffect, useState } from "react";
import { QRCodeSVG } from "qrcode.react";
import SectionLayout from "../../components/layout/SectionLayout";
import { workflowRequest } from "../workflowClient";
import "../workflow.css";
import GoodsHandoverPanel from "../stock/GoodsHandoverPanel";

const root = "/api/v1/commerce/goods-orders";
const money = n => `${Number(n).toLocaleString("vi-VN")} ₫`;
const stateLabel = { Cart: "Giỏ hàng", PendingPayment: "Chờ chuyển khoản", Paid: "Đã thanh toán", Invoiced: "Đã xuất hóa đơn", Completed: "Hoàn tất",NeedsReview: "Cần đối soát", Pending: "Chờ thanh toán", Unpaid: "Chưa thanh toán" };

export default function GoodsPaymentPage() {
  const [orders, setOrders] = useState([]);
  const [order, setOrder] = useState(null);
  const [received, setReceived] = useState("");
  const [cashAcknowledged, setCashAcknowledged] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");
  useEffect(() => {
    let active = true;
    async function load() {
      try {
        const list = await workflowRequest(root);
        if (active) setOrders(list);
        const id = new URLSearchParams(window.location.search).get("goodsOrderId");
        if (id && /^\d+$/.test(id)) {
          const detail = await workflowRequest(`${root}/${id}`);
          if (active) setOrder(detail);
        }
      } catch (e) { if (active) setError(e.message); }
    }
    load();
    return () => { active = false; };
  }, []);

  async function run(action) {
    setBusy(true); setError(""); setMessage("");
    try { await action(); } catch (e) { setError(e.message); } finally { setBusy(false); }
  }
  async function select(id) {
    setOrder(null); setReceived(""); setCashAcknowledged(false);
    if (id) await run(async () => setOrder(await workflowRequest(`${root}/${id}`)));
  }
  async function mutate(action, body) {
    await run(async () => {
      const updated = await workflowRequest(`${root}/${order.id}/${action}`, "POST", body);
      setOrder(updated);
      setOrders(list => list.map(o => o.id === updated.id ? updated : o));
      setMessage(updated.paymentState === "Paid" ? "Đã xác nhận đủ tiền cho đơn hàng hóa." :
        updated.paymentState === "NeedsReview" ? "Giao dịch cần đối soát. Đơn chưa được xác nhận thanh toán." :
        action === "confirm-transfer" ? "Chưa có giao dịch đủ điều kiện. Hãy kiểm tra lại sau khi khách chuyển khoản." : "Đã tạo hướng dẫn chuyển khoản.");
    });
  }
  const cart = order?.status?.toLowerCase() === "cart";
  const paid = order?.paymentState === "Paid";
  return <SectionLayout title="Thanh toán hàng hóa" description="Nhận Cart từ bán hàng, thu tiền và xuất hóa đơn.">
    <div className="workflow">
      {error && <p className="workflow-error" role="alert">{error}</p>}
      {message && <p role="status">{message}</p>}
      <div className="workflow-actions"><button className="button button-secondary" disabled={busy} onClick={() => run(async () => {
        setOrders(await workflowRequest(root));
        if (order) setOrder(await workflowRequest(`${root}/${order.id}`));
      })}>Làm mới danh sách</button></div>
      <label>Đơn hàng hóa<select disabled={busy} value={order?.id ?? ""} onChange={e => select(e.target.value)}>
        <option value="">Chọn đơn đã lập tại quầy</option>{orders.map(o => <option key={o.id} value={o.id}>{o.orderCode} · {money(o.totalAmount)} · {stateLabel[o.status] || o.status}</option>)}
      </select></label>
      {!orders.length && <p>Chưa có đơn hàng hóa. Lập Cart trong luồng bán hàng trước khi thu tiền.</p>}
      {order && <>
        <h3>{order.orderCode} · {order.customerName || "Khách tại quầy"}</h3>
        <p>{stateLabel[order.status] || order.status} · {stateLabel[order.paymentState] || order.paymentState || "Chưa thanh toán"}</p>
        <div className="workflow-table"><table><thead><tr><th>Sản phẩm</th><th>Số lượng</th><th>Đơn giá</th><th>Thành tiền</th></tr></thead><tbody>
          {order.lines.map((line, i) => <tr key={i}><td>{line.name}</td><td>{line.quantity}</td><td>{money(line.unitPriceVnd)}</td><td>{money(line.subtotalVnd)}</td></tr>)}
        </tbody></table></div><strong>Tổng thanh toán: {money(order.totalAmount)}</strong>
        {cart && <>
          <button className="button button-primary" disabled={busy} onClick={() => mutate("payment-instructions")}>Tạo mã chuyển khoản</button>
          <fieldset disabled={busy}><legend>Thanh toán tiền mặt</legend>
            <label>Số tiền thực thu cho đơn (VND)<input type="number" min="1" step="1" value={received} onChange={e => setReceived(e.target.value)} /></label>
            <label><span><input style={{ width: "auto" }} type="checkbox" checked={cashAcknowledged} onChange={e => setCashAcknowledged(e.target.checked)} /> Tôi đã nhận đủ tiền mặt cho đơn này.</span></label>
          </fieldset>
          <button className="button button-secondary" disabled={busy || !cashAcknowledged || Number(received) !== order.totalAmount} onClick={() => mutate("confirm-cash", { receivedAmount: Number(received) })}>Xác nhận đã nhận tiền mặt</button>
        </>}
        {order.referenceCode && <p>Mã thanh toán: <strong>{order.referenceCode}</strong></p>}
        {order.checkoutUrl && <>
          <p>Quét mã để mở trang thanh toán payOS và dùng mã QR ngân hàng trên trang đó.</p>
          <QRCodeSVG value={order.checkoutUrl} size={200} title="Mở thanh toán hàng hóa trên payOS" />
          <a href={order.checkoutUrl} target="_blank" rel="noreferrer">Mở hướng dẫn chuyển khoản</a>
          {order.expiresAt && <p>Hạn thanh toán: {new Date(order.expiresAt).toLocaleString("vi-VN", { timeZone: "Asia/Ho_Chi_Minh" })}</p>}
        </>}
        {!paid && order.paymentMethod === "payos" && <div className="workflow-actions">
          {order.paymentState === "Pending" && !order.checkoutUrl && <button className="button button-secondary" disabled={busy} onClick={() => mutate("payment-instructions")}>Lấy lại hướng dẫn chuyển khoản</button>}
          <button className="button button-primary" disabled={busy || order.paymentState === "NeedsReview"} onClick={() => mutate("confirm-transfer")}>Kiểm tra chuyển khoản</button>
        </div>}
        {paid && !order.invoice && <button className="button button-primary" disabled={busy} onClick={() => mutate("invoice")}>Xuất hóa đơn</button>}
        {order.invoice && <article aria-label="Hóa đơn bán hàng">
          <h3>Hóa đơn {order.invoice.code}</h3>
          <p>Ngày xuất: {new Date(order.invoice.issuedAt).toLocaleString("vi-VN", { timeZone: "Asia/Ho_Chi_Minh" })}</p>
          <p>Khách hàng: {order.invoice.customerName || "Khách tại quầy"}</p>
          <p>Đã thanh toán {money(order.invoice.totalAmount)} · {order.invoice.method === "cash" ? "Tiền mặt" : "Chuyển khoản"}</p>
          <div className="workflow-table"><table><thead><tr><th>Sản phẩm</th><th>Số lượng</th><th>Thành tiền</th></tr></thead><tbody>{order.invoice.lines.map((line, i) => <tr key={i}><td>{line.name}</td><td>{line.quantity}</td><td>{money(line.subtotalVnd)}</td></tr>)}</tbody></table></div>
          <button className="button button-secondary" onClick={() => {
            const invoice = order.invoice;
            const text = [`TKSC — HÓA ĐƠN ${invoice.code}`, `Đơn: ${invoice.orderCode}`, `Ngày: ${new Date(invoice.issuedAt).toLocaleString("vi-VN", { timeZone: "Asia/Ho_Chi_Minh" })}`, `Khách: ${invoice.customerName || "Khách tại quầy"}`,
              ...invoice.lines.map(l => `${l.name} | ${l.quantity} x ${money(l.unitPriceVnd)} | ${money(l.subtotalVnd)}`), `Tổng: ${money(invoice.totalAmount)}`, `Thanh toán: ${invoice.method}`, `Mã: ${invoice.paymentReference}`].join("\n");
            const url = URL.createObjectURL(new Blob(["\uFEFF", text], { type: "text/plain;charset=utf-8" }));
            const a = document.createElement("a"); a.href = url; a.download = `${invoice.code}.txt`; a.click();
            window.setTimeout(() => URL.revokeObjectURL(url), 1000);
          }}>Tải hóa đơn</button>
        </article>}
        <GoodsHandoverPanel order={order} onUpdated={updated => {
          setOrder(updated);
          setOrders(list => list.map(o => o.id === updated.id ? updated : o));
        }} />
      </>}
    </div>
  </SectionLayout>;
}
