import { useEffect, useState } from "react";
import { workflowRequest } from "../workflowClient";
import "../workflow.css";
import "./skill-reviews.css";

const base = "/api/v1/skill-reviews";
const labels = { Pending: "Chờ xử lý", Approved: "Đã duyệt", Rejected: "Từ chối", UnderReview: "Đang xem xét", Kept: "Giữ nguyên điểm", Corrected: "Đã điều chỉnh điểm" };

function ReasonForm({ label, onSubmit, children, decisions, maxLength = 2000 }) {
  const [reason, setReason] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  return <form onSubmit={async e => {
    e.preventDefault(); if (busy || !reason.trim()) return; setBusy(true); setError("");
    try { await onSubmit(reason.trim(), e.nativeEvent.submitter?.value); setReason(""); } catch (err) { setError(err.message); } finally { setBusy(false); }
  }}>
    <label>Lý do / nhận xét<textarea required maxLength={maxLength} value={reason} onChange={e => setReason(e.target.value)} /></label>
    {children}
    {error && <p role="alert">{error}</p>}
    {decisions ? <div className="workflow-actions">{decisions.map(action => <button key={action.value} type="submit" value={action.value} className={action.value === "approve" ? "button button-primary" : "button button-secondary"} disabled={busy || !reason.trim()}>{busy ? "Đang lưu…" : action.label}</button>)}</div> : <button className="button button-primary" disabled={busy || !reason.trim()}>{busy ? "Đang lưu…" : label}</button>}
  </form>;
}

