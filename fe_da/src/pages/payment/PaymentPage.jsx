import { useEffect, useState } from "react";
import { demoMode, paymentClient } from "./paymentClient";
import "./payment.css";
import { ShieldCheck, Ticket, CreditCard } from "lucide-react";
import StatePanel from "../../components/ui/StatePanel";

const money = (value) =>
  new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND" }).format(
    value,
  );
const labels = {
  pending: "Chờ thanh toán",
  paid: "Đã thanh toán",
  failed: "Thanh toán không thành công",
  review_required: "Chờ nhân viên kiểm tra",
  reserved: "Chưa có hiệu lực",
  confirmed: "Vé đã xác nhận",
  cancelled: "Đã hủy",
};
export default function PaymentPage({
  initialView = "payment",
  staff = false,
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
    setNotice("Đã cập nhật trạng thái từ hệ thống.");
  }
  async function showTickets() {
    setTickets(await paymentClient.tickets());
    setView("tickets");
  }
  async function process() {
    const data = await paymentClient.process(order.id);
    if (data.status === "refresh_required") {
      setOrder(await paymentClient.refresh(order.id));
      setNotice(
        "Đã cập nhật kết quả payOS. Nếu đã trả tiền, vui lòng chờ hệ thống xác minh giao dịch.",
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
  const latest = order?.payments[0];
  const state =
    order?.status === "pending" ? latest?.status || "pending" : order?.status;
  const seconds = order?.expiresAt
    ? Math.max(0, Math.ceil((Date.parse(order.expiresAt) - now) / 1000))
    : 0;
  const canPay = order?.status === "pending" && seconds > 0;

  return (
    <div className="payment-workspace" aria-busy={busy}>
      <div className="payment-heading">
        <div>
          <span className="eyebrow">THANH TOÁN & VÉ RA SÂN</span>
          <h1>
            {staff
              ? "Thanh toán cần kiểm tra"
              : view === "tickets"
                ? "Vé của tôi"
                : "Sẵn sàng cho buổi chơi."}
          </h1>
          <p className="payment-hint">
            {staff
              ? "Theo dõi các giao dịch cần đối soát trong một không gian."
              : "Quản lý thanh toán và theo dõi vé đăng ký của bạn."}
          </p>
        </div>
        <span className={`payment-badge ${demoMode ? "" : "confirmed"}`}>
          <ShieldCheck size={15} />
          {demoMode ? "MINH HỌA · KHÔNG THU TIỀN" : "payOS"}
        </span>
      </div>
      {demoMode && (
        <p className="payment-hint">
          Dữ liệu thử lưu trên trình duyệt này. Vé minh họa không dùng để vào
          sân.
        </p>
      )}
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
            <div>
              {tickets.length === 0 && (
                <StatePanel
                  title="Buổi chơi đầu tiên đang chờ bạn"
                  description="Bạn chưa có vé. Vé sẽ xuất hiện tại đây sau khi đăng ký buổi chơi."
                />
              )}
              {tickets.map((ticket) => (
                <article className="payment-card ticket-card" key={ticket.id}>
                  <span className={`payment-badge ${ticket.status}`}>
                    {labels[ticket.status] || ticket.status}
                  </span>
                  <h4>Buổi chơi #{ticket.sessionId}</h4>
                  {ticket.status === "pending" && (
                    <button
                      className="button button-primary"
                      onClick={() =>
                        run(async () => {
                          setOrderId(String(ticket.orderId));
                          setOrder(await paymentClient.display(ticket.orderId));
                          setView("payment");
                        })
                      }
                    >
                      Thanh toán đơn #{ticket.orderId}
                    </button>
                  )}
                  <p>
                    Người chơi #{ticket.playerId} · Đơn #{ticket.orderId}
                  </p>
                  {ticket.sessionDate && (
                    <p>
                      {ticket.sessionDate} · {ticket.startTime} –{" "}
                      {ticket.endTime}
                    </p>
                  )}
                  <strong>{money(ticket.amountVnd)}</strong>
                  {ticket.valid ? (
                    <>
                      <p>Mã vé</p>
                      <code className="ticket-code">{ticket.ticketCode}</code>
                      <p>Xuất trình mã vé và thông tin người chơi tại sân.</p>
                    </>
                  ) : (
                    <p>
                      Vé chưa có hiệu lực để vào sân. Kiểm tra trạng thái thanh
                      toán của đơn.
                    </p>
                  )}
                </article>
              ))}
            </div>
          ) : (
            <>
              <form
                className="payment-order-form"
                onSubmit={(event) => {
                  event.preventDefault();
                  setOrder(null);
                  setGateway(false);
                  run(async () =>
                    setOrder(await paymentClient.display(orderId)),
                  );
                }}
              >
                <label htmlFor="payment-order">Mã số đơn đăng ký</label>
                <input
                  id="payment-order"
                  inputMode="numeric"
                  pattern="[1-9][0-9]*"
                  required
                  value={orderId}
                  disabled={demoMode || busy}
                  onChange={(event) => setOrderId(event.target.value)}
                />
                <button className="button button-secondary" disabled={busy}>
                  Xem thanh toán
                </button>
              </form>
              {!order && (
                <StatePanel
                  title="Tra cứu đơn đăng ký"
                  description="Nhập mã số đơn ở trên để xem số tiền và thời hạn thanh toán."
                />
              )}
              {order && (
                <>
                  <ol className="payment-steps" aria-label="Tiến trình">
                    <li>Thông tin đơn</li>
                    <li>Cổng thanh toán</li>
                    <li>Xác nhận</li>
                    <li>Vé</li>
                  </ol>
                  <article className="payment-card">
                    <span className={`payment-badge ${state}`}>
                      {labels[state] || state}
                    </span>
                    <h4>{order.orderCode}</h4>
                    <dl className="payment-details">
                      <dt>Số vé</dt>
                      <dd>{order.tickets.length}</dd>
                      <dt>Tổng thanh toán</dt>
                      <dd className="payment-total">
                        {money(order.totalAmount)}
                      </dd>
                      <dt>Phương thức</dt>
                      <dd>payOS</dd>
                      <dt>Hạn thanh toán</dt>
                      <dd>
                        {order.expiresAt
                          ? new Date(order.expiresAt).toLocaleString("vi-VN")
                          : "Chưa xác định"}
                      </dd>
                      {latest && (
                        <>
                          <dt>Mã giao dịch</dt>
                          <dd className="payment-reference">
                            {latest.referenceCode}
                          </dd>
                        </>
                      )}
                    </dl>
                    {order.status === "pending" && (
                      <p role="timer">
                        {seconds
                          ? `Còn ${Math.floor(seconds / 60)}:${String(seconds % 60).padStart(2, "0")}`
                          : "Đã hết hạn thanh toán. Vui lòng đăng ký lại hoặc gửi yêu cầu kiểm tra nếu đã trả tiền."}
                      </p>
                    )}
                    {state === "paid" && (
                      <p>Hệ thống đã xác nhận thanh toán. Bạn có thể xem vé.</p>
                    )}
                    {state === "review_required" && (
                      <p>
                        Giao dịch cần đối soát. Vui lòng không thanh toán lại.
                        Vé chưa được kích hoạt trong thời gian chờ xử lý.
                      </p>
                    )}
                    {state === "failed" && (
                      <p>
                        Giao dịch chưa thành công. Bạn có thể thử lại khi đơn
                        còn hạn; nếu đã bị trừ tiền, hãy gửi yêu cầu kiểm tra.
                      </p>
                    )}
                    <div className="payment-actions">
                      {canPay && (
                        <button
                          className="button button-primary"
                          disabled={busy}
                          onClick={() => run(process)}
                        >
                          {latest?.status === "pending"
                            ? "Tiếp tục tại payOS"
                            : "Thanh toán qua payOS"}
                        </button>
                      )}
                      <button
                        className="button button-secondary"
                        disabled={busy}
                        onClick={() => run(refresh)}
                      >
                        Kiểm tra trạng thái
                      </button>
                      {state === "paid" && (
                        <button
                          className="button button-primary"
                          disabled={busy}
                          onClick={() => run(showTickets)}
                        >
                          Xem vé
                        </button>
                      )}
                    </div>
                  </article>
                  {gateway && (
                    <section
                      className="payment-card demo-gateway"
                      aria-label="Cổng thanh toán minh họa"
                    >
                      <h4>Cổng thanh toán minh họa</h4>
                      <p>
                        Chọn kết quả để thử các nhánh xử lý. Không có giao dịch
                        ngân hàng được tạo.
                      </p>
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
                    </section>
                  )}
                  {latest &&
                    state !== "paid" &&
                    state !== "review_required" && (
                      <form
                        className="payment-card"
                        onSubmit={(e) => {
                          e.preventDefault();
                          run(async () => {
                            await paymentClient.review(order.id, reason.trim());
                            setOrder(await paymentClient.display(order.id));
                            setGateway(false);
                            setReason("");
                            setNotice("Đã gửi yêu cầu kiểm tra cho nhân viên.");
                          });
                        }}
                      >
                        <h4>Đã trả tiền nhưng chưa nhận vé?</h4>
                        <label htmlFor="payment-reason">
                          Nội dung cần kiểm tra
                        </label>
                        <textarea
                          id="payment-reason"
                          required
                          minLength={10}
                          maxLength={1000}
                          value={reason}
                          onChange={(e) => setReason(e.target.value)}
                          placeholder="Thời gian giao dịch, mã tham chiếu và vấn đề gặp phải…"
                        />
                        <p className="payment-hint">
                          Không nhập mật khẩu, mã OTP hoặc số thẻ đầy đủ.
                        </p>
                        <button
                          className="button button-secondary"
                          disabled={busy || reason.trim().length < 10}
                        >
                          Gửi yêu cầu kiểm tra
                        </button>
                      </form>
                    )}
                </>
              )}
            </>
          )}
        </>
      )}
      {busy && <p role="status">Đang xử lý…</p>}
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
