import {
  DIMENSION_ANCHORS,
  parseSubSkillEvidence,
} from "../pages/skill/dimensionAnchors";
import { useEffect, useMemo, useState } from "react";
import SkillReviews from "../pages/skill/SkillReviews";
import {
  Award,
  CheckCircle2,
  ClipboardCheck,
  TrendingUp,
  History,
  Sparkles,
  ShieldCheck,
  ArrowRight,
  RefreshCw,
  Calendar,
  UserCheck,
  AlertCircle,
  Lock,
} from "lucide-react";
import { api } from "../api";
import "../styles/skill-profile.css";

/**
 * Mô tả tự đánh giá theo cảm nhận thực tế trên sân (không lộ mức điểm hay công thức cộng điểm).
 */


// Alias D1..D5 from LamNV's badminton-pilot-r1 rubric to the 5 Badminton anchors
DIMENSION_ANCHORS.D1 = DIMENSION_ANCHORS.BM_SERVE_NET;
DIMENSION_ANCHORS.D2 = DIMENSION_ANCHORS.BM_CLEAR_BACKHAND;
DIMENSION_ANCHORS.D3 = DIMENSION_ANCHORS.BM_SMASH_DEFENSE;
DIMENSION_ANCHORS.D4 = DIMENSION_ANCHORS.BM_FOOTWORK;
DIMENSION_ANCHORS.D5 = DIMENSION_ANCHORS.BM_DOUBLES;

const SCORE_LEVEL_META = {
  0: {
    tag: "Mức 0/4 · Chưa ổn định (0đ)",
    surveyTag: "Mức 1 — Mới làm quen",
    shortLabel: "Chưa ổn định",
  },
  1: {
    tag: "Mức 1/4 · Cơ bản an toàn (5đ)",
    surveyTag: "Mức 2 — Cơ bản",
    shortLabel: "Cơ bản an toàn",
  },
  2: {
    tag: "Mức 2/4 · Ổn định phong trào (10đ)",
    surveyTag: "Mức 3 — Ổn định",
    shortLabel: "Ổn định phong trào",
  },
  3: {
    tag: "Mức 3/4 · Chủ động & Biến hóa (15đ)",
    surveyTag: "Mức 4 — Chủ động",
    shortLabel: "Chủ động & Biến hóa",
  },
  4: {
    tag: "Mức 4/4 · Kiểm soát nâng cao (20đ)",
    surveyTag: "Mức 5 — Thành thạo cao",
    shortLabel: "Kiểm soát nâng cao",
  },
};

function isOnCourtType(type, role) {
  const normType = String(type || "")
    .toLowerCase()
    .replace(/_/g, "");
  const normRole = String(role || "").toLowerCase();
  return normType === "oncourt" || normRole.includes("referee");
}

function ScoreChange({ current, previous, unit = "điểm" }) {
  if (current == null || previous == null) return null;
  const delta = Math.round((Number(current) - Number(previous)) * 10) / 10;
  if (!Number.isFinite(delta)) return null;
  return (
    <span className={`sp-score-change ${delta > 0 ? "is-up" : delta < 0 ? "is-down" : "is-same"}`}>
      {delta > 0 ? "↑ Tăng" : delta < 0 ? "↓ Giảm" : "→ Không đổi"}
      {delta !== 0 && ` ${Math.abs(delta)} ${unit}`}
    </span>
  );
}

function getNormalizedDimensionAnchor(dimCode, sportId = 1, index = 0) {
  const fallbackCodes =
    sportId === 2
      ? [
          "PK_SERVE_RETURN",
          "PK_THIRD_SHOT",
          "PK_DINK_NVZ",
          "PK_VOLLEY_RESET",
          "PK_DOUBLES_STRATEGY",
        ]
      : [
          "BM_SERVE_NET",
          "BM_CLEAR_BACKHAND",
          "BM_SMASH_DEFENSE",
          "BM_FOOTWORK",
          "BM_DOUBLES",
        ];

  const resolvedCode =
    dimCode && DIMENSION_ANCHORS[dimCode]
      ? dimCode
      : fallbackCodes[index % 5];

  const raw = DIMENSION_ANCHORS[resolvedCode] || null;

  const observations =
    raw?.refereeObservation ||
    raw?.observations ||
    raw?.subtitle ||
    "Trọng tài quan sát kỹ thuật thực tế, độ ổn định và khả năng xử lý tình huống trong trận đấu.";

  const rawLevels = Array.isArray(raw?.levels) ? raw.levels : [0, 1, 2, 3, 4];

  const levels = [0, 1, 2, 3, 4].map((score) => {
    const item = rawLevels[score];
    const meta = SCORE_LEVEL_META[score] || SCORE_LEVEL_META[2];
    if (typeof item === "string") {
      const cleanedDesc = item.replace(/^\s*\d+\s*[-–:]\s*/, "").trim();
      return {
        score,
        weightedPoints: score * 5,
        tag: meta.tag,
        surveyTag: meta.surveyTag,
        shortLabel: meta.shortLabel,
        desc: cleanedDesc || item,
        rawLabel: item,
      };
    }
    if (item && typeof item === "object") {
      const s = Number(item.score ?? score);
      const m = SCORE_LEVEL_META[s] || meta;
      return {
        score: s,
        weightedPoints: s * 5,
        tag: item.tag || m.tag,
        surveyTag: item.surveyTag || item.tag || m.surveyTag,
        shortLabel: m.shortLabel,
        desc: item.desc || "Mô tả tiêu chí kỹ năng trên sân.",
        rawLabel: `${s} - ${item.desc || ""}`,
      };
    }
    return {
      score,
      weightedPoints: score * 5,
      tag: meta.tag,
      surveyTag: meta.surveyTag,
      shortLabel: meta.shortLabel,
      desc: "Mô tả cảm nhận và mức độ thực hiện kỹ năng trên sân.",
      rawLabel: `${score} - ${meta.shortLabel}`,
    };
  });

  return {
    code: resolvedCode,
    observations,
    subtitle: observations,
    levels,
  };
}

const BAND_METADATA = {
  1: {
    standardLabel: "Thang trình độ Cầu lông CLB",
    bands: [
      { code: "B1", range: "0 – 19", title: "Mới chơi" },
      { code: "B2", range: "20 – 39", title: "Cơ bản" },
      { code: "B3", range: "40 – 59", title: "Trung bình" },
      { code: "B4", range: "60 – 79", title: "Trung bình khá" },
      { code: "B5", range: "80 – 100", title: "Khá / Nâng cao" },
    ],
  },
  2: {
    standardLabel: "Thang trình độ Pickleball",
    bands: [
      { code: "B1", range: "0 – 19", title: "Mới chơi" },
      { code: "B2", range: "20 – 39", title: "Sơ cấp" },
      { code: "B3", range: "40 – 59", title: "Trung cấp" },
      { code: "B4", range: "60 – 79", title: "Trung khá" },
      { code: "B5", range: "80 – 100", title: "Nâng cao" },
    ],
  },
};

function getSportNameVi(sportId, sportName) {
  if (sportId === 2 || sportName === "Pickleball") return "Pickleball";
  return "Cầu lông";
}

function mapScoreToBand(score) {
  if (score == null || Number.isNaN(score)) return "B1";
  if (score < 20) return "B1";
  if (score < 40) return "B2";
  if (score < 60) return "B3";
  if (score < 80) return "B4";
  return "B5";
}

