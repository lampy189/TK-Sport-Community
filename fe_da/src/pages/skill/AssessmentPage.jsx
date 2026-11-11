import { useEffect, useState } from "react";
import SectionLayout from "../../components/layout/SectionLayout";
import { workflowRequest } from "../workflowClient";
import "../workflow.css";
import "../../styles/skill-profile.css";
import "./assessment.css";
<<<<<<< HEAD
import { DIMENSION_ANCHORS } from "./dimensionAnchors";
=======
import {
  DIMENSION_ANCHORS,
  calculateCriterionFromSubSkills,
  getSliderTierInfo,
  parseSubSkillEvidence,
  serializeSubSkillEvidence,
} from "./dimensionAnchors";
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c

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
<<<<<<< HEAD
=======
// Observation guidance for retained pilot forms; scores and weights are unchanged.
const pilotDescriptions = {
  D1: ["Chưa kiểm soát được giao, trả và đánh cầu qua lưới dù ở tình huống đơn giản.", "Giao và trả cầu đơn giản được từng lúc; phông, nâng và bỏ nhỏ còn thiếu kiểm soát.", "Dùng được giao, phông, nâng và bỏ nhỏ trong pha cầu thông thường; tạt, đập còn hạn chế khi bị ép.", "Phối hợp giao, trả, phông, bỏ nhỏ, tạt và đập ổn định, có hướng và điểm rơi rõ.", "Điều chỉnh lực, hướng và loại cú đánh hiệu quả ngay khi bị ép; giữ kiểm soát qua nhiều tình huống khó."],
  D2: ["Chưa có tư thế sẵn sàng; thường không tới được cầu hoặc mất thăng bằng khi đánh.", "Di chuyển tới cầu gần nhưng còn chậm, mất thăng bằng và ít hồi vị.", "Di chuyển được tới các góc sân ở tốc độ vừa; giữ thăng bằng và biết hồi vị.", "Chuẩn bị chân tốt, tới cầu đúng nhịp và hồi vị ổn định trong nhiều pha liên tiếp.", "Đổi hướng, xử lý cầu bị ép và hồi vị nhanh; giữ thăng bằng khi nhịp cầu tăng."],
  D3: ["Thường đánh hỏng ngay ở pha cầu đơn giản; chưa kiểm soát được điểm rơi.", "Giữ được vài lượt cầu chậm nhưng điểm rơi thất thường và còn nhiều lỗi tự đánh hỏng.", "Duy trì pha cầu tốc độ vừa, có điểm rơi chủ đích; lỗi tăng khi bị gây áp lực.", "Lặp lại điểm rơi phù hợp và ít tự đánh hỏng trong nhiều pha cầu tương đương.", "Giữ độ chính xác, độ bền pha cầu và hạn chế lỗi ngay khi tốc độ hoặc áp lực tăng."],
  D4: ["Chưa phân biệt tình huống nên giữ cầu, phòng thủ hay tấn công.", "Có ý định lựa chọn cú đánh nhưng thường đánh vào vị trí thuận lợi của đối phương.", "Biết giữ cầu khi bị động, tấn công cầu thuận lợi và tìm khoảng trống dễ thấy.", "Chọn công–thủ hợp lý, khai thác khoảng trống và điều chỉnh theo diễn biến pha cầu.", "Đọc tình huống sớm, thay đổi nhịp và phương án để tạo lợi thế khi đối phương thích ứng."],
  D5: ["Chưa phân chia trách nhiệm; thường bỏ cầu giữa sân hoặc di chuyển trùng đồng đội.", "Có gọi cầu và phân chia vị trí nhưng bọc lót, chuyển công–thủ còn chậm.", "Biết nhận cầu giữa sân, bọc lót và giữ đội hình công–thủ cơ bản.", "Giao tiếp rõ, luân chuyển vị trí và bọc lót ổn định qua nhiều pha cầu.", "Phối hợp chuyển công–thủ nhanh, xử lý khoảng trống và hỗ trợ đồng đội hiệu quả khi bị ép."],
};

