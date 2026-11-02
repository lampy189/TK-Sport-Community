import { useState, useEffect } from "react";
import {
  ArrowLeft,
  Users,
  ShieldCheck,
  Sparkles,
  CheckCircle2,
  Clock,
} from "lucide-react";
import StatePanel from "./ui/StatePanel";
import "../styles/sessions.css";
import { api } from "../api";

function CountdownTimer({ expiresAt }) {
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

  return (
    <span className="countdown">
      <Clock size={15} aria-hidden="true" />
      {timeFormatted}
    </span>
  );
}

export function SessionDetailView({
  sessionId,
  user,
  onBack,
  onOpenLogin,
  onPayment,
  onOpenSkills,
}) {
  const [details, setDetails] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  // Registration state & feedback
  const [registering, setRegistering] = useState(false);
  const [registrationFeedback, setRegistrationFeedback] = useState(null);

  const displayName = user?.fullName || "Khách";

  useEffect(() => {
    loadSessionDetails();
  }, [sessionId, user]);

  async function loadSessionDetails() {
    setLoading(true);
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
        message:
          "Bạn đang xem với tư cách Khách. Vui lòng đăng nhập tài khoản Player để hệ thống kiểm tra điều kiện trình độ và tạo đơn giữ chỗ tạm thời.",
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
        data: result,
      });
      loadSessionDetails();
    } catch (err) {
      // Branch A: Skill Ineligible or Branch B: Session Full or Other
      if (err.code === "SKILL_INELIGIBLE") {
        setRegistrationFeedback({
          type: "SKILL_INELIGIBLE",
          reason: err.reason,
          message: err.message,
        });
      } else if (err.code === "SESSION_FULL") {
        setRegistrationFeedback({
          type: "SESSION_FULL",
          message: err.message || "Buổi chơi đã đủ người.",
        });
        if (details) {
          setDetails({
            ...details,
            summary: {
              ...details.summary,
              availableSlots: 0,
              isFull: true,
            },
          });
        }
      } else if (err.code === "ALREADY_REGISTERED") {
        setRegistrationFeedback({
          type: "ALREADY_REGISTERED",
          message:
            err.message || "Bạn đã có vé hoặc đang giữ chỗ cho buổi chơi này.",
        });
      } else {
        setRegistrationFeedback({
          type: "ERROR",
          message: err.message || "Không thể đăng ký buổi chơi lúc này.",
        });
      }
    } finally {
      setRegistering(false);
    }
  }

  function formatPrice(val) {
    return new Intl.NumberFormat("vi-VN", {
      style: "currency",
      currency: "VND",
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
      if (s.timeLabel?.includes("18:00") && s.band === "B2")
        return "Cầu lông sau giờ làm";
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
    return (
      date.toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit" }) +
      " " +
      date.toLocaleDateString("vi-VN", {
        day: "2-digit",
        month: "2-digit",
        year: "numeric",
      })
    );
  }

  if (loading)
    return (
      <StatePanel
        kind="loading"
        title="Đang tải buổi chơi"
        description="Thông tin sân và chỗ trống sẽ sẵn sàng trong giây lát."
      />
    );
  if (error || !details)
    return (
      <StatePanel
        kind="error"
        title="Chưa thể mở buổi chơi"
        description={error || "Buổi chơi không tồn tại."}
        action={
          <button className="button button-secondary" onClick={onBack}>
            <ArrowLeft size={16} /> Quay lại danh sách
          </button>
        }
      />
    );
  const { summary, publicParticipants = [] } = details;
  const availableSlots =
    summary.availableSlots ?? summary.capacity - summary.occupiedPlaces;
  const isFull = availableSlots <= 0;
  const isPastStart = (() => {
    const dateStr = summary.sessionDate;
    const timeStr = (summary.startTime || summary.timeLabel?.split("-")[0] || "")
      .trim()
      .slice(0, 5);
    if (!dateStr || !timeStr) return false;
    const [y, m, d] = dateStr.split("-").map(Number);
    const [hh, mm] = timeStr.split(":").map(Number);
    if (!y || !m || !d || Number.isNaN(hh) || Number.isNaN(mm)) return false;
    return new Date(y, m - 1, d, hh, mm, 0, 0).getTime() <= Date.now();
  })();
  const isSessionLocked = isFull || isPastStart;
  const isEligible = summary.isEligibleSuggestion;
  return (
    <div className="session-detail">
      <button className="text-button detail-back" onClick={onBack}>
        <ArrowLeft size={16} /> Quay lại khám phá
      </button>
      <header className="page-heading">
        <span className="eyebrow">
          BUỔI CHƠI ·{" "}
          {summary.sportName === "Badminton" ? "CẦU LÔNG" : "PICKLEBALL"}
        </span>
        <h1>{getSessionTitle(summary)}</h1>
        <p>
          {summary.courtName} · {summary.venueName}{" "}
          <span className="detail-user">/ {displayName}</span>
        </p>
      </header>
      <div className="session-detail-grid">
        <section className="detail-card">
          <div className="detail-section-title">
            <h2>Thông tin buổi chơi</h2>
            <Users size={20} />
          </div>
          <div
            className={`eligibility-note ${isEligible === false ? "is-warning" : ""}`}
          >
            {isEligible === true ? (
              <>
                <Sparkles size={16} /> Bạn phù hợp với buổi chơi này
              </>
            ) : isEligible === false ? (
              <>
                Chưa phù hợp trình độ:{" "}
                {summary.ineligibilityReason || "Lệch band kỹ năng"}
              </>
            ) : (
              <>
                <ShieldCheck size={16} /> Đăng nhập để xem đối chiếu trình độ
              </>
            )}
          </div>
          <div className="detail-facts">
            <div className="detail-fact">
              <span>Môn thể thao</span>
              <strong>
                {summary.sportName === "Badminton" ? "Cầu lông" : "Pickleball"}
              </strong>
            </div>
            <div className="detail-fact">
              <span>Địa điểm</span>
              <strong>
                {summary.courtName} · {summary.venueName}
              </strong>
            </div>
            <div className="detail-fact">
              <span>Ngày ra sân</span>
              <strong>{formatSessionDate(summary.sessionDate)}</strong>
            </div>
            <div className="detail-fact">
              <span>Khung giờ</span>
              <strong>
                {summary.timeLabel ||
                  `${summary.startTime} - ${summary.endTime}`}
              </strong>
            </div>
            <div className="detail-fact">
              <span>Trình độ yêu cầu</span>
              <strong>
                {summary.band ? `Band ${summary.band}` : "Tự do / Mọi trình độ"}
              </strong>
            </div>
            <div className="detail-fact">
              <span>Đã xác nhận</span>
              <strong>
                {summary.occupiedPlaces} / {summary.capacity} người
              </strong>
            </div>
          </div>
          <h3 className="participants-heading">Cùng ra sân</h3>
          {publicParticipants.length === 0 ? (
            <p className="participants-empty">
              Chưa có người chơi nào tham gia buổi này. Một kết nối mới đang chờ
              bạn.
            </p>
          ) : (
            <div className="participants-list">
              {publicParticipants.map((p, idx) => (
                <span key={p.ticketId || idx} className="participant">
                  <span className="participant-avatar" aria-hidden="true">
                    {p.fullName?.slice(0, 1)}
                  </span>
                  {p.fullName}
                  {p.currentBand && <small>{p.currentBand}</small>}
                </span>
              ))}
            </div>
          )}
          <div className="detail-reminder">
            <ShieldCheck size={20} />
            <p>
              Có mặt trước giờ chơi để đối chiếu vé. Mang theo vợt và trang phục
              thể thao để sẵn sàng ra sân.
            </p>
          </div>
        </section>
        <aside className="detail-card detail-booking">
          <span className="eyebrow">HẸN GẶP BẠN TRÊN SÂN</span>
          <h2>Đăng ký tham gia</h2>
          <div className="booking-price">
            {formatPrice(summary.priceVnd)}
            <span>/ người</span>
          </div>
          <span
            className={
              isSessionLocked ? "session-status is-full" : "session-status"
            }
          >
            {isPastStart
              ? "Đã qua giờ bắt đầu"
              : isFull
                ? "Đã hết chỗ"
                : `Còn ${availableSlots} chỗ`}
          </span>
          <p className="booking-note">
            {isPastStart
              ? "Buổi chơi đã qua thời điểm bắt đầu và đã khóa đăng ký."
              : isFull
                ? "Buổi chơi đã đủ số lượng người tham gia và đã khóa đăng ký."
                : "Hệ thống kiểm tra trình độ và chỗ trống trước khi giữ chỗ."}
          </p>
          <button
            className="button button-primary booking-action"
            disabled={
              registering ||
              isSessionLocked ||
              registrationFeedback?.type === "SUCCESS_PENDING"
            }
            onClick={handleRegister}
          >
            {registering
              ? "Đang xử lý…"
              : isPastStart
                ? "Đã khóa (Qua giờ bắt đầu)"
                : isFull
                  ? "Đã khóa (Đủ chỗ)"
                  : "Đăng ký buổi chơi"}
          </button>
          {registrationFeedback?.type === "SUCCESS_PENDING" && (
            <button
              className="button button-accent booking-action"
              onClick={() => onPayment(registrationFeedback.data.orderId)}
            >
              Tiếp tục thanh toán qua payOS
            </button>
          )}
          <button
            className="button button-secondary booking-action"
            onClick={() =>
              alert(
                "Chức năng nộp đơn Xin ngoại lệ trình độ sẽ gửi tới Trọng tài duyệt.",
              )
            }
          >
            Xin ngoại lệ trình độ
          </button>
          <p className="booking-footnote">
            Ngoại lệ chỉ áp dụng cho buổi chơi được duyệt.
          </p>
          {registrationFeedback && (
            <div
              className="registration-feedback"
              role="status"
              aria-live="polite"
            >
              {registrationFeedback.type === "NEED_LOGIN" && (
                <div className="feedback-box">
                  <h3>Đăng nhập để tiếp tục</h3>
                  <p>{registrationFeedback.message}</p>
                  <button
                    className="button button-primary booking-action"
                    onClick={onOpenLogin}
                  >
                    Đăng nhập tài khoản Player
                  </button>
                </div>
              )}
              {registrationFeedback.type === "SKILL_INELIGIBLE" && (
                <div className="feedback-box feedback-error">
                  <h3>Chưa đủ điều kiện trình độ</h3>
                  <p>
                    {registrationFeedback.reason === "chưa làm survey"
                      ? "Bạn chưa làm bài khảo sát trình độ cho môn thể thao này. Vui lòng làm khảo sát trước khi đăng ký."
                      : registrationFeedback.reason === "chưa có hồ sơ môn này"
                        ? `Bạn chưa có hồ sơ trình độ cho môn ${summary.sportName === "Badminton" ? "Cầu lông" : "Pickleball"}.`
                        : registrationFeedback.reason === "lệch band kỹ năng"
                          ? `Trình độ của bạn chưa khớp với yêu cầu Band ${summary.band} của buổi chơi.`
                          : registrationFeedback.message}
                  </p>
                  {onOpenSkills && (
                    <button
                      type="button"
                      className="button button-primary booking-action"
                      onClick={onOpenSkills}
                    >
                      Mở Hồ sơ kỹ năng &amp; Khảo sát (UC-06)
                    </button>
                  )}
                  <p>
                    Bạn cũng có thể chọn “Xin ngoại lệ trình độ” để tìm hiểu
                    bước tiếp theo.
                  </p>
                </div>
              )}
              {registrationFeedback.type === "SESSION_FULL" && (
                <div className="feedback-box feedback-warning">
                  <h3>Buổi chơi đã đầy chỗ</h3>
                  <p>
                    {registrationFeedback.message ||
                      "Buổi chơi đã đủ số lượng người đăng ký."}
                  </p>
                  <p>Quay lại khám phá để chọn buổi chơi khác còn chỗ.</p>
                </div>
              )}
              {registrationFeedback.type === "ALREADY_REGISTERED" && (
                <div className="feedback-box">
                  <h3>Đã có thông tin đăng ký</h3>
                  <p>{registrationFeedback.message}</p>
                </div>
              )}
              {registrationFeedback.type === "ERROR" && (
                <div className="feedback-box feedback-error">
                  <h3>Chưa thể đăng ký</h3>
                  <p>{registrationFeedback.message}</p>
                </div>
              )}
              {registrationFeedback.type === "SUCCESS_PENDING" && (
                <div className="feedback-box feedback-success">
                  <CheckCircle2 size={24} />
                  <h3>Đã giữ chỗ cho bạn!</h3>
                  <p>Hoàn tất thanh toán trong thời hạn để xác nhận vé.</p>
                  <div className="reservation-timer">
                    <span>Thời gian còn lại</span>
                    <CountdownTimer
                      expiresAt={registrationFeedback.data.expiresAt}
                    />
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
                </div>
              )}
            </div>
          )}
        </aside>
      </div>
    </div>
  );
}
