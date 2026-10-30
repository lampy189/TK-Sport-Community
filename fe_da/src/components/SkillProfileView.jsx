import { useEffect, useMemo, useState } from "react";
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
const DIMENSION_ANCHORS = {
  // ================= BADMINTON =================
  BM_SERVE_NET: {
    subtitle:
      "Cảm giác giao cầu, đỡ giao cầu và xử lý các pha cầu ngắn gần lưới",
    levels: [
      {
        score: 0,
        tag: "Mới làm quen",
        desc: "Mới tập giao cầu, đôi khi cầu còn vướng lưới hoặc bay bổng lên giữa sân cho đối phương dễ tấn công.",
      },
      {
        score: 1,
        tag: "Giao cầu qua lưới cơ bản",
        desc: "Giao cầu cao sâu hoặc giao ngắn qua lưới khá đều tay; đỡ giao cầu trả sang sân đối phương an toàn.",
      },
      {
        score: 2,
        tag: "Giao ngắn ổn định & Biết bỏ nhỏ",
        desc: "Giao cầu ngắn trái tay sát mép lưới khá chuẩn; biết bỏ nhỏ và đẩy cầu ngang lưới khi có cơ hội.",
      },
      {
        score: 3,
        tag: "Xử lý lưới linh hoạt",
        desc: "Kết hợp giao cầu ngắn và búng cầu nhanh ra sau để tạo bất ngờ; chủ động chụp lưới và gài cầu xoáy sát lưới.",
      },
      {
        score: 4,
        tag: "Làm chủ khu vực lưới",
        desc: "Giao cầu và trả giao rất kín kẽ, kiểm soát nhịp độ trên lưới và thường xuyên ép đối phương phải nâng cầu bổng.",
      },
    ],
  },
  BM_CLEAR_BACKHAND: {
    subtitle: "Khả năng phông cầu về cuối sân và xử lý khi cầu sang góc trái tay",
    levels: [
      {
        score: 0,
        tag: "Cầu chưa đi hết sân",
        desc: "Phông cầu thường chỉ bay tới khoảng giữa sân; khi cầu sang bên trái tay còn lúng túng hoặc đánh hụt nhịp.",
      },
      {
        score: 1,
        tag: "Phông cầu thuận tay cuối sân",
        desc: "Khi đứng thoải mái bên thuận tay có thể phông cầu sâu về cuối sân; góc trái tay mới dừng ở mức đỡ cầu qua lưới.",
      },
      {
        score: 2,
        tag: "Phông sâu & Biết ve cầu trái tay",
        desc: "Phông cầu thuận tay sâu về cuối sân đều đặn; biết xoay người ve cầu trái tay trả qua lưới về giữa sân hoặc chéo sân.",
      },
      {
        score: 3,
        tag: "Thoát cầu tốt cả hai góc sau",
        desc: "Đánh phông lao hoặc phông phòng thủ đều tới cuối sân; cú trái tay trên cao thoát cầu sâu sang phần sân sau của đối thủ.",
      },
      {
        score: 4,
        tag: "Điều cầu cuối sân toàn diện",
        desc: "Phông cầu và ve trái tay đi thẳng hoặc chéo sân nhẹ nhàng, giữ được độ sâu ngay cả khi đang bị ép thế bị động.",
      },
    ],
  },
  BM_SMASH_DEFENSE: {
    subtitle:
      "Cảm giác đập cầu tấn công, chặt cầu bỏ nhỏ và phản xạ phòng thủ",
    levels: [
      {
        score: 0,
        tag: "Đang tập động tác",
        desc: "Chưa quen nhịp đập cầu trên cao; khi đối phương đập mạnh thường bị giật mình hoặc đỡ cầu văng ra ngoài.",
      },
      {
        score: 1,
        tag: "Đập cầu & Đỡ đập cơ bản",
        desc: "Đập được các quả cầu bổng ở giữa sân qua lưới; đỡ được những cú đập vừa phải trả lại qua lưới.",
      },
      {
        score: 2,
        tag: "Biết phối hợp Đập & Chặt cầu",
        desc: "Đập cầu có hướng xuống hai biên kết hợp chặt cầu bỏ nhỏ; khi phòng thủ biết bung cầu cao sâu hoặc tạt ngang.",
      },
      {
        score: 3,
        tag: "Tấn công có điểm rơi & Thủ chắc",
        desc: "Đập cầu cắm và có lực tốt; khi phòng thủ biết điều cầu vào khoảng trống để chuyển nhanh từ thủ sang phản công.",
      },
      {
        score: 4,
        tag: "Công thủ tốc độ cao",
        desc: "Duy trì chuỗi tấn công đập cầu rồi tràn lưới dứt điểm liên tục; phòng thủ phản tạt rất chắc trước các pha cầu nhanh.",
      },
    ],
  },
  BM_FOOTWORK: {
    subtitle: "Nhịp bước chân di chuyển khắp sân và giữ thăng bằng sau khi đánh",
    levels: [
      {
        score: 0,
        tag: "Di chuyển theo bản năng",
        desc: "Chạy theo cầu tự nhiên, đôi khi còn bước chéo chân hoặc đứng nguyên tại chỗ sau khi vừa chạm cầu.",
      },
      {
        score: 1,
        tag: "Có ý thức lùi về giữa sân",
        desc: "Biết bước chân thuận lên lưới và lùi về sau cơ bản; sau khi đánh xong có thói quen quay lại khu vực trung tâm.",
      },
      {
        score: 2,
        tag: "Di chuyển đều các góc sân",
        desc: "Bộ pháp lên lưới, lùi hai góc sau khá nhịp nhàng, giữ được thăng bằng cơ thể trong các pha cầu qua lại.",
      },
      {
        score: 3,
        tag: "Bước chân thanh thoát & Linh hoạt",
        desc: "Có nhịp nhún chuẩn bị trước khi đối phương chạm cầu, đổi hướng nhanh khi bị lừa cầu và hồi vị rất kịp thời.",
      },
      {
        score: 4,
        tag: "Bộ pháp gọn & Bền bỉ",
        desc: "Di chuyển ít bước nhưng bao quát toàn sân, bật nhảy và thu chân liền mạch, giữ được tốc độ tốt suốt buổi chơi.",
      },
    ],
  },
  BM_DOUBLES: {
    subtitle: "Sự ăn ý khi đứng sân đánh đôi và khả năng giữ nhịp pha cầu bền",
    levels: [
      {
        score: 0,
        tag: "Chưa quen cách đứng đội hình đôi",
        desc: "Thường đứng cố định một bên sân hoặc di chuyển trùng vào vị trí của đồng đội; pha cầu thường kết thúc sớm.",
      },
      {
        score: 1,
        tag: "Hiểu vị trí Công - Thủ cơ bản",
        desc: "Biết đứng một trên một dưới khi đội nhà tấn công và tách sang hai bên khi phông cầu bổng để phòng thủ.",
      },
      {
        score: 2,
        tag: "Xoay tua vị trí nhịp nhàng",
        desc: "Chủ động tràn lên bắt lưới khi đồng đội lùi đập cầu và lùi xuống bọc lót khi đồng đội tiến lên; giữ cầu bền ít tự hỏng.",
      },
      {
        score: 3,
        tag: "Phối hợp tạo cơ hội cho đồng đội",
        desc: "Biết điều cầu ép điểm yếu của đối phương để mồi cầu cho đồng đội tuyến sau dứt điểm.",
      },
      {
        score: 4,
        tag: "Bọc lót & Đọc tình huống ăn ý",
        desc: "Chuyển đổi trạng thái công – thủ tức thì cùng đồng đội, hầu như không để lộ khoảng trống trên sân.",
      },
    ],
  },

  // ================= PICKLEBALL =================
  PK_SERVE_RETURN: {
    subtitle: "Cảm giác giao bóng, trả giao bóng sâu và di chuyển lên vạch bếp",
    levels: [
      {
        score: 0,
        tag: "Đang làm quen luật & Giao bóng",
        desc: "Đang làm quen cách giao bóng dưới thắt lưng và quy tắc bóng nảy hai lần; đôi khi giao bóng còn ra ngoài hoặc chạm lưới.",
      },
      {
        score: 1,
        tag: "Giao & Trả bóng qua lưới an toàn",
        desc: "Giao bóng vào ô khá đều tay; đỡ giao bóng trả qua lưới ổn định nhưng bóng thường rơi ở khoảng giữa sân.",
      },
      {
        score: 2,
        tag: "Trả giao sâu & Chủ động tiến lên",
        desc: "Giao bóng và trả giao bóng sâu về cuối sân đối phương, sau khi trả bóng kịp chạy dâng lên sát vạch bếp.",
      },
      {
        score: 3,
        tag: "Giao bóng có hướng & Lên lưới vững",
        desc: "Biết giao bóng sâu vào góc trái tay đối thủ; trả giao bóng sâu để có đủ thời gian chạy lên đứng vững ở vạch bếp.",
      },
      {
        score: 4,
        tag: "Giao & Trả bóng xoáy áp đặt",
        desc: "Giao và trả giao bóng có độ xoáy, điểm rơi sát vạch cuối sân giúp cặp đấu chiếm lợi thế trên lưới ngay từ đầu.",
      },
    ],
  },
  PK_THIRD_SHOT: {
    subtitle:
      "Cách xử lý cú đánh thứ 3 từ cuối sân để tìm đường tiến lên vạch bếp",
    levels: [
      {
        score: 0,
        tag: "Thường đứng cuối sân đánh bóng",
        desc: "Sau khi giao bóng thường đứng lại ở vạch cuối sân đánh qua lại, bóng dễ bay cao cho đối phương trên lưới đập xuống.",
      },
      {
        score: 1,
        tag: "Tập bỏ nhỏ hoặc đánh thẳng lên lưới",
        desc: "Hiểu mục tiêu cần tiến lên vạch bếp sau cú đánh thứ 3; đang tập bỏ nhỏ từ cuối sân nhưng bóng đôi khi còn hơi cao.",
      },
      {
        score: 2,
        tag: "Bỏ nhỏ cú thứ 3 vào vùng bếp",
        desc: "Thực hiện được cú bỏ nhỏ rơi êm vào vùng bếp hoặc đánh bóng thấp qua lưới để cùng đồng đội tiến dần lên trên.",
      },
      {
        score: 3,
        tag: "Kết hợp đánh nhanh & Bỏ nhỏ linh hoạt",
        desc: "Biết chọn lúc đánh bóng căng tầm thấp rồi nối tiếp bằng cú bỏ nhỏ rơi sát lưới để dâng lên vạch bếp an toàn.",
      },
      {
        score: 4,
        tag: "Lên lưới mượt mà từ mọi vị trí",
        desc: "Đưa bóng rơi thấp vào chân đối phương ở cả bên thuận tay lẫn trái tay, tiến lên chiếm vạch bếp rất tự nhiên.",
      },
    ],
  },
  PK_DINK_NVZ: {
    subtitle: "Khả năng gõ bóng ngắn (Dink) và kiểm soát bóng tại vạch bếp",
    levels: [
      {
        score: 0,
        tag: "Chưa quen lực chạm bóng ngắn",
        desc: "Khi đứng gần lưới chạm bóng còn hơi mạnh làm bóng nảy cao; đôi khi bước đà theo bóng chạm vào vạch bếp.",
      },
      {
        score: 1,
        tag: "Gõ bóng qua lại vài nhịp cơ bản",
        desc: "Nắm rõ quy tắc không dẫm vạch bếp khi bắt bóng trên không; giữ được vài nhịp bóng ngắn trước khi bóng nảy cao.",
      },
      {
        score: 2,
        tag: "Duy trì nhịp Dink thẳng & chéo sân",
        desc: "Giữ tư thế thấp tại vạch bếp, gõ bóng ngắn qua lại ổn định và biết điều bóng chéo sân.",
      },
      {
        score: 3,
        tag: "Dink kiên nhẫn & Biết chọn bóng tấn công",
        desc: "Đấu bóng ngắn bền bỉ không nóng vội; biết gõ bóng ép chân đối phương và nhận ra bóng hơi cao để tăng tốc độ.",
      },
      {
        score: 4,
        tag: "Kiểm soát hoàn toàn vạch bếp",
        desc: "Điều bóng ngắn biến hóa hai góc kèm độ xoáy, chủ động gài đối phương nâng bóng cao để dứt điểm.",
      },
    ],
  },
  PK_VOLLEY_RESET: {
    subtitle:
      "Phản xạ bắt bóng trên không (Volley) và làm mềm bóng khi bị tấn công",
    levels: [
      {
        score: 0,
        tag: "Phản xạ trên lưới còn chậm",
        desc: "Khi bóng đi nhanh trên lưới thường vung vợt rộng hoặc đỡ bóng bay bổng ra ngoài sân.",
      },
      {
        score: 1,
        tag: "Bắt bóng tốc độ vừa trước mặt",
        desc: "Đỡ được các pha bóng trên không ở tốc độ trung bình; khi đối phương đánh mạnh vào người còn khó hãm lực bóng.",
      },
      {
        score: 2,
        tag: "Bắt Volley gọn & Biết giảm lực bóng",
        desc: "Luôn giơ vợt sẵn trước ngực, đẩy bóng gọn gàng và bắt đầu biết lỏng tay hãm lực khi đứng ở vùng giữa sân.",
      },
      {
        score: 3,
        tag: "Đấu tay đôi nhanh & Reset bóng êm",
        desc: "Phản xạ nhanh trong các pha đôi công tốc độ cao trên lưới; làm mềm được những cú đánh mạnh rơi êm lại vào vùng bếp.",
      },
      {
        score: 4,
        tag: "Phản công sắc bén & Biết bỏ bóng ngoài",
        desc: "Đấu tay đôi rất chắc, phản đòn chuẩn xác, đồng thời phán đoán tốt để nhả những quả bóng mạnh bay ra ngoài.",
      },
    ],
  },
  PK_DOUBLES_STRATEGY: {
    subtitle: "Sự phối hợp di chuyển cùng đồng đội và bao sân khi đánh đôi",
    levels: [
      {
        score: 0,
        tag: "Di chuyển còn lệch nhịp đồng đội",
        desc: "Thường một người đứng trên lưới còn một người đứng dưới cuối sân; dễ nhường nhau hoặc chạm vợt ở bóng giữa sân.",
      },
      {
        score: 1,
        tag: "Biết cùng đồng đội tiến lên vạch bếp",
        desc: "Có ý thức dâng lên đứng ngang hàng cùng đồng đội tại vạch bếp; đánh bóng nảy đất bên thuận tay ổn định.",
      },
      {
        score: 2,
        tag: "Phân chia bóng ở trục giữa sân",
        desc: "Đánh bóng cuối sân đều cả hai bên; biết thống nhất người có tay thuận ở giữa sẽ chủ động bao các đường bóng trung lộ.",
      },
      {
        score: 3,
        tag: "Di chuyển ngang đồng bộ cùng nhau",
        desc: "Hai người dịch chuyển sang trái/phải nhịp nhàng theo hướng bóng để khép góc, biết lốp bóng qua đầu khi cần.",
      },
      {
        score: 4,
        tag: "Phối hợp chiến thuật đôi ăn ý",
        desc: "Hiểu ý nhau trong việc đổi vị trí, cắt bóng trên lưới và cùng lùi phòng thủ hoặc cùng dâng tấn công rất nhịp nhàng.",
      },
    ],
  },
};

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

