import { useState, useEffect, useMemo } from "react";
import {
  CalendarDays,
  CalendarRange,
  ChevronLeft,
  ChevronRight,
  Clock,
  LayoutGrid,
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

const DEFAULT_TIME_SLOTS = [
  { id: 1, startTime: "08:00:00", endTime: "10:00:00", label: "08:00 - 10:00" },
  { id: 2, startTime: "10:00:00", endTime: "12:00:00", label: "10:00 - 12:00" },
  { id: 3, startTime: "14:00:00", endTime: "16:00:00", label: "14:00 - 16:00" },
  { id: 4, startTime: "16:00:00", endTime: "18:00:00", label: "16:00 - 18:00" },
  { id: 5, startTime: "18:00:00", endTime: "20:00:00", label: "18:00 - 20:00" },
  { id: 6, startTime: "20:00:00", endTime: "22:00:00", label: "20:00 - 22:00" },
];

const WEEKDAY_LABELS = [
  { short: "Mon", vi: "Thứ 2" },
  { short: "Tue", vi: "Thứ 3" },
  { short: "Wed", vi: "Thứ 4" },
  { short: "Thu", vi: "Thứ 5" },
  { short: "Fri", vi: "Thứ 6" },
  { short: "Sat", vi: "Thứ 7" },
  { short: "Sun", vi: "CN" },
];

function parseLocalDate(dateStr) {
  if (!dateStr) return new Date();
  const parts = dateStr.split("-").map((n) => parseInt(n, 10));
  if (parts.length === 3 && parts.every((n) => !Number.isNaN(n))) {
    return new Date(parts[0], parts[1] - 1, parts[2], 12, 0, 0);
  }
  return new Date();
}

function getMondayOfWeek(dateObj) {
  const d = new Date(
    dateObj.getFullYear(),
    dateObj.getMonth(),
    dateObj.getDate(),
    12,
    0,
    0,
  );
  const day = d.getDay(); // 0 = Sun, 1 = Mon, ...
  const diff = day === 0 ? -6 : 1 - day;
  d.setDate(d.getDate() + diff);
  return d;
}

function addDays(dateObj, days) {
  const d = new Date(
    dateObj.getFullYear(),
    dateObj.getMonth(),
    dateObj.getDate(),
    12,
    0,
    0,
  );
  d.setDate(d.getDate() + days);
  return d;
}

function toIsoDateString(dateObj) {
  const y = dateObj.getFullYear();
  const m = String(dateObj.getMonth() + 1).padStart(2, "0");
  const d = String(dateObj.getDate()).padStart(2, "0");
  return `${y}-${m}-${d}`;
}

function formatDayMonth(dateObj) {
  const d = String(dateObj.getDate()).padStart(2, "0");
  const m = String(dateObj.getMonth() + 1).padStart(2, "0");
  return `${d}/${m}`;
}

function getIsoWeekInfo(dateObj) {
  const d = new Date(
    Date.UTC(dateObj.getFullYear(), dateObj.getMonth(), dateObj.getDate()),
  );
  const dayNum = d.getUTCDay() || 7;
  d.setUTCDate(d.getUTCDate() + 4 - dayNum);
  const yearStart = new Date(Date.UTC(d.getUTCFullYear(), 0, 1));
  const weekNo = Math.ceil(((d - yearStart) / 86400000 + 1) / 7);
  return { week: weekNo, year: d.getUTCFullYear() };
}

function getSlotPeriodTag(slot) {
  const raw = slot.startTime || slot.label || "";
  const hour = parseInt(raw.slice(0, 2), 10);
  if (Number.isNaN(hour)) return "";
  if (hour < 11) return "Sáng";
  if (hour < 13) return "Trưa";
  if (hour < 18) return "Chiều";
  return "Tối";
}

function formatSlotTimeRange(slot) {
  if (slot.label) return slot.label;
  const start = (slot.startTime || "").slice(0, 5);
  const end = (slot.endTime || "").slice(0, 5);
  return start && end ? `${start} - ${end}` : "";
}

export function BrowseSessionsView({
  user,
  onSelectSession,
  onNavigate,
  initialSport = "ALL",
}) {
  const [sessions, setSessions] = useState([]);
  const [timeSlots, setTimeSlots] = useState(DEFAULT_TIME_SLOTS);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  // View mode: "list" (normal cards) | "timetable" (weekly slot timetable)
  const [viewMode, setViewMode] = useState("list");
  const [weekMonday, setWeekMonday] = useState(() =>
    getMondayOfWeek(new Date()),
  );

  // Filters state
  const [selectedSport, setSelectedSport] = useState(initialSport);
  const [selectedDate, setSelectedDate] = useState("");
  const [selectedSlot, setSelectedSlot] = useState("ALL");
  const [filterEligibleOnly, setFilterEligibleOnly] = useState(false);

  // Load venue time_slots from backend once
  useEffect(() => {
    let active = true;
    api
      .getTimeSlots()
      .then((data) => {
        if (active && Array.isArray(data) && data.length > 0) {
          setTimeSlots(data);
        }
      })
      .catch(() => {
        /* Fallback to DEFAULT_TIME_SLOTS when offline */
      });
    return () => {
      active = false;
    };
  }, []);

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

  function handleDateFilterChange(val) {
    setSelectedDate(val);
    if (val) {
      setWeekMonday(getMondayOfWeek(parseLocalDate(val)));
    }
  }

  function shiftWeek(offsetWeeks) {
    if (selectedDate) {
      setSelectedDate("");
    }
    setWeekMonday((prev) => addDays(prev, offsetWeeks * 7));
  }

  function jumpToCurrentWeek() {
    if (selectedDate) {
      setSelectedDate("");
    }
    setWeekMonday(getMondayOfWeek(new Date()));
  }

  function jumpToDateWeek(dateStr) {
    if (!dateStr) return;
    if (selectedDate && selectedDate !== dateStr) {
      setSelectedDate("");
    }
    setWeekMonday(getMondayOfWeek(parseLocalDate(dateStr)));
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

  const displayedSessions = useMemo(
    () =>
      filterEligibleOnly
        ? sessions.filter((s) => s.isEligibleSuggestion === true)
        : sessions,
    [sessions, filterEligibleOnly],
  );

  // Build 7 days of the currently selected week (Monday -> Sunday)
  const todayIso = toIsoDateString(new Date());
  const currentWeekMondayIso = toIsoDateString(getMondayOfWeek(new Date()));
  const isViewingCurrentWeek =
    toIsoDateString(weekMonday) === currentWeekMondayIso;
  const weekInfo = getIsoWeekInfo(weekMonday);

  const weekDays = useMemo(() => {
    return WEEKDAY_LABELS.map((item, idx) => {
      const dateObj = addDays(weekMonday, idx);
      const iso = toIsoDateString(dateObj);
      return {
        ...item,
        dateObj,
        iso,
        dayMonth: formatDayMonth(dateObj),
        isToday: iso === todayIso,
        isSelectedDate: Boolean(selectedDate && iso === selectedDate),
      };
    });
  }, [weekMonday, todayIso, selectedDate]);

  // Visible time slots of the venue (all slots from dbo.time_slots, or filtered slot)
  const visibleTimeSlots = useMemo(() => {
    if (selectedSlot === "ALL") return timeSlots;
    return timeSlots.filter((ts) => String(ts.id) === String(selectedSlot));
  }, [timeSlots, selectedSlot]);

  // Sessions inside the currently viewed week
  const weekStartIso = weekDays[0].iso;
  const weekEndIso = weekDays[6].iso;
  const sessionsInSelectedWeek = useMemo(
    () =>
      displayedSessions.filter(
        (s) => s.sessionDate >= weekStartIso && s.sessionDate <= weekEndIso,
      ),
    [displayedSessions, weekStartIso, weekEndIso],
  );

  // Nearest session date if the current week has no sessions (helps quick navigation)
  const nearestSessionWeek = useMemo(() => {
    if (sessionsInSelectedWeek.length > 0 || displayedSessions.length === 0) {
      return null;
    }
    const sorted = [...displayedSessions].sort((a, b) =>
      (a.sessionDate || "").localeCompare(b.sessionDate || ""),
    );
    const target =
      sorted.find((s) => s.sessionDate >= todayIso) ||
      sorted[sorted.length - 1];
    if (!target?.sessionDate) return null;
    const targetMonday = getMondayOfWeek(parseLocalDate(target.sessionDate));
    const targetSunday = addDays(targetMonday, 6);
    return {
      dateStr: target.sessionDate,
      label: `${formatDayMonth(targetMonday)} - ${formatDayMonth(targetSunday)}/${targetSunday.getFullYear()}`,
    };
  }, [
    sessionsInSelectedWeek.length,
    displayedSessions,
    todayIso,
  ]);

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
          onClick={() => onNavigate && onNavigate("skills")}
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
              onChange={(e) => handleDateFilterChange(e.target.value)}
            />
          </div>
          <div className="filter-field">
            <label htmlFor="session-slot">Khung giờ của sân</label>
            <select
              id="session-slot"
              value={selectedSlot}
              onChange={(e) => setSelectedSlot(e.target.value)}
            >
              <option value="ALL">Tất cả khung giờ</option>
              {timeSlots.map((slot) => {
                const range = formatSlotTimeRange(slot);
                const period = getSlotPeriodTag(slot);
                return (
                  <option key={slot.id} value={String(slot.id)}>
                    {range}
                    {period ? ` (${period})` : ""}
                  </option>
                );
              })}
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
        <div className="results-heading-left">
          <h2>Lịch social</h2>
          <span role="status">
            {loading
              ? "Đang tìm buổi chơi…"
              : error
                ? "Chưa tải được dữ liệu"
                : viewMode === "timetable"
                  ? `${sessionsInSelectedWeek.length} buổi trong tuần (${displayedSessions.length} tổng)`
                  : `${displayedSessions.length} buổi chơi`}
          </span>
        </div>
        <div
          className="view-mode-toggle"
          role="group"
          aria-label="Chế độ hiển thị lịch đấu"
        >
          <button
            type="button"
            className={
              viewMode === "list"
                ? "view-mode-btn is-active"
                : "view-mode-btn"
            }
            aria-pressed={viewMode === "list"}
            onClick={() => setViewMode("list")}
          >
            <LayoutGrid size={15} />
            <span>Danh sách</span>
          </button>
          <button
            type="button"
            className={
              viewMode === "timetable"
                ? "view-mode-btn is-active"
                : "view-mode-btn"
            }
            aria-pressed={viewMode === "timetable"}
            onClick={() => setViewMode("timetable")}
          >
            <CalendarRange size={15} />
            <span>Thời gian biểu</span>
          </button>
        </div>
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
      ) : viewMode === "timetable" ? (
        <section
          className="timetable-section"
          aria-label="Thời gian biểu theo tuần của sân"
        >
          <div className="timetable-toolbar">
            <div className="timetable-week-picker">
              <label htmlFor="timetable-week-date">Select week / Chọn tuần</label>
              <div className="timetable-week-input-row">
                <span className="timetable-week-badge">
                  Week {weekInfo.week}, {weekInfo.year} (
                  {weekDays[0].dayMonth} - {weekDays[6].dayMonth})
                </span>
                <input
                  id="timetable-week-date"
                  type="date"
                  aria-label="Chọn ngày bất kỳ để xem tuần tương ứng"
                  value={toIsoDateString(weekMonday)}
                  onChange={(e) => {
                    if (e.target.value) jumpToDateWeek(e.target.value);
                  }}
                />
              </div>
            </div>
            <div className="timetable-nav-buttons">
              <button
                type="button"
                className="timetable-nav-btn"
                onClick={() => shiftWeek(-1)}
              >
                <ChevronLeft size={15} />
                <span>Previous week</span>
              </button>
              <button
                type="button"
                className={
                  isViewingCurrentWeek
                    ? "timetable-nav-btn is-current"
                    : "timetable-nav-btn"
                }
                onClick={jumpToCurrentWeek}
              >
                <CalendarDays size={15} />
                <span>Current week</span>
              </button>
              <button
                type="button"
                className="timetable-nav-btn"
                onClick={() => shiftWeek(1)}
              >
                <span>Next week</span>
                <ChevronRight size={15} />
              </button>
            </div>
          </div>

          {nearestSessionWeek && (
            <div className="timetable-quick-jump" role="status">
              <span>
                Tuần đang chọn ({weekDays[0].dayMonth} - {weekDays[6].dayMonth})
                chưa có lịch mở trên hệ thống.
              </span>
              <button
                type="button"
                className="text-button"
                onClick={() => jumpToDateWeek(nearestSessionWeek.dateStr)}
              >
                Xem tuần có lịch ({nearestSessionWeek.label}) →
              </button>
            </div>
          )}

          <div className="timetable-card">
            <div className="timetable-card-header">
              <div>
                <h3>Weekly timetable · Lịch theo khung giờ sân</h3>
                <p>
                  Week ({weekDays[0].dayMonth} - {weekDays[6].dayMonth}/
                  {weekDays[6].dateObj.getFullYear()})
                </p>
              </div>
              <div className="timetable-legend" aria-label="Chú thích">
                <span className="legend-item">
                  <i className="legend-dot is-badminton" /> Cầu lông
                </span>
                <span className="legend-item">
                  <i className="legend-dot is-pickleball" /> Pickleball
                </span>
                <span className="legend-item">
                  <i className="legend-dot is-full" /> Hết chỗ
                </span>
              </div>
            </div>

            <div className="timetable-scroll">
              <table className="timetable-table">
                <thead>
                  <tr>
                    <th scope="col" className="timetable-slot-col">
                      Slot / Khung giờ
                    </th>
                    {weekDays.map((day) => (
                      <th
                        scope="col"
                        key={day.iso}
                        className={
                          day.isToday || day.isSelectedDate
                            ? "timetable-day-col is-today"
                            : "timetable-day-col"
                        }
                      >
                        <span className="day-title">
                          {day.short} ({day.dayMonth})
                        </span>
                        <small className="day-subtitle">{day.vi}</small>
                      </th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {visibleTimeSlots.map((slot, slotIdx) => {
                    const rangeLabel = formatSlotTimeRange(slot);
                    const periodTag = getSlotPeriodTag(slot);
                    return (
                      <tr key={slot.id}>
                        <th scope="row" className="timetable-slot-cell">
                          <span className="slot-number">
                            Slot {slot.id ?? slotIdx + 1}
                          </span>
                          <span className="slot-range">{rangeLabel}</span>
                          {periodTag && (
                            <small className="slot-period">Ca {periodTag}</small>
                          )}
                        </th>
                        {weekDays.map((day) => {
                          const cellSessions = displayedSessions.filter(
                            (s) =>
                              s.sessionDate === day.iso &&
                              (Number(s.timeSlotId) === Number(slot.id) ||
                                (!s.timeSlotId && s.timeLabel === rangeLabel)),
                          );

                          return (
                            <td
                              key={`${slot.id}-${day.iso}`}
                              className={
                                day.isToday || day.isSelectedDate
                                  ? "timetable-cell is-today-col"
                                  : "timetable-cell"
                              }
                            >
                              {cellSessions.length === 0 ? (
                                <span
                                  className="timetable-empty"
                                  aria-label="Trống"
                                >
                                  -
                                </span>
                              ) : (
                                <div className="timetable-cell-list">
                                  {cellSessions.map((s) => {
                                    const availableSlots =
                                      s.availableSlots ??
                                      s.capacity - s.occupiedPlaces;
                                    const isFull = availableSlots <= 0;
                                    const isBadminton =
                                      s.sportName === "Badminton";
                                    const bandLabel =
                                      s.band || "Tự do";

                                    return (
                                      <button
                                        type="button"
                                        key={s.id}
                                        className={[
                                          "timetable-event",
                                          isBadminton
                                            ? "is-badminton"
                                            : "is-pickleball",
                                          isFull ? "is-full" : "",
                                        ]
                                          .filter(Boolean)
                                          .join(" ")}
                                        onClick={() => onSelectSession(s.id)}
                                        title={`Xem chi tiết: ${getSessionTitle(s)} (${s.courtName})`}
                                      >
                                        <div className="timetable-event-title">
                                          <span>
                                            {isBadminton
                                              ? "Cầu lông"
                                              : "Pickleball"}{" "}
                                            - {bandLabel}
                                          </span>
                                          {s.isEligibleSuggestion === true && (
                                            <Sparkles
                                              size={12}
                                              className="timetable-eligible-icon"
                                              aria-label="Phù hợp trình độ"
                                            />
                                          )}
                                        </div>
                                        <div className="timetable-event-time">
                                          <Clock size={12} />
                                          <span>
                                            {s.timeLabel || rangeLabel}
                                          </span>
                                        </div>
                                        <div className="timetable-event-meta">
                                          <MapPin size={12} />
                                          <span>{s.courtName}</span>
                                        </div>
                                        <div className="timetable-event-footer">
                                          <span
                                            className={
                                              isFull
                                                ? "timetable-places is-full"
                                                : "timetable-places"
                                            }
                                          >
                                            {isFull
                                              ? "Hết chỗ"
                                              : `Còn ${availableSlots}/${s.capacity} chỗ`}
                                          </span>
                                          <span className="timetable-price">
                                            {formatPrice(s.priceVnd)}
                                          </span>
                                        </div>
                                      </button>
                                    );
                                  })}
                                </div>
                              )}
                            </td>
                          );
                        })}
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          </div>
        </section>
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