function previewBandFromScore(score) {
  return score == null ? null : 'B' + Math.min(5, Math.floor(score / 20) + 1);
}
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c

export default function AssessmentPage() {
  const [sessions, setSessions] = useState([]);
  const [sessionId, setSessionId] = useState("");
  const [players, setPlayers] = useState([]);
  const [form, setForm] = useState(null);
  const [dirty, setDirty] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");
<<<<<<< HEAD
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
=======
  const [stage, setStage] = useState("score");
  const [confirmed, setConfirmed] = useState(false);
  const [playerContext, setPlayerContext] = useState(null);

  useEffect(() => {
    if (!dirty) return;
    const warn = (event) => {
      event.preventDefault();
      event.returnValue = "";
    };
    window.addEventListener("beforeunload", warn);
    return () => window.removeEventListener("beforeunload", warn);
  }, [dirty]);

  useEffect(() => {
    let active = true;
    workflowRequest("/api/v1/skill/sessions")
      .then((data) => {
        if (active) setSessions(data);
      })
      .catch((e) => {
        if (active) setError(e.message);
      });
    return () => {
      active = false;
    };
  }, []);

  async function run(action) {
    setBusy(true);
    setError("");
    setMessage("");
    try {
      await action();
    } catch (e) {
      setError(e.message);
    } finally {
      setBusy(false);
    }
  }

  function selectSession(id) {
    setStage("score");
    setConfirmed(false);
    setSessionId(id);
    setPlayers([]);
    setForm(null);
    setDirty(false);
    setPlayerContext(null);
    if (id) {
      run(async () =>
        setPlayers(await workflowRequest(`/api/v1/skill/sessions/${id}/attendees`))
      );
    }
  }

  function open(playerId) {
    run(async () => {
      const next = await workflowRequest(
        `/api/v1/skill/sessions/${sessionId}/players/${playerId}/assessment`,
        "POST"
      );
      setForm(next);
      setStage(next.status === "published" ? "review" : "score");
      setConfirmed(false);
      setDirty(false);
      setPlayerContext(null);
    });
  }

  async function saveDraft(review = false) {
    const next = await workflowRequest(
      `/api/v1/skill/assessments/${form.id}`,
      "PUT",
      { revision: form.revision, marks: form.marks.map(m => {
        const c = form.rubric.criteria.find(c => c.id === m.dimensionId);
        return { ...m, sliderScores: sliderValues(c, m) };
      }) }
    );
    setForm(next);
    setDirty(false);
    setConfirmed(false);
    if (review) setStage("review");
    setMessage(
      review
        ? "Đã lưu phiếu. Rà soát điểm trước khi xác nhận."
        : "Đã lưu nháp. Có thể tiếp tục quan sát ở buổi này."
    );
  }

  function updateCriterionLevelPreset(criterionCode, dimensionId, newScore0to4) {
    setForm((current) => ({
      ...current,
      marks: current.marks.map((m) => {
        if (m.dimensionId !== dimensionId) return m;
        if (newScore0to4 == null) {
          return { ...m, score: null, sliderScores: null };
        }
        const parsed = parseSubSkillEvidence(criterionCode, m.evidence, newScore0to4);
        const anchor = DIMENSION_ANCHORS[criterionCode];
        const subSkills = anchor?.subSkills || [];
        if (subSkills.length === 0) {
          return { ...m, score: newScore0to4 };
        }
        const nextMap = {};
        subSkills.forEach((sk, i) => {
          nextMap[sk.code] = i % 2 ? Math.ceil(newScore0to4 * 2.5) : Math.floor(newScore0to4 * 2.5);
        });
        const nextEvidence = serializeSubSkillEvidence(
          criterionCode,
          nextMap,
          parsed.customNote
        );
        return {
          ...m,
          score: newScore0to4,
          sliderScores: subSkills.map(sk => nextMap[sk.code]),
          evidence: nextEvidence,
        };
      }),
    }));
    setDirty(true);
    setMessage("");
    setConfirmed(false);
  }

  function updateSubSkillSlider(criterionCode, dimensionId, subSkillCode, newValue10) {
    const clamped = Math.min(10, Math.max(0, Math.round(Number(newValue10 ?? 0))));
    setForm((current) => ({
      ...current,
      marks: current.marks.map((m) => {
        if (m.dimensionId !== dimensionId) return m;
        const parsed = parseSubSkillEvidence(criterionCode, m.evidence, m.score);
        const nextMap = {
          ...parsed.subSkillMap,
          [subSkillCode]: clamped,
        };
        const anchor = DIMENSION_ANCHORS[criterionCode];
        const subSkills = anchor?.subSkills || [];
        const values = subSkills.map((sk) => nextMap[sk.code] ?? 5);
        const calc = calculateCriterionFromSubSkills(values);
        const nextEvidence = serializeSubSkillEvidence(
          criterionCode,
          nextMap,
          parsed.customNote
        );
        return {
          ...m,
          score: calc.score0to4,
          sliderScores: values,
          evidence: nextEvidence,
        };
      }),
    }));
    setDirty(true);
    setMessage("");
    setConfirmed(false);
  }

  function updateCustomCourtNote(criterionCode, dimensionId, newNote) {
    setForm((current) => ({
      ...current,
      marks: current.marks.map((m) => {
        if (m.dimensionId !== dimensionId) return m;
        const parsed = parseSubSkillEvidence(criterionCode, m.evidence, m.score);
        if (!parsed.hasRecordedSliders && m.score == null) {
          return { ...m, evidence: newNote };
        }
        const nextEvidence = serializeSubSkillEvidence(
          criterionCode,
          parsed.subSkillMap,
          newNote
        );
        return { ...m, evidence: nextEvidence };
      }),
    }));
    setDirty(true);
    setMessage("");
    setConfirmed(false);
  }

  const published = form?.status === "published";
  function sliderValues(c, m) {
    if (m.score == null) return null;
    if (m.sliderScores?.length === 4) return m.sliderScores;
    // Recalculate old drafts only when the referee saves; published results remain historical.
    const parsed = parseSubSkillEvidence(c.code, m.evidence, m.score);
    return !published && parsed.hasRecordedSliders && parsed.subSkillList.length === 4
      ? parsed.subSkillList.map(sk => sk.value10) : null;
  }
  function criterionPoints(c, m) {
    if (!m || m.score == null) return null;
    const values = sliderValues(c, m);
    return values ? values.reduce((sum, v) => sum + v, 0) / 2 : m.score * c.weightPercent / 4;
  }
  const scored = form?.marks.filter((m) => m.score != null).length || 0;
  const preview =
    form?.rubric.scoringSupported && scored === form.rubric.criteria.length
      ? Math.round(form.rubric.criteria.reduce((sum, c) => sum + criterionPoints(c, form.marks.find(m => m.dimensionId === c.id)), 0))
      : null;
  const previewBand =
    preview == null ? null : `B${Math.min(5, Math.floor(preview / 20) + 1)}`;
  const liveBandLabel = previewBandFromScore(preview);
  const ready = preview != null;
  const player = players.find((p) => p.playerId === form?.playerId);
  const session = sessions.find((s) => String(s.id) === sessionId);

  return (
    <SectionLayout
      title="Đánh giá kỹ năng"
    >
      <div className="workflow assessment-workspace">
        {error && (
          <p role="alert" className="workflow-error">
            {error}
          </p>
        )}
        {message && <p role="status">{message}</p>}

        <ol className="assessment-steps" aria-label="Quy trình đánh giá">
          {[
            "Chọn hồ sơ",
            "Quan sát & chấm điểm",
            "Rà soát & xác nhận",
            "Đã công bố",
          ].map((label, index) => {
            const current = published ? 3 : !form ? 0 : stage === "score" ? 1 : 2;
            return (
              <li
                key={label}
                aria-current={index === current ? "step" : undefined}
                className={index < current ? "is-done" : ""}
              >
                <span>{index < current ? "✓" : index + 1}</span>
                {label}
              </li>
            );
          })}
        </ol>

        <label>
          Buổi được phân công
          <select
            value={sessionId}
            disabled={busy || dirty}
            onChange={(e) => selectSession(e.target.value)}
          >
            <option value="">Chọn buổi chơi</option>
            {sessions.map((s) => (
              <option key={s.id} value={s.id}>
                {s.sessionDate} · {s.startTime} · {s.name} · {s.court}
              </option>
            ))}
          </select>
        </label>
        {!sessions.length && !error && (
          <p>Chưa có buổi được phân công còn trong ngày đánh giá hoặc sắp tới.</p>
        )}
        {sessionId && !busy && players.length === 0 && (
          <p>
            Chưa có người chơi đủ điều kiện: vé đã thanh toán và đã điểm danh.
          </p>
        )}
        <div className="workflow-actions">
          {players.map((p) => (
            <button
              key={p.playerId}
              aria-pressed={form?.playerId === p.playerId}
              disabled={busy || dirty || !p.profileId}
              onClick={() => open(p.playerId)}
              className="button button-secondary"
            >
              {p.name} · {p.band || "Chưa xếp hạng"}
              {p.assessmentId ? " · Có phiếu" : " · Chưa chấm"}
              {!p.profileId && " · Thiếu hồ sơ môn"}
            </button>
          ))}
        </div>

        {players.length > 0 && (
          <section className="assessment-protocol">
            <h3>Hồ sơ người chơi trong sân</h3>
            <div className="workflow-actions">
              {players.map((p) => (
                <button
                  key={p.playerId}
                  className="button button-secondary"
                  disabled={busy}
                  onClick={() =>
                    run(async () => {
                      setPlayerContext(null);
                      setPlayerContext(
                        await workflowRequest(
                          `/api/v1/skill/sessions/${sessionId}/players/${p.playerId}/context`
                        )
                      );
                    })
                  }
                >
                  Xem hồ sơ · {p.name}
                </button>
              ))}
            </div>
            {playerContext && (
              <div className="assessment-player-context">
                <h4>
                  {playerContext.name} · #{playerContext.playerId}
                </h4>
                <p>
                  {playerContext.profile?.sport || session?.name} · Điểm:{" "}
                  {playerContext.profile?.currentScore ?? "Chưa có"} ·{" "}
                  {playerContext.profile?.currentBand || "Chưa xếp Band"} ·{" "}
                  {playerContext.profile?.ratingStatus?.toLowerCase() === "rated"
                    ? "Đã xác nhận"
                    : "Chưa xác nhận"}
                </p>
                <p className="assessment-note">
                  Tối đa 20 bản ghi gần nhất cho từng lịch sử, cùng môn và trung
                  tâm. Lịch sử là thông tin tham khảo; chấm điểm dựa trên quan
                  sát tại buổi hiện tại.
                </p>
                <details open>
                  <summary>
                    Buổi đã tham gia ({playerContext.participation.length})
                  </summary>
                  {playerContext.participation.length ? (
                    <ul>
                      {playerContext.participation.map((p) => (
                        <li key={p.sessionId}>
                          {p.sessionDate} · {p.startTime} · {p.court} · Đã điểm
                          danh
                        </li>
                      ))}
                    </ul>
                  ) : (
                    <p>Chưa có buổi tham gia trước đó.</p>
                  )}
                  <p className="assessment-note">
                    Hệ thống chưa ghi nhận tỷ số hoặc kết quả thắng/thua từng
                    trận.
                  </p>
                </details>
                <details open>
                  <summary>
                    Lịch sử điểm chính thức ({playerContext.ratings.length})
                  </summary>
                  {playerContext.ratings.length ? (
                    <ul>
                      {playerContext.ratings.map((r) => (
                        <li key={r.id}>
                          {r.sessionDate} · {r.oldScore ?? "—"} /{" "}
                          {r.oldBand || "Chưa xếp hạng"} →{" "}
                          <strong>
                            {r.newScore ?? "—"} / {r.newBand}
                          </strong>{" "}
                          · {r.rubric}
                        </li>
                      ))}
                    </ul>
                  ) : (
                    <p>Chưa có kết quả chấm chính thức.</p>
                  )}
                </details>
                <button
                  className="button button-secondary"
                  onClick={() => setPlayerContext(null)}
                >
                  Đóng hồ sơ xem nhanh
                </button>
              </div>
            )}
          </section>
        )}

        {form && (
          <>
            <div className="assessment-form-banner">
              <div>
                <h3>
                  Phiếu #{form.id} · {player?.name}
                </h3>
                <p>
                  {session?.sessionDate} · {session?.startTime} ·{" "}
                  {session?.court} · {session?.name}
                </p>
                <p>
                  {form.rubric.name} · {form.rubric.version} ·{" "}
                  {published ? "Đã công bố" : "Bản nháp"}
                </p>
              </div>
              <div className="assessment-live-summary">
                <span>Tổng điểm</span>
                <strong>
                  {preview != null
                    ? `${preview} / 100 điểm · ${liveBandLabel}`
                    : `Đã chấm ${scored}/${form.rubric.criteria.length} tiêu chí`}
                </strong>
              </div>
            </div>

            {!published && stage === "score" && (
              <section className="assessment-protocol">
                <label>
                  Tiến độ chấm: {scored}/{form.rubric.criteria.length}
                  <progress value={scored} max={form.rubric.criteria.length} />
                </label>
              </section>
            )}

            {!form.rubric.scoringSupported && (
              <div className="workflow-error" role="alert">
                <strong>Phiếu đang bị khóa chấm điểm</strong>
                <p>
                  Hệ thống chưa bật chấm điểm cho bộ tiêu chí này. Nếu vừa cập
                  nhật ứng dụng, cần khởi động lại backend rồi tải lại phiếu.
                </p>
                <button
                  className="button button-secondary"
                  disabled={busy}
                  onClick={() =>
                    run(async () => {
                      setForm(
                        await workflowRequest(
                          `/api/v1/skill/assessments/${form.id}`
                        )
                      );
                      setDirty(false);
                    })
                  }
                >
                  Tải lại phiếu
                </button>
              </div>
            )}

            {form.rubric.version === "PK-IPTPA-DUPR-V1" && (
              <p className="assessment-rubric-note">
                Điểm Pickleball nội bộ, không phải xếp hạng DUPR chính thức.
              </p>
            )}

            <details className="assessment-summary">
              <summary>Điểm và Band</summary>
                  <div
                    className="assessment-bands"
                    aria-label="Ngưỡng điểm Band nội bộ"
                  >
                    {[
                      ["B1", "0–19"],
                      ["B2", "20–39"],
                      ["B3", "40–59"],
                      ["B4", "60–79"],
                      ["B5", "80–100"],
                    ].map(([band, range]) => (
                      <div
                        key={band}
                        className={previewBand === band ? "is-current" : ""}
                      >
                        <strong>{band}</strong>
                        <span>{range} điểm</span>
                        {previewBand === band && <small>Phiếu hiện tại</small>}
                      </div>
                    ))}
                  </div>
                  <p className="assessment-preview" role="status">
                    Đã chấm {scored}/{form.rubric.criteria.length} tiêu chí ·{" "}
                    {preview == null
                      ? "Chưa đủ quan sát để quy đổi Band"
                      : `Điểm phiếu hiện tại: ${preview}/100 · ${previewBand}`}
                    {dirty && " · Chưa lưu"}
                  </p>
              <p>
                Hồ sơ hiện tại:{" "}
                {form.currentScore == null
                  ? "Chưa có điểm"
                  : `${form.currentScore}/100`}{" "}
                · {form.currentBand || "Chưa xếp Band"}. Lưu nháp chưa thay đổi
                hồ sơ.
              </p>
            </details>

            {stage === "score" && (
              <fieldset
                className="assessment-criteria sp-questions-list"
                disabled={busy || published}
              >
                <legend>Chấm điểm</legend>
                {form.rubric.criteria.map((c, index) => {
                  const m = form.marks.find((x) => x.dimensionId === c.id) || {
                    dimensionId: c.id,
                    score: null,
                    evidence: "",
                  };
                  const anchors =
                    DIMENSION_ANCHORS[c.code] ||
                    (pilotDescriptions[c.code] && {
                      subtitle: observations[c.code],
                      levels: levels.map((level, i) => ({
                        ...level,
                        desc: pilotDescriptions[c.code][i],
                      })),
                    });
                  const parsed = parseSubSkillEvidence(
                    c.code,
                    m.evidence,
                    m.score
                  );
                  const subSkills = parsed.subSkillList || [];
                  const isObserved = m.score != null;

                  return (
                    <section
                      className="sp-question-card assessment-criterion-card"
                      key={c.id}
                      aria-labelledby={`criterion-${c.id}`}
                    >
                      <div className="sp-question-head">
                        <div>
                          <span className="sp-q-index">
                            Tiêu chí {index + 1} / {form.rubric.criteria.length}{" "}
                            · {c.weightPercent}%
                          </span>
                          <h3 id={`criterion-${c.id}`}>
                            {c.code} — {c.name}
                          </h3>
                        </div>
                        <div className="sp-q-score-pill">
                          <strong>
                            {m.score == null ? "—" : `${criterionPoints(c, m)} / 20`}
                          </strong>
                          <span>
                            {m.score == null
                              ? "Chưa quan sát"
                              : `Mức ${m.score}/4`}
                          </span>
                        </div>
                      </div>

                      {/* 4 THANH TRƯỢT KỸ NĂNG THÀNH PHẦN (0 - 10) */}
                      {subSkills.length > 0 && (
                        <div className="assessment-subskills-box">
                          <div className="assessment-subskills-header">
                            <strong>Kỹ năng thành phần (0–10)</strong>
                          </div>

                          <div className="assessment-sliders-grid">
                            {subSkills.map((sk, skIdx) => {
                              const currentVal = isObserved ? sk.value10 : 0;
                              const tierInfo = getSliderTierInfo(currentVal);
                              return (
                                <div
                                  key={sk.code}
                                  className={`assessment-slider-item ${
                                    isObserved
                                      ? tierInfo.colorClass
                                      : "is-unobserved"
                                  }`}
                                >
                                  <div className="assessment-slider-top">
                                    <div>
                                      <span className="assessment-slider-title">
                                        {index + 1}.{skIdx + 1}. {sk.name}
                                      </span>
                                      {sk.unit && (
                                        <span className="assessment-slider-unit">
                                          Đơn vị đo: {sk.unit}
                                        </span>
                                      )}
                                    </div>
                                    <div className="assessment-slider-badges">
                                      <span
                                        className={`assessment-slider-tier-pill is-tier-${tierInfo.tierIndex}`}
                                      >
                                        {isObserved
                                          ? `${tierInfo.label} (${tierInfo.bandHint})`
                                          : "Chưa kéo thanh trượt"}
                                      </span>
                                      <strong className="assessment-slider-score">
                                        {isObserved
                                          ? `${currentVal} / 10`
                                          : "— / 10"}
                                      </strong>
                                    </div>
                                  </div>

                                  <div className="assessment-slider-control-row">
                                    <input
                                      type="range"
                                      min="0"
                                      max="10"
                                      step="1"
                                      value={currentVal}
                                      disabled={!form.rubric.scoringSupported}
                                      aria-label={sk.name}
                                      onChange={(e) =>
                                        updateSubSkillSlider(
                                          c.code,
                                          c.id,
                                          sk.code,
                                          e.target.value
                                        )
                                      }
                                      className="assessment-range-input"
                                    />
                                  </div>

                                  <div className="assessment-slider-ticks">
                                    {[0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10].map(
                                      (tick) => (
                                        <button
                                          key={tick}
                                          type="button"
                                          disabled={
                                            !form.rubric.scoringSupported
                                          }
                                          className={`assessment-tick-btn ${
                                            isObserved && currentVal === tick
                                              ? "is-active-tick"
                                              : ""
                                          }`}
                                          onClick={() =>
                                            updateSubSkillSlider(
                                              c.code,
                                              c.id,
                                              sk.code,
                                              tick
                                            )
                                          }
                                        >
                                          {tick}
                                        </button>
                                      )
                                    )}
                                  </div>

                                </div>
                              );
                            })}
                          </div>
                        </div>
                      )}

                      {/* 2. MỨC QUY ĐỔI TIÊU CHÍ (0 - 4) */}
                      <div className="assessment-level-presets-header">
                        <span>
                          Mức quy đổi (0–4)
                        </span>
                      </div>

                      <div
                        className="sp-level-options"
                        role="radiogroup"
                        aria-labelledby={`criterion-${c.id}`}
                      >
                        {[
                          {
                            score: null,
                            tag: "Chưa quan sát",
                            desc: "Chưa đủ bằng chứng để chấm tiêu chí này. Không tính là 0 điểm.",
                          },
                          ...(anchors?.levels || levels),
                        ].map((level) => (
                          <label
                            key={level.score ?? "unobserved"}
                            className={`sp-level-option ${
                              m.score === level.score ? "is-checked" : ""
                            }`}
                          >
                            <input
                              type="radio"
                              name={`assessment-${form.id}-${c.id}`}
                              checked={m.score === level.score}
                              disabled={!form.rubric.scoringSupported}
                              onChange={() =>
                                updateCriterionLevelPreset(
                                  c.code,
                                  c.id,
                                  level.score
                                )
                              }
                            />
                            <span className="sp-level-top">
                              <span className="sp-level-tag">{level.tag}</span>
                              {level.score != null && (
                                <span className="sp-level-pts">
                                  {level.score} điểm
                                </span>
                              )}
                            </span>
                            <span className="sp-level-desc">{level.desc}</span>
                          </label>
                        ))}
                      </div>

                      {!form.rubric.scoringSupported && (
                        <p>
                          Môn/rubric này chưa hỗ trợ chấm điểm. Bạn vẫn có thể
                          lưu bằng chứng quan sát.
                        </p>
                      )}

                      <label className="assessment-evidence">
                        Nhận xét
                        <textarea
                          maxLength={800}
                          placeholder="Nhận xét quan sát"
                          value={parsed.customNote || ""}
                          onChange={(e) => updateCustomCourtNote(c.code, c.id, e.target.value)}
                        />
                      </label>
                    </section>
                  );
                })}
              </fieldset>
            )}

            {stage === "review" && (
              <section
                className="assessment-review"
                aria-labelledby="review-title"
              >
                <h3 id="review-title">
                  {published
                    ? "Biên bản đánh giá đã công bố"
                    : "Rà soát phiếu trước khi công bố"}
                </h3>
                <p>
                  Người chơi: <strong>{player?.name}</strong> · Phiếu #{form.id}{" "}
                  · {form.rubric.name}
                </p>
                <div className="assessment-table-scroll">
                  <table className="assessment-review-table">
                    <thead>
                      <tr>
                        <th scope="col">Tiêu chí</th>
                        <th scope="col">Điểm / 20</th>
                        <th scope="col">Mức tham khảo</th>
                        <th scope="col">
                          Nhận xét
                        </th>
                      </tr>
                    </thead>
                    <tbody>
                      {form.rubric.criteria.map((c) => {
                        const m = form.marks.find(
                          (item) => item.dimensionId === c.id
                        );
                        return (
                          <tr key={c.id}>
                            <th scope="row">
                              {c.name}
                              <small>Trọng số {c.weightPercent}%</small>
                            </th>
                            <td>{criterionPoints(c, m) ?? "Chưa quan sát"}</td>
                            <td>
                              {m?.score == null || !form.rubric.scoringSupported
                                ? "—"
                                : `${m.score}/4`}
                            </td>
                            <td className="assessment-evidence-text">
                              {parseSubSkillEvidence(c.code, m?.evidence, m?.score).customNote || "—"}
                            </td>
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                </div>
                {!published && (
                  <>
                    <ul className="assessment-checklist">
                      <li>
                        {preview != null ? "✓" : "○"} Đã chấm đầy đủ các tiêu
                        chí
                      </li>
                      <li>{!dirty ? "✓" : "○"} Phiếu đã lưu trên hệ thống</li>
                    </ul>
                    <p>
                      Công bố sẽ cập nhật hồ sơ kỹ năng khi đáp ứng điều kiện
                      nghiệp vụ, lưu lịch sử và kiểm tra lại vé tương lai theo
                      Band. Phiếu đã công bố không sửa trực tiếp; điều chỉnh cần
                      Manager review.
                    </p>
                    <label className="assessment-confirm">
                      <input
                        type="checkbox"
                        checked={confirmed}
                        disabled={busy || !ready || dirty}
                        onChange={(e) => setConfirmed(e.target.checked)}
                      />
                      Tôi đã kiểm tra người chơi và điểm từng tiêu chí; xác nhận công bố kết quả.
                    </label>
                  </>
                )}
              </section>
            )}

            <p>
              Kết quả đã lưu: {form.totalScore ?? "Chưa đủ quan sát"}{" "}
              {form.bandResult && `· ${form.bandResult}`}
            </p>
            {published && (
              <p role="status">
                Điểm chính thức trên hồ sơ: {form.currentScore} ·{" "}
                {form.currentBand} · Rated
              </p>
            )}
            {!published && (
              <div className="workflow-actions">
                {stage === "score" && (
                  <>
                    <button
                      className="button button-secondary"
                      disabled={busy}
                      onClick={() => run(() => saveDraft())}
                    >
                      Lưu nháp
                    </button>
                    <button
                      className="button button-primary"
                      disabled={busy || preview == null}
                      onClick={() => run(() => saveDraft(true))}
                    >
                      Lưu và rà soát phiếu
                    </button>
                  </>
                )}
                {stage === "review" && (
                  <>
                    <button
                      className="button button-secondary"
                      disabled={busy}
                      onClick={() => {
                        setStage("score");
                        setConfirmed(false);
                      }}
                    >
                      Quay lại chỉnh sửa
                    </button>
                    <button
                      className="button button-primary"
                      disabled={
                        busy ||
                        !confirmed ||
                        !ready ||
                        dirty ||
                        form.totalScore == null
                      }
                      onClick={() =>
                        run(async () => {
                          setForm(
                            await workflowRequest(
                              `/api/v1/skill/assessments/${form.id}/publish`,
                              "POST",
                              {
                                revision: form.revision,
                                expectedScore: form.currentScore,
                                expectedBand: form.currentBand,
                              }
                            )
                          );
                          setConfirmed(false);
                          setMessage(
                            "Đã công bố điểm. Người chơi có thể xem kết quả chính thức trong Hồ sơ kỹ năng."
                          );
                        })
                      }
                    >
                      {busy ? "Đang xử lý…" : "Xác nhận công bố"}
                    </button>
                  </>
                )}
                <button
                  className="button button-secondary"
                  disabled={busy}
                  onClick={() =>
                    run(async () => {
                      if (
                        dirty &&
                        !window.confirm(
                          "Bỏ các thay đổi chưa lưu và tải lại bản đã lưu?"
                        )
                      )
                        return;
                      const next = await workflowRequest(
                        `/api/v1/skill/assessments/${form.id}`
                      );
                      setForm(next);
                      setDirty(false);
                      setConfirmed(false);
                      setStage(next.status === "published" ? "review" : "score");
                    })
                  }
                >
                  Tải lại bản đã lưu
                </button>
              </div>
            )}
            {!published && preview == null && (
              <p role="status">
                Cần chấm đủ {form.rubric.criteria.length} tiêu chí để chuyển
                sang rà soát. Có thể lưu nháp và tiếp tục quan sát.
              </p>
            )}
            {dirty && (
              <p>
                Phiếu có thay đổi chưa lưu. Lưu trước khi công bố hoặc chọn
                người chơi khác.
              </p>
            )}
          </>
        )}
      </div>
    </SectionLayout>
  );
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
}
