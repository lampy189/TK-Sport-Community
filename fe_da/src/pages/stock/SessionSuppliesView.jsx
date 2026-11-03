import { useEffect, useState } from "react";
import { CalendarDays, PackageOpen, Undo2 } from "lucide-react";
import SectionLayout from "../../components/layout/SectionLayout";
import StatePanel from "../../components/ui/StatePanel";
import { stockClient } from "./stockClient";

const number = (value) => new Intl.NumberFormat("vi-VN").format(value);
const vietnamToday = () =>
  new Date(Date.now() + 7 * 3600_000).toISOString().slice(0, 10);

// UC-41 / UC-42: issue supplies to one session, then close with the unused quantity returned.
export default function SessionSuppliesView() {
  const [date, setDate] = useState(vietnamToday);
  const [sessions, setSessions] = useState({ loading: true, items: [], error: "" });
  const [sessionId, setSessionId] = useState(null);

  useEffect(() => {
    let active = true;
    stockClient
      .supplySessions(date)
      .then((items) => active && setSessions({ loading: false, items, error: "" }))
      .catch((e) => active && setSessions({ loading: false, items: [], error: e.message }));
    return () => {
      active = false;
    };
  }, [date]);

  function changeDate(value) {
    setDate(value);
    setSessionId(null);
    setSessions({ loading: true, items: [], error: "" });
  }
  function updateSummary(session) {
    setSessions((s) => ({
      ...s,
      items: s.items.map((x) => (x.id === session.id ? session : x)),
    }));
  }

  return (
    <SectionLayout
      title="Vật tư buổi chơi"
      description="Cấp cầu, bóng cho từng ca chơi và chốt số đã dùng, số trả lại kho cuối buổi."
      className="stock-page"
    >
      <div className="stock-toolbar">
        <label className="supply-date">
          <CalendarDays size={16} aria-hidden="true" />
          <span className="sr-only">Ngày chơi</span>
          <input type="date" value={date} onChange={(e) => changeDate(e.target.value)} required />
        </label>
      </div>
      {sessions.loading ? (
        <StatePanel kind="loading" title="Đang tải buổi chơi…" />
      ) : sessions.error ? (
        <StatePanel kind="error" title="Không tải được buổi chơi" description={sessions.error} />
      ) : sessions.items.length === 0 ? (
        <StatePanel title="Không có buổi chơi trong ngày" description="Chọn ngày khác để cấp vật tư." />
      ) : (
        <div className="supply-layout">
          <ul className="supply-sessions" aria-label="Buổi chơi trong ngày">
            {sessions.items.map((s) => (
              <li key={s.id}>
                <button
                  className={s.id === sessionId ? "is-active" : ""}
                  aria-pressed={s.id === sessionId}
                  onClick={() => setSessionId(s.id)}
                >
                  <strong>{s.timeLabel}</strong>
                  <span>
                    {s.sportName} · {s.courtName}
                  </span>
                  {s.outstandingItems > 0 ? (
                    <span className="stock-badge-warning">{s.outstandingItems} vật tư chưa chốt</span>
                  ) : s.reconciledItems > 0 ? (
                    <span className="badge-lime">Đã chốt</span>
                  ) : (
                    <span className="badge-gray">Chưa cấp</span>
                  )}
                </button>
              </li>
            ))}
          </ul>
          {sessionId ? (
            <SupplyDetails key={sessionId} sessionId={sessionId} onSummary={updateSummary} />
          ) : (
            <StatePanel title="Chọn một buổi chơi" description="Chọn ca ở danh sách để cấp hoặc chốt vật tư." />
          )}
        </div>
      )}
    </SectionLayout>
  );
}

