import { useEffect, useState } from "react";
import SectionLayout from "../../components/layout/SectionLayout";
import { workflowRequest } from "../workflowClient";
import "../workflow.css";
import "../../styles/skill-profile.css";
import "./assessment.css";
import { DIMENSION_ANCHORS } from "./dimensionAnchors";

// RP1 Appendix A2: keep the referee rubric distinct from the self-survey.
const observations = {
  D1: "Quan sát giao và trả cầu, phông, nâng, bỏ nhỏ, cầu lưới, tạt và đập cầu; chú ý khả năng kiểm soát và sự đa dạng kỹ thuật.",
  D2: "Quan sát tư thế sẵn sàng, di chuyển tới cầu, thăng bằng khi chạm cầu và hồi vị phù hợp.",
  D3: "Quan sát độ ổn định, điểm rơi và lỗi tự đánh hỏng qua nhiều pha cầu có tốc độ tương đương.",
  D4: "Quan sát lựa chọn giữ cầu, tấn công, phòng thủ, sử dụng khoảng trống và thích ứng với tình huống.",
  D5: "Quan sát giao tiếp, trách nhiệm cầu giữa sân, bọc lót và đội hình công–thủ khi phối hợp với đồng đội.",
};
const levels = [
  { score: 0, tag: "Chưa thể hiện", desc: "Cần hướng dẫn nhiều lần hoặc chưa thực hiện được trong tình huống quan sát đơn giản." },
  { score: 1, tag: "Bước đầu hình thành", desc: "Đôi lúc thực hiện được động tác đơn giản, nhưng chưa duy trì khi điều kiện thay đổi." },
  { score: 2, tag: "Đáp ứng cơ bản", desc: "Sử dụng được trong các pha cầu giao lưu thông thường, còn hạn chế khi chịu áp lực." },
  { score: 3, tag: "Ổn định", desc: "Lặp lại kỹ thuật và quyết định hiệu quả trong nhiều tình huống giao lưu tương đương." },
  { score: 4, tag: "Thích ứng tốt", desc: "Giữ kiểm soát và điều chỉnh hiệu quả trong các tình huống giao lưu có độ khó cao hơn." },
];

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
        {!form.rubric.scoringSupported && <div className="workflow-error" role="alert">
          <strong>Phiếu đang bị khóa chấm điểm</strong>
          <p>Hệ thống chưa bật chấm điểm cho bộ tiêu chí này. Nếu vừa cập nhật ứng dụng, cần khởi động lại backend rồi tải lại phiếu.</p>
          <button className="button button-secondary" disabled={busy} onClick={() => run(async () => {
            setForm(await workflowRequest(`/api/v1/skill/assessments/${form.id}`)); setDirty(false);
          })}>Tải lại phiếu</button>
        </div>}
        {form.rubric.version === "PK-IPTPA-DUPR-V1" && <p>Điểm Pickleball nội bộ: 5 tiêu chí, mỗi tiêu chí 0–4, trọng số 20%. Tổng điểm 0–100, Band B1–B5; không phải xếp hạng DUPR chính thức.</p>}
        <fieldset className="assessment-criteria sp-questions-list" disabled={busy || published}>
          <legend>Đánh giá từng tiêu chí</legend>
          {form.rubric.criteria.map((c, index) => {
            const m = form.marks.find(m => m.dimensionId === c.id);
            const anchors = DIMENSION_ANCHORS[c.code];
            return <section className="sp-question-card" key={c.id} aria-labelledby={`criterion-${c.id}`}>
              <div className="sp-question-head"><div>
                <span className="sp-q-index">Tiêu chí {index + 1} / {form.rubric.criteria.length} · {c.weightPercent}%</span>
                <h3 id={`criterion-${c.id}`}>{c.code} — {c.name}</h3>
                <p>{anchors?.subtitle || observations[c.code]}</p>
              </div><div className="sp-q-score-pill"><strong>{m.score == null ? "—" : `${m.score} / 4`}</strong><span>{m.score == null ? "Chưa quan sát" : "Điểm đã chọn"}</span></div></div>
              <div className="sp-level-options" role="radiogroup" aria-labelledby={`criterion-${c.id}`}>
                {[{ score: null, tag: "Chưa quan sát", desc: "Chưa đủ bằng chứng để chấm tiêu chí này. Không tính là 0 điểm." }, ...(anchors?.levels || levels)].map(level =>
                  <label key={level.score ?? "unobserved"} className={`sp-level-option ${m.score === level.score ? "is-checked" : ""}`}>
                    <input type="radio" name={`assessment-${form.id}-${c.id}`} checked={m.score === level.score}
                      disabled={!form.rubric.scoringSupported} onChange={() => mark(c.id, "score", level.score)} />
                    <span className="sp-level-top"><span className="sp-level-tag">{level.tag}</span>{level.score != null && <span className="sp-level-pts">{level.score} điểm</span>}</span>
                    <span className="sp-level-desc">{level.desc}</span>
                  </label>)}
              </div>
              {!form.rubric.scoringSupported && <p>Môn/rubric này chưa hỗ trợ chấm điểm. Bạn vẫn có thể lưu bằng chứng quan sát.</p>}
              <label className="assessment-evidence">Bằng chứng quan sát{(m.score === 0 || m.score === 4) && " (bắt buộc để công bố)"}
                <textarea maxLength={2000} placeholder="Ghi tình huống thực tế, động tác hoặc quyết định đã quan sát trên sân…" value={m.evidence || ""} onChange={e => mark(c.id, "evidence", e.target.value)} />
              </label>
            </section>;
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