function getBandInfo(sportId, bandCode) {
  const meta = BAND_METADATA[sportId] || BAND_METADATA[1];
  return (
    meta.bands.find((b) => b.code === bandCode) || {
      code: bandCode || "—",
      range: "—",
      title: "Chưa xếp hạng",
    }
  );
}

function getProfileLockState(prof, officialList = []) {
  if (!prof) {
    return {
      isRefereeRated: false,
      isAlreadySurveyed: false,
      canTakeSurvey: true,
      badgeLabel: "Người mới · Chưa khảo sát",
      badgeTone: "unrated",
    };
  }
  const hasOnCourt =
    isOnCourtType(
      prof.latestAssessment?.assessmentType,
      prof.latestAssessment?.assessorRole,
    ) ||
    (prof.ratingHistory || []).some((h) =>
      isOnCourtType(h.assessmentType, h.changedByRole),
    ) ||
    (Array.isArray(officialList) && officialList.length > 0);
  const isRefereeRated =
    prof.ratingStatus === "Rated" ||
    prof.ratingStatus === "Confirmed" ||
    prof.ratingStatus === "Official" ||
    hasOnCourt;

  const isAlreadySurveyed =
    !isRefereeRated &&
    prof.currentBand != null &&
    prof.ratingStatus === "Provisional";

  const canTakeSurvey = !isRefereeRated && !isAlreadySurveyed;

  if (isRefereeRated) {
    return {
      isRefereeRated: true,
      isAlreadySurveyed: false,
      canTakeSurvey: false,
      badgeLabel: "Chính thức · Trọng tài đã chấm",
      badgeTone: "official",
    };
  }
  if (isAlreadySurveyed) {
    return {
      isRefereeRated: false,
      isAlreadySurveyed: true,
      canTakeSurvey: false,
      badgeLabel: "Điểm tạm thời · Chờ Trọng tài chấm",
      badgeTone: "provisional",
    };
  }
  return {
    isRefereeRated: false,
    isAlreadySurveyed: false,
    canTakeSurvey: true,
    badgeLabel: "Người mới · Chưa khảo sát",
    badgeTone: "unrated",
  };
}

function formatSessionInfo(item) {
  if (!item) return null;
  const parts = [];
  if (item.venueName) parts.push(item.venueName);
  if (item.courtName) parts.push(item.courtName);
  if (item.sessionDate) parts.push(`Ngày ${item.sessionDate}`);
  if (item.timeLabel) parts.push(`Ca ${item.timeLabel}`);
  return parts.length > 0 ? parts.join(" · ") : null;
}

