import { useState, useEffect } from "react";
import {
  CalendarDays,
  Clock,
  MapPin,
  Users,
  Sparkles,
  ArrowRight,
  SlidersHorizontal,
  Ticket,
} from "lucide-react";
import StatePanel from "./ui/StatePanel";
import "../styles/sessions.css";
import { api } from "../api";

export function BrowseSessionsView({
  user,
  onSelectSession,
  onNavigate,
  initialSport = "ALL",
}) {
  const [sessions, setSessions] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  // Filters state
  const [selectedSport, setSelectedSport] = useState(initialSport);
  const [selectedDate, setSelectedDate] = useState("");
  const [selectedSlot, setSelectedSlot] = useState("ALL");
  const [filterEligibleOnly, setFilterEligibleOnly] = useState(false);

  useEffect(() => {
    loadSessions();
  }, [user, selectedSport, selectedDate, selectedSlot]);

  async function loadSessions() {
    setLoading(true);
    setError(null);
    try {
      const params = {};
      if (selectedSport !== "ALL") {
        params.sport = selectedSport;
      }
      if (selectedDate) {
        params.date = selectedDate;
      }
      if (selectedSlot !== "ALL") {
        params.timeSlotId = selectedSlot;
      }
      const data = await api.getSessions(params);
      setSessions(Array.isArray(data) ? data : []);
    } catch (err) {
      console.error("loadSessions error:", err);
      setError("Chưa thể tải buổi chơi. Vui lòng kiểm tra kết nối và thử lại.");
    } finally {
      setLoading(false);
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

  const displayedSessions = filterEligibleOnly
    ? sessions.filter((s) => s.isEligibleSuggestion === true)
    : sessions;

  return (
    <div className="sessions-page">
      <header className="page-heading">
        <span className="eyebrow">KHÁM PHÁ · KẾT NỐI · RA SÂN</span>
        <h1>
          Tìm buổi chơi của bạn<span>.</span>
        </h1>
        <p>Đúng bộ môn. Đúng trình độ. Cùng nhau tạo nên một buổi chơi hay.</p>
      </header>
      <section className="session-banner">
        <div className="banner-icon">
          <Users size={28} strokeWidth={1.5} />
        </div>
        <div>
          <h2>Một cuộc hẹn trên sân, thêm một kết nối.</h2>
          <p>
            Khám phá các buổi giao lưu phù hợp với bạn và cộng đồng TK Sport.
          </p>
        </div>
        <button
          className="button button-secondary banner-action"
          onClick={() =>
            alert(
              "Chức năng Hoàn thiện hồ sơ sẽ mở giao diện cập nhật hồ sơ kỹ năng.",
            )
          }
        >
          Hoàn thiện hồ sơ <ArrowRight size={16} />
        </button>
      </section>
      <section className="session-filters" aria-label="Bộ lọc buổi chơi">
        <div className="filter-heading">
          <SlidersHorizontal size={17} />
          <strong>Tìm buổi chơi phù hợp</strong>
        </div>
        <div className="filter-fields">
          <div className="filter-field">
            <label htmlFor="session-sport">Môn thể thao</label>
            <select
              id="session-sport"
              value={selectedSport}
              onChange={(e) => setSelectedSport(e.target.value)}
            >
              <option value="ALL">Tất cả bộ môn</option>
              <option value="Badminton">Cầu lông</option>
              <option value="Pickleball">Pickleball</option>
            </select>
          </div>
          <div className="filter-field">
            <label htmlFor="session-date">Ngày ra sân</label>
            <input
              id="session-date"
              type="date"
              value={selectedDate}
              onChange={(e) => setSelectedDate(e.target.value)}
            />
          </div>
          <div className="filter-field">
            <label htmlFor="session-slot">Khung giờ</label>
            <select
              id="session-slot"
              value={selectedSlot}
              onChange={(e) => setSelectedSlot(e.target.value)}
            >
              <option value="ALL">Tất cả khung giờ</option>
              <option value="1">08:00 - 10:00 (Sáng)</option>
              <option value="2">10:00 - 12:00 (Trưa)</option>
              <option value="3">14:00 - 16:00 (Chiều)</option>
              <option value="4">16:00 - 18:00 (Chiều)</option>
              <option value="5">18:00 - 20:00 (Tối)</option>
              <option value="6">20:00 - 22:00 (Tối)</option>
            </select>
          </div>
          <button className="button button-primary" onClick={loadSessions}>
            Tìm buổi chơi <ArrowRight size={16} />
          </button>
        </div>
        <div className="filter-footer">
          <label className="eligibility-toggle">
            <input
              type="checkbox"
              checked={filterEligibleOnly}
              onChange={() => setFilterEligibleOnly(!filterEligibleOnly)}
            />
            <span>Chỉ buổi chơi phù hợp với tôi</span>
          </label>
          <button
            className="text-button"
            onClick={() => {
              setSelectedSport("ALL");
              setSelectedDate("");
              setSelectedSlot("ALL");
              setFilterEligibleOnly(false);
            }}
          >
            Đặt lại bộ lọc
          </button>
        </div>
      </section>
      <div className="results-heading">
        <h2>Lịch social</h2>
        <span role="status">
          {loading
            ? "Đang tìm buổi chơi…"
            : error
              ? "Chưa tải được dữ liệu"
              : `${displayedSessions.length} buổi chơi`}
        </span>
      </div>
      {loading ? (
        <div
          className="session-grid"
          role="status"
          aria-label="Đang tải buổi chơi"
          aria-busy="true"
        >
          {[1, 2, 3].map((i) => (
            <div key={i} className="session-skeleton">
              <span />
              <span />
              <span />
              <span />
            </div>
          ))}
        </div>
      ) : error ? (
        <StatePanel
          kind="error"
          title="Kết nối đang gián đoạn"
          description={error}
          action={
            <button className="button button-primary" onClick={loadSessions}>
              Thử lại
            </button>
          }
        />
      ) : displayedSessions.length === 0 ? (
        <StatePanel
          title="Chưa tìm thấy buổi chơi"
          description="Thử chọn ngày khác hoặc mở rộng bộ lọc để tìm buổi chơi phù hợp."
          action={
            <button
              className="button button-primary"
              onClick={() => {
                setSelectedSport("ALL");
                setSelectedDate("");
                setSelectedSlot("ALL");
                setFilterEligibleOnly(false);
              }}
            >
              Xóa bộ lọc
            </button>
          }
        />
      ) : (
        <div className="session-grid">
          {displayedSessions.map((s) => {
            const isEligible = s.isEligibleSuggestion;
            const availableSlots =
              s.availableSlots ?? s.capacity - s.occupiedPlaces;
            const isFull = availableSlots <= 0;
            const bandLabel =
              s.band === "B1"
                ? "Mới chơi / B1"
                : s.band === "B2"
                  ? "Trung bình / B2"
                  : s.band || "Tự do";
            return (
              <article className="session-card" key={s.id}>
                <div className="session-card-top">
                  <span className="sport-label">
                    <ActivityMark />
                    {s.sportName === "Badminton" ? "Cầu lông" : "Pickleball"}
                  </span>
                  <span
                    className={
                      isFull ? "session-status is-full" : "session-status"
                    }
                  >
                    {isFull ? "Đã hết chỗ" : `Còn ${availableSlots} chỗ`}
                  </span>
                </div>
                <h3>{getSessionTitle(s)}</h3>
                {isEligible === true && (
                  <span className="session-match">
                    <Sparkles size={13} /> Phù hợp trình độ của bạn
                  </span>
                )}
                <ul className="session-meta">
                  <li>
                    <MapPin size={16} />
                    <span>
                      {s.courtName} · {s.venueName}
                    </span>
                  </li>
                  <li>
                    <CalendarDays size={16} />
                    <span>{formatSessionDate(s.sessionDate)}</span>
                  </li>
                  <li>
                    <Clock size={16} />
                    <span>
                      {s.timeLabel || `${s.startTime} - ${s.endTime}`}
                    </span>
                  </li>
                  <li>
                    <Users size={16} />
                    <span>
                      Trình độ <strong>{bandLabel}</strong>
                    </span>
                  </li>
                </ul>
                <div className="session-card-bottom">
                  <div className="session-price">
                    {formatPrice(s.priceVnd)}
                    <span>/ người</span>
                  </div>
                  <button
                    className="button button-accent"
                    onClick={() => onSelectSession(s.id)}
                  >
                    Xem buổi chơi <ArrowRight size={16} />
                  </button>
                </div>
              </article>
            );
          })}
        </div>
      )}
      <aside className="tickets-callout">
        <span className="callout-icon">
          <Ticket size={24} />
        </span>
        <div>
          <strong>Lịch ra sân, luôn trong tầm tay.</strong>
          <p>Theo dõi vé và thông tin đăng ký của bạn tại một nơi.</p>
        </div>
        <button
          className="button button-secondary"
          onClick={() => onNavigate && onNavigate("tickets")}
        >
          Xem vé của tôi <ArrowRight size={16} />
        </button>
      </aside>
    </div>
  );
}
function ActivityMark() {
  return <span className="sport-dot" aria-hidden="true" />;
}