function SupplyDetails({ sessionId, onSummary }) {
  const [details, setDetails] = useState(null);
  const [products, setProducts] = useState([]);
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const [busy, setBusy] = useState(false);
  const [form, setForm] = useState({ productId: "", quantity: "", recipientId: "" });
  const [closing, setClosing] = useState(null);

  useEffect(() => {
    let active = true;
    Promise.all([stockClient.supplyDetails(sessionId), stockClient.products()])
      .then(([d, p]) => {
        if (!active) return;
        setDetails(d);
        setProducts(p.filter((x) => x.isActive));
      })
      .catch((e) => active && setError(e.message));
    return () => {
      active = false;
    };
  }, [sessionId]);

  async function apply(action, message) {
    setBusy(true);
    setError("");
    setNotice("");
    try {
      const updated = await action();
      setDetails(updated);
      onSummary(updated.session);
      setProducts(await stockClient.products().then((p) => p.filter((x) => x.isActive)));
      setNotice(message);
      return true;
    } catch (e) {
      setError(e.message);
      return false;
    } finally {
      setBusy(false);
    }
  }

  async function issue(e) {
    e.preventDefault();
    const product = products.find((p) => p.id === Number(form.productId));
    const ok = await apply(
      () =>
        stockClient.issueSupply(sessionId, {
          productId: Number(form.productId),
          quantity: Number(form.quantity),
          recipientId: Number(form.recipientId),
        }),
      `Đã cấp ${number(form.quantity)} × ${product?.name ?? "vật tư"} cho buổi chơi.`,
    );
    if (ok) setForm((f) => ({ ...f, quantity: "" }));
  }

  async function reconcile(e) {
    e.preventDefault();
    const ok = await apply(
      () => stockClient.reconcileSupply(sessionId, closing.productId, Number(closing.returned)),
      `Đã chốt ${closing.name}: trả lại ${number(closing.returned)}.`,
    );
    if (ok) setClosing(null);
  }

  if (!details)
    return error ? (
      <StatePanel kind="error" title="Không tải được buổi chơi" description={error} />
    ) : (
      <StatePanel kind="loading" title="Đang tải vật tư…" />
    );

  const { session, recipients, allocations } = details;
  const closed = new Set(allocations.filter((a) => a.isReconciled).map((a) => a.productId));
  const selected = products.find((p) => p.id === Number(form.productId));

  return (
    <div className="supply-details">
      <h3>
        {session.timeLabel} · {session.sportName} · {session.courtName}
      </h3>
      {notice && (
        <p className="stock-notice" role="status">
          {notice}
        </p>
      )}
      {error && (
        <p className="stock-error" role="alert">
          {error}
        </p>
      )}

      {allocations.length === 0 ? (
        <p className="stock-hint">Chưa cấp vật tư cho buổi này.</p>
      ) : (
        <div className="stock-table-wrap">
          <table className="stock-table">
            <thead>
              <tr>
                <th scope="col">Vật tư</th>
                <th scope="col" className="num">Đã cấp</th>
                <th scope="col" className="num">Trả lại</th>
                <th scope="col" className="num">Đã dùng</th>
                <th scope="col">Người nhận</th>
                <th scope="col"><span className="sr-only">Thao tác</span></th>
              </tr>
            </thead>
            <tbody>
              {allocations.map((a) => (
                <tr key={a.productId}>
                  <td data-label="Vật tư">
                    <strong>{a.name}</strong>
                    <span className="stock-sku">{a.sku}</span>
                  </td>
                  <td data-label="Đã cấp" className="num">{number(a.issued)}</td>
                  <td data-label="Trả lại" className="num">{a.isReconciled ? number(a.returned) : "—"}</td>
                  <td data-label="Đã dùng" className="num">{a.used ?? "—"}</td>
                  <td data-label="Người nhận">{a.recipientName || "—"}</td>
                  <td className="stock-actions">
                    {a.isReconciled ? (
                      <span className="badge-lime">Đã chốt</span>
                    ) : (
                      <button
                        className="button button-secondary"
                        disabled={busy}
                        onClick={() =>
                          setClosing({ productId: a.productId, name: a.name, max: a.outstanding, returned: "0" })
                        }
                      >
                        <Undo2 size={15} aria-hidden="true" /> Chốt cuối buổi
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {closing && (
        <form className="stock-form supply-close" onSubmit={reconcile}>
          <label>
            Số {closing.name} còn dư trả lại kho (đã cấp {number(closing.max)})
            <input
              type="number"
              inputMode="numeric"
              min="0"
              max={closing.max}
              value={closing.returned}
              onChange={(e) => setClosing({ ...closing, returned: e.target.value })}
              required
              autoFocus
            />
          </label>
          <p className="stock-hint">
            Đã dùng = {number(closing.max - (Number(closing.returned) || 0))}. Sau khi chốt không cấp thêm vật tư này cho buổi.
          </p>
          <div className="stock-form-actions">
            <button type="button" className="button button-secondary" onClick={() => setClosing(null)}>
              Hủy
            </button>
            <button className="button button-primary" disabled={busy || Number(closing.returned) > closing.max}>
              Xác nhận chốt
            </button>
          </div>
        </form>
      )}

      <form className="stock-form supply-issue" onSubmit={issue}>
        <h4>
          <PackageOpen size={16} aria-hidden="true" /> Cấp vật tư cho buổi
        </h4>
        {recipients.length === 0 && (
          <p className="stock-hint">
            Buổi chưa phân công trọng tài và chưa có Quản lý nhận vật tư.
          </p>
        )}
        <div className="stock-form-row">
          <label>
            Vật tư
            <select
              value={form.productId}
              onChange={(e) => setForm({ ...form, productId: e.target.value })}
              required
            >
              <option value="">Chọn vật tư</option>
              {products
                .filter((p) => !closed.has(p.id))
                .map((p) => (
                  <option key={p.id} value={p.id} disabled={p.quantityOnHand === 0}>
                    {p.name} (tồn {number(p.quantityOnHand)})
                  </option>
                ))}
            </select>
          </label>
          <label>
            Số lượng
            <input
              type="number"
              inputMode="numeric"
              min="1"
              max={selected?.quantityOnHand || 1}
              value={form.quantity}
              onChange={(e) => setForm({ ...form, quantity: e.target.value })}
              required
            />
          </label>
          <label>
            Người nhận
            <select
              value={form.recipientId}
              onChange={(e) => setForm({ ...form, recipientId: e.target.value })}
              required
            >
              <option value="">Chọn người nhận</option>
              {recipients.map((r) => (
                <option key={r.userId} value={r.userId}>
                  {r.fullName} · {r.role === "Referee" ? "Trọng tài" : "Quản lý"}
                </option>
              ))}
            </select>
          </label>
        </div>
        <div className="stock-form-actions">
          <button className="button button-primary" disabled={busy || recipients.length === 0}>
            {busy ? "Đang ghi…" : "Cấp vật tư"}
          </button>
        </div>
      </form>
    </div>
  );
}
