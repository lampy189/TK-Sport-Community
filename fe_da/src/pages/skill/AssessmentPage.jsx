import { useEffect, useState } from "react";
import SectionLayout from "../../components/layout/SectionLayout";
import { workflowRequest } from "../workflowClient";
import "../workflow.css";

export default function AssessmentPage() {
  const [sessions, setSessions] = useState([]);
  const [sessionId, setSessionId] = useState("");
  const [players, setPlayers] = useState([]);
  const [form, setForm] = useState(null);
  const [dirty, setDirty] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");
  useEffect(() => {
    let active = true;
    workflowRequest("/api/v1/skill/sessions").then(data => { if (active) setSessions(data); })
      .catch(e => { if (active) setError(e.message); });
    return () => { active = false; };
  }, []);
  async function run(action) {
    setBusy(true); setError(""); setMessage("");
    try { await action(); } catch (e) { setError(e.message); } finally { setBusy(false); }
  }
  function selectSession(id) {
    setSessionId(id); setPlayers([]); setForm(null); setDirty(false);
    if (id) run(async () => setPlayers(await workflowRequest(`/api/v1/skill/sessions/${id}/attendees`)));
  }
  function open(playerId) {
    run(async () => {
      setForm(await workflowRequest(`/api/v1/skill/sessions/${sessionId}/players/${playerId}/assessment`, "POST"));
      setDirty(false);
    });
  }
  function mark(id, key, value) {
    setForm(current => ({ ...current, marks: current.marks.map(m => m.dimensionId === id ? { ...m, [key]: value } : m) }));
    setDirty(true); setMessage("");
  }
  const published = form?.status === "published";
  return <SectionLayout title="Đánh giá kỹ năng" description="Lưu phiếu quan sát, kiểm tra kết quả và công bố điểm chính thức.">
    <div className="workflow">
      {error && <p role="alert" className="workflow-error">{error}</p>}
      {message && <p role="status">{message}</p>}
      <label>Buổi được phân công<select value={sessionId} disabled={busy || dirty} onChange={e => selectSession(e.target.value)}>
        <option value="">Chọn buổi chơi</option>{sessions.map(s => <option key={s.id} value={s.id}>{s.sessionDate} · {s.startTime} · {s.name} · {s.court}</option>)}
      </select></label>
      {sessionId && !busy && players.length === 0 && <p>Chưa có người chơi đủ điều kiện: vé đã thanh toán và đã điểm danh.</p>}
      <div className="workflow-actions">{players.map(p => <button key={p.playerId} disabled={busy || dirty || !p.profileId} onClick={() => open(p.playerId)} className="button button-secondary">{p.name} · {p.band || "Chưa xếp hạng"}</button>)}</div>
      {form && <>
        <h3>Phiếu #{form.id} · {players.find(p => p.playerId === form.playerId)?.name}</h3>
        <p>{form.rubric.name} · {form.rubric.version} · {published ? "Đã công bố" : "Bản nháp"}</p>
        <fieldset disabled={busy || published}>
          {form.rubric.criteria.map(c => {
            const m = form.marks.find(m => m.dimensionId === c.id);
            return <div className="workflow-criterion" key={c.id}><label>{c.code} — {c.name}
              <select value={m.score ?? ""} disabled={!form.rubric.scoringSupported} onChange={e => mark(c.id, "score", e.target.value === "" ? null : Number(e.target.value))}>
                <option value="">Chưa quan sát</option>{[0, 1, 2, 3, 4].map(n => <option key={n} value={n}>{n}</option>)}
              </select></label><label>Bằng chứng quan sát<textarea maxLength={2000} value={m.evidence || ""} onChange={e => mark(c.id, "evidence", e.target.value)} /></label></div>;
          })}
        </fieldset>
        <p>Kết quả đã lưu: {form.totalScore ?? "Chưa đủ quan sát"} {form.bandResult && `· ${form.bandResult}`}</p>
        {published && <p role="status">Điểm chính thức trên hồ sơ: {form.currentScore} · {form.currentBand} · Rated</p>}
        {!published && <div className="workflow-actions">
          <button className="button button-secondary" disabled={busy} onClick={() => run(async () => {
            setForm(await workflowRequest(`/api/v1/skill/assessments/${form.id}`, "PUT", { revision: form.revision, marks: form.marks }));
            setDirty(false); setMessage("Đã lưu bản ghi đánh giá. Kiểm tra điểm trước khi công bố.");
          })}>Lưu bản ghi đánh giá</button>
          <button className="button button-primary" disabled={busy || dirty || form.totalScore == null || !form.rubric.scoringSupported} onClick={() => run(async () => {
            setForm(await workflowRequest(`/api/v1/skill/assessments/${form.id}/publish`, "POST", { revision: form.revision, expectedScore: form.currentScore, expectedBand: form.currentBand }));
            setMessage("Đã công bố điểm. Người chơi có thể xem kết quả chính thức trong Hồ sơ kỹ năng.");
          })}>Công bố điểm</button>
          <button className="button button-secondary" disabled={busy} onClick={() => run(async () => {
            setForm(await workflowRequest(`/api/v1/skill/assessments/${form.id}`)); setDirty(false);
          })}>Tải lại bản đã lưu</button>
        </div>}
        {dirty && <p>Phiếu có thay đổi chưa lưu. Lưu trước khi công bố hoặc chọn người chơi khác.</p>}
      </>}
    </div>
  </SectionLayout>;
}
