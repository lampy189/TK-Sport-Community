import { useEffect, useState } from "react";
import { CircleCheck, RefreshCw, TriangleAlert } from "lucide-react";
import SectionLayout from "../../components/layout/SectionLayout";
import StatePanel from "../../components/ui/StatePanel";
import { number } from "./stockFormat";
import { stockClient } from "./stockClient";

const vnToday = () => new Date().toLocaleDateString("en-CA", { timeZone: "Asia/Ho_Chi_Minh" });
const monthStart = (day) => `${day.slice(0, 8)}01`;

// Supply and stock figures of the center for a period, plus the stock check:
// on hand = sum of all movements, reserved = lines of orders still holding stock.
export default function SupplyReportView() {
  const [to, setTo] = useState(vnToday);
  const [from, setFrom] = useState(() => monthStart(vnToday()));
  const [state, setState] = useState({ loading: true, report: null, recon: [], error: "" });
  const [reloadKey, setReloadKey] = useState(0);

  useEffect(() => {
    let active = true;
    Promise.all([stockClient.supplyReport(from, to), stockClient.reconciliation()])
      .then(([report, recon]) => active && setState({ loading: false, report, recon, error: "" }))
      .catch((err) => active && setState({ loading: false, report: null, recon: [], error: err.message }));
    return () => {
      active = false;
    };
  }, [from, to, reloadKey]);

  const mismatches = state.recon.filter((l) => !l.onHandMatches || !l.reservedMatches);

  return (
    <SectionLayout
      title="Báo cáo vật tư & đối soát tồn"
      description="Số nhập, bán, cấp cho buổi chơi, thu hồi, điều chỉnh trong kỳ và kiểm tra tồn kho khớp với lịch sử phiếu."
      action={
        <button className="button button-secondary" onClick={() => (setState((s) => ({ ...s, loading: true })), setReloadKey((k) => k + 1))}>
          <RefreshCw size={15} aria-hidden="true" /> Tải lại
        </button>
      }
      className="stock-page"
    >
      <div className="stock-toolbar">
        <label>
          Từ ngày
          <input type="date" value={from} max={to} onChange={(e) => e.target.value && setFrom(e.target.value)} />
        </label>
        <label>
          Đến ngày
          <input type="date" value={to} min={from} onChange={(e) => e.target.value && setTo(e.target.value)} />
        </label>
      </div>

      {state.loading ? (
        <StatePanel kind="loading" title="Đang tổng hợp số liệu…" />
      ) : state.error ? (
        <StatePanel kind="error" title="Không tải được báo cáo" description={state.error} />
      ) : (
        <>
          <p className={mismatches.length ? "stock-error" : "stock-notice"} role="status">
            {mismatches.length ? (
              <>
                <TriangleAlert size={15} aria-hidden="true" /> {mismatches.length} mặt hàng có tồn kho hoặc số đang giữ không khớp lịch sử phiếu. Cần kiểm kê.
              </>
            ) : (
              <>
                <CircleCheck size={15} aria-hidden="true" /> Tồn kho của tất cả mặt hàng khớp với lịch sử nhập/xuất, số đang giữ khớp với đơn chưa giao.
              </>
            )}
          </p>
          <div className="stock-table-wrap">
            <table className="stock-table">
              <thead>
                <tr>
                  <th scope="col">Mặt hàng</th>
                  <th scope="col" className="num">Nhập</th>
                  <th scope="col" className="num">Bán</th>
                  <th scope="col" className="num">Cấp buổi chơi</th>
                  <th scope="col" className="num">Thu hồi</th>
                  <th scope="col" className="num">Đã dùng</th>
                  <th scope="col" className="num">Thất thoát</th>
                  <th scope="col" className="num">Xuất khác</th>
                  <th scope="col" className="num">Điều chỉnh</th>
                  <th scope="col" className="num">Tồn / đang giữ</th>
                </tr>
              </thead>
              <tbody>
                {state.report.lines.map((l) => {
                  const recon = state.recon.find((r) => r.productId === l.productId);
                  const bad = recon && (!recon.onHandMatches || !recon.reservedMatches);
                  return (
                    <tr key={l.productId}>
                      <td data-label="Mặt hàng">
                        <strong>{l.name}</strong>
                        <span className="stock-sku">{l.sku} · {l.baseUnit}</span>
                      </td>
                      <td data-label="Nhập" className="num">{number(l.received)}</td>
                      <td data-label="Bán" className="num">{number(l.soldOut)}</td>
                      <td data-label="Cấp buổi chơi" className="num">{number(l.sessionIssued)}</td>
                      <td data-label="Thu hồi" className="num">{number(l.sessionReturned)}</td>
                      <td data-label="Đã dùng" className="num">{number(l.sessionUsed)}</td>
                      <td data-label="Thất thoát" className="num">
                        <span className={l.unaccountedSupply ? "is-out" : ""}>{number(l.unaccountedSupply)}</span>
                      </td>
                      <td data-label="Xuất khác" className="num">{number(l.manualIssued)}</td>
                      <td data-label="Điều chỉnh" className="num">
                        {l.adjustedIn ? `+${number(l.adjustedIn)}` : ""}
                        {l.adjustedIn && l.adjustedOut ? " / " : ""}
                        {l.adjustedOut ? `−${number(l.adjustedOut)}` : ""}
                        {!l.adjustedIn && !l.adjustedOut ? "0" : ""}
                      </td>
                      <td data-label="Tồn / đang giữ" className="num">
                        <strong className={bad ? "is-out" : ""}>
                          {number(l.quantityOnHand)} / {number(l.reservedQuantity)}
                        </strong>
                        {bad && (
                          <span className="stock-reorder">
                            Lịch sử phiếu: {number(recon.movementTotal)} · Đơn đang giữ: {number(recon.heldOrderQuantity)}
                          </span>
                        )}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
          <p className="stock-hint">
            Thất thoát = số đã cấp cho buổi chơi nhưng không được báo dùng và không được trả lại (ghi khi nhân viên xác nhận thu hồi).
            Dữ liệu cho báo cáo tổng hợp: GET /api/stock/reports/supplies?from=…&amp;to=….
          </p>
        </>
      )}
    </SectionLayout>
  );
}