export function ExceptionRequest({ sessionId, user, onLogin, onUpdated, canRequest = true, defaultOpen = false }) {
  const [rows, setRows] = useState([]);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [checked, setChecked] = useState(false);
  const [open, setOpen] = useState(defaultOpen);
  async function load() { setRows(await workflowRequest(`${base}/exceptions?sessionId=${sessionId}`)); }
  useEffect(() => {
    setOpen(defaultOpen);
  }, [defaultOpen]);
  useEffect(() => {
    let active = true;
    if (!user) return;
    workflowRequest(`${base}/exceptions?sessionId=${sessionId}`).then(data => { if (active) setRows(data); })
      .catch(err => { if (active) setError(err.message); }).finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [sessionId, user]);
  if (!user) return canRequest ? <button className="button button-secondary booking-action" onClick={onLogin}>Đăng nhập để xin ngoại lệ trình độ</button> : null;
  if (loading) return canRequest ? <p style={{ fontSize: "13px", color: "#64748b", margin: "8px 0 0" }}>Đang kiểm tra yêu cầu ngoại lệ…</p> : null;
  if (!rows.length && !canRequest) return null;
  if (!rows.length && !open) {
    return (
      <button
        type="button"
        className="button button-secondary booking-action"
        onClick={() => setOpen(true)}
      >
        Xin ngoại lệ trình độ
      </button>
    );
  }
  return <section className="skill-reviews" aria-label="Xin ngoại lệ trình độ">
    <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", gap: "8px" }}>
      <h3>Xin ngoại lệ trình độ</h3>
      {!rows.length && (
        <button type="button" className="text-button" style={{ fontSize: "13px" }} onClick={() => setOpen(false)}>
          Thu gọn
        </button>
      )}
    </div>
    {error && <p role="alert">{error}</p>}
    {rows.length ? <>
      <div className={`exception-result is-${rows[0].status.toLowerCase()}`} role="status" aria-live="polite">
        <span className="exception-result-label">KẾT QUẢ YÊU CẦU</span>
        <strong className="exception-result-title">{labels[rows[0].status] || rows[0].status}</strong>
        <p>{rows[0].status === "Approved" ? "Bạn đã được duyệt ngoại lệ trình độ cho buổi này. Hãy bấm Đăng ký buổi chơi để tiếp tục; chỗ trống và các điều kiện khác vẫn được kiểm tra."
          : rows[0].status === "Rejected" ? "Yêu cầu chưa được chấp thuận. Bạn có thể chọn một buổi phù hợp với trình độ hiện tại."
          : "Yêu cầu đã được gửi. Trọng tài hoặc Quản lý sẽ xem xét trước khi bạn đăng ký."}</p>
        <div className="exception-result-reason"><span>{rows[0].status === "Pending" ? "Lý do bạn đã gửi" : "Phản hồi từ người duyệt"}</span>
          <p>{rows[0].events?.at(-1)?.reason || rows[0].reason}</p>
        </div>
        {checked && <small>Đã cập nhật kết quả mới nhất.</small>}
      </div>
      <button className="button button-secondary" disabled={refreshing} onClick={async () => {
        setRefreshing(true); setError(""); setChecked(false);
        try { await load(); await onUpdated(); setChecked(true); } catch (err) { setError(err.message); } finally { setRefreshing(false); }
      }}>{refreshing ? "Đang kiểm tra…" : "Kiểm tra kết quả"}</button>
    </> : <ReasonForm maxLength={255} label="Gửi yêu cầu ngoại lệ" onSubmit={async reason => {
      await workflowRequest(`${base}/sessions/${sessionId}/exceptions`, "POST", { reason }); await load();
    }} />}
  </section>;
}

function Decision({ row, kind, role, reload }) {
  const [choice, setChoice] = useState("keep");
  const [score, setScore] = useState("");
  if (kind === "exceptions" && row.status === "Pending" && ["referee", "manager"].includes(role)) return <ReasonForm
    decisions={[{ value: "approve", label: "Chấp nhận" }, { value: "reject", label: "Từ chối" }]}
    onSubmit={async (reason, decision) => {
      if (!["approve", "reject"].includes(decision)) return;
      await workflowRequest(`${base}/exceptions/${row.id}/decision`, "POST", { reason, approved: decision === "approve" });
      await reload();
    }} />;
  if (kind !== "appeals" || !["Pending", "UnderReview"].includes(row.status)) return null;
  if (role === "referee") return <ReasonForm label="Gửi nhận xét nội bộ" onSubmit={async reason => {
    await workflowRequest(`${base}/appeals/${row.id}/review`, "POST", { reason }); await reload();
  }} />;
  if (role !== "manager") return null;
  if (row.status === "Pending") return <p>Chờ Trọng tài ghi nhận ý kiến review trước khi quyết định.</p>;
  return <ReasonForm label="Phê duyệt kết quả" onSubmit={async reason => {
    await workflowRequest(`${base}/appeals/${row.id}/decision`, "POST", { reason, correct: choice === "correct", score: choice === "correct" ? Number(score) : null, expectedHistoryId: row.expectedHistoryId }); await reload();
  }}><label>Quyết định<select value={choice} onChange={e => setChoice(e.target.value)}><option value="keep">Giữ nguyên điểm</option><option value="correct">Điều chỉnh điểm</option></select></label>
    {choice === "correct" && <label>Điểm điều chỉnh (0–100)<input type="number" min="0" max="100" step="1" required value={score} onChange={e => setScore(e.target.value)} /></label>}
  </ReasonForm>;
}

export default function SkillReviews({ role, kind = "appeals", sportId }) {
  return <ReviewList key={`${role}-${kind}-${sportId}`} role={role} kind={kind} sportId={sportId} />;
}

function ReviewList({ role, kind, sportId }) {
  const [rows, setRows] = useState([]);
  const [results, setResults] = useState([]);
  const [selected, setSelected] = useState("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  async function reload() { setRows(await workflowRequest(`${base}/${kind}`)); }
  useEffect(() => {
    let active = true;
    Promise.all([workflowRequest(`${base}/${kind}`), role === "player" && sportId
      ? workflowRequest(`/api/v1/skill/my-results?sportId=${sportId}`) : Promise.resolve([])])
      .then(([items, history]) => { if (active) { setRows(items); setResults(history.filter((r, i) => history.findIndex(h => h.assessmentId === r.assessmentId) === i)); } })
      .catch(err => { if (active) setError(err.message); }).finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [role, kind, sportId]);
  return <section className="workflow-page skill-reviews">
    <h2>{kind === "exceptions" ? "Ngoại lệ trình độ" : "Khiếu nại đánh giá"}</h2>
    {role === "referee" && kind === "exceptions" && <p>Danh sách yêu cầu của các buổi bạn được phân công làm Trọng tài.</p>}
    {loading ? <p role="status">Đang tải…</p> : error ? <p role="alert">{error}</p> : <>
      <button className="button button-secondary" onClick={() => reload().catch(err => setError(err.message))}>Tải lại danh sách</button>
      {role === "player" && kind === "appeals" && (results.length ? <>
        <label>Kết quả muốn khiếu nại<select value={selected} onChange={e => setSelected(e.target.value)}><option value="">Chọn kết quả đã công bố</option>
          {results.map(r => <option key={r.id} value={r.assessmentId}>Kết quả #{r.id} — {r.score} điểm / {r.band}</option>)}</select></label>
        {selected && <ReasonForm key={selected} label="Gửi khiếu nại" onSubmit={async reason => {
          await workflowRequest(`${base}/assessments/${selected}/appeals`, "POST", { reason }); await reload(); setSelected("");
        }} />}
      </> : <p>Chưa có kết quả Trọng tài đã công bố cho môn này để khiếu nại.</p>)}
      {!rows.length && <p>{role === "referee" && kind === "exceptions" ? "Chưa có yêu cầu trong các buổi bạn được phân công. Nếu Player đã gửi đơn, hãy nhờ Quản lý kiểm tra phân công Trọng tài của buổi đó." : "Chưa có yêu cầu."}</p>}
      {rows.map(row => <article className="detail-card" key={row.id}>
        <h3>#{row.id} · {row.player} · {row.sport}</h3><p><strong>{labels[row.status] || row.status}</strong></p>
        {kind === "exceptions" ? <p>Buổi #{row.sessionId} · {row.sessionDate} · Band yêu cầu: {row.sessionBand} · Band hiện tại: {row.currentBand}</p>
          : <p>Kết quả bị khiếu nại: {row.disputedScore} / {row.disputedBand} · Hiện tại: {row.currentScore} / {row.currentBand}</p>}
        {row.reason && <p>{row.reason}</p>}
        {row.events?.map((event, i) => <p key={i}><time>{new Date(event.at).toLocaleString("vi-VN")}</time> · {labels[event.status] || event.status}: {event.reason}</p>)}
        {row.history?.length > 0 && <details><summary>Lịch sử kết quả</summary>{row.history.map(h => <p key={h.id}>#{h.id}: {h.oldScore ?? "—"} / {h.oldBand ?? "—"} → {h.newScore} / {h.newBand}</p>)}</details>}
        {role !== "player" && row.evidence && <details><summary>Bằng chứng chấm điểm (nội bộ)</summary>{(row.evidence.Marks || []).map(m => <p key={m.DimensionId}>Tiêu chí #{m.DimensionId}: {m.Score ?? "Chưa quan sát"} — {m.Evidence || "Chưa có ghi chú"}</p>)}</details>}
        <Decision row={row} kind={kind} role={role} reload={reload} />
      </article>)}
    </>}
  </section>;
}
