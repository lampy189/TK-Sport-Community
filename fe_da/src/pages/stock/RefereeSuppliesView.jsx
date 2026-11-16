import { useEffect, useState } from "react";
import { CalendarDays, PackagePlus } from "lucide-react";
import SectionLayout from "../../components/layout/SectionLayout";
import StatePanel from "../../components/ui/StatePanel";
import { requestStatusLabels, stockClient } from "./stockClient";
import "./stock.css";

const number = (value) => new Intl.NumberFormat("vi-VN").format(value);
const vietnamToday = () =>
  new Date(Date.now() + 7 * 3600_000).toISOString().slice(0, 10);

// UC-41 / UC-42: the assigned Referee requests supplies, sees what was issued and reports use and returns;
// Staff issues the request and confirms what physically came back.
export default function RefereeSuppliesView() {
  const [date, setDate] = useState(vietnamToday);
  const [sessions, setSessions] = useState({ loading: true, items: [], error: "" });
  const [sessionId, setSessionId] = useState(null);

  useEffect(() => {
    let active = true;
    stockClient
      .refereeSessions(date)
      .then((items) => {
        if (!active) return;
        setSessions({ loading: false, items, error: "" });
        setSessionId((current) => current ?? items[0]?.id ?? null);
      })
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

  return (
    <SectionLayout
      title="Vật tư buổi chơi"
      description="Gửi yêu cầu vật tư cho buổi bạn phụ trách, xem số được cấp và báo số đã dùng, số trả lại sau buổi."
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
        <StatePanel kind="loading" title="Đang tải buổi được phân công…" />
      ) : sessions.error ? (
        <StatePanel kind="error" title="Không tải được buổi chơi" description={sessions.error} />
      ) : sessions.items.length === 0 ? (
        <StatePanel
          title="Không có buổi được phân công"
          description="Bạn chưa được phân công buổi nào trong ngày này."
        />
      ) : (
        <div className="supply-layout">
          <ul className="supply-sessions" aria-label="Buổi được phân công">
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
                </button>
              </li>
            ))}
          </ul>
          {sessionId && <RefereeSupplyDetails key={sessionId} sessionId={sessionId} />}
        </div>
      )}
    </SectionLayout>
  );
}

