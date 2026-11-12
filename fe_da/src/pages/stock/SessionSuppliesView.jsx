import { useEffect, useState } from "react";
import { CalendarDays, Check, PackageOpen, Undo2, X } from "lucide-react";
import SectionLayout from "../../components/layout/SectionLayout";
import StatePanel from "../../components/ui/StatePanel";
import { requestStatusLabels, stockClient } from "./stockClient";

const number = (value) => new Intl.NumberFormat("vi-VN").format(value);
const vietnamToday = () =>
  new Date(Date.now() + 7 * 3600_000).toISOString().slice(0, 10);

// UC-41 / UC-42: referee requests -> Staff issues to the session -> referee reports use and returns
// -> Staff confirms what physically came back. readOnly: the Manager follows without changing stock.
export default function SessionSuppliesView({ readOnly = false }) {
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
                  {s.pendingRequests > 0 && (
                    <span className="stock-badge-warning">{s.pendingRequests} yêu cầu chờ cấp</span>
                  )}
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
            <SupplyDetails key={sessionId} sessionId={sessionId} onSummary={updateSummary} readOnly={readOnly} />
          ) : (
            <StatePanel title="Chọn một buổi chơi" description="Chọn ca ở danh sách để cấp hoặc chốt vật tư." />
          )}
        </div>
      )}
    </SectionLayout>
  );
}

