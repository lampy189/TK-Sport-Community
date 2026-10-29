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
  Info,
  Calendar,
  UserCheck,
  AlertCircle,
  ChevronDown,
  ChevronUp,
} from "lucide-react";
import { api } from "../api";
import "../styles/skill-profile.css";

/**
 * Detailed sport-specific 0..4 level anchors & quantitative benchmarks
 * for Badminton (Chuẩn CLB Phong trào VN) and Pickleball (Chuẩn USA Pickleball / IPTPA / DUPR).
 */
const DIMENSION_ANCHORS = {
  // ================= BADMINTON (BM-CLB-V1) =================
  BM_SERVE_NET: {
    subtitle:
      "Giao cầu thuận/trái tay, trả giao cầu và kỹ thuật bỏ nhỏ / gài lưới",
    levels: [
      {
        score: 0,
        tag: "Mức 0 · Mới cầm vợt (B1)",
        desc: "Chưa nắm vững luật giao cầu chéo ô; giao cầu hay rúc lưới hoặc bổng cao giữa sân (< 5/10 quả qua lưới ổn định).",
      },
      {
        score: 1,
        tag: "Mức 1 · Cơ bản (B2 - TB-)",
        desc: "Giao cầu cao sâu (flick/high serve) hoặc giao ngắn qua lưới đạt ≥ 6/10 quả; đỡ giao cầu trả qua lưới nhưng chưa điều hướng.",
      },
      {
        score: 2,
        tag: "Mức 2 · Ổn định (B3 - TB)",
        desc: "Giao cầu ngắn trái tay sát mép lưới ổn định (≥ 7/10 quả vào vạch T); biết bỏ nhỏ (net shot) và đẩy cầu ngang lưới.",
      },
      {
        score: 3,
        tag: "Mức 3 · Chủ động (B4 - TB+)",
        desc: "Giao cầu biến hóa (ngắn + búng flick ra 2 góc sau); chụp lưới (net kill), gài lưới xoáy (spinning net) và kéo lưới chéo sân chủ động.",
      },
      {
        score: 4,
        tag: "Mức 4 · Thành thạo (B5 - Khá)",
        desc: "Kiểm soát hoàn toàn khu vực lưới (≥ 9/10 quả giao sát lưới không bổng); bắt bài trả giao ép đối phương phông cầu bị động.",
      },
    ],
  },
  BM_CLEAR_BACKHAND: {
    subtitle:
      "Độ sâu cú phông cầu thuận tay từ cuối sân và kỹ thuật xử lý góc trái tay",
    levels: [
      {
        score: 0,
        tag: "Mức 0 · Chưa cuối sân (B1)",
        desc: "Phông cầu chỉ tới giữa sân đối phương; góc trái tay (backhand) phải quay lưng hoặc đánh hụt nhịp.",
      },
      {
        score: 1,
        tag: "Mức 1 · Cuối sân thuận tay (B2 - TB-)",
        desc: "Thuận tay (forehand) phông cầu tới vạch 2 cuối sân khi đứng chủ động; trái tay trên cao mới chỉ đẩy trả qua lưới.",
      },
      {
        score: 2,
        tag: "Mức 2 · Phông cầu ổn định (B3 - TB)",
        desc: "Phông cầu thuận tay sâu sát vạch cuối sân ≥ 7/10 quả; trái tay (ve cầu) trả được qua lưới về giữa sân hoặc chéo sân.",
      },
      {
        score: 3,
        tag: "Mức 3 · Ve cầu sâu & Phông lao (B4 - TB+)",
        desc: "Phông tấn công (attacking clear) và phông phòng thủ đều tới cuối sân; ve cầu trái tay (backhand clear/drop) thoát cầu sang nửa sau sân.",
      },
      {
        score: 4,
        tag: "Mức 4 · Toàn diện 2 góc sau (B5 - Khá)",
        desc: "Phông cầu và ve trái tay cuối sân đi thẳng/chéo sâu như nhau ngay cả khi bị ép thế bị động.",
      },
    ],
  },
  BM_SMASH_DEFENSE: {
    subtitle:
      "Uy lực/điểm rơi cú đập cầu, chặt cầu (drop shot), phản tạt (drive) và bung cầu thủ",
    levels: [
      {
        score: 0,
        tag: "Mức 0 · Chưa biết đập/thủ (B1)",
        desc: "Chưa định hình động tác đập cầu trên cao; bị đối phương đập cầu là giật mình hoặc đỡ bung ra ngoài.",
      },
      {
        score: 1,
        tag: "Mức 1 · Đập cơ bản (B2 - TB-)",
        desc: "Đập cầu (smash) qua lưới khi cầu bổng giữa sân; đỡ đập (block) trả được cầu qua lưới với bóng tốc độ vừa.",
      },
      {
        score: 2,
        tag: "Mức 2 · Biết phối hợp Đập - Chặt (B3 - TB)",
        desc: "Đập cầu có hướng xuống 2 biên, kết hợp chặt cầu (drop shot) bỏ nhỏ; thủ bung cầu cao sâu hoặc tạt ngang (drive) ổn định.",
      },
      {
        score: 3,
        tag: "Mức 3 · Công sắc & Thủ phản công (B4 - TB+)",
        desc: "Đập cầu cắm sát biên / nhảy đập (jump smash); thủ điều cầu chéo lưới chuyển từ thế bị động sang phản công nhanh.",
      },
      {
        score: 4,
        tag: "Mức 4 · Áp đảo tốc độ cao (B5 - Khá)",
        desc: "Chuỗi tấn công liên hoàn (đập + tràn lưới dứt điểm); phòng thủ phản tạt tốc độ cao trước các cú smash mạnh.",
      },
    ],
  },
  BM_FOOTWORK: {
    subtitle:
      "Bước tách (split-step), di chuyển 6 góc sân và tốc độ hồi vị trung tâm",
    levels: [
      {
        score: 0,
        tag: "Mức 0 · Chạy tự nhiên (B1)",
        desc: "Di chuyển theo bản năng, hay chéo chân hoặc đứng yên sau khi đánh cầu; chưa biết hồi vị về trung tâm.",
      },
      {
        score: 1,
        tag: "Mức 1 · Biết hồi vị (B2 - TB-)",
        desc: "Bước chân lên lưới và lùi sau cơ bản đúng chân thuận; có ý thức quay về vị trí trung tâm sau khi chạm cầu.",
      },
      {
        score: 2,
        tag: "Mức 2 · Bộ pháp 4–6 góc (B3 - TB)",
        desc: "Có nhịp bước đệm (split-step), di chuyển 6 góc sân mạch lạc và giữ thăng bằng tốt trong các pha cầu trung bình.",
      },
      {
        score: 3,
        tag: "Mức 3 · Nhanh & Linh hoạt (B4 - TB+)",
        desc: "Bộ pháp lướt nhẹ, chuyển hướng đột ngột khi bị lừa cầu vẫn kịp cứu cầu và hồi vị ngay lập tức.",
      },
      {
        score: 4,
        tag: "Mức 4 · Bộ pháp chuẩn thi đấu (B5 - Khá)",
        desc: "Tối ưu số bước di chuyển toàn sân, bật nhảy thu chân liền mạch, duy trì tốc độ cao suốt 3 set đấu.",
      },
    ],
  },
  BM_DOUBLES: {
    subtitle:
      "Luân chuyển đội hình Công (Trên-Dưới) ↔ Thủ (Hai bên) và duy trì chuỗi Rally",
    levels: [
      {
        score: 0,
        tag: "Mức 0 · Chưa rõ vị trí đôi (B1)",
        desc: "Đứng cố định 1 bên sân hoặc chạy trùng vị trí đồng đội; chuỗi qua lại (rally) thường đứt dưới 6 chạm.",
      },
      {
        score: 1,
        tag: "Mức 1 · Hiểu Công/Thủ cơ bản (B2 - TB-)",
        desc: "Biết đứng Trên–Dưới khi đội nhà đập cầu và tách Ngang 2 bên khi phông bổng; duy trì rally trung bình 8–10 chạm.",
      },
      {
        score: 2,
        tag: "Mức 2 · Xoay tua nhịp nhàng (B3 - TB)",
        desc: "Chủ động tràn lưới khi đồng đội đập cầu và lùi bọc lót khi đồng đội lên lưới; duy trì rally ≥ 15 chạm ít tự đánh hỏng.",
      },
      {
        score: 3,
        tag: "Mức 3 · Phối hợp chiến thuật (B4 - TB+)",
        desc: "Biết khoét điểm yếu đối phương, điều cầu tạo cơ hội cho đồng đội công sau và giữ nhịp độ trận đấu ổn định.",
      },
      {
        score: 4,
        tag: "Mức 4 · Đánh đôi ăn ý cao (B5 - Khá)",
        desc: "Xoay chuyển đội hình tức thời không khoảng trống, đọc tình huống bọc lót và tổ chức phản công sắc bén.",
      },
    ],
  },

  // ================= PICKLEBALL (PK-IPTPA-DUPR-V1) =================
  PK_SERVE_RETURN: {
    subtitle:
      "Giao bóng đúng luật vào nửa sau ô và trả giao bóng sâu để dâng lên vạch Kitchen",
    levels: [
      {
        score: 0,
        tag: "Mức 0 · DUPR < 2.0 (B1)",
        desc: "Đang làm quen luật giao bóng dưới thắt lưng và luật 2 lần nảy (2-bounce rule); giao bóng trong sân < 6/10 quả.",
      },
      {
        score: 1,
        tag: "Mức 1 · DUPR ~2.5 (B2)",
        desc: "Giao bóng đúng luật vào ô ≥ 7/10 quả; trả giao bóng (return of serve) qua lưới ổn định nhưng bóng còn nông giữa sân.",
      },
      {
        score: 2,
        tag: "Mức 2 · DUPR ~3.0 (B3)",
        desc: "Giao bóng và trả giao bóng sâu về 1/3 cuối sân đối phương (≥ 7/10 quả), sau khi trả bóng kịp chạy dâng sát vạch NVZ.",
      },
      {
        score: 3,
        tag: "Mức 3 · DUPR ~3.5 (B4)",
        desc: "Giao bóng xoáy sâu ép góc trái tay đối phương; trả giao sâu có thời gian dừng bước (split-step) vững tại vạch Kitchen.",
      },
      {
        score: 4,
        tag: "Mức 4 · DUPR 4.0+ (B5)",
        desc: "Đạt ≥ 9/10 quả giao và trả giao bóng sâu sát baseline với tốc độ/độ xoáy biến hóa, chiếm trọn lợi thế vạch lưới.",
      },
    ],
  },
  PK_THIRD_SHOT: {
    subtitle:
      "Thực hiện 3rd Shot Drop rơi êm vào Kitchen hoặc 3rd Shot Drive kết hợp tiến lên NVZ",
    levels: [
      {
        score: 0,
        tag: "Mức 0 · DUPR < 2.0 (B1)",
        desc: "Chưa có khái niệm Cú đánh thứ 3 (3rd shot); thường đứng luôn ở baseline đánh bóng bổng dễ bị đối phương đập trên lưới.",
      },
      {
        score: 1,
        tag: "Mức 1 · DUPR ~2.5 (B2)",
        desc: "Hiểu mục tiêu tiến lên lưới sau cú thứ 3; thử thực hiện 3rd Shot Drop hoặc Drive nhưng bóng còn cao hoặc rúc lưới.",
      },
      {
        score: 2,
        tag: "Mức 2 · DUPR ~3.0 (B3)",
        desc: "Thực hiện được 3rd Shot Drop rơi vào vùng Kitchen hoặc Drive thấp qua lưới ≥ 5/10 lần để di chuyển qua vùng Transition.",
      },
      {
        score: 3,
        tag: "Mức 3 · DUPR ~3.5 (B4)",
        desc: "Kết hợp linh hoạt 3rd Shot Drive (tạo bóng bật ra) + 5th Shot Drop rơi êm vào NVZ (≥ 7/10 lần thành công lên lưới).",
      },
      {
        score: 4,
        tag: "Mức 4 · DUPR 4.0+ (B5)",
        desc: "3rd Shot Drop có điểm rơi thấp sát lưới vào góc chân đối phương ở cả thuận tay lẫn trái tay, tiến lên NVZ an toàn.",
      },
    ],
  },
  PK_DINK_NVZ: {
    subtitle:
      "Duy trì chuỗi Dink thấp thẳng/chéo sân và tuân thủ luật vạch Non-Volley Zone",
    levels: [
      {
        score: 0,
        tag: "Mức 0 · DUPR < 2.0 (B1)",
        desc: "Chưa kiểm soát được lực Dink ngắn tại vạch Kitchen; dễ dẫm vạch NVZ khi bắt volley.",
      },
      {
        score: 1,
        tag: "Mức 1 · DUPR ~2.5 (B2)",
        desc: "Nắm rõ luật NVZ; duy trì được 3–5 chạm Dink qua lại trước khi bóng nảy cao (pop-up) hoặc tự hỏng.",
      },
      {
        score: 2,
        tag: "Mức 2 · DUPR ~3.0 (B3)",
        desc: "Duy trì chuỗi Dink ≥ 8–10 chạm ổn định; biết điều bóng Dink chéo sân (cross-court dink) và giữ tư thế thấp tại vạch NVZ.",
      },
      {
        score: 3,
        tag: "Mức 3 · DUPR ~3.5 (B4)",
        desc: "Dink 14–20 chạm kiên nhẫn không pop-up; biết Dink ép chân (attackable/topspin dink) và nhận diện bóng cao để tăng tốc (speed-up).",
      },
      {
        score: 4,
        tag: "Mức 4 · DUPR 4.0+ (B5)",
        desc: "Làm chủ hoàn toàn vạch NVZ bằng Dink xoáy biến hướng 2 góc, ép đối phương nâng bóng cao để kết thúc điểm.",
      },
    ],
  },
  PK_VOLLEY_RESET: {
    subtitle:
      "Punch Volley, phản xạ đấu tay đôi trên lưới (Firefight) và Reset bóng từ Mid-court",
    levels: [
      {
        score: 0,
        tag: "Mức 0 · DUPR < 2.0 (B1)",
        desc: "Phản xạ bắt bóng trên không (volley) còn chậm; thường vung vợt quá rộng khiến bóng bay ra ngoài.",
      },
      {
        score: 1,
        tag: "Mức 1 · DUPR ~2.5 (B2)",
        desc: "Đỡ được các pha volley tốc độ trung bình trước mặt; khi bị bắn bóng căng vào người dễ bật bóng bổng.",
      },
      {
        score: 2,
        tag: "Mức 2 · DUPR ~3.0 (B3)",
        desc: "Punch volley gọn trước ngực, giữ mặt vợt chuẩn bị (ready position); bắt đầu biết giảm lực (block/reset) khi đứng ở vùng giữa sân.",
      },
      {
        score: 3,
        tag: "Mức 3 · DUPR ~3.5 (B4)",
        desc: "Phản xạ đấu tay đôi (hand battle) nhanh trên lưới; Reset được bóng tấn công mạnh từ vùng Transition rơi êm lại vào Kitchen.",
      },
      {
        score: 4,
        tag: "Mức 4 · DUPR 4.0+ (B5)",
        desc: "Phản công (counter-attack) chuẩn xác trong các pha tốc độ cao, biết nhả bóng out (let it go) và Reset cực kỳ ổn định.",
      },
    ],
  },
  PK_DOUBLES_STRATEGY: {
    subtitle:
      "Groundstroke 2 bên, di chuyển đồng bộ (Shading) và phối hợp bao bóng giữa (Middle Ball)",
    levels: [
      {
        score: 0,
        tag: "Mức 0 · DUPR < 2.0 (B1)",
        desc: "Một người đứng lưới, một người đứng cuối sân (lệch nhịp); hay nhường nhau hoặc va vợt ở bóng giữa sân.",
      },
      {
        score: 1,
        tag: "Mức 1 · DUPR ~2.5 (B2)",
        desc: "Biết cùng đồng đội dâng lên vạch NVZ; cú đánh chạm đất (groundstroke) thuận tay ổn định hơn trái tay.",
      },
      {
        score: 2,
        tag: "Mức 2 · DUPR ~3.0 (B3)",
        desc: "Groundstroke thuận/trái tay đều đặn; biết phân công người thuận tay (forehand) bao bóng ở trục giữa sân.",
      },
      {
        score: 3,
        tag: "Mức 3 · DUPR ~3.5 (B4)",
        desc: "Di chuyển ngang đồng bộ theo hướng bóng (side-by-side shading), biết lốp bóng chiến thuật (offensive lob) và bọc lót ăn ý.",
      },
      {
        score: 4,
        tag: "Mức 4 · DUPR 4.0+ (B5)",
        desc: "Thành thạo đội hình Stacking, Poaching trên lưới và chuyển đổi trạng thái phòng thủ sang tấn công đồng bộ cả cặp.",
      },
    ],
  },
};

