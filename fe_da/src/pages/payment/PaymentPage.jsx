import { paymentState as deriveState } from './ticketState';
import TicketList from "./TicketList";
import { useEffect, useState } from "react";
import { demoMode, paymentClient } from "./paymentClient";
import "./payment.css";
import {
  ShieldCheck,
  Ticket,
  CreditCard,
  Timer,
} from "lucide-react";
import StatePanel from "../../components/ui/StatePanel";

const money = (value) =>
  new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND" }).format(
    value,
  );
const labels = {
  pending: "Chờ thanh toán",
  paid: "Đã thanh toán",
  failed: "Thanh toán không thành công",
  review_required: "Đang kiểm tra",
  reserved: "Chưa thanh toán",
  confirmed: "Đã xác nhận",
  cancelled: "Đã hủy",
  expired: "Hết hạn thanh toán",
  processing: "Đang chờ xác nhận",
};
const norm = (s) => String(s || "").toLowerCase();
const formatDate = (d) =>
  d
    ? new Date(d).toLocaleDateString("vi-VN", {
        weekday: "short",
        day: "2-digit",
        month: "2-digit",
        year: "numeric",
      })
    : "";
export default function PaymentPage({
  initialView = "payment",
  staff = false,
  onBrowse,
}) {
  const [view, setView] = useState(initialView);
  const [orderId, setOrderId] = useState(
    () =>
      new URLSearchParams(window.location.search).get("orderId") ||
      (demoMode ? "1" : ""),
  );
  const [order, setOrder] = useState(null);
  const [tickets, setTickets] = useState([]);
  const [reviews, setReviews] = useState([]);
  const [gateway, setGateway] = useState(false);
  const [reason, setReason] = useState("");
  const [busy, setBusy] = useState(false);
  const [initialLoading, setInitialLoading] = useState(true);
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const [now, setNow] = useState(() => Date.now());

  useEffect(() => {
    const timer = setInterval(() => setNow(Date.now()), 1000);
    return () => clearInterval(timer);
  }, []);
  useEffect(() => {
    let active = true;
    const load = async () => {
      try {
        if (staff) {
          const data = await paymentClient.reviews();
          if (active) setReviews(data);
        } else if (initialView === "tickets") {
          const data = await paymentClient.tickets();
          if (active) setTickets(data);
        } else {
          const list = await paymentClient.tickets();
          if (active) setTickets(list);
          const id =
            new URLSearchParams(window.location.search).get("orderId") ||
            (demoMode ? "1" : "");
          if (id) {
            const data = await paymentClient.refresh(id);
            if (active) setOrder(data);
          }
        }
      } catch (e) {
        if (active) setError(e.message);
      } finally {
        if (active) setInitialLoading(false);
      }
    };
    load();
    return () => {
      active = false;
    };
  }, [initialView, staff]);

  useEffect(() => {
    if (staff) return;
    let active = true;
    let loading = false;
    async function sync() {
      if (loading || document.hidden || busy) return;
      loading = true;
      try {
        const list = await paymentClient.tickets();
        if (active) setTickets(list);
        if (order?.id) {
          const current = await paymentClient.display(order.id);
          if (active) setOrder(current);
        }
      } catch (e) { if (active) setError(e.message); }
      finally { loading = false; }
    }
    const timer = setInterval(sync, 15000);
    window.addEventListener('focus', sync);
    window.addEventListener('tksc:ticket-changed', sync);
    return () => { active = false; clearInterval(timer); window.removeEventListener('focus', sync); window.removeEventListener('tksc:ticket-changed', sync); };
  }, [staff, order?.id, busy]);

  async function run(action) {
    if (busy) return;
    setBusy(true);
    setError("");
    setNotice("");
    try {
      await action();
    } catch (e) {
      setError(e.message);
    } finally {
      setBusy(false);
    }
  }
  async function refresh() {
    const data = await paymentClient.refresh(order?.id || orderId);
    setOrder(data);
    setNotice("Đã cập nhật trạng thái.");
  }
  async function showTickets() {
    if (order?.id) setOrder(await paymentClient.refresh(order.id));
    setTickets(await paymentClient.tickets());
    setView("tickets");
  }
  async function process() {
    const data = await paymentClient.process(order.id);
    if (data.status === "refresh_required") {
      setOrder(await paymentClient.refresh(order.id));
      setNotice(
        "Đã cập nhật kết quả payOS. Nếu đã trả tiền, vui lòng chờ hệ thống xác minh.",
      );
      return;
    }
    if (demoMode) {
      setOrder(await paymentClient.display(order.id));
      setGateway(true);
    } else {
      const url = new URL(data.paymentUrl);
      if (url.protocol !== "https:")
        throw new Error("Địa chỉ cổng thanh toán không hợp lệ.");
      setNotice("Đang chuyển đến trang thanh toán payOS…");
      window.location.assign(url.href);
    }
  }
  async function simulate(outcome) {
    setOrder(await paymentClient.simulate(outcome));
    setGateway(false);
  }
  const latest = order?.payments?.[0];
  const orderStatus = norm(order?.status);
  const payStatus = norm(latest?.status);
  const seconds = order?.expiresAt
    ? Math.max(0, Math.ceil((Date.parse(order.expiresAt) - now) / 1000))
    : 0;
  const state = order
    ? deriveState(order.status, latest?.status, order.expiresAt, now)
    : "";
  const canPay = ["pending", "processing", "failed"].includes(state) && orderStatus === "pending" && seconds > 0 && payStatus !== "paid";
  const timeLeft = `${Math.floor(seconds / 60)}:${String(seconds % 60).padStart(2, "0")}`;

  const statusMessage = {
    paid: "Thanh toán thành công. Vé của bạn đã sẵn sàng.",
    review_required:
      "Giao dịch đang được kiểm tra. Vui lòng không thanh toán lại.",
    failed: "Giao dịch chưa thành công. Bạn có thể thử lại khi đơn còn hạn.",
    processing:
      "Đã tạo giao dịch. Nếu bạn đã chuyển khoản, bấm “Kiểm tra lại” để cập nhật.",
    expired: "Đơn đã hết thời gian giữ chỗ. Vui lòng đặt lại buổi chơi.",
    cancelled: "Đơn đã bị hủy.",
  }[state];

  return (
    <div className="payment-workspace" aria-busy={busy}>
      <div className="payment-heading">
        <h1>
          {staff
            ? "Thanh toán cần kiểm tra"
            : view === "tickets"
              ? "Vé của tôi"
              : "Thanh toán"}
        </h1>
        {demoMode && (
          <span className="payment-badge">
            <ShieldCheck size={15} />
            Chế độ minh họa
          </span>
        )}
      </div>
      {error && (
        <p className="payment-error" role="alert">
          {error}
        </p>
      )}
      {notice && (
        <p className="payment-notice" role="status">
          {notice}
        </p>
      )}
      {initialLoading ? (
        <StatePanel
          kind="loading"
          title="Đang tải thông tin"
          description="Vui lòng đợi trong giây lát."
        />
      ) : staff ? (
        <>
          <button
            className="button button-secondary"
            disabled={busy}
            onClick={() =>
              run(async () => setReviews(await paymentClient.reviews()))
            }
          >
            Tải lại danh sách
          </button>
          {reviews.length === 0 ? (
            <StatePanel
              title="Không có giao dịch chờ kiểm tra"
              description="Các giao dịch cần đối soát sẽ xuất hiện tại đây."
            />
          ) : (
            reviews.map((p) => (
              <article className="payment-card" key={p.id}>
                <h4>{p.referenceCode}</h4>
                <p>
                  Đơn #{p.orderId} · {money(p.amountVnd)}
                </p>
                <span className="payment-badge review_required">
                  Chờ kiểm tra
                </span>
                {p.reasons?.map((r, i) => (
                  <p key={i}>{r.newValue}</p>
                ))}
              </article>
            ))
          )}
        </>
      ) : (
        <>
          <nav className="payment-tabs" aria-label="Thanh toán và vé">
            <button
              className="button"
              aria-current={view === "payment" ? "page" : undefined}
              disabled={busy}
              onClick={() => {
                setView("payment");
                setGateway(false);
              }}
            >
              <CreditCard size={17} />
              Thanh toán
            </button>
            <button
              className="button"
              aria-current={view === "tickets" ? "page" : undefined}
              disabled={busy}
              onClick={() => run(showTickets)}
            >
              <Ticket size={17} />
              Vé của tôi
            </button>
          </nav>

          {view === "tickets" ? (
            <TicketList tickets={tickets} now={now} busy={busy} onBrowse={onBrowse}
              onRefresh={() => run(showTickets)}
              onCancel={(id) => run(async () => {
                const result = await paymentClient.cancel(id);
                setTickets(await paymentClient.tickets());
                setOrder(null);
                setNotice(result.refundAmountVnd > 0
                  ? 'Đã hủy vé. Yêu cầu hoàn tiền đang chờ nhân viên xử lý.' : 'Đã hủy vé và trả lại chỗ.');
              })}
              onPay={(ticket) => run(async () => {
                setOrderId(String(ticket.orderId));
                setOrder(await paymentClient.refresh(ticket.orderId));
                setView('payment');
              })} />
          ) : !order ? (
            <>
              <StatePanel
                title="Chọn vé để thanh toán"
                description="Chọn vé chờ thanh toán hoặc tìm buổi chơi phù hợp."
              />
              <TicketList key="pending-checkouts" tickets={tickets} initialFilter="pending" now={now} busy={busy} onBrowse={onBrowse}
                onRefresh={() => run(async () => setTickets(await paymentClient.tickets()))}
                onCancel={(id) => run(async () => { await paymentClient.cancel(id); setTickets(await paymentClient.tickets()); })}
                onPay={(ticket) => run(async () => { setOrderId(String(ticket.orderId)); setOrder(await paymentClient.refresh(ticket.orderId)); })} />
            </>
          ) : (
            <div className="checkout">
              <section className="payment-card checkout-detail">
                <div className="ticket-top">
                  <h4>Chi tiết đơn đặt chỗ</h4>
                  <span className={`payment-badge ${state}`}>
                    {labels[state] || state}
                  </span>
                </div>
                <p className="checkout-code">Mã đơn: {order.orderCode}</p>
                <ul className="checkout-items">
                  {order.tickets.filter(t => norm(t.status) !== "cancelled").map((t, i) => (
                    <li key={t.id ?? i}>
                      <span>
                        <strong>{t.sportName || 'Vé'} · {t.courtName || 'Buổi chơi'}</strong>
                        <small className="checkout-item-meta">{t.venueName}</small>
                        <small className="checkout-item-meta">{formatDate(t.sessionDate)} · {t.startTime?.slice(0,5)} – {t.endTime?.slice(0,5)}</small>
                        <small className="checkout-item-meta">{t.holderName}</small>
                      </span>
                      {t.amountVnd != null && <span>{money(t.amountVnd)}</span>}
                    </li>
                  ))}
                </ul>
                {statusMessage && (
                  <p className={`checkout-message ${state}`}>{statusMessage}</p>
                )}

                {gateway && (
                  <div className="demo-gateway">
                    <strong>Cổng thanh toán minh họa</strong>
                    <div className="payment-actions">
                      <button
                        className="button button-primary"
                        disabled={busy}
                        onClick={() => run(() => simulate("success"))}
                      >
                        Thành công
                      </button>
                      <button
                        className="button button-secondary"
                        disabled={busy}
                        onClick={() => run(() => simulate("failed"))}
                      >
                        Thất bại / Hủy
                      </button>
                      <button
                        className="button button-secondary"
                        disabled={busy}
                        onClick={() => run(() => simulate("mismatch"))}
                      >
                        Sai số tiền
                      </button>
                    </div>
                  </div>
                )}

                {latest &&
                  ["processing", "failed", "expired"].includes(state) && (
                    <details className="payment-support">
                      <summary>Đã trả tiền nhưng chưa nhận được vé?</summary>
                      <form
                        onSubmit={(e) => {
                          e.preventDefault();
                          run(async () => {
                            await paymentClient.review(order.id, reason.trim());
                            setOrder(await paymentClient.display(order.id));
                            setGateway(false);
                            setReason("");
                            setNotice("Đã gửi yêu cầu kiểm tra.");
                          });
                        }}
                      >
                        <textarea
                          id="payment-reason"
                          aria-label="Nội dung cần kiểm tra"
                          required
                          minLength={10}
                          maxLength={1000}
                          value={reason}
                          onChange={(e) => setReason(e.target.value)}
                          placeholder="Mô tả ngắn: thời gian chuyển khoản, số tiền…"
                        />
                        <button
                          className="button button-secondary"
                          disabled={busy || reason.trim().length < 10}
                        >
                          Gửi yêu cầu
                        </button>
                      </form>
                    </details>
                  )}
              </section>

              <aside className="payment-card checkout-summary">
                <div className="summary-row">
                  <span>Tạm tính ({order.tickets.length} vé)</span>
                  <span>{money(order.totalAmount)}</span>
                </div>
                <div className="summary-row summary-total">
                  <span>Tổng cộng</span>
                  <strong>{money(order.totalAmount)}</strong>
                </div>
                {canPay && (
                  <p className="checkout-timer" role="timer">
                    <Timer size={16} />
                    Giữ chỗ trong {timeLeft}
                  </p>
                )}
                {canPay && (
                  <button
                    className="button button-primary checkout-pay"
                    disabled={busy}
                    onClick={() => run(process)}
                  >
                    {payStatus === "pending"
                      ? "Tiếp tục thanh toán"
                      : `Thanh toán ${money(order.totalAmount)}`}
                  </button>
                )}
                {state === "paid" ? (
                  <button
                    className="button button-primary checkout-pay"
                    disabled={busy}
                    onClick={() => run(showTickets)}
                  >
                    Xem vé
                  </button>
                ) : (
                  ["processing", "review_required", "failed"].includes(state) && (
                    <button
                      className="button button-secondary checkout-pay"
                      disabled={busy}
                      onClick={() => run(refresh)}
                    >
                      {state === "review_required"
                        ? "Cập nhật trạng thái"
                        : "Tôi đã thanh toán, kiểm tra lại"}
                    </button>
                  )
                )}
                <p className="checkout-secure">
                  <ShieldCheck size={14} /> Thanh toán an toàn qua payOS
                </p>
              </aside>
            </div>
          )}
        </>
      )}
      {busy && <p className="payment-hint" role="status">Đang xử lý…</p>}
      {demoMode && (
        <button
          className="payment-reset"
          disabled={busy}
          onClick={() =>
            run(async () => {
              const data = paymentClient.reset();
              setOrder(data);
              setOrderId("1");
              setTickets([]);
              setReviews([]);
              setGateway(false);
              setView("payment");
              setReason("");
              setNotice("Đã tạo đơn minh họa mới.");
            })
          }
        >
          Tạo lại dữ liệu minh họa
        </button>
      )}
    </div>
  );
}
