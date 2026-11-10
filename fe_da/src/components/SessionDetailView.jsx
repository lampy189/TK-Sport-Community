import { useState, useEffect } from "react";
import { ArrowLeft, Users, UserPlus, ShieldCheck, Sparkles, CheckCircle2, Clock } from "lucide-react";
import StatePanel from "./ui/StatePanel";
import { PublicPlayerProfileModal } from "./PublicPlayerProfileModal";
import { InviteFriendsToSessionModal } from "./SessionInviteCard";
import "../styles/sessions.css";
import { api } from "../api";
import { LateEntryPurchase } from '../pages/late-entry/LateEntrySales';
import { ExceptionRequest } from "../pages/skill/SkillReviews";
function parseTimeToMinutes(rawTime) {
  if (!rawTime) return null;
  const clean = String(rawTime).trim().slice(0, 5);
  const parts = clean.split(":").map(n => parseInt(n, 10));
  if (parts.length < 2 || parts.some(n => Number.isNaN(n))) return null;
  return parts[0] * 60 + parts[1];
}
function getDurationMinutes(item) {
  if (!item) return 120;
  if (typeof item.durationMinutes === "number" && item.durationMinutes > 0) {
    return item.durationMinutes;
  }
  const startStr = item.startTime || (item.timeLabel || "").split("-")[0]?.trim();
  const endStr = item.endTime || (item.timeLabel || "").split("-")[1]?.trim();
  const sMin = parseTimeToMinutes(startStr);
  const eMin = parseTimeToMinutes(endStr);
  if (sMin != null && eMin != null && eMin > sMin) {
    return eMin - sMin;
  }
  return 120;
}
function CountdownTimer({
  expiresAt
}) {
  const [secondsLeft, setSecondsLeft] = useState(() => calcSeconds(expiresAt));
  function calcSeconds(target) {
    if (!target) return 0;
    const diff = Math.floor((new Date(target).getTime() - Date.now()) / 1000);
    return diff > 0 ? diff : 0;
  }
  useEffect(() => {
    const timer = setInterval(() => {
      const remaining = calcSeconds(expiresAt);
      setSecondsLeft(remaining);
      if (remaining <= 0) clearInterval(timer);
    }, 1000);
    return () => clearInterval(timer);
  }, [expiresAt]);
  const mins = Math.floor(secondsLeft / 60);
  const secs = secondsLeft % 60;
  const timeFormatted = `${mins.toString().padStart(2, "0")}:${secs.toString().padStart(2, "0")}`;
  return <span className="countdown">
      <Clock size={15} aria-hidden="true" />
      {timeFormatted}
    </span>;
}
export function SessionDetailView({
  sessionId,
  user,
  onBack,
  onOpenLogin,
  onPayment,
  onOpenSkills,
  onOpenCommunity
}) {
  const [details, setDetails] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  // Registration state & feedback
  const [registering, setRegistering] = useState(false);
  const [registrationFeedback, setRegistrationFeedback] = useState(null);

  // SWL 1 (UC-05 & UC-25c): Selected participant in "Cùng ra sân" to view Public Profile, Friend & Direct Chat
  const [selectedParticipantPlayerId, setSelectedParticipantPlayerId] = useState(null);
  const [showInviteFriendModal, setShowInviteFriendModal] = useState(false);
  const displayName = user?.fullName || "Khách";
  useEffect(() => {
    window.scrollTo({
      top: 0,
      behavior: "instant"
    });
    loadSessionDetails();
  }, [sessionId, user]);
  useEffect(() => {
    const onOpenSameSession = e => {
      if (Number(e.detail?.sessionId) === Number(sessionId)) {
        loadSessionDetails();
        window.scrollTo({
          top: 0,
          behavior: "smooth"
        });
      }
    };
    window.addEventListener("tksc:open-session", onOpenSameSession);
    return () => window.removeEventListener("tksc:open-session", onOpenSameSession);
  }, [sessionId]);
  async function loadSessionDetails(silent = false) {
    if (!silent) setLoading(true);
    setError(null);
    try {
      const data = await api.getSessionById(sessionId);
      setDetails(data);
    } catch (err) {
      setError(err.message || "Không tìm thấy thông tin buổi chơi.");
    } finally {
      setLoading(false);
    }
  }
  async function handleRegister() {
    if (!user) {
      setRegistrationFeedback({
        type: "NEED_LOGIN",
        message: "Bạn đang xem với tư cách Khách. Vui lòng đăng nhập tài khoản Player để hệ thống kiểm tra điều kiện trình độ và tạo đơn giữ chỗ tạm thời."
      });
      return;
    }
    setRegistering(true);
    setRegistrationFeedback(null);
    try {
      const result = await api.registerSession(sessionId);
      // Branch C: Record Pending Registration Success
      setRegistrationFeedback({
        type: "SUCCESS_PENDING",
        data: result
      });
      loadSessionDetails();
    } catch (err) {
      // Branch A: Skill Ineligible or Branch B: Session Full or Other
      if (err.code === "SKILL_INELIGIBLE") {
        setRegistrationFeedback({
          type: "SKILL_INELIGIBLE",
          reason: err.reason,
          message: err.message
        });
      } else if (err.code === "SESSION_FULL") {
        setRegistrationFeedback({
          type: "SESSION_FULL",
          message: err.message || "Buổi chơi đã đủ người."
        });
        if (details) {
          setDetails({
            ...details,
            summary: {
              ...details.summary,
              availableSlots: 0,
              isFull: true
            }
          });
        }
      } else if (err.code === "ALREADY_REGISTERED") {
        setRegistrationFeedback({
          type: "ALREADY_REGISTERED",
          message: err.message || "Bạn đã có vé hoặc đang giữ chỗ cho buổi chơi này."
        });
      } else {
        setRegistrationFeedback({
          type: "ERROR",
          message: err.message || "Không thể đăng ký buổi chơi lúc này."
        });
      }
    } finally {
      setRegistering(false);
    }
  }
  function formatPrice(val) {
    return new Intl.NumberFormat("vi-VN", {
      style: "currency",
      currency: "VND"
    }).format(val);
  }
  function formatSessionDate(dateStr) {
    if (!dateStr) return "";
    try {
      const parts = dateStr.split("-");
      if (parts.length === 3) {
        const year = parseInt(parts[0], 10);
        const month = parseInt(parts[1], 10);
        const day = parseInt(parts[2], 10);
        return `${day.toString().padStart(2, "0")}/${month.toString().padStart(2, "0")}/${year}`;
      }
    } catch {
      /* Keep the original date when formatting is unavailable. */
    }
    return dateStr;
  }
  function getSessionTitle(s) {
    if (!s) return "Buổi chơi";
    if (s.sportName === "Badminton") {
      if (s.timeLabel?.includes("18:00") && s.band === "B2") return "Cầu lông sau giờ làm";
      if (s.timeLabel?.includes("20:00")) return "Cầu lông phong trào tối";
      if (s.band === "B3") return "Cầu lông nâng cao";
      return "Giao lưu cầu lông";
    } else {
      if (s.timeLabel?.includes("08:00")) return "Pickleball cuối tuần";
      if (!s.band) return "Pickleball giao lưu mở";
      return "Giao lưu Pickleball";
    }
  }
  function formatDateTime(isoString) {
    if (!isoString) return "";
    const date = new Date(isoString);
    return date.toLocaleTimeString("vi-VN", {
      hour: "2-digit",
      minute: "2-digit"
    }) + " " + date.toLocaleDateString("vi-VN", {
      day: "2-digit",
      month: "2-digit",
      year: "numeric"
    });
  }
  if (loading) return <StatePanel kind="loading" title="Đang tải buổi chơi" description="Thông tin sân và chỗ trống sẽ sẵn sàng trong giây lát." />;
  if (error || !details) return <StatePanel kind="error" title="Chưa thể mở buổi chơi" description={error || "Buổi chơi không tồn tại."} action={<button className="button button-secondary" onClick={onBack}>
            <ArrowLeft size={16} /> Quay lại danh sách
          </button>} />;
  const {
    summary,
    publicParticipants = []
  } = details;
  const availableSlots = summary.availableSlots ?? summary.capacity - summary.occupiedPlaces;
  const isFull = availableSlots <= 0;
  const isPastStart = (() => {
    const dateStr = summary.sessionDate;
    const timeStr = (summary.startTime || summary.timeLabel?.split("-")[0] || "").trim().slice(0, 5);
    if (!dateStr || !timeStr) return false;
    const [y, m, d] = dateStr.split("-").map(Number);
    const [hh, mm] = timeStr.split(":").map(Number);
    if (!y || !m || !d || Number.isNaN(hh) || Number.isNaN(mm)) return false;
    return new Date(y, m - 1, d, hh, mm, 0, 0).getTime() <= Date.now();
  })();
  const isSessionLocked = isFull || isPastStart;
  const durationMins = getDurationMinutes(summary);
  const isEligible = summary.isEligibleSuggestion;
  return <div className="session-detail">
      <button className="text-button detail-back" onClick={onBack}>
        <ArrowLeft size={16} /> Quay lại khám phá lịch sân
      </button>
      <header className="page-heading">
        <span className="eyebrow">
          {"BUỔI CHƠI SOCIAL · "}
          {summary.sportName === "Badminton" ? "CẦU LÔNG" : "PICKLEBALL"}
        </span>
        <h1>{getSessionTitle(summary)}</h1>
        <p>
          {summary.courtName}{" "}
          <span className="detail-user">/ {displayName}</span>
        </p>
      </header>
      <LateEntryPurchase sessionId={sessionId} user={user} onLogin={onOpenLogin} onPayment={onPayment} />
      <div className="session-detail-grid">
        <section className="detail-card">
          <div className="detail-section-title">
            <h2>
              {"Thông tin buổi chơi"}
            </h2>
            {<Users size={22} />}
          </div>

          {<div className={`eligibility-note ${isEligible === false ? "is-warning" : ""}`}>
              {isEligible === true ? <>
                  <Sparkles size={17} /> Bạn phù hợp với buổi chơi này
                </> : isEligible === false ? <>
                  Chưa phù hợp trình độ:{" "}
                  {summary.ineligibilityReason === "chưa làm survey" || summary.ineligibilityReason === "chưa có hồ sơ môn này" ? "Chưa hoàn tất khảo sát trình độ môn này" : summary.ineligibilityReason === "lệch band kỹ năng" ? `Chưa khớp yêu cầu Band ${summary.band} (Có thể xin ngoại lệ trình độ)` : summary.ineligibilityReason || "Lệch band kỹ năng"}
                </> : <>
                  <ShieldCheck size={17} /> Đăng nhập để xem đối chiếu trình độ
                </>}
            </div>}

          <div className="detail-facts">
            <div className="detail-fact">
              <span>Sân &amp; Bộ môn</span>
              <strong>
                {summary.courtName} ·{" "}
                {summary.sportName === "Badminton" ? "Cầu lông" : "Pickleball"}
              </strong>
            </div>
            <div className="detail-fact">
              <span>Ngày ra sân</span>
              <strong>{formatSessionDate(summary.sessionDate)}</strong>
            </div>
            <div className="detail-fact">
              <span>Khung giờ &amp; Thời lượng</span>
              <strong>
                {summary.timeLabel || `${summary.startTime} - ${summary.endTime}`}{" "}
                ({`${durationMins} phút`})
              </strong>
            </div>
            <div className="detail-fact">
              <span>Loại vé &amp; Trình độ</span>
              <strong>
                {summary.band ? `Lịch Social · Band ${summary.band}` : "Lịch Social · Tự do"}
              </strong>
            </div>
            <div className="detail-fact">
              <span>Số chỗ đã đặt</span>
              <strong>
                {summary.occupiedPlaces} / {summary.capacity} chỗ (Còn{" "}
                {availableSlots} chỗ)
              </strong>
            </div>
          </div>
          <div style={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: "center",
          gap: "14px",
          flexWrap: "wrap",
          marginTop: "24px",
          marginBottom: "12px"
        }}>
            <h3 className="participants-heading" style={{
            margin: 0
          }}>
              Cùng ra sân{" "}
              <small style={{
              fontSize: "14px",
              fontWeight: 500,
              color: "#64748b",
              marginLeft: "6px"
            }}>
                (Bấm vào người chơi để xem Hồ sơ công khai, kết bạn &amp; nhắn tin)
              </small>
            </h3>
            {user && !isSessionLocked && <button type="button" className="comm-btn comm-btn-outline" onClick={() => setShowInviteFriendModal(true)}>
                <UserPlus size={15} />
                <span>Rủ bạn bè cùng đặt sân</span>
              </button>}
          </div>
          {publicParticipants.length === 0 ? <p className="participants-empty">
              Chưa có người chơi nào tham gia buổi này. Một kết nối mới đang chờ
              bạn.
            </p> : <div className="participants-list">
              {publicParticipants.map((p, idx) => <button key={p.ticketId || idx} type="button" className="participant" style={{
            cursor: "pointer",
            border: "1px solid #cbd5e1",
            background: "#ffffff",
            transition: "all 0.15s ease"
          }} title={`Xem hồ sơ công khai, kết bạn & nhắn tin với ${p.fullName}`} onClick={() => p.playerId && setSelectedParticipantPlayerId(p.playerId)}>
                  <span className="participant-avatar" aria-hidden="true">
                    {p.avatarUrl ? <img src={p.avatarUrl} alt={p.fullName} /> : p.fullName?.slice(0, 1)}
                  </span>
                  <span>{p.fullName}</span>
                  {p.currentBand && <small>{p.currentBand}</small>}
                </button>)}
            </div>}
          <div className="detail-reminder">
            <ShieldCheck size={20} />
            <p>
              Có mặt trước giờ chơi để đối chiếu vé. Mang theo vợt và trang phục
              thể thao để sẵn sàng ra sân.
            </p>
          </div>
        </section>
        {!isPastStart && <aside className="detail-card detail-booking">
          <span className="eyebrow">
            {"HẸN GẶP BẠN TRÊN SÂN"}
          </span>
          <h2>{"Đăng ký tham gia"}</h2>
          <div className="booking-price">
            {formatPrice(summary.priceVnd)}
            <span>{"/ người"}</span>
          </div>
          <span className={isSessionLocked ? "session-status is-full" : "session-status"}>
            {isPastStart ? "Đã qua giờ bắt đầu" : isFull ? "Đã hết chỗ" : `Còn ${availableSlots}/${summary.capacity} chỗ`}
          </span>
          <p className="booking-note">
            {isPastStart ? "Khung giờ này đã qua thời điểm bắt đầu và đã khóa đăng ký." : isFull ? "Khung giờ này đã đủ số lượng người đặt và đã khóa đăng ký." : "Hệ thống kiểm tra trình độ và chỗ trống trước khi giữ chỗ."}
          </p>
          <button className="button button-primary booking-action" disabled={registering || isSessionLocked || registrationFeedback?.type === "SUCCESS_PENDING"} onClick={handleRegister}>
            {registering ? "Đang xử lý…" : isPastStart ? "Đã khóa (Qua giờ bắt đầu)" : isFull ? "Đã khóa (Đủ chỗ)" : "Đăng ký buổi chơi"}
          </button>
          {registrationFeedback?.type === "SUCCESS_PENDING" && <button className="button button-accent booking-action" onClick={() => onPayment(registrationFeedback.data.orderId)}>
              Tiếp tục thanh toán qua payOS
            </button>}
          <ExceptionRequest key={sessionId} sessionId={sessionId} user={user} onLogin={onOpenLogin} onUpdated={() => loadSessionDetails(true)} canRequest={!isSessionLocked && Boolean(summary.band) && (user ? isEligible === false && summary.ineligibilityReason === "lệch band kỹ năng" : true)} defaultOpen={registrationFeedback?.type === "SKILL_INELIGIBLE" && registrationFeedback?.reason === "lệch band kỹ năng"} />
          {!isSessionLocked && Boolean(summary.band) && isEligible === false && (summary.ineligibilityReason === "chưa làm survey" || summary.ineligibilityReason === "chưa có hồ sơ môn này") && !registrationFeedback && onOpenSkills && <button type="button" className="button button-secondary booking-action" onClick={onOpenSkills}>
                Mở hồ sơ kỹ năng &amp; làm khảo sát
              </button>}
          {!isSessionLocked && Boolean(summary.band) && isEligible === false && summary.ineligibilityReason === "lệch band kỹ năng" && <p className="booking-footnote">
                Ngoại lệ chỉ áp dụng cho buổi chơi được duyệt.
              </p>}
          {registrationFeedback && <div className="registration-feedback" role="status" aria-live="polite">
              {registrationFeedback.type === "NEED_LOGIN" && <div className="feedback-box">
                  <h3>Đăng nhập để tiếp tục</h3>
                  <p>{registrationFeedback.message}</p>
                  <button className="button button-primary booking-action" onClick={onOpenLogin}>
                    Đăng nhập tài khoản Player
                  </button>
                </div>}
              {registrationFeedback.type === "SKILL_INELIGIBLE" && <div className="feedback-box feedback-error">
                  <h3>Chưa đủ điều kiện trình độ</h3>
                  <p>
                    {registrationFeedback.reason === "chưa làm survey" ? "Bạn chưa làm bài khảo sát trình độ cho môn thể thao này. Vui lòng làm khảo sát trước khi đăng ký." : registrationFeedback.reason === "chưa có hồ sơ môn này" ? `Bạn chưa có hồ sơ trình độ cho môn ${summary.sportName === "Badminton" ? "Cầu lông" : "Pickleball"}.` : registrationFeedback.reason === "lệch band kỹ năng" ? `Trình độ hiện tại của bạn chưa khớp với yêu cầu Band ${summary.band} của buổi chơi.` : registrationFeedback.message}
                  </p>
                  {onOpenSkills && (registrationFeedback.reason === "chưa làm survey" || registrationFeedback.reason === "chưa có hồ sơ môn này") && <button type="button" className="button button-primary booking-action" onClick={onOpenSkills}>
                        Mở hồ sơ kỹ năng &amp; làm khảo sát
                      </button>}
                  {registrationFeedback.reason === "lệch band kỹ năng" && <p style={{
              marginTop: "6px"
            }}>
                      Nếu vẫn muốn tham gia buổi này, vui lòng điền lý do tại mục{" "}
                      <strong>“Xin ngoại lệ trình độ”</strong> phía trên để Trọng
                      tài hoặc Quản lý xem xét phê duyệt.
                    </p>}
                </div>}
              {registrationFeedback.type === "SESSION_FULL" && <div className="feedback-box feedback-warning">
                  <h3>Buổi chơi đã đầy chỗ</h3>
                  <p>
                    {registrationFeedback.message || "Buổi chơi đã đủ số lượng người đăng ký."}
                  </p>
                  <p>Quay lại khám phá để chọn buổi chơi khác còn chỗ.</p>
                </div>}
              {registrationFeedback.type === "ALREADY_REGISTERED" && <div className="feedback-box">
                  <h3>Đã có thông tin đăng ký</h3>
                  <p>{registrationFeedback.message}</p>
                </div>}
              {registrationFeedback.type === "ERROR" && <div className="feedback-box feedback-error">
                  <h3>Chưa thể đăng ký</h3>
                  <p>{registrationFeedback.message}</p>
                </div>}
              {registrationFeedback.type === "SUCCESS_PENDING" && <div className="feedback-box feedback-success">
                  <CheckCircle2 size={24} />
                  <h3>Đã giữ chỗ cho bạn!</h3>
                  <p>Hoàn tất thanh toán trong thời hạn để xác nhận vé.</p>
                  <div className="reservation-timer">
                    <span>Thời gian còn lại</span>
                    <CountdownTimer expiresAt={registrationFeedback.data.expiresAt} />
                  </div>
                  <dl className="reservation-details">
                    <dt>Mã đơn giữ chỗ</dt>
                    <dd>{registrationFeedback.data.orderCode}</dd>
                    <dt>Mã vé tạm</dt>
                    <dd>{registrationFeedback.data.ticketCode}</dd>
                    <dt>Số tiền giữ chỗ</dt>
                    <dd>{formatPrice(registrationFeedback.data.amountVnd)}</dd>
                    <dt>Hạn giữ chỗ</dt>
                    <dd>
                      {formatDateTime(registrationFeedback.data.expiresAt)}
                    </dd>
                  </dl>
                  <p>Vé có hiệu lực sau khi thanh toán được xác nhận.</p>
                </div>}
            </div>}
        </aside>}
      </div>

      {selectedParticipantPlayerId && <PublicPlayerProfileModal playerId={selectedParticipantPlayerId} onClose={() => setSelectedParticipantPlayerId(null)} onOpenFullChat={onOpenCommunity ? () => {
      setSelectedParticipantPlayerId(null);
      onOpenCommunity();
    } : undefined} />}

      {showInviteFriendModal && <InviteFriendsToSessionModal sessionSummary={{
      ...summary,
      id: summary.id || sessionId
    }} onClose={() => setShowInviteFriendModal(false)} />}
    </div>;
}