function getProfileLockState(prof) {
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
    prof.latestAssessment?.assessmentType === "OnCourt" ||
    (prof.ratingHistory || []).some((h) => h.assessmentType === "OnCourt");
  const isRefereeRated =
    prof.ratingStatus === "Rated" ||
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
  if (item.courtName) parts.push(item.courtName);
  if (item.sessionDate) parts.push(item.sessionDate);
  if (item.timeLabel) parts.push(item.timeLabel);
  return parts.length > 0 ? parts.join(" · ") : null;
}

export default function SkillProfileView({ onBrowseSport }) {
  const [overview, setOverview] = useState(null);
  const [rubrics, setRubrics] = useState([]);
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

  useEffect(() => {
    loadData();
  }, []);

  async function loadData() {
    setLoading(true);
    setError(null);
    try {
      const [rubricList, myData] = await Promise.all([
        api.getSkillRubrics(),
        api.getMySkillProfiles(),
      ]);
      setRubrics(Array.isArray(rubricList) ? rubricList : []);
      setOverview(myData);
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
  const isPickleball = selectedSportId === 2;
  const activeSportNameVi = getSportNameVi(
    selectedSportId,
    activeProfile?.sportName,
  );
  const activeLock = getProfileLockState(activeProfile);

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
          const lockState = getProfileLockState(prof);
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
                  <Calendar size={14} /> Nguồn gần nhất:{" "}
                  {latestType === "OnCourt"
                    ? `Trọng tài chấm (${prof.latestAssessment?.assessorName || "Trọng tài"})`
                    : latestType === "SelfSurvey"
                      ? "Tự khảo sát ban đầu (Điểm tạm thời)"
                      : "Chưa có bản đánh giá"}
                </span>
                <span>
                  <History size={14} /> {prof.ratingHistory?.length || 0} lần
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
                    const anchorInfo = DIMENSION_ANCHORS[dimCode] || {
                      subtitle: "",
                      levels: [0, 1, 2, 3, 4].map((s) => ({
                        score: s,
                        tag: `Lựa chọn ${s + 1}`,
                        desc: "Mô tả cảm nhận kỹ năng trên sân.",
                      })),
                    };
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
                                    {lvl.tag}
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
              <span className="sp-eyebrow-sm">CHI TIẾT ĐÁNH GIÁ KỸ NĂNG</span>
              <h2>Bảng điểm 5 Tiêu chí · {activeSportNameVi}</h2>
            </div>
          </div>

          {!latestAssessment ? (
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
              {/* Latest Assessment Summary Banner */}
              <div className="sp-assessment-meta-card">
                <div className="sp-assessor-info">
                  <span
                    className={`sp-source-chip ${
                      latestAssessment.assessmentType === "OnCourt"
                        ? "is-oncourt"
                        : "is-survey"
                    }`}
                  >
                    {latestAssessment.assessmentType === "OnCourt" ? (
                      <>
                        <ShieldCheck size={15} /> Trọng tài chấm trên sân
                      </>
                    ) : (
                      <>
                        <ClipboardCheck size={15} /> Tự khảo sát ban đầu (Điểm
                        tạm thời)
                      </>
                    )}
                  </span>
                  <h4>
                    Người đánh giá:{" "}
                    <strong>{latestAssessment.assessorName}</strong>
                  </h4>
                  {formatSessionInfo(latestAssessment) && (
                    <p>
                      Buổi chơi:{" "}
                      <strong>{formatSessionInfo(latestAssessment)}</strong>
                    </p>
                  )}
                </div>
                <div className="sp-assessment-total">
                  <span>Kết quả kỳ này</span>
                  <strong>
                    {latestAssessment.bandResult || latestAssessment.resultBand}{" "}
                    · {Number(latestAssessment.totalScore || 0).toFixed(0)}đ
                  </strong>
                  <small>
                    {
                      getBandInfo(
                        selectedSportId,
                        latestAssessment.bandResult ||
                          latestAssessment.resultBand,
                      ).title
                    }
                  </small>
                </div>
              </div>

              {/* 5 Dimensions Progress Breakdown */}
              <div className="sp-dimensions-list">
                {(activeRubric?.dimensions || []).map((dim, idx) => {
                  const dimId = dim.id ?? dim.dimensionId;
                  const dimCode = dim.code ?? dim.dimensionCode;
                  const scoreRow = (latestAssessment.scores || []).find(
                    (s) =>
                      s.dimensionId === dimId || s.dimensionCode === dimCode,
                  );
                  const rawScore =
                    scoreRow != null
                      ? Number(scoreRow.score ?? scoreRow.rawScore ?? 0)
                      : 0;
                  const weightedPts =
                    scoreRow != null
                      ? Number(scoreRow.weightedPoints ?? rawScore * 5)
                      : rawScore * 5;
                  const pct = Math.min(100, Math.max(0, (rawScore / 4) * 100));
                  const anchorInfo = DIMENSION_ANCHORS[dimCode];
                  const achievedAnchor = anchorInfo?.levels?.find(
                    (l) => l.score === Math.round(rawScore),
                  );

                  return (
                    <div key={dimId} className="sp-dimension-item">
                      <div className="sp-dim-header">
                        <div>
                          <span className="sp-dim-code">
                            Tiêu chí {idx + 1}
                          </span>
                          <h4>{dim.name}</h4>
                        </div>
                        <div className="sp-dim-numbers">
                          <strong>{weightedPts.toFixed(0)} / 20 điểm</strong>
                        </div>
                      </div>

                      <div className="sp-dim-bar">
                        <div
                          className="sp-dim-bar-fill"
                          style={{ width: `${pct}%` }}
                        />
                      </div>

                      {achievedAnchor && (
                        <p className="sp-dim-achieved">
                          <strong>{achievedAnchor.tag}:</strong>{" "}
                          {achievedAnchor.desc}
                        </p>
                      )}
                    </div>
                  );
                })}
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
                const isReferee =
                  item.assessmentType === "OnCourt" ||
                  (item.changedByRole &&
                    item.changedByRole.toLowerCase().includes("referee"));
                const hasOld = item.oldScore != null;
                const delta = hasOld
                  ? Number(item.newScore || 0) - Number(item.oldScore || 0)
                  : null;
                const isUp = delta != null && delta > 0;
                const isDown = delta != null && delta < 0;
                const sessLabel = formatSessionInfo(item);

                return (
                  <article
                    key={item.id || idx}
                    className={`sp-history-item ${
                      isReferee ? "is-referee" : "is-survey"
                    }`}
                  >
                    <div className="sp-history-marker">
                      {isReferee ? (
                        <UserCheck size={16} />
                      ) : (
                        <ClipboardCheck size={16} />
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
                    </div>
                  </article>
                );
              })}
            </div>
          )}
        </section>
      </div>
    </div>
  );
}
