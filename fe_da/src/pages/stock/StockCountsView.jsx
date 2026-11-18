import { useEffect, useState } from "react";
import { Check, ClipboardCheck, RefreshCw, X } from "lucide-react";
import SectionLayout from "../../components/layout/SectionLayout";
import StatePanel from "../../components/ui/StatePanel";
import Dialog from "./StockDialog";
import { dateTime, number } from "./stockFormat";
import { countStatusLabels, stockClient } from "./stockClient";

const statusFilters = [
  { value: "Pending", label: "Chờ duyệt" },
  { value: "", label: "Tất cả" },
  { value: "Applied", label: "Đã điều chỉnh" },
  { value: "Rejected", label: "Bị từ chối" },
  { value: "NoDifference", label: "Khớp sổ" },
];

const signed = (value, unit) => `${value > 0 ? "+" : ""}${number(value)} ${unit}`;

// RP3 UC-43 / UC-44 (task split 13/11/2026): Staff counts and proposes; the Manager decides; stock moves once.
export default function StockCountsView({ role = "staff" }) {
  const canDecide = role === "manager" || role === "owner";
  const [status, setStatus] = useState(canDecide ? "Pending" : "");
  const [state, setState] = useState({ loading: true, items: [], error: "" });
  const [reloadKey, setReloadKey] = useState(0);
  const [decision, setDecision] = useState(null);
  const [notice, setNotice] = useState("");

  useEffect(() => {
    let active = true;
    stockClient
      .counts(status)
      .then((items) => active && setState({ loading: false, items, error: "" }))
      .catch((err) => active && setState({ loading: false, items: [], error: err.message }));
    return () => {
      active = false;
    };
  }, [status, reloadKey]);

  function reload() {
    setState((s) => ({ ...s, loading: true }));
    setReloadKey((k) => k + 1);
  }

  function decided(count) {
    setDecision(null);
    setNotice(
      count.status === "Applied"
        ? `Đã duyệt: ${count.name} ${signed(count.difference, count.baseUnit)}. Tồn kho hiện tại ${number(count.currentOnHand)} ${count.baseUnit} (phiếu điều chỉnh #${count.appliedMovementId}).`
        : `Đã từ chối đề xuất của ${count.name}. Tồn kho không thay đổi.`,
    );
    reload();
  }

  return (
    <SectionLayout
      title={canDecide ? "Duyệt điều chỉnh kho" : "Kiểm kê & điều chỉnh"}
      description={
        canDecide
          ? "Xem số đếm thực tế, lý do và bằng chứng của nhân viên. Chỉ khi duyệt, tồn kho mới được điều chỉnh, đúng một lần."
          : "Ghi số đếm thực tế từ nút Kiểm kê ở danh mục. Chênh lệch được gửi Quản lý duyệt; tồn kho không đổi khi chưa duyệt."
      }
      action={
        <button className="button button-secondary" onClick={reload} disabled={state.loading}>
          <RefreshCw size={15} aria-hidden="true" /> Tải lại
        </button>
      }
      className="stock-page"
    >
      <div className="stock-toolbar">
        <label>
          <span className="sr-only">Trạng thái</span>
          <select value={status} onChange={(e) => setStatus(e.target.value)}>
            {statusFilters.map((f) => (
              <option key={f.value} value={f.value}>
                {f.label}
              </option>
            ))}
          </select>
        </label>
      </div>

      {notice && (
        <p className="stock-notice" role="status">
          {notice}
          <button aria-label="Đóng thông báo" onClick={() => setNotice("")}>
            <X size={14} />
          </button>
        </p>
      )}

      {state.loading ? (
        <StatePanel kind="loading" title="Đang tải phiếu kiểm kê…" />
      ) : state.error ? (
        <StatePanel kind="error" title="Không tải được phiếu kiểm kê" description={state.error} />
      ) : state.items.length === 0 ? (
        <StatePanel
          title={status === "Pending" ? "Không có đề xuất chờ duyệt" : "Chưa có phiếu kiểm kê"}
          description={canDecide ? "Đề xuất điều chỉnh mới của nhân viên sẽ hiện ở đây." : "Dùng nút Kiểm kê ở danh mục để ghi số đếm thực tế."}
        />
      ) : (
        <div className="stock-table-wrap">
          <table className="stock-table">
            <thead>
              <tr>
                <th scope="col">Mặt hàng</th>
                <th scope="col" className="num">Sổ sách</th>
                <th scope="col" className="num">Thực đếm</th>
                <th scope="col" className="num">Chênh lệch</th>
                <th scope="col">Lý do / bằng chứng</th>
                <th scope="col">Trạng thái</th>
                {canDecide && <th scope="col"><span className="sr-only">Thao tác</span></th>}
              </tr>
            </thead>
            <tbody>
              {state.items.map((c) => (
                <tr key={c.id}>
                  <td data-label="Mặt hàng">
                    <strong>{c.name}</strong>
                    <span className="stock-sku">
                      {c.sku} · Phiếu #{c.id} · {c.countedByName} · {dateTime(c.countedAt)}
                    </span>
                  </td>
                  <td data-label="Sổ sách" className="num">{number(c.systemQuantity)} {c.baseUnit}</td>
                  <td data-label="Thực đếm" className="num">
                    {number(c.countedQuantity)} {c.baseUnit}
                    {c.enteredUnit && c.enteredQuantity != null && c.enteredUnit !== c.baseUnit && (
                      <span className="stock-reorder">({number(c.enteredQuantity)} {c.enteredUnit})</span>
                    )}
                  </td>
                  <td data-label="Chênh lệch" className="num">
                    <strong className={c.difference < 0 ? "is-out" : c.difference > 0 ? "is-in" : ""}>
                      {signed(c.difference, c.baseUnit)}
                    </strong>
                  </td>
                  <td data-label="Lý do">
                    {c.reason || "—"}
                    {c.evidence && <span className="stock-reorder">Bằng chứng: {c.evidence}</span>}
                    {c.decisionNote && (
                      <span className="stock-reorder">
                        {c.decidedByName}: {c.decisionNote}
                      </span>
                    )}
                  </td>
                  <td data-label="Trạng thái">
                    <span className={c.status === "Pending" ? "stock-badge-warning" : c.status === "Rejected" ? "badge-gray" : "badge-lime"}>
                      {countStatusLabels[c.status] || c.status}
                    </span>
                    {c.decidedAt && <span className="stock-reorder">{dateTime(c.decidedAt)}</span>}
                  </td>
                  {canDecide && (
                    <td className="stock-actions">
                      {c.status === "Pending" && (
                        <>
                          <button className="button button-primary" onClick={() => setDecision({ count: c, approve: true })}>
                            <Check size={15} aria-hidden="true" /> Duyệt
                          </button>
                          <button className="button button-secondary" onClick={() => setDecision({ count: c, approve: false })}>
                            Từ chối
                          </button>
                        </>
                      )}
                    </td>
                  )}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {decision && (
        <DecisionDialog
          count={decision.count}
          approve={decision.approve}
          onClose={() => setDecision(null)}
          onDone={decided}
        />
      )}
    </SectionLayout>
  );
}

function DecisionDialog({ count, approve, onClose, onDone }) {
  const [note, setNote] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const after = count.currentOnHand + count.difference;

  async function submit(e) {
    e.preventDefault();
    setBusy(true);
    setError("");
    try {
      onDone(
        approve
          ? await stockClient.approveCount(count.id, count.difference, note)
          : await stockClient.rejectCount(count.id, note),
      );
    } catch (err) {
      setError(err.message);
      setBusy(false);
    }
  }

  return (
    <Dialog title={`${approve ? "Duyệt" : "Từ chối"} điều chỉnh – ${count.name}`} onClose={onClose}>
      <form className="stock-form" onSubmit={submit}>
        <div className="stock-balance">
          <div>
            <span>Tồn hiện tại</span>
            <strong>{number(count.currentOnHand)} {count.baseUnit}</strong>
          </div>
          <div>
            <span>{approve ? "Sau khi duyệt" : "Chênh lệch đề xuất"}</span>
            <strong>
              {approve ? `${number(after)} ${count.baseUnit}` : signed(count.difference, count.baseUnit)}
            </strong>
          </div>
        </div>
        <p className="stock-hint">
          Nhân viên {count.countedByName} đếm {number(count.countedQuantity)} {count.baseUnit} khi sổ ghi{" "}
          {number(count.systemQuantity)} {count.baseUnit}. Lý do: {count.reason || "—"}
          {count.evidence ? `. Bằng chứng: ${count.evidence}` : ""}.
        </p>
        <label>
          {approve ? "Ghi chú (không bắt buộc)" : "Lý do từ chối"}
          <textarea
            rows={3}
            maxLength={500}
            value={note}
            onChange={(e) => setNote(e.target.value)}
            required={!approve}
            autoFocus
          />
        </label>
        {approve && (
          <p className="stock-hint">
            Duyệt sẽ ghi một phiếu &quot;Điều chỉnh sau kiểm kê&quot; {signed(count.difference, count.baseUnit)}. Bấm lại nhiều lần cũng chỉ điều chỉnh một lần.
          </p>
        )}
        {error && (
          <p className="stock-error" role="alert">
            {error}
          </p>
        )}
        <div className="stock-form-actions">
          <button type="button" className="button button-secondary" onClick={onClose}>
            Đóng
          </button>
          <button className={approve ? "button button-primary" : "button button-secondary"} disabled={busy}>
            {busy ? "Đang ghi…" : approve ? "Xác nhận duyệt" : "Xác nhận từ chối"}
          </button>
        </div>
      </form>
    </Dialog>
  );
}

// Count form opened from the catalogue: the stock version is kept so a change during counting is detected.
export function CountDialog({ product, onClose, onSaved }) {
  const [sheet, setSheet] = useState(null);
  const [loadError, setLoadError] = useState("");
  const [unitName, setUnitName] = useState(product.baseUnit);
  const [quantity, setQuantity] = useState("");
  const [reason, setReason] = useState("");
  const [evidence, setEvidence] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    let active = true;
    stockClient
      .countSheet(product.id)
      .then((s) => active && setSheet(s))
      .catch((err) => active && setLoadError(err.message));
    return () => {
      active = false;
    };
  }, [product.id]);

  const units = [...(sheet?.units ?? product.units ?? [])].sort((a, b) => a.factorToBase - b.factorToBase);
  const factor = units.find((u) => u.unitName === unitName)?.factorToBase ?? 1;
  const counted = quantity === "" ? null : Number(quantity) * factor;
  const difference = sheet && counted != null ? counted - sheet.quantityOnHand : null;

  async function submit(e) {
    e.preventDefault();
    setBusy(true);
    setError("");
    try {
      onSaved(
        await stockClient.createCount({
          productId: product.id,
          countedQuantity: Number(quantity),
          unitName,
          stockVersion: sheet.stockVersion,
          reason: reason.trim() || null,
          evidence: evidence.trim() || null,
        }),
      );
    } catch (err) {
      if (err.data?.sheet) setSheet(err.data.sheet);
      setError(err.message);
      setBusy(false);
    }
  }

  return (
    <Dialog title={`Kiểm kê – ${product.name}`} onClose={onClose}>
      {loadError ? (
        <p className="stock-error" role="alert">{loadError}</p>
      ) : !sheet ? (
        <p className="stock-hint">Đang tải số liệu sổ sách…</p>
      ) : (
        <form className="stock-form" onSubmit={submit}>
          <div className="stock-balance">
            <div>
              <span>Sổ sách</span>
              <strong>{number(sheet.quantityOnHand)} {sheet.baseUnit}</strong>
            </div>
            <div>
              <span>Chênh lệch</span>
              <strong className={difference < 0 ? "is-invalid" : ""}>
                {difference == null ? "–" : signed(difference, sheet.baseUnit)}
              </strong>
            </div>
          </div>
          {sheet.reservedQuantity > 0 && (
            <p className="stock-hint">
              Trong đó {number(sheet.reservedQuantity)} {sheet.baseUnit} đang giữ cho đơn chưa giao: vẫn phải có mặt trong kho khi đếm.
            </p>
          )}
          <div className="stock-form-row">
            <label>
              Đơn vị đếm
              <select value={unitName} onChange={(e) => setUnitName(e.target.value)}>
                {units.map((u) => (
                  <option key={u.unitName} value={u.unitName}>
                    {u.unitName}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Số lượng thực đếm
              <input
                type="number"
                inputMode="numeric"
                min="0"
                max="100000"
                value={quantity}
                onChange={(e) => setQuantity(e.target.value)}
                required
                autoFocus
              />
            </label>
          </div>
          {factor > 1 && counted != null && (
            <p className="stock-hint">
              {number(Number(quantity))} {unitName} = {number(counted)} {sheet.baseUnit}
            </p>
          )}
          <label>
            Lý do chênh lệch {difference ? "(bắt buộc)" : ""}
            <textarea rows={2} maxLength={500} value={reason} onChange={(e) => setReason(e.target.value)} required={Boolean(difference)} />
          </label>
          <label>
            Bằng chứng (số biên bản, ảnh, người chứng kiến…)
            <input maxLength={500} value={evidence} onChange={(e) => setEvidence(e.target.value)} />
          </label>
          <p className="stock-hint">
            <ClipboardCheck size={14} aria-hidden="true" /> Lưu phiếu không làm thay đổi tồn kho. Có chênh lệch thì đề xuất được gửi Quản lý duyệt.
          </p>
          {error && (
            <p className="stock-error" role="alert">
              {error}
            </p>
          )}
          <div className="stock-form-actions">
            <button type="button" className="button button-secondary" onClick={onClose}>
              Hủy
            </button>
            <button className="button button-primary" disabled={busy || quantity === ""}>
              {busy ? "Đang lưu…" : difference ? "Gửi đề xuất điều chỉnh" : "Lưu phiếu kiểm kê"}
            </button>
          </div>
        </form>
      )}
    </Dialog>
  );
}