function SupplyDetails({ sessionId, onSummary, readOnly }) {
  const [details, setDetails] = useState(null);
  const [products, setProducts] = useState([]);
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const [busy, setBusy] = useState(false);
  const [form, setForm] = useState({ productId: "", unitName: "", quantity: "", recipientId: "" });
  const [closing, setClosing] = useState(null);
  const [rejecting, setRejecting] = useState(null);

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
          unitName: form.unitName || null,
        }),
      `Đã cấp ${number(form.quantity)} ${form.unitName || product?.baseUnit || ""} × ${product?.name ?? "vật tư"} cho buổi chơi.`,
    );
    if (ok) setForm((f) => ({ ...f, quantity: "" }));
  }

  async function issueRequest(r) {
    await apply(
      () => stockClient.issueSupplyRequest(sessionId, r.id),
      `Đã cấp ${number(r.enteredQuantity ?? r.quantity)} ${r.enteredUnit ?? r.baseUnit} × ${r.name} cho ${r.requestedByName}.`,
    );
  }

  async function rejectRequest(e) {
    e.preventDefault();
    const ok = await apply(
      () => stockClient.rejectSupplyRequest(sessionId, rejecting.id, rejecting.note),
      `Đã từ chối yêu cầu ${rejecting.name} của ${rejecting.requestedByName}.`,
    );
    if (ok) setRejecting(null);
  }

  async function reconcile(e) {
    e.preventDefault();
    const ok = await apply(
      () => stockClient.reconcileSupply(sessionId, closing.productId, Number(closing.returned)),
      `Đã xác nhận thu hồi ${closing.name}: nhận lại ${number(closing.returned)}.`,
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
  const requests = details.requests ?? [];
  const closed = new Set(allocations.filter((a) => a.isReconciled).map((a) => a.productId));
  const selected = products.find((p) => p.id === Number(form.productId));
  const selectedUnits = [...(selected?.units ?? [])].sort((a, b) => a.factorToBase - b.factorToBase);
  const selectedFactor = selectedUnits.find((u) => u.unitName === form.unitName)?.factorToBase ?? 1;

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

      {requests.length > 0 && (
        <div className="supply-requests">
          <h4>Yêu cầu vật tư của trọng tài</h4>
          <ul>
            {requests.map((r) => (
              <li key={r.id}>
                <span>
                  <strong>
                    {r.name}: {number(r.enteredQuantity ?? r.quantity)} {r.enteredUnit ?? r.baseUnit}
                    {r.enteredUnit && r.enteredUnit !== r.baseUnit ? ` (= ${number(r.quantity)} ${r.baseUnit})` : ""}
                  </strong>
                  <small>
                    {r.requestedByName}
                    {r.note ? ` · Ghi chú: ${r.note}` : ""}
                    {r.handledByName ? ` · ${requestStatusLabels[r.status]} bởi ${r.handledByName}` : ""}
                    {r.status === "Issued" && r.issuedQuantity != null ? ` · cấp ${number(r.issuedQuantity)} ${r.baseUnit}` : ""}
                    {r.decisionNote ? ` · ${r.decisionNote}` : ""}
                  </small>
                </span>
                {r.status === "Pending" && !readOnly ? (
                  <span className="stock-actions">
                    <button className="button button-primary" disabled={busy} onClick={() => issueRequest(r)}>
                      <Check size={15} aria-hidden="true" /> Cấp
                    </button>
                    <button className="button button-secondary" disabled={busy} onClick={() => setRejecting({ ...r, note: "" })}>
                      <X size={15} aria-hidden="true" /> Từ chối
                    </button>
                  </span>
                ) : (
                  <span className={r.status === "Pending" ? "stock-badge-warning" : r.status === "Issued" ? "badge-lime" : "badge-gray"}>
                    {requestStatusLabels[r.status] || r.status}
                  </span>
                )}
              </li>
            ))}
          </ul>
        </div>
      )}

      {rejecting && (
        <form className="stock-form supply-close" onSubmit={rejectRequest}>
          <label>
            Lý do từ chối yêu cầu {rejecting.name} của {rejecting.requestedByName}
            <input
              maxLength={300}
              value={rejecting.note}
              onChange={(e) => setRejecting({ ...rejecting, note: e.target.value })}
              required
              autoFocus
            />
          </label>
          <div className="stock-form-actions">
            <button type="button" className="button button-secondary" onClick={() => setRejecting(null)}>
              Hủy
            </button>
            <button className="button button-primary" disabled={busy}>
              Xác nhận từ chối
            </button>
          </div>
        </form>
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
                <th scope="col" className="num">TT báo dùng</th>
                <th scope="col" className="num">TT trả lại</th>
                <th scope="col" className="num">Thực nhận lại</th>
                <th scope="col" className="num">Đã dùng</th>
                <th scope="col" className="num">Chênh lệch</th>
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
                  <td data-label="Đã cấp" className="num">{number(a.issued)} {a.baseUnit}</td>
                  <td data-label="TT báo dùng" className="num">{a.reportedUsed ?? "—"}</td>
                  <td data-label="TT trả lại" className="num">{a.returnOffered ?? "—"}</td>
                  <td data-label="Thực nhận lại" className="num">{a.isReconciled ? number(a.returned) : "—"}</td>
                  <td data-label="Đã dùng" className="num">{a.used ?? "—"}</td>
                  <td data-label="Chênh lệch" className="num">
                    {a.difference == null ? "—" : (
                      <span className={a.difference > 0 ? "is-out" : ""} title={a.difference > 0 ? "Thiếu so với báo cáo của trọng tài: cần kiểm kê" : undefined}>
                        {a.difference > 0 ? `Thiếu ${number(a.difference)}` : a.difference < 0 ? `Dư ${number(-a.difference)}` : "0"}
                      </span>
                    )}
                  </td>
                  <td data-label="Người nhận">{a.recipientName || "—"}</td>
                  <td className="stock-actions">
                    {a.isReconciled ? (
                      <span className="badge-lime">Đã chốt</span>
                    ) : readOnly ? (
                      <span className="stock-badge-warning">Chưa thu hồi</span>
                    ) : (
                      <button
                        className="button button-secondary"
                        disabled={busy}
                        onClick={() =>
                          // Prefill from the referee's report; Staff enters what was physically returned.
                          setClosing({
                            productId: a.productId,
                            name: a.name,
                            max: a.outstanding,
                            reportedUsed: a.reportedUsed,
                            returned: String(a.returnOffered ?? (a.reportedUsed == null ? 0 : a.outstanding - a.reportedUsed)),
                          })
                        }
                      >
                        <Undo2 size={15} aria-hidden="true" /> Xác nhận thu hồi
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
            Số {closing.name} thực nhận lại về kho (còn ở buổi {number(closing.max)})
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
            Đã dùng = {number(closing.max - (Number(closing.returned) || 0))}.
            {closing.reportedUsed != null && closing.max - (Number(closing.returned) || 0) - closing.reportedUsed > 0
              ? ` Thiếu ${number(closing.max - (Number(closing.returned) || 0) - closing.reportedUsed)} so với số trọng tài báo dùng: chênh lệch được ghi lại để kiểm kê.`
              : ""}{" "}
            Sau khi chốt không cấp thêm vật tư này cho buổi.
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

      {!readOnly && <form className="stock-form supply-issue" onSubmit={issue}>
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
              onChange={(e) => setForm({ ...form, productId: e.target.value, unitName: "" })}
              required
            >
              <option value="">Chọn vật tư</option>
              {products
                .filter((p) => !closed.has(p.id))
                .map((p) => (
                  <option key={p.id} value={p.id} disabled={p.availableQuantity <= 0}>
                    {p.name} (khả dụng {number(p.availableQuantity)} {p.baseUnit})
                  </option>
                ))}
            </select>
          </label>
          <label>
            Đơn vị
            <select
              value={form.unitName}
              disabled={!selected}
              onChange={(e) => setForm({ ...form, unitName: e.target.value })}
            >
              <option value="">{selected?.baseUnit ?? "Chọn vật tư trước"}</option>
              {selectedUnits
                .filter((u) => u.factorToBase > 1)
                .map((u) => (
                  <option key={u.unitName} value={u.unitName}>
                    {u.unitName} (= {number(u.factorToBase)} {selected.baseUnit})
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
              max={Math.max(1, Math.floor((selected?.availableQuantity ?? 1) / selectedFactor))}
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
      </form>}
    </div>
  );
}
