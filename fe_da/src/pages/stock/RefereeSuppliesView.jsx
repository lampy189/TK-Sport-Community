import { useEffect, useState } from "react";
import { CalendarDays } from "lucide-react";
import SectionLayout from "../../components/layout/SectionLayout";
import StatePanel from "../../components/ui/StatePanel";
import { stockClient } from "./stockClient";
import "./stock.css";

const number = (value) => new Intl.NumberFormat("vi-VN").format(value);
const vietnamToday = () =>
  new Date(Date.now() + 7 * 3600_000).toISOString().slice(0, 10);

// UC-42: the assigned Referee sees issued supplies and reports observed use; Staff confirms returns.
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
      description="Xem vật tư được cấp cho buổi bạn phụ trách và báo số đã dùng sau buổi."
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
  const [drafts, setDrafts] = useState({});
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    let active = true;
    stockClient
      .refereeDetails(sessionId)
      .then((d) => active && setDetails(d))
      .catch((e) => active && setError(e.message));
    return () => {
      active = false;
    };
  }, [sessionId]);

  async function report(allocation) {
    const used = Number(drafts[allocation.productId]);
    setBusy(true);
    setError("");
    setNotice("");
    try {
      setDetails(await stockClient.reportSupplyUse(sessionId, allocation.productId, used));
      setNotice(`Đã báo dùng ${number(used)} ${allocation.name}. Nhân viên sẽ xác nhận số trả lại kho.`);
    } catch (e) {
      setError(e.message);
    } finally {
      setBusy(false);
    }
  }

  if (!details)
    return error ? (
      <StatePanel kind="error" title="Không tải được vật tư" description={error} />
    ) : (
      <StatePanel kind="loading" title="Đang tải vật tư…" />
    );

  const { session, allocations } = details;
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
        <p className="stock-hint">Buổi này chưa được cấp vật tư. Liên hệ nhân viên kho nếu cần.</p>
      ) : (
        <div className="stock-table-wrap">
          <table className="stock-table">
            <thead>
              <tr>
                <th scope="col">Vật tư</th>
                <th scope="col" className="num">Được cấp</th>
                <th scope="col">Người nhận</th>
                <th scope="col">Số đã dùng</th>
              </tr>
            </thead>
            <tbody>
              {allocations.map((a) => (
                <tr key={a.productId}>
                  <td data-label="Vật tư">
                    <strong>{a.name}</strong>
                    <span className="stock-sku">{a.sku}</span>
                  </td>
<<<<<<< HEAD
                  <td data-label="Được cấp" className="num">{number(a.issued)}</td>
                  <td data-label="Người nhận">{a.recipientName || "—"}</td>
                  <td data-label="Số đã dùng">
                    {a.isReconciled ? (
                      <span className="badge-lime">Đã chốt: dùng {number(a.used)}</span>
=======
                  <td data-label="Được cấp" className="num">{number(a.issued)} {a.baseUnit}</td>
                  <td data-label="Người nhận">{a.recipientName || "—"}</td>
                  <td data-label="Số đã dùng">
                    {a.isReconciled ? (
                      <span className="badge-lime">Đã chốt: dùng {number(a.used)} {a.baseUnit}</span>
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
                    ) : (
                      <form
                        className="supply-use"
                        onSubmit={(e) => {
                          e.preventDefault();
                          report(a);
                        }}
                      >
                        <label>
<<<<<<< HEAD
                          <span className="sr-only">Số {a.name} đã dùng</span>
=======
                          <span className="sr-only">Số {a.name} đã dùng ({a.baseUnit})</span>
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
                          <input
                            type="number"
                            inputMode="numeric"
                            min="0"
                            max={a.outstanding}
                            value={drafts[a.productId] ?? a.reportedUsed ?? ""}
                            onChange={(e) => setDrafts({ ...drafts, [a.productId]: e.target.value })}
                            required
                          />
                        </label>
                        <button className="button button-secondary" disabled={busy}>
                          {a.reportedUsed == null ? "Báo dùng" : "Cập nhật"}
                        </button>
                      </form>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
      <p className="stock-hint">
        Số còn dư mang trả quầy kho; nhân viên xác nhận số thực nhận và chốt vật tư cho buổi.
      </p>
    </div>
  );
}