const BAND_METADATA = {
  1: {
    // Badminton
    standardLabel: "Chuẩn CLB Cầu lông Phong trào VN (BM-CLB-V1)",
    bands: [
      {
        code: "B1",
        range: "0 – 19",
        title: "Yếu / Mới chơi",
        short: "Tân binh",
      },
      {
        code: "B2",
        range: "20 – 39",
        title: "Trung bình yếu (TB-)",
        short: "TB-",
      },
      {
        code: "B3",
        range: "40 – 59",
        title: "Trung bình (TB)",
        short: "TB",
      },
      {
        code: "B4",
        range: "60 – 79",
        title: "Trung bình khá (TB+)",
        short: "TB+",
      },
      {
        code: "B5",
        range: "80 – 100",
        title: "Khá / Nâng cao",
        short: "Khá+",
      },
    ],
  },
  2: {
    // Pickleball
    standardLabel:
      "Chuẩn USA Pickleball · IPTPA & Quy đổi DUPR (PK-IPTPA-DUPR-V1)",
    bands: [
      {
        code: "B1",
        range: "0 – 19",
        title: "DUPR ~2.0 · Tân binh",
        short: "DUPR 2.0",
      },
      {
        code: "B2",
        range: "20 – 39",
        title: "DUPR ~2.5 · Sơ cấp",
        short: "DUPR 2.5",
      },
      {
        code: "B3",
        range: "40 – 59",
        title: "DUPR ~3.0 · Trung cấp",
        short: "DUPR 3.0",
      },
      {
        code: "B4",
        range: "60 – 79",
        title: "DUPR ~3.5 · Trung khá",
        short: "DUPR 3.5",
      },
      {
        code: "B5",
        range: "80 – 100",
        title: "DUPR 4.0+ · Nâng cao",
        short: "DUPR 4.0+",
      },
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
      short: "Chưa xếp hạng",
    }
  );
}