export default function SkillProfileView({ onBrowseSport }) {
  const [overview, setOverview] = useState(null);
  const [rubrics, setRubrics] = useState([]);
  const [officialResults, setOfficialResults] = useState({ 1: [], 2: [] });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  // Selected sport tab: 1 = Badminton, 2 = Pickleball
  const [selectedSportId, setSelectedSportId] = useState(1);

  // Active sub-mode: false = Result & History, true = Self-assessment form
  const [showSurveyForm, setShowSurveyForm] = useState(false);

  // Survey state: map of dimension.id -> score (0..4)
  const [surveyAnswers, setSurveyAnswers] = useState({});
  const [experienceText, setExperienceText] = useState("6 tháng - 2 năm");
  const [frequencyText, setFrequencyText] = useState("2 - 3 buổi / tuần");
  const [submitting, setSubmitting] = useState(false);
  const [submitBanner, setSubmitBanner] = useState(null);
  const [selectedAssessmentId, setSelectedAssessmentId] = useState(null);
  const [expandedDimIds, setExpandedDimIds] = useState({});

  useEffect(() => {
    loadData();
  }, []);

  useEffect(() => {
    if (showSurveyForm) return;
    let active = true;
    let pending = false;
    async function refreshPublishedResults() {
      if (document.hidden || pending) return;
      pending = true;
      try {
        const [data, offBm, offPk] = await Promise.all([
          api.getMySkillProfiles(),
          api.getMyOfficialSkillResults(1),
          api.getMyOfficialSkillResults(2),
        ]);
        if (active) {
          setOverview(data);
          setOfficialResults({
            1: Array.isArray(offBm) ? offBm : [],
            2: Array.isArray(offPk) ? offPk : [],
          });
        }
      } catch { /* Keep the last result; the next refresh retries. */ }
      finally { pending = false; }
    }
    const timer = window.setInterval(refreshPublishedResults, 10000);
    window.addEventListener("focus", refreshPublishedResults);
    return () => { active = false; window.clearInterval(timer); window.removeEventListener("focus", refreshPublishedResults); };
  }, [showSurveyForm]);

  async function loadData() {
    setLoading(true);
    setError(null);
    try {
      const [rubricList, myData, offBm, offPk] = await Promise.all([
        api.getSkillRubrics(),
        api.getMySkillProfiles(),
        api.getMyOfficialSkillResults(1),
        api.getMyOfficialSkillResults(2),
      ]);
      setRubrics(Array.isArray(rubricList) ? rubricList : []);
      setOverview(myData);
      setOfficialResults({
        1: Array.isArray(offBm) ? offBm : [],
        2: Array.isArray(offPk) ? offPk : [],
      });
    } catch (err) {
      setError(err.message || "Không thể tải hồ sơ kỹ năng.");
    } finally {
      setLoading(false);
    }
  }

  const activeProfile = useMemo(() => {
    if (!overview?.profiles) return null;
    return (
      overview.profiles.find((p) => p.sportId === selectedSportId) ||
      overview.profiles[0] ||
      null
    );
  }, [overview, selectedSportId]);

  const activeRubric = useMemo(() => {
    if (activeProfile?.activeRubric) return activeProfile.activeRubric;
    return rubrics.find((r) => r.sportId === selectedSportId) || null;
  }, [activeProfile, rubrics, selectedSportId]);

  useEffect(() => {
    setSelectedAssessmentId(null);
  }, [selectedSportId]);

  useEffect(() => {
    if (!activeRubric?.dimensions) return;
    const initial = {};
    const existingScores = activeProfile?.latestAssessment?.scores || [];
    activeRubric.dimensions.forEach((dim) => {
      const dimId = dim.id ?? dim.dimensionId;
      const dimCode = dim.code ?? dim.dimensionCode;
      const found = existingScores.find(
        (s) => s.dimensionId === dimId || s.dimensionCode === dimCode,
      );
      initial[dimId] =
        found != null
          ? Math.round(Number(found.score ?? found.rawScore ?? 2))
          : 2;
    });
    setSurveyAnswers(initial);
  }, [selectedSportId, activeRubric, activeProfile]);

  async function handleSurveySubmit(e) {
    e.preventDefault();
    if (!activeRubric?.dimensions?.length) return;
    setSubmitting(true);
    setSubmitBanner(null);
    try {
      let sum = 0;
      const answers = activeRubric.dimensions.map((dim) => {
        const dimId = dim.id ?? dim.dimensionId;
        const val = Number(surveyAnswers[dimId] ?? 2);
        sum += val;
        return {
          dimensionId: dimId,
          score: val,
        };
      });

      const res = await api.submitSkillSurvey({
        sportId: selectedSportId,
        answers,
        experienceContext: experienceText,
        frequencyContext: frequencyText,
      });

      const updatedProfile = res?.profile || res?.updatedProfile || res;

      if (updatedProfile && updatedProfile.sportId) {
        setOverview((prev) => {
          if (!prev) return prev;
          return {
            ...prev,
            profiles: prev.profiles.map((p) =>
              p.sportId === selectedSportId ? updatedProfile : p,
            ),
          };
        });
      } else {
        await loadData();
      }

      const fallbackScore = Math.round(sum * 5);
      const sportNameVi = getSportNameVi(
        selectedSportId,
        updatedProfile?.sportName,
      );
      setSubmitBanner({
        sportId: selectedSportId,
        sportNameVi,
        band: updatedProfile?.currentBand || mapScoreToBand(fallbackScore),
        score: updatedProfile?.currentScore ?? fallbackScore,
      });
      setShowSurveyForm(false);
    } catch (err) {
      alert(err.message || "Không thể lưu kết quả khảo sát.");
    } finally {
      setSubmitting(false);
    }
  }

  if (loading) {
    return (
      <div className="skill-profile-page">
        <div className="sp-loading-card">
          <RefreshCw className="sp-spin" size={26} />
          <h3>Đang tải Hồ sơ Kỹ năng…</h3>
          <p>Đồng bộ thông tin trình độ và lịch sử đánh giá của bạn.</p>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="skill-profile-page">
        <div className="sp-error-card">
          <AlertCircle size={28} />
          <h3>Chưa thể tải hồ sơ kỹ năng</h3>
          <p>{error}</p>
          <button className="button button-primary" onClick={loadData}>
            Thử lại
          </button>
        </div>
      </div>
    );
  }

  const profiles = overview?.profiles || [];
  const sportMeta = BAND_METADATA[selectedSportId] || BAND_METADATA[1];
  const latestAssessment = activeProfile?.latestAssessment || null;
  const ratingHistory = activeProfile?.ratingHistory || [];

  // Build selectable assessments from latestAssessment + ratingHistory.assessmentDetails
  const availableAssessments = (() => {
    const map = new Map();
    if (latestAssessment?.id) {
      map.set(Number(latestAssessment.id), latestAssessment);
    }
    for (const h of ratingHistory) {
      const det = h.assessmentDetails;
      if (det?.id && !map.has(Number(det.id))) {
        map.set(Number(det.id), det);
      }
    }
    return Array.from(map.values());
  })();

  const displayedAssessment = (() => {
    if (selectedAssessmentId != null) {
      const found = availableAssessments.find(
        (a) => Number(a.id) === Number(selectedAssessmentId),
      );
      if (found) return found;
    }
    return latestAssessment || availableAssessments[0] || null;
  })();

  // History is returned newest first. Compare with the preceding publication,
  // not with the latest assessment when the player selects an older result.
  const orderedHistory = [...ratingHistory].sort((a, b) => Number(b.id) - Number(a.id));
  const historyIndex = orderedHistory.findIndex(
    (h) => Number(h.assessmentId) === Number(displayedAssessment?.id),
  );
  const displayedHistory = historyIndex >= 0 ? orderedHistory[historyIndex] : null;
  const previousHistory = historyIndex >= 0
    ? orderedHistory.slice(historyIndex + 1).find(
        (h) => h.assessmentId != null && Number(h.assessmentId) !== Number(displayedAssessment?.id),
      )
    : null;
  const previousAssessment = previousHistory?.assessmentDetails || null;
  const previousTotal = displayedHistory?.oldScore ?? displayedAssessment?.oldScore
    ?? previousHistory?.newScore ?? null;
  const previousBand = displayedHistory?.oldBand ?? displayedAssessment?.oldBand
    ?? previousHistory?.newBand ?? null;
  const canCompareDimensions = Boolean(previousAssessment && displayedAssessment
    && Number(previousAssessment.rubricId) === Number(displayedAssessment.rubricId));

  const isPickleball = selectedSportId === 2;
  const activeSportNameVi = getSportNameVi(
    selectedSportId,
    activeProfile?.sportName,
  );
  const activeLock = getProfileLockState(
    activeProfile,
    officialResults[selectedSportId],
  );

  return (
    <div className="skill-profile-page">
      {/* Page Header */}
      <header className="sp-hero">
        <div className="sp-hero-copy">
          <span className="eyebrow">HỒ SƠ KỸ NĂNG &amp; LỊCH SỬ TRÌNH ĐỘ</span>
          <h1>
            Hồ sơ Kỹ năng theo Bộ môn<span>.</span>
          </h1>
          <p>
            Người chơi mới thực hiện khảo sát tự đánh giá 1 lần để nhận mức điểm
            tạm thời đi đặt sân. Sau khi ra sân và được Trọng tài chấm điểm,
            chức năng tự đánh giá sẽ khóa lại để đảm bảo tính khách quan.
          </p>
        </div>
      </header>

      {/* Submission Success Banner */}
      {submitBanner && (
        <div className="sp-success-banner" role="status">
          <div className="sp-success-main">
            <CheckCircle2 size={24} />
            <div>
              <strong>
                Đã lưu điểm tạm thời môn {submitBanner.sportNameVi}!
              </strong>
              <p>
                Trình độ tạm thời của bạn được ghi nhận ở mức{" "}
                <b>
                  Band {submitBanner.band} ({submitBanner.score}/100 điểm)
                </b>
                . Tự đánh giá hiện đã được khóa — điểm chính thức của bạn sẽ do
                Trọng tài chấm khi tham gia buổi chơi trên sân.
              </p>
            </div>
          </div>
          <div className="sp-success-actions">
            <button
              className="button button-primary"
              onClick={() =>
                onBrowseSport?.(
                  submitBanner.sportId === 2 ? "Pickleball" : "Badminton",
                )
              }
            >
              Tìm buổi chơi {submitBanner.sportNameVi} ngay{" "}
              <ArrowRight size={16} />
            </button>
          </div>
        </div>
      )}

      {/* 1. Dual Sport Overview Cards */}
      <section
        className="sp-dual-cards"
        aria-label="Tổng quan trình độ 2 môn thể thao"
      >
        {profiles.map((prof) => {
          const isPk = prof.sportId === 2;
          const isSelected = prof.sportId === selectedSportId;
          const sportVi = getSportNameVi(prof.sportId, prof.sportName);
          const bandInfo = getBandInfo(prof.sportId, prof.currentBand);
          const lockState = getProfileLockState(
            prof,
            officialResults[prof.sportId],
          );
          const scoreVal =
            prof.currentScore != null ? Number(prof.currentScore) : 0;
          const latestType = prof.latestAssessment?.assessmentType;

          return (
            <article
              key={prof.sportId}
              className={`sp-sport-card ${
                isPk ? "is-pickleball" : "is-badminton"
              } ${isSelected ? "is-selected" : ""}`}
              onClick={() => {
                setSelectedSportId(prof.sportId);
              }}
            >
              <div className="sp-card-top">
                <div className="sp-sport-identity">
                  <span className="sp-sport-dot" />
                  <div>
                    <span className="sp-sport-eyebrow">
                      {isPk ? "BỘ MÔN PICKLEBALL" : "BỘ MÔN CẦU LÔNG"}
                    </span>
                    <h2>{sportVi}</h2>
                  </div>
                </div>
                <span
                  className={`sp-status-badge is-${lockState.badgeTone}`}
                >
                  {lockState.badgeLabel}
                </span>
              </div>

              <div className="sp-card-metrics">
                <div className="sp-band-box">
                  <span className="sp-metric-label">Band hiện tại</span>
                  <div className="sp-band-value">
                    <strong>{prof.currentBand || "—"}</strong>
                    <span>{bandInfo.title}</span>
                  </div>
                </div>
                <div className="sp-score-box">
                  <span className="sp-metric-label">Điểm kỹ năng</span>
                  <div className="sp-score-value">
                    <strong>
                      {prof.currentScore != null
                        ? Number(prof.currentScore).toFixed(0)
                        : "—"}
                    </strong>
                    <span>/ 100 điểm</span>
                  </div>
                </div>
              </div>

              {/* Band Progress Scale B1..B5 */}
              <div className="sp-scale-bar-wrap">
                <div className="sp-scale-bar">
                  <div
                    className="sp-scale-fill"
                    style={{
                      width: `${Math.min(100, Math.max(4, scoreVal))}%`,
                    }}
                  />
                </div>
                <div className="sp-scale-ticks">
                  {(BAND_METADATA[prof.sportId]?.bands || []).map((b) => (
                    <span
                      key={b.code}
                      className={
                        prof.currentBand === b.code ? "is-current-band" : ""
                      }
                    >
                      {b.code}
                    </span>
                  ))}
                </div>
              </div>

              <div className="sp-card-meta">
                <span>
                  <Calendar size={15} /> Nguồn gần nhất:{" "}
                  {isOnCourtType(
                    latestType,
                    prof.latestAssessment?.assessorRole,
                  )
                    ? `Trọng tài chấm (${prof.latestAssessment?.assessorName || "Trọng tài"})`
                    : latestType === "SelfSurvey"
                      ? "Tự khảo sát ban đầu (Điểm tạm thời)"
                      : "Chưa có bản đánh giá"}
                </span>
                <span>
                  <History size={15} /> {prof.ratingHistory?.length || 0} lần
                  cập nhật
                </span>
              </div>

              <div
                className="sp-card-actions"
                onClick={(e) => e.stopPropagation()}
              >
                {lockState.canTakeSurvey ? (
                  <button
                    type="button"
                    className="button button-primary sp-btn-sm"
                    onClick={() => {
                      setSelectedSportId(prof.sportId);
                      setShowSurveyForm(true);
                    }}
                  >
                    <ClipboardCheck size={15} />
                    Khảo sát tự đánh giá ban đầu
                  </button>
                ) : (
                  <span
                    className="sp-locked-pill"
                    title="Chức năng tự đánh giá đã khóa để tránh tự ý thêm bớt điểm"
                  >
                    <Lock size={14} />
                    {lockState.isRefereeRated
                      ? "Đã khóa tự đánh giá · Trọng tài đã chấm"
                      : "Đã chốt điểm tạm thời · Chờ Trọng tài chấm"}
                  </span>
                )}
                <button
                  type="button"
                  className="button button-secondary sp-btn-sm"
                  onClick={() =>
                    onBrowseSport?.(
                      prof.sportId === 2 ? "Pickleball" : "Badminton",
                    )
                  }
                >
                  Tìm sân {sportVi} <ArrowRight size={15} />
                </button>
              </div>
            </article>
          );
        })}
      </section>

      {/* 2. Sport Workspace Switcher */}
      <div className="sp-workspace-bar">
        <div className="sp-sport-tabs" role="tablist">
          {profiles.map((prof) => {
            const sportVi = getSportNameVi(prof.sportId, prof.sportName);
            return (
              <button
                key={prof.sportId}
                type="button"
                role="tab"
                aria-selected={selectedSportId === prof.sportId}
                className={`sp-sport-tab ${
                  prof.sportId === 2 ? "is-pickleball" : "is-badminton"
                } ${selectedSportId === prof.sportId ? "is-active" : ""}`}
                onClick={() => setSelectedSportId(prof.sportId)}
              >
                <span>{sportVi}</span>
                <small>
                  {prof.currentBand
                    ? `${prof.currentBand} · ${Number(prof.currentScore || 0).toFixed(0)}đ`
                    : "Chưa xếp hạng"}
                </small>
              </button>
            );
          })}
        </div>

        <div className="sp-mode-actions">
          <button
            type="button"
            className={`sp-mode-btn ${!showSurveyForm ? "is-active" : ""}`}
            onClick={() => setShowSurveyForm(false)}
          >
            <Award size={16} /> Kết quả đánh giá &amp; Lịch sử
          </button>
          {activeLock.canTakeSurvey ? (
            <button
              type="button"
              className={`sp-mode-btn is-survey-cta ${
                showSurveyForm ? "is-active" : ""
              }`}
              onClick={() => setShowSurveyForm(true)}
            >
              <ClipboardCheck size={16} /> Khảo sát tự đánh giá ban đầu
            </button>
          ) : (
            <button
              type="button"
              className={`sp-mode-btn is-locked ${
                showSurveyForm ? "is-active" : ""
              }`}
              onClick={() => setShowSurveyForm(true)}
            >
              <Lock size={15} /> Tự đánh giá đã khóa
            </button>
          )}
        </div>
      </div>

      {/* 3A. Self-Assessment Survey Section (Active for New Player OR Locked Notice once Rated/Surveyed) */}
      {showSurveyForm && activeRubric && (
        <section
          className={`sp-survey-section ${isPickleball ? "is-pickleball" : ""}`}
          aria-label="Khảo sát tự đánh giá trình độ ban đầu"
        >
          {!activeLock.canTakeSurvey ? (
            <div className="sp-locked-card">
              <div className="sp-locked-icon">
                <Lock size={28} />
              </div>
              <h3>
                {activeLock.isRefereeRated
                  ? `Tự đánh giá môn ${activeSportNameVi} đã bị khóa (Đã có điểm Trọng tài)`
                  : `Bạn đã hoàn thành khảo sát ban đầu môn ${activeSportNameVi}`}
              </h3>
              <p>
                {activeLock.isRefereeRated ? (
                  <>
                    Hồ sơ môn <strong>{activeSportNameVi}</strong> của bạn đã
                    được Trọng tài chấm điểm trực tiếp trên sân (
                    <strong>
                      Band {activeProfile?.currentBand} ·{" "}
                      {Number(activeProfile?.currentScore || 0).toFixed(0)}/100
                      điểm
                    </strong>
                    ). Để tránh việc người chơi tự ý thêm bớt điểm, chức năng tự
                    đánh giá đã được khóa — điểm trình độ chỉ thay đổi khi Trọng
                    tài chấm ở các buổi chơi tiếp theo.
                  </>
                ) : (
                  <>
                    Bạn đã nhận mức điểm tạm thời{" "}
                    <strong>
                      Band {activeProfile?.currentBand} (
                      {Number(activeProfile?.currentScore || 0).toFixed(0)}/100
                      điểm)
                    </strong>{" "}
                    từ bài khảo sát ban đầu để đủ điều kiện đặt sân. Chức năng tự
                    đánh giá hiện được khóa lại để chờ Trọng tài đánh giá chính
                    thức khi bạn ra sân.
                  </>
                )}
              </p>
              <div className="sp-locked-actions">
                <button
                  type="button"
                  className="button button-secondary"
                  onClick={() => setShowSurveyForm(false)}
                >
                  Xem bảng điểm &amp; Lịch sử
                </button>
                <button
                  type="button"
                  className="button button-primary"
                  onClick={() =>
                    onBrowseSport?.(
                      selectedSportId === 2 ? "Pickleball" : "Badminton",
                    )
                  }
                >
                  Tìm buổi chơi {activeSportNameVi} <ArrowRight size={16} />
                </button>
              </div>
            </div>
          ) : (
            <>
              <div className="sp-section-header">
                <div>
                  <h2>Khảo sát Tự đánh giá Trình độ {activeSportNameVi}</h2>
                </div>
                <button
                  type="button"
                  className="button button-secondary sp-btn-sm"
                  onClick={() => setShowSurveyForm(false)}
                >
                  Đóng khảo sát
                </button>
              </div>

              <form onSubmit={handleSurveySubmit} className="sp-survey-form">
                {/* Playing Context Questions */}
                <div className="sp-context-grid">
                  <div className="sp-context-field">
                    <label>Thời gian bạn đã chơi {activeSportNameVi}</label>
                    <select
                      value={experienceText}
                      onChange={(e) => setExperienceText(e.target.value)}
                    >
                      <option value="Mới bắt đầu (< 3 tháng)">
                        Mới bắt đầu (&lt; 3 tháng)
                      </option>
                      <option value="3 - 6 tháng">Từ 3 – 6 tháng</option>
                      <option value="6 tháng - 2 năm">
                        Từ 6 tháng – 2 năm
                      </option>
                      <option value="2 - 4 năm (Phong trào thường xuyên)">
                        Từ 2 – 4 năm (Chơi thường xuyên)
                      </option>
                      <option value="Trên 4 năm / Từng thi đấu giải CLB">
                        Trên 4 năm / Từng giao lưu giải CLB
                      </option>
                    </select>
                  </div>
                  <div className="sp-context-field">
                    <label>Tần suất ra sân trung bình</label>
                    <select
                      value={frequencyText}
                      onChange={(e) => setFrequencyText(e.target.value)}
                    >
                      <option value="1 buổi / tuần hoặc ít hơn">
                        1 buổi / tuần hoặc ít hơn
                      </option>
                      <option value="2 - 3 buổi / tuần">
                        2 – 3 buổi / tuần
                      </option>
                      <option value="4+ buổi / tuần">4+ buổi / tuần</option>
                    </select>
                  </div>
                </div>

                {/* 5 Intuitive Questions */}
                <div className="sp-questions-list">
                  {activeRubric.dimensions.map((dim, idx) => {
                    const dimId = dim.id ?? dim.dimensionId;
                    const dimCode = dim.code ?? dim.dimensionCode;
                    const anchorInfo = getNormalizedDimensionAnchor(
                      dimCode,
                      selectedSportId,
                      idx,
                    );
                    const selectedVal = Number(surveyAnswers[dimId] ?? 2);

                    return (
                      <div key={dimId} className="sp-question-card">
                        <div className="sp-question-head">
                          <div>
                            <span className="sp-q-index">
                              Tiêu chí {idx + 1} / 5
                            </span>
                            <h3>{dim.name}</h3>
                            <p>{anchorInfo.subtitle}</p>
                          </div>
                        </div>

                        <div className="sp-level-options">
                          {anchorInfo.levels.map((lvl) => {
                            const checked = selectedVal === lvl.score;
                            return (
                              <label
                                key={lvl.score}
                                className={`sp-level-option ${
                                  checked ? "is-checked" : ""
                                }`}
                              >
                                <input
                                  type="radio"
                                  name={`dim-${dimId}`}
                                  value={lvl.score}
                                  checked={checked}
                                  onChange={() =>
                                    setSurveyAnswers((prev) => ({
                                      ...prev,
                                      [dimId]: lvl.score,
                                    }))
                                  }
                                />
                                <div className="sp-level-top">
                                  <span className="sp-level-tag">
                                    {lvl.surveyTag || lvl.tag}
                                  </span>
                                </div>
                                <p className="sp-level-desc">{lvl.desc}</p>
                              </label>
                            );
                          })}
                        </div>
                      </div>
                    );
                  })}
                </div>

                {/* Clean Submit Footer */}
                <div className="sp-survey-footer">
                  <div className="sp-preview-details">
                    <strong>
                      Xác nhận tạo điểm tạm thời môn {activeSportNameVi}
                    </strong>
                    <p>
                      Khảo sát tự đánh giá ban đầu chỉ thực hiện 1 lần để tạo
                      điểm tạm thời đi đặt sân. Sau khi lưu, tự đánh giá sẽ khóa
                      lại để chờ Trọng tài chấm chính thức trên sân.
                    </p>
                  </div>
                  <div className="sp-survey-submit-group">
                    <button
                      type="button"
                      className="button button-secondary"
                      onClick={() => setShowSurveyForm(false)}
                    >
                      Hủy
                    </button>
                    <button
                      type="submit"
                      className="button button-primary"
                      disabled={submitting}
                    >
                      <Sparkles size={16} />
                      {submitting
                        ? "Đang lưu hồ sơ…"
                        : "Lưu khảo sát & Nhận điểm tạm thời"}
                    </button>
                  </div>
                </div>
              </form>
            </>
          )}
        </section>
      )}

      {/* 3B. Latest 5-Dimension Skill Evaluation Result & History */}
      <div className="sp-main-grid">
        <section
          className={`sp-panel sp-result-panel ${
            isPickleball ? "is-pickleball" : ""
          }`}
          aria-label="Kết quả chấm điểm 5 tiêu chí gần nhất"
        >
          <div className="sp-panel-head">
            <div>
              <span className="sp-eyebrow-sm">
                KẾT QUẢ &amp; TIẾN BỘ KỸ NĂNG
              </span>
              <h2>Bảng điểm 5 Tiêu chí · {activeSportNameVi}</h2>
              <p className="sp-subtext">
                Xem điểm và mức tăng giảm so với lần trước. Mở từng tiêu chí
                để xem chỉ số kỹ thuật và nhận xét của Trọng tài.
              </p>
            </div>
          </div>

          {availableAssessments.length > 1 && (
            <div className="sp-assessment-switcher" role="tablist">
              {availableAssessments.map((ass) => {
                const isRef = isOnCourtType(
                  ass.assessmentType,
                  ass.assessorRole,
                );
                const isCurrent =
                  Number(displayedAssessment?.id) === Number(ass.id);
                return (
                  <button
                    key={ass.id}
                    type="button"
                    className={`sp-ass-switch-btn ${
                      isCurrent ? "is-active" : ""
                    } ${isRef ? "is-referee" : "is-survey"}`}
                    onClick={() => setSelectedAssessmentId(ass.id)}
                  >
                    {isRef ? (
                      <ShieldCheck size={15} />
                    ) : (
                      <ClipboardCheck size={15} />
                    )}
                    <span>
                      {isRef ? "Trọng tài chấm" : "Tự khảo sát"} · Band{" "}
                      {ass.bandResult || ass.resultBand} (
                      {Number(ass.totalScore || 0).toFixed(0)}đ)
                      {ass.sessionDate ? ` · ${ass.sessionDate}` : ""}
                    </span>
                  </button>
                );
              })}
            </div>
          )}

          {!displayedAssessment ? (
            <div className="sp-empty-box">
              <ClipboardCheck size={32} />
              <h3>
                Bạn chưa có điểm trình độ môn {activeSportNameVi}
              </h3>
              <p>
                Người chơi mới vui lòng làm bài khảo sát tự đánh giá ban đầu để
                nhận mức điểm tạm thời và đặt các buổi chơi phù hợp.
              </p>
              <button
                type="button"
                className="button button-primary"
                onClick={() => setShowSurveyForm(true)}
              >
                Làm khảo sát tự đánh giá ban đầu <ArrowRight size={16} />
              </button>
            </div>
          ) : (
            <>
              {/* Displayed Assessment Summary Banner */}
              {(() => {
                const isRef = isOnCourtType(
                  displayedAssessment.assessmentType,
                  displayedAssessment.assessorRole,
                );
                const bandCode =
                  displayedAssessment.bandResult ||
                  displayedAssessment.resultBand ||
                  "B1";
                const totalScoreNum = Number(
                  displayedAssessment.totalScore || 0,
                );
                const sessInfo = formatSessionInfo(displayedAssessment);
                const evaluatedTime = displayedAssessment.evaluatedAt
                  ? new Date(displayedAssessment.evaluatedAt).toLocaleString(
                      "vi-VN",
                      {
                        day: "2-digit",
                        month: "2-digit",
                        year: "numeric",
                        hour: "2-digit",
                        minute: "2-digit",
                      },
                    )
                  : null;

                return (
                  <div className="sp-assessment-meta-card">
                    <div className="sp-assessor-info">
                      <div className="sp-assessor-badges">
                        <span
                          className={`sp-source-chip ${
                            isRef ? "is-oncourt" : "is-survey"
                          }`}
                        >
                          {isRef ? (
                            <>
                              <ShieldCheck size={16} /> Xác nhận bởi Trọng tài
                              trên sân (On-Court)
                            </>
                          ) : (
                            <>
                              <ClipboardCheck size={16} /> Tự khảo sát ban đầu
                              (Điểm tạm thời)
                            </>
                          )}
                        </span>
                        {displayedAssessment.rubricVersion && (
                          <span className="sp-rubric-version-chip">
                            Rubric: {displayedAssessment.rubricName} (
                            {displayedAssessment.rubricVersion})
                          </span>
                        )}
                      </div>

                      <h4>
                        {isRef ? "Trọng tài chấm điểm:" : "Người thực hiện:"}{" "}
                        <strong>
                          {displayedAssessment.assessorName || "Trọng tài TKSC"}
                        </strong>
                      </h4>

                      {sessInfo && (
                        <p>
                          Buổi chơi đánh giá: <strong>{sessInfo}</strong>
                        </p>
                      )}
                      {evaluatedTime && (
                        <p>
                          Thời điểm ghi nhận: <strong>{evaluatedTime}</strong>
                        </p>
                      )}
                      {displayedAssessment.publishReason && (
                        <div className="sp-publish-reason-box">
                          <strong>Kết luận công bố từ Trọng tài:</strong>{" "}
                          <span>
                            {displayedAssessment.publishReason ===
                            "Published complete on-court observation"
                              ? "Đã hoàn tất quan sát thực tế cả 5 tiêu chí kỹ thuật trên sân và công bố điểm trình độ chính thức."
                              : displayedAssessment.publishReason}
                          </span>
                        </div>
                      )}
                    </div>

                    <div className="sp-assessment-total">
                      <span>
                        {isRef ? "Điểm Trọng tài chấm" : "Kết quả kỳ này"}
                      </span>
                      <strong>
                        {bandCode} · {totalScoreNum.toFixed(0)}/100đ
                      </strong>
                      <small>
                        {getBandInfo(selectedSportId, bandCode).title}
                      </small>
                      {previousTotal != null ? (
                        <>
                          <small className="sp-prev-score-note">
                            Lần trước: {previousBand || "—"} · {previousTotal}/100đ
                          </small>
                          <ScoreChange current={totalScoreNum} previous={previousTotal} />
                        </>
                      ) : <small>Đánh giá đầu tiên · Chưa có điểm trước để so sánh</small>}
                    </div>
                  </div>
                );
              })()}

              {/* 5 Dimensions Progress Breakdown */}
              <div className="sp-dimensions-list">
                {(() => {
                  const isRef = isOnCourtType(
                    displayedAssessment.assessmentType,
                    displayedAssessment.assessorRole,
                  );
                  const scoredRows = displayedAssessment.scores || [];
                  const rubricDims = activeRubric?.dimensions || [];
                  const criteriaList =
                    scoredRows.length > 0
                      ? scoredRows.map((s, idx) => {
                          const matchedRubricDim = rubricDims.find(
                            (d) =>
                              (d.id ?? d.dimensionId) === s.dimensionId ||
                              (d.code ?? d.dimensionCode) ===
                                (s.dimensionCode ?? s.code),
                          );
                          return {
                            id: s.dimensionId ?? matchedRubricDim?.id ?? idx + 1,
                            code:
                              s.dimensionCode ??
                              s.code ??
                              matchedRubricDim?.code ??
                              `D${idx + 1}`,
                            name:
                              s.dimensionName ??
                              s.name ??
                              matchedRubricDim?.name ??
                              `Tiêu chí ${idx + 1}`,
                            weightPercent:
                              s.weightPercent ??
                              matchedRubricDim?.weightPercent ??
                              20,
                            scoreRow: s,
                          };
                        })
                      : rubricDims.map((d, idx) => ({
                          id: d.id ?? d.dimensionId ?? idx + 1,
                          code: d.code ?? d.dimensionCode ?? `D${idx + 1}`,
                          name: d.name ?? d.dimensionName ?? `Tiêu chí ${idx + 1}`,
                          weightPercent: d.weightPercent ?? 20,
                          scoreRow: null,
                        }));

                  return criteriaList.map((crit, idx) => {
                    const dimId = crit.id;
                    const dimCode = crit.code;
                    const scoreRow = crit.scoreRow;
                    const rawScore =
                      scoreRow != null
                        ? Math.round(
                            Number(scoreRow.score ?? scoreRow.rawScore ?? 0),
                          )
                        : 0;
                    const weightedPts =
                      scoreRow != null
                        ? Number(scoreRow.weightedPoints ?? rawScore * 5)
                        : rawScore * 5;
                    const pct = Math.min(
                      100,
                      Math.max(0, (rawScore / 4) * 100),
                    );
                    const anchorInfo = getNormalizedDimensionAnchor(
                      dimCode,
                      selectedSportId,
                      idx,
                    );
                    const achievedAnchor =
                      anchorInfo.levels.find((l) => l.score === rawScore) ||
                      anchorInfo.levels[0];
                    const nextAnchor =
                      rawScore < 4
                        ? anchorInfo.levels.find(
                            (l) => l.score === rawScore + 1,
                          )
                        : null;
                    const refereeEvidence = scoreRow?.evidence
                      ? String(scoreRow.evidence).trim()
                      : "";
                    const parsedSub = parseSubSkillEvidence(
                      anchorInfo.code || dimCode,
                      refereeEvidence,
                      rawScore,
                    );
                    const previousRow = canCompareDimensions
                      ? previousAssessment.scores?.find((s) =>
                          Number(s.dimensionId) === Number(dimId)
                          || s.dimensionCode === dimCode)
                      : null;
                    const previousRaw = previousRow?.score ?? previousRow?.rawScore ?? null;
                    const previousPoints = previousRaw == null ? null
                      : Number(previousRow.weightedPoints ?? Number(previousRaw) * 5);
                    const previousSub = previousRow
                      ? parseSubSkillEvidence(anchorInfo.code || dimCode, previousRow.evidence, previousRaw)
                      : null;
                    const isExpanded = Boolean(expandedDimIds[dimId]);

                    return (
                      <div key={dimId} className="sp-dimension-item">
                        <div className="sp-dim-header">
                          <div className="sp-dim-title-wrap">
                            <div className="sp-dim-meta-row">
                              <span className="sp-dim-code">
                                Tiêu chí {idx + 1} / 5 · {dimCode}
                              </span>
                              <span className="sp-dim-weight-pill">
                                Trọng số {crit.weightPercent}%
                              </span>
                              <span className="sp-dim-avg10-pill">
                                {parsedSub.hasRecordedSliders
                                  ? `TB 4 chỉ số: ${parsedSub.avg10}/10`
                                  : "Chưa ghi nhận điểm thành phần"}
                              </span>
                            </div>
                            <h4>{crit.name}</h4>
                          </div>
                          <div className="sp-dim-numbers">
                            <span className="sp-dim-raw-badge">
                              Mức {rawScore} / 4
                            </span>
                            <strong>{weightedPts.toFixed(0)} / 20 điểm</strong>
                            {previousPoints != null && (
                              <>
                                <span className="sp-previous-value">Lần trước: {previousPoints}/20 · Mức {previousRaw}/4</span>
                                <ScoreChange current={weightedPts} previous={previousPoints} />
                              </>
                            )}
                          </div>
                        </div>

                        {/* Visual 0..4 Scale & Bar */}
                        <div className="sp-dim-bar">
                          <div
                            className="sp-dim-bar-fill"
                            style={{ width: `${pct}%` }}
                          />
                          {previousRaw != null && (
                            <span className="sp-previous-marker"
                              style={{ left: `${Math.min(100, Math.max(0, Number(previousRaw) / 4 * 100))}%` }}
                              title={`Mức cũ: ${previousRaw}/4`}
                              aria-label={`Mức cũ: ${previousRaw}/4`} />
                          )}
                        </div>
                        <div className="sp-dim-level-steps">
                          {[0, 1, 2, 3, 4].map((lv) => (
                            <span
                              key={lv}
                              className={`sp-dim-step ${
                                lv === rawScore
                                  ? "is-exact"
                                  : previousRaw != null && lv === Number(previousRaw)
                                    ? "is-previous"
                                  : lv < rawScore
                                    ? "is-filled"
                                    : ""
                              }`}
                            >
                              Mức {lv} ({lv * 5}đ)
                              {previousRaw != null && lv === Number(previousRaw) && lv !== rawScore ? " · Cũ" : ""}
                            </span>
                          ))}
                        </div>

                        {!canCompareDimensions && previousTotal != null && (
                          <p className="sp-comparison-note">Chưa có bảng điểm lần trước cùng bộ tiêu chí để so sánh từng kỹ năng.</p>
                        )}
                        <details className="sp-result-details" key={displayedAssessment.id}>
                          <summary>Xem chỉ số chi tiết &amp; nhận xét</summary>
                        {/* What the Referee Observes on Court */}
                        {anchorInfo.observations && (
                          <div className="sp-dim-observation-guide">
                            <strong>Trọng tâm quan sát trên sân:</strong>{" "}
                            <span>{anchorInfo.observations}</span>
                          </div>
                        )}

                        {/* 4 Quantitative Sub-skill Progress Bars (0–10) */}
                        {parsedSub.hasRecordedSliders && parsedSub.subSkillList?.length > 0 && (
                          <div className="sp-subskills-panel">
                            <div className="sp-subskills-panel-head">
                              <strong>
                                {isRef
                                  ? "4 Chỉ số kỹ thuật thành phần do Trọng tài chấm (Thang 0 – 10)"
                                  : "4 Chỉ số kỹ thuật thành phần quy chiếu (Thang 0 – 10)"}
                              </strong>
                              <span>
                                Trung bình: <b>{parsedSub.avg10}/10</b> → Quy đổi{" "}
                                <b>Mức {rawScore}/4 ({weightedPts.toFixed(0)}đ)</b>
                              </span>
                            </div>
                            <div className="sp-subskills-grid">
                              {parsedSub.subSkillList.map((sub) => {
                                const oldValue = previousSub?.hasRecordedSliders
                                  ? previousSub.subSkillMap[sub.code] : null;
                                return (
                                <div
                                  key={sub.code}
                                  className="sp-subskill-card"
                                >
                                  <div className="sp-subskill-top">
                                    <div>
                                      <strong className="sp-subskill-name">
                                        {sub.name}
                                      </strong>
                                      {sub.unit && (
                                        <span className="sp-subskill-unit">
                                          Đo lường: {sub.unit}
                                        </span>
                                      )}
                                    </div>
                                    <div className="sp-subskill-badges">
                                      <span
                                        className={`sp-subskill-tier is-tier-${sub.tierInfo.level0to4}`}
                                      >
                                        {sub.tierInfo.label}
                                      </span>
                                      <strong className="sp-subskill-score">
                                        {sub.value10}/10
                                      </strong>
                                    </div>
                                  </div>
                                  <div className="sp-subskill-bar">
                                    <div
                                      className={`sp-subskill-bar-fill is-tier-${sub.tierInfo.level0to4}`}
                                      style={{
                                        width: `${sub.value10 * 10}%`,
                                      }}
                                    />
                                    {oldValue != null && <span className="sp-previous-marker"
                                      style={{ left: `${oldValue * 10}%` }}
                                      title={`Lần trước: ${oldValue}/10`} aria-label={`Lần trước: ${oldValue}/10`} />}
                                  </div>
                                  {oldValue != null && <div className="sp-subskill-change">
                                    <span className="sp-previous-value">Lần trước: {oldValue}/10</span>
                                    <ScoreChange current={sub.value10} previous={oldValue} />
                                  </div>}
                                  <p className="sp-subskill-desc">
                                    {sub.tierDesc}
                                  </p>
                                </div>
                              );})}
                            </div>

                            {(parsedSub.strengthText ||
                              parsedSub.weaknessText) && (
                              <div className="sp-subskills-insights">
                                {parsedSub.strengthText && (
                                  <div className="sp-insight-chip is-strength">
                                    <strong>Điểm mạnh nổi bật:</strong>{" "}
                                    <span>{parsedSub.strengthText}</span>
                                  </div>
                                )}
                                {parsedSub.weaknessText && (
                                  <div className="sp-insight-chip is-weakness">
                                    <strong>Kỹ năng cần ưu tiên rèn thêm:</strong>{" "}
                                    <span>{parsedSub.weaknessText}</span>
                                  </div>
                                )}
                              </div>
                            )}
                          </div>
                        )}

                        {/* Meaning of Achieved Score Level */}
                        {achievedAnchor && (
                          <div className="sp-dim-achieved-box">
                            <div className="sp-dim-achieved-head">
                              <CheckCircle2 size={16} />
                              <strong>
                                Ý nghĩa mức điểm tổng hợp ({achievedAnchor.tag}):
                              </strong>
                            </div>
                            <p className="sp-dim-achieved-desc">
                              {achievedAnchor.desc}
                            </p>
                            {nextAnchor && (
                              <p className="sp-dim-next-target">
                                <strong>
                                  Mục tiêu lên Mức {rawScore + 1}/4 (
                                  {nextAnchor.weightedPoints}đ):
                                </strong>{" "}
                                {nextAnchor.desc}
                              </p>
                            )}
                          </div>
                        )}

                        {/* Referee Observation / Comment (Synced from AssessmentPage.jsx) */}
                        <div
                          className={`sp-referee-comment-box ${
                            refereeEvidence ? "has-custom-note" : ""
                          }`}
                        >
                          <div className="sp-referee-comment-head">
                            <ShieldCheck size={16} />
                            <strong>
                              {isRef
                                ? `Nhận xét & Bằng chứng quan sát của Trọng tài (${displayedAssessment.assessorName || "Trọng tài"}):`
                                : "Ghi chú đánh giá tiêu chí:"}
                            </strong>
                          </div>
                          {parsedSub.customNote && (
                            <p className="sp-referee-custom-note">
                              <strong>Ghi chú trực tiếp trên sân:</strong> “
                              {parsedSub.customNote}”
                            </p>
                          )}
                          {!parsedSub.customNote && <p>
                            {isRef ? "Trọng tài chưa ghi nhận xét riêng cho tiêu chí này."
                              : "Kết quả từ khảo sát tự đánh giá ban đầu."}
                          </p>}
                          {!parsedSub.hasRecordedSliders && <p>
                            Lần đánh giá này chưa lưu điểm riêng cho 4 chỉ số thành phần.
                          </p>}
                        </div>

                        {/* Expandable 5-Level Anchor Scale Reference */}
                        <div className="sp-dim-scale-toggle-wrap">
                          <button
                            type="button"
                            className="sp-dim-scale-toggle-btn"
                            onClick={() =>
                              setExpandedDimIds((prev) => ({
                                ...prev,
                                [dimId]: !prev[dimId],
                              }))
                            }
                          >
                            {isExpanded
                              ? "Ẩn bảng mô tả 5 mức điểm (0 → 4)"
                              : "Xem đầy đủ ý nghĩa 5 mức điểm của tiêu chí này (0 → 4)"}
                          </button>
                          {isExpanded && (
                            <div className="sp-dim-all-levels">
                              {anchorInfo.levels.map((lvl) => {
                                const isHit = lvl.score === rawScore;
                                return (
                                  <div
                                    key={lvl.score}
                                    className={`sp-dim-level-row ${
                                      isHit ? "is-hit" : ""
                                    }`}
                                  >
                                    <span className="sp-dim-level-badge">
                                      Mức {lvl.score}/4 ({lvl.weightedPoints}đ)
                                    </span>
                                    <span className="sp-dim-level-text">
                                      {lvl.desc}
                                    </span>
                                    {isHit && (
                                      <span className="sp-dim-level-current-tag">
                                        Đạt mức này
                                      </span>
                                    )}
                                  </div>
                                );
                              })}
                            </div>
                          )}
                        </div>
                        </details>
                      </div>
                    );
                  });
                })()}
              </div>
            </>
          )}

          {/* Band Reference Table */}
          <div className="sp-band-legend">
            <h4>{sportMeta.standardLabel}</h4>
            <div className="sp-band-chips">
              {sportMeta.bands.map((b) => (
                <div
                  key={b.code}
                  className={`sp-band-chip ${
                    activeProfile?.currentBand === b.code ? "is-active" : ""
                  }`}
                >
                  <strong>
                    {b.code} ({b.range}đ)
                  </strong>
                  <span>{b.title}</span>
                </div>
              ))}
            </div>
          </div>
        </section>

        {/* 3C. Rating History Timeline */}
        <section
          className="sp-panel sp-history-panel"
          aria-label="Lịch sử biến động trình độ"
        >
          <div className="sp-panel-head">
            <div>
              <span className="sp-eyebrow-sm">LỊCH SỬ THAY ĐỔI TRÌNH ĐỘ</span>
              <h2>Lịch sử Biến động · {activeSportNameVi}</h2>
              <p className="sp-subtext">
                Theo dõi quá trình thay đổi trình độ từ khảo sát ban đầu đến các
                buổi được Trọng tài đánh giá trên sân.
              </p>
            </div>
          </div>

          {ratingHistory.length === 0 ? (
            <div className="sp-empty-box">
              <History size={30} />
              <h3>Chưa có lịch sử biến động điểm</h3>
              <p>
                Sau khi bạn hoàn thành khảo sát ban đầu hoặc được Trọng tài chấm
                điểm sau buổi chơi, lịch sử tăng/giảm sẽ hiển thị tại đây.
              </p>
            </div>
          ) : (
            <div className="sp-history-timeline">
              {ratingHistory.map((item, idx) => {
                const isReferee = isOnCourtType(
                  item.assessmentType,
                  item.changedByRole,
                );
                const hasOld = item.oldScore != null;
                const delta = hasOld
                  ? Number(item.newScore || 0) - Number(item.oldScore || 0)
                  : null;
                const isUp = delta != null && delta > 0;
                const isDown = delta != null && delta < 0;
                const sessLabel = formatSessionInfo(item);
                const itemAssId =
                  item.assessmentId ?? item.assessmentDetails?.id ?? null;
                const isViewingThis =
                  itemAssId != null &&
                  Number(displayedAssessment?.id) === Number(itemAssId);

                return (
                  <article
                    key={item.id || idx}
                    className={`sp-history-item ${
                      isReferee ? "is-referee" : "is-survey"
                    } ${isViewingThis ? "is-selected-history" : ""}`}
                  >
                    <div className="sp-history-marker">
                      {isReferee ? (
                        <UserCheck size={17} />
                      ) : (
                        <ClipboardCheck size={17} />
                      )}
                    </div>
                    <div className="sp-history-body">
                      <div className="sp-history-top">
                        <span
                          className={`sp-history-tag ${
                            isReferee ? "is-referee" : "is-survey"
                          }`}
                        >
                          {isReferee
                            ? "Trọng tài chấm trên sân"
                            : "Tự khảo sát ban đầu"}
                        </span>
                        {item.sessionDate && (
                          <time>Ngày: {item.sessionDate}</time>
                        )}
                      </div>

                      <div className="sp-history-transition">
                        <div className="sp-trans-nodes">
                          {hasOld ? (
                            <>
                              <span className="sp-node-old">
                                {item.oldBand || "—"} (
                                {Number(item.oldScore).toFixed(0)}đ)
                              </span>
                              <ArrowRight size={15} />
                            </>
                          ) : (
                            <span className="sp-node-init">
                              Khởi tạo tạm thời →
                            </span>
                          )}
                          <strong className="sp-node-new">
                            Band {item.newBand} (
                            {Number(item.newScore || 0).toFixed(0)}đ)
                          </strong>
                        </div>

                        {delta != null && (
                          <span
                            className={`sp-delta-badge ${
                              isUp ? "is-up" : isDown ? "is-down" : "is-same"
                            }`}
                          >
                            <TrendingUp size={13} />
                            {delta > 0
                              ? `+${delta.toFixed(0)} điểm`
                              : delta < 0
                                ? `${delta.toFixed(0)} điểm`
                                : "Giữ nguyên (0đ)"}
                          </span>
                        )}
                      </div>

                      <div className="sp-history-details">
                        {item.changedByName && (
                          <span>
                            Người ghi nhận: <strong>{item.changedByName}</strong>
                          </span>
                        )}
                        {sessLabel && (
                          <span>
                            Buổi chơi: <strong>{sessLabel}</strong>
                          </span>
                        )}
                      </div>

                      {itemAssId != null && (
                        <div className="sp-history-actions">
                          <button
                            type="button"
                            className={`sp-history-view-btn ${
                              isViewingThis ? "is-active" : ""
                            }`}
                            onClick={() => setSelectedAssessmentId(itemAssId)}
                          >
                            {isViewingThis
                              ? "✓ Đang hiển thị chi tiết 5 tiêu chí bên trái"
                              : "Xem chi tiết điểm & nhận xét 5 tiêu chí →"}
                          </button>
                        </div>
                      )}
                    </div>
                  </article>
                );
              })}
            </div>
          )}
        </section>
      </div>
      <SkillReviews role="player" sportId={selectedSportId} />
    </div>
  );
}