function RefereeSupplyDetails({ sessionId }) {
  const [details, setDetails] = useState(null);
  const [products, setProducts] = useState([]);
  const [drafts, setDrafts] = useState({});
  const [request, setRequest] = useState({ productId: "", unitName: "", quantity: "", note: "" });
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    let active = true;
    Promise.all([stockClient.refereeDetails(sessionId), stockClient.refereeProducts()])
      .then(([d, p]) => {
        if (!active) return;
        setDetails(d);
        setProducts(p);
      })
      .catch((e) => active && setError(e.message));
    return () => {
      active = false;
    };
  }, [sessionId]);

  async function run(action, message) {
    setBusy(true);
    setError("");
    setNotice("");
    try {
      setDetails(await action());
      setNotice(message);
      return true;
    } catch (e) {
      setError(e.message);
      return false;
    } finally {
      setBusy(false);
    }
  }

  function draft(a) {
    return drafts[a.productId] ?? { used: a.reportedUsed ?? "", returned: a.returnOffered ?? "" };
  }

  async function report(a) {
    const d = draft(a);
    const used = Number(d.used);
    const returned = d.returned === "" ? 0 : Number(d.returned);
    await run(
      () => stockClient.reportSupplyUse(sessionId, a.productId, used, returned),
      `Đã báo dùng ${number(used)}, trả lại ${number(returned)} ${a.name}. Mang số trả lại về quầy để nhân viên xác nhận thu hồi.`,
    );
  }

  async function submitRequest(e) {
    e.preventDefault();
    const product = products.find((p) => p.id === Number(request.productId));
    const ok = await run(
      () =>
        stockClient.requestSupply(sessionId, {
          productId: Number(request.productId),
          quantity: Number(request.quantity),
          unitName: request.unitName || null,
          note: request.note.trim() || null,
        }),
      `Đã gửi yêu cầu ${number(request.quantity)} ${request.unitName || product?.baseUnit || ""} × ${product?.name ?? "vật tư"}. Chờ nhân viên kho cấp.`,
    );
    if (ok) setRequest({ productId: "", unitName: "", quantity: "", note: "" });
  }

  if (!details)
    return error ? (
      <StatePanel kind="error" title="Không tải được vật tư" description={error} />
    ) : (
      <StatePanel kind="loading" title="Đang tải vật tư…" />
    );

  const { session, allocations } = details;
  const requests = details.requests ?? [];
  const selected = products.find((p) => p.id === Number(request.productId));
  const selectedUnits = [...(selected?.units ?? [])].sort((a, b) => a.factorToBase - b.factorToBase);
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
          <h4>Yêu cầu của bạn</h4>
          <ul>
            {requests.map((r) => (
              <li key={r.id}>
                <span>
                  <strong>
                    {r.name}: {number(r.enteredQuantity ?? r.quantity)} {r.enteredUnit ?? r.baseUnit}
                  </strong>
                  <small>
                    {r.status === "Issued" && r.issuedQuantity != null ? `Được cấp ${number(r.issuedQuantity)} ${r.baseUnit}` : ""}
                    {r.decisionNote ? `Lý do: ${r.decisionNote}` : ""}
                    {r.note ? ` · Ghi chú: ${r.note}` : ""}
                  </small>
                </span>
                {r.status === "Pending" ? (
                  <span className="stock-actions">
                    <span className="stock-badge-warning">Chờ cấp</span>
                    <button
                      className="button button-secondary"
                      disabled={busy}
                      onClick={() => run(() => stockClient.cancelSupplyRequest(sessionId, r.id), `Đã hủy yêu cầu ${r.name}.`)}
                    >
                      Hủy yêu cầu
                    </button>
                  </span>
                ) : (
                  <span className={r.status === "Issued" ? "badge-lime" : "badge-gray"}>{requestStatusLabels[r.status] || r.status}</span>
                )}
              </li>
            ))}
          </ul>
        </div>
      )}

      {allocations.length === 0 ? (
        <p className="stock-hint">Buổi này chưa được cấp vật tư. Gửi yêu cầu bên dưới để nhân viên kho cấp.</p>
      ) : (
        <div className="stock-table-wrap">
          <table className="stock-table">
            <thead>
              <tr>
                <th scope="col">Vật tư</th>
                <th scope="col" className="num">Được cấp</th>
                <th scope="col">Người nhận</th>
                <th scope="col">Đã dùng / trả lại</th>
              </tr>
            </thead>
            <tbody>
              {allocations.map((a) => {
                const d = draft(a);
                const over = (Number(d.used) || 0) + (Number(d.returned) || 0) > a.outstanding;
                return (
                  <tr key={a.productId}>
                    <td data-label="Vật tư">
                      <strong>{a.name}</strong>
                      <span className="stock-sku">{a.sku}</span>
                    </td>
                    <td data-label="Được cấp" className="num">{number(a.issued)} {a.baseUnit}</td>
                    <td data-label="Người nhận">{a.recipientName || "—"}</td>
                    <td data-label="Đã dùng / trả lại">
                      {a.isReconciled ? (
                        <span className="badge-lime">
                          Đã chốt: dùng {number(a.used)}, nhân viên nhận lại {number(a.returned)} {a.baseUnit}
                        </span>
                      ) : (
                        <form
                          className="supply-use"
                          onSubmit={(e) => {
                            e.preventDefault();
                            report(a);
                          }}
                        >
                          <label>
                            Đã dùng
                            <input
                              type="number"
                              inputMode="numeric"
                              min="0"
                              max={a.outstanding}
                              value={d.used}
                              onChange={(e) => setDrafts({ ...drafts, [a.productId]: { ...d, used: e.target.value } })}
                              required
                            />
                          </label>
                          <label>
                            Trả lại
                            <input
                              type="number"
                              inputMode="numeric"
                              min="0"
                              max={a.outstanding}
                              value={d.returned}
                              onChange={(e) => setDrafts({ ...drafts, [a.productId]: { ...d, returned: e.target.value } })}
                            />
                          </label>
                          <button className="button button-secondary" disabled={busy || over}>
                            {a.reportedUsed == null ? "Báo dùng" : "Cập nhật"}
                          </button>
                          {over && <small className="stock-error">Dùng + trả lại vượt số được cấp ({number(a.outstanding)}).</small>}
                        </form>
                      )}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      {session.status !== "Cancelled" && (
        <form className="stock-form supply-issue" onSubmit={submitRequest}>
          <h4>
            <PackagePlus size={16} aria-hidden="true" /> Yêu cầu vật tư cho buổi
          </h4>
          <div className="stock-form-row">
            <label>
              Vật tư
              <select
                value={request.productId}
                onChange={(e) => setRequest({ ...request, productId: e.target.value, unitName: "" })}
                required
              >
                <option value="">Chọn vật tư</option>
                {products.map((p) => (
                  <option key={p.id} value={p.id}>
                    {p.name} (còn {number(p.availableQuantity)} {p.baseUnit})
                  </option>
                ))}
              </select>
            </label>
            <label>
              Đơn vị
              <select
                value={request.unitName}
                disabled={!selected}
                onChange={(e) => setRequest({ ...request, unitName: e.target.value })}
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
                max="100000"
                value={request.quantity}
                onChange={(e) => setRequest({ ...request, quantity: e.target.value })}
                required
              />
            </label>
          </div>
          <label>
            Ghi chú
            <input maxLength={300} value={request.note} onChange={(e) => setRequest({ ...request, note: e.target.value })} />
          </label>
          <div className="stock-form-actions">
            <button className="button button-primary" disabled={busy}>
              {busy ? "Đang gửi…" : "Gửi yêu cầu"}
            </button>
          </div>
        </form>
      )}
      <p className="stock-hint">
        Số còn dư mang trả quầy kho; nhân viên xác nhận số thực nhận. Phần thiếu so với số bạn báo dùng được ghi lại để kiểm kê.
      </p>
    </div>
  );
}