function getRatingStatusBadge(status) {
  if (status === "Rated" || status === "Official") {
    return {
      label: "Chính thức · Trọng tài xác thực",
      tone: "official",
      desc: "Đã được Trọng tài chấm trực tiếp trên sân (Swimlane 2).",
    };
  }
  if (status === "Provisional") {
    return {
      label: "Tạm thời · Đủ điều kiện đặt sân",
      tone: "provisional",
      desc: "Đã có điểm Band để kiểm tra trình độ đặt sân (Swimlane 1).",
    };
  }
  return {
    label: "Chưa khảo sát · Cần hoàn thiện",
    tone: "unrated",
    desc: "Làm khảo sát tự đánh giá (UC-06) để mở khóa đặt sân theo Band.",
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

export default function SkillProfileView({ user, onBrowseSport }) {
  const [overview, setOverview] = useState(null);
  const [rubrics, setRubrics] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  // Selected sport tab: 1 = Badminton, 2 = Pickleball
  const [selectedSportId, setSelectedSportId] = useState(1);

  // Active sub-mode: false = Result & History, true = Self-assessment form UC-06
  const [showSurveyForm, setShowSurveyForm] = useState(false);
  const [expandedRubricRef, setExpandedRubricRef] = useState(false);

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

  // Pre-populate survey answers from latest assessment or default to 2 (B3) when switching sport
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

  const previewCalc = useMemo(() => {
    if (!activeRubric?.dimensions?.length) {
      return { rawSum: 0, totalScore: 0, band: "B1" };
    }
    let sum = 0;
    activeRubric.dimensions.forEach((dim) => {
      const dimId = dim.id ?? dim.dimensionId;
      const val = Number(surveyAnswers[dimId] ?? 2);
      sum += val;
    });
    const totalScore = Math.round(sum * 5 * 10) / 10;
    const band = mapScoreToBand(totalScore);
    return { rawSum: sum, totalScore, band };
  }, [activeRubric, surveyAnswers]);

  async function handleSurveySubmit(e) {
    e.preventDefault();
    if (!activeRubric?.dimensions?.length) return;
    setSubmitting(true);
    setSubmitBanner(null);
    try {
      const answers = activeRubric.dimensions.map((dim) => {
        const dimId = dim.id ?? dim.dimensionId;
        return {
          dimensionId: dimId,
          score: Number(surveyAnswers[dimId] ?? 2),
        };
      });

      const res = await api.submitSkillSurvey({
        sportId: selectedSportId,
        answers,
        experienceContext: experienceText,
        frequencyContext: frequencyText,
      });

      const updatedProfile = res?.updatedProfile || res;

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

      const sportNameVi = getSportNameVi(
        selectedSportId,
        updatedProfile?.sportName,
      );
      setSubmitBanner({
        sportId: selectedSportId,
        sportNameVi,
        band: updatedProfile?.currentBand || previewCalc.band,
        score: updatedProfile?.currentScore ?? previewCalc.totalScore,
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
          <h3>Đang tải Hồ sơ Kỹ năng &amp; Lịch sử Trình độ…</h3>
          <p>
            Đồng bộ dữ liệu PlayerSportProfiles, Rubric 5 tiêu chí và lịch sử
            chấm điểm.
          </p>
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

  return (
    <div className="skill-profile-page">
      {/* Page Header */}
      <header className="sp-hero">
        <div className="sp-hero-copy">
          <span className="eyebrow">
            UC-06 · UC-07 · UC-25 · HỒ SƠ KỸ NĂNG &amp; LỊCH SỬ TRÌNH ĐỘ
          </span>
          <h1>
            Hồ sơ Kỹ năng theo Bộ môn<span>.</span>
          </h1>
          <p>
            Hiển thị điểm trình độ hiện tại của 2 môn (Cầu lông &amp;
            Pickleball) từ bảng <code>PlayerSportProfiles</code>, làm khảo sát
            tự đánh giá ban đầu để khởi tạo dữ liệu kiểm tra trình độ đặt sân
            (Đầu vào Swimlane 1) và xem kết quả chấm điểm từ Trọng tài cùng
            lịch sử biến động trình độ (Đầu ra Swimlane 2).
          </p>
        </div>
        <div className="sp-swimlane-pills">
          <div className="sp-swl-pill">
            <ClipboardCheck size={16} />
            <div>
              <strong>Đầu vào SWL 1 (UC-06)</strong>
              <span>Khởi tạo Band cho bước Check Skill Eligibility</span>
            </div>
          </div>
          <div className="sp-swl-pill is-swl2">
            <Award size={16} />
            <div>
              <strong>Đầu ra SWL 2 (UC-25)</strong>
              <span>View Skill Result &amp; Lịch sử tăng/giảm</span>
            </div>
          </div>
        </div>
      </header>

      {/* Submission Success Banner */}
      {submitBanner && (
        <div className="sp-success-banner" role="status">
          <div className="sp-success-main">
            <CheckCircle2 size={24} />
            <div>
              <strong>
                Đã lưu khảo sát tự đánh giá môn {submitBanner.sportNameVi}!
              </strong>
              <p>
                Hồ sơ của bạn đã được cập nhật lên{" "}
                <b>
                  Band {submitBanner.band} ({submitBanner.score}/100 điểm · Tạm
                  thời)
                </b>{" "}
                trong <code>PlayerSportProfiles</code>. Bạn đã đủ điều kiện qua
                bước <em>Check Sport-Specific Skill Eligibility</em> để đặt các
                buổi chơi {submitBanner.sportNameVi} phù hợp trình độ.
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

      {/* 1. Dual Sport Overview Cards (UC-07: View Player Sport Skill Profile) */}
      <section
        className="sp-dual-cards"
        aria-label="Tổng quan trình độ 2 môn thể thao"
      >
        {profiles.map((prof) => {
          const isPk = prof.sportId === 2;
          const isSelected = prof.sportId === selectedSportId;
          const sportVi = getSportNameVi(prof.sportId, prof.sportName);
          const bandInfo = getBandInfo(prof.sportId, prof.currentBand);
          const statusBadge = getRatingStatusBadge(prof.ratingStatus);
          const scoreVal =
            prof.currentScore != null ? Number(prof.currentScore) : 0;
          const hasProfile =
            prof.currentBand != null &&
            prof.ratingStatus !== "Unrated" &&
            prof.ratingStatus !== "PendingSurvey";
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
                      {isPk
                        ? "PICKLEBALL · CHUẨN IPTPA / DUPR"
                        : "CẦU LÔNG · CHUẨN CLB PHONG TRÀO VN"}
                    </span>
                    <h2>{sportVi}</h2>
                  </div>
                </div>
                <span className={`sp-status-badge is-${statusBadge.tone}`}>
                  {statusBadge.label}
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
                      {b.code} ({b.short})
                    </span>
                  ))}
                </div>
              </div>

              <div className="sp-card-meta">
                <span>
                  <Calendar size={14} /> Nguồn gần nhất:{" "}
                  {latestType === "OnCourt"
                    ? `Trọng tài chấm (${prof.latestAssessment?.assessorName || "Referee"})`
                    : latestType === "SelfSurvey"
                      ? "Khảo sát tự đánh giá (UC-06)"
                      : "Chưa có bản đánh giá"}
                </span>
                <span>
                  <History size={14} /> {prof.ratingHistory?.length || 0} kỳ
                  biến động
                </span>
              </div>

              <div
                className="sp-card-actions"
                onClick={(e) => e.stopPropagation()}
              >
                <button
                  type="button"
                  className="button button-primary sp-btn-sm"
                  onClick={() => {
                    setSelectedSportId(prof.sportId);
                    setShowSurveyForm(true);
                  }}
                >
                  <ClipboardCheck size={15} />
                  {hasProfile
                    ? "Cập nhật khảo sát (UC-06)"
                    : "Khảo sát tự đánh giá (UC-06)"}
                </button>
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
            <Award size={16} /> Kết quả 5 tiêu chí &amp; Lịch sử (UC-07 / UC-25)
          </button>
          <button
            type="button"
            className={`sp-mode-btn is-survey-cta ${
              showSurveyForm ? "is-active" : ""
            }`}
            onClick={() => setShowSurveyForm(true)}
          >
            <ClipboardCheck size={16} /> Form Khảo sát Tự đánh giá (UC-06)
          </button>
        </div>
      </div>

      {/* 3A. Self-Assessment Survey Form (UC-06 - Input for Swimlane 1) */}
      {showSurveyForm && activeRubric && (
        <section
          className={`sp-survey-section ${isPickleball ? "is-pickleball" : ""}`}
          aria-label="Khảo sát tự đánh giá trình độ ban đầu"
        >
          <div className="sp-section-header">
            <div>
              <span className="sp-badge-pill">
                UC-06 · INITIAL SKILL SELF-ASSESSMENT · ĐẦU VÀO SWIMLANE 1
              </span>
              <h2>Khảo sát Tự đánh giá Trình độ {activeSportNameVi}</h2>
              <p>
                Bộ tiêu chí:{" "}
                <strong>{activeRubric.name || activeRubric.rubricName}</strong>{" "}
                (Mã:{" "}
                <code>{activeRubric.version || activeRubric.rubricCode}</code>).
                Chọn mức mô tả sát nhất với kỹ năng thực tế của bạn ở 5 tiêu chí
                (thang <code>0 – 4</code> mỗi tiêu chí, trọng số{" "}
                <code>20%</code>). Hệ thống tự động quy đổi{" "}
                <code>Tổng điểm = 5 × Tổng 5 tiêu chí (0 – 100)</code> và gán
                Band <code>B1..B5</code> vào <code>PlayerSportProfiles</code>.
              </p>
            </div>
            <button
              type="button"
              className="button button-secondary sp-btn-sm"
              onClick={() => setShowSurveyForm(false)}
            >
              Đóng form khảo sát
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
                  <option value="6 tháng - 2 năm">Từ 6 tháng – 2 năm</option>
                  <option value="2 - 4 năm (Phong trào thường xuyên)">
                    Từ 2 – 4 năm (Phong trào thường xuyên)
                  </option>
                  <option value="Trên 4 năm / Từng thi đấu giải CLB">
                    Trên 4 năm / Từng thi đấu giải CLB
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
                  <option value="2 - 3 buổi / tuần">2 – 3 buổi / tuần</option>
                  <option value="4+ buổi / tuần">4+ buổi / tuần</option>
                </select>
              </div>
            </div>

            {/* 5 Concrete Rubric Questions */}
            <div className="sp-questions-list">
              {activeRubric.dimensions.map((dim, idx) => {
                const dimId = dim.id ?? dim.dimensionId;
                const dimCode = dim.code ?? dim.dimensionCode;
                const anchorInfo = DIMENSION_ANCHORS[dimCode] || {
                  subtitle: "",
                  levels: [0, 1, 2, 3, 4].map((s) => ({
                    score: s,
                    tag: `Mức ${s}`,
                    desc: `Điểm thành phần ${s}/4 (${s * 5}/20 điểm quy đổi).`,
                  })),
                };
                const selectedVal = Number(surveyAnswers[dimId] ?? 2);

                return (
                  <div key={dimId} className="sp-question-card">
                    <div className="sp-question-head">
                      <div>
                        <span className="sp-q-index">
                          Tiêu chí {idx + 1}/5 · {dimCode} · Trọng số{" "}
                          {dim.weightPercent}%
                        </span>
                        <h3>{dim.name}</h3>
                        <p>{anchorInfo.subtitle}</p>
                      </div>
                      <div className="sp-q-score-pill">
                        <strong>{selectedVal} / 4</strong>
                        <span>({selectedVal * 5} / 20 điểm)</span>
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
                              <span className="sp-level-tag">{lvl.tag}</span>
                              <span className="sp-level-pts">
                                +{lvl.score * 5}đ
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

            {/* Sticky Live Calculation Footer */}
            <div className="sp-survey-footer">
              <div className="sp-preview-summary">
                <div className="sp-preview-badge">
                  <span>Band dự kiến</span>
                  <strong>{previewCalc.band}</strong>
                </div>
                <div className="sp-preview-details">
                  <strong>
                    Tổng điểm quy đổi: {previewCalc.totalScore} / 100 điểm{" "}
                    <span>
                      ({getBandInfo(selectedSportId, previewCalc.band).title})
                    </span>
                  </strong>
                  <p>
                    Tổng điểm thô 5 tiêu chí:{" "}
                    <code>{previewCalc.rawSum}/20</code> → Lưu vào{" "}
                    <code>PlayerSportProfiles</code> với trạng thái{" "}
                    <code>Provisional</code> (Đủ điều kiện kiểm tra trình độ đặt
                    sân ở Swimlane 1).
                  </p>
                </div>
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
                    : `Lưu kết quả khảo sát (${previewCalc.band} · ${previewCalc.totalScore}đ)`}
                </button>
              </div>
            </div>
          </form>
        </section>
      )}

      {/* 3B. Latest 5-Dimension Skill Evaluation Result (UC-07 & UC-25 - View Skill Result) */}
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
                UC-07 &amp; UC-25 · VIEW SKILL RESULT (CUỐI SWIMLANE 2)
              </span>
              <h2>Bảng điểm 5 Tiêu chí Chuyên môn · {activeSportNameVi}</h2>
              <p className="sp-subtext">{sportMeta.standardLabel}</p>
            </div>
            <button
              type="button"
              className="button button-secondary sp-btn-sm"
              onClick={() => setExpandedRubricRef((v) => !v)}
            >
              <Info size={15} />
              {expandedRubricRef ? "Ẩn bảng mốc 0-4" : "Xem mốc chuẩn 0-4"}
              {expandedRubricRef ? (
                <ChevronUp size={15} />
              ) : (
                <ChevronDown size={15} />
              )}
            </button>
          </div>

          {!latestAssessment ? (
            <div className="sp-empty-box">
              <ClipboardCheck size={32} />
              <h3>
                Bạn chưa có bản đánh giá nào cho môn {activeSportNameVi}
              </h3>
              <p>
                Hãy hoàn thành bài khảo sát tự đánh giá ban đầu (UC-06) để khởi
                tạo điểm trình độ vào <code>PlayerSportProfiles</code> và mở
                khóa đặt sân theo Band.
              </p>
              <button
                type="button"
                className="button button-primary"
                onClick={() => setShowSurveyForm(true)}
              >
                Làm khảo sát tự đánh giá ngay <ArrowRight size={16} />
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
                        <ShieldCheck size={15} /> Trọng tài chấm trên sân (SWL 2)
                      </>
                    ) : (
                      <>
                        <ClipboardCheck size={15} /> Khảo sát tự đánh giá (SWL 1)
                      </>
                    )}
                  </span>
                  <h4>
                    Người đánh giá:{" "}
                    <strong>{latestAssessment.assessorName}</strong>{" "}
                    <small>({latestAssessment.assessorRole})</small>
                  </h4>
                  <p>
                    Bộ tiêu chí: <code>{latestAssessment.rubricVersion}</code>
                    {formatSessionInfo(latestAssessment) && (
                      <>
                        {" "}
                        · Buổi chơi:{" "}
                        <strong>{formatSessionInfo(latestAssessment)}</strong>
                      </>
                    )}
                  </p>
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
                            Tiêu chí {idx + 1} · {dimCode} ({dim.weightPercent}
                            %)
                          </span>
                          <h4>{dim.name}</h4>
                        </div>
                        <div className="sp-dim-numbers">
                          <strong>{rawScore.toFixed(1)} / 4.0</strong>
                          <span>({weightedPts.toFixed(0)} / 20 điểm)</span>
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

                      {expandedRubricRef && anchorInfo?.levels && (
                        <div className="sp-dim-anchor-table">
                          {anchorInfo.levels.map((lvl) => (
                            <div
                              key={lvl.score}
                              className={`sp-anchor-row ${
                                Math.round(rawScore) === lvl.score
                                  ? "is-current"
                                  : ""
                              }`}
                            >
                              <span className="sp-anchor-tag">{lvl.tag}</span>
                              <span className="sp-anchor-desc">{lvl.desc}</span>
                            </div>
                          ))}
                        </div>
                      )}
                    </div>
                  );
                })}
              </div>
            </>
          )}

          {/* Band Reference Table */}
          <div className="sp-band-legend">
            <h4>Thang quy đổi Band trình độ ({sportMeta.standardLabel})</h4>
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

        {/* 3C. Rating History Timeline (UC-25 - PlayerRatingHistories / Output Swimlane 2) */}
        <section
          className="sp-panel sp-history-panel"
          aria-label="Lịch sử biến động trình độ"
        >
          <div className="sp-panel-head">
            <div>
              <span className="sp-eyebrow-sm">
                UC-25 · PLAYERRATINGHISTORIES (LỊCH SỬ TĂNG / GIẢM)
              </span>
              <h2>Lịch sử Biến động Trình độ · {activeSportNameVi}</h2>
              <p className="sp-subtext">
                Theo dõi toàn bộ quá trình từ khảo sát tự đánh giá ban đầu đến
                các kỳ Trọng tài chấm trực tiếp trên sân.
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
                            ? "Trọng tài chấm trên sân (SWL 2)"
                            : "Khảo sát tự đánh giá (SWL 1)"}
                        </span>
                        <time>
                          {item.sessionDate
                            ? `Ngày buổi chơi: ${item.sessionDate}`
                            : `Bản ghi #${item.id}`}
                        </time>
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
                              Khởi tạo ban đầu →
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
                            Người ghi nhận: <strong>{item.changedByName}</strong>{" "}
                            <small>({item.changedByRole})</small>
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
