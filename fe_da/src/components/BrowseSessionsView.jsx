import { useState, useEffect, useMemo } from "react";
<<<<<<< HEAD
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
=======
import { CalendarDays, CalendarRange, ChevronLeft, ChevronRight, Clock, LayoutGrid, MapPin, Users, Sparkles, ArrowRight, SlidersHorizontal, Ticket, Zap } from "lucide-react";
import StatePanel from "./ui/StatePanel";
import "../styles/sessions.css";
import { api } from "../api";
import { LateEntryOffers } from '../pages/late-entry/LateEntrySales';
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
  const startStr = item.startTime || (item.timeLabel || item.label || "").split("-")[0]?.trim();
  const endStr = item.endTime || (item.timeLabel || item.label || "").split("-")[1]?.trim();
  const sMin = parseTimeToMinutes(startStr);
  const eMin = parseTimeToMinutes(endStr);
  if (sMin != null && eMin != null && eMin > sMin) {
    return eMin - sMin;
  }
  return 120;
}
const DEFAULT_TIME_SLOTS = [{
  id: 1,
  startTime: "08:00:00",
  endTime: "10:00:00",
  label: "08:00 - 10:00"
}, {
  id: 2,
  startTime: "10:00:00",
  endTime: "12:00:00",
  label: "10:00 - 12:00"
}, {
  id: 3,
  startTime: "14:00:00",
  endTime: "16:00:00",
  label: "14:00 - 16:00"
}, {
  id: 4,
  startTime: "16:00:00",
  endTime: "18:00:00",
  label: "16:00 - 18:00"
}, {
  id: 5,
  startTime: "18:00:00",
  endTime: "20:00:00",
  label: "18:00 - 20:00"
}, {
  id: 6,
  startTime: "20:00:00",
  endTime: "22:00:00",
  label: "20:00 - 22:00"
}];
const WEEKDAY_LABELS = [{
  short: "Mon",
  vi: "Thứ 2"
}, {
  short: "Tue",
  vi: "Thứ 3"
}, {
  short: "Wed",
  vi: "Thứ 4"
}, {
  short: "Thu",
  vi: "Thứ 5"
}, {
  short: "Fri",
  vi: "Thứ 6"
}, {
  short: "Sat",
  vi: "Thứ 7"
}, {
  short: "Sun",
  vi: "CN"
}];
function parseLocalDate(dateStr) {
  if (!dateStr) return new Date();
  const parts = dateStr.split("-").map(n => parseInt(n, 10));
  if (parts.length === 3 && parts.every(n => !Number.isNaN(n))) {
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
    return new Date(parts[0], parts[1] - 1, parts[2], 12, 0, 0);
  }
  return new Date();
}
<<<<<<< HEAD

function getMondayOfWeek(dateObj) {
  const d = new Date(
    dateObj.getFullYear(),
    dateObj.getMonth(),
    dateObj.getDate(),
    12,
    0,
    0,
  );
=======
function getMondayOfWeek(dateObj) {
  const d = new Date(dateObj.getFullYear(), dateObj.getMonth(), dateObj.getDate(), 12, 0, 0);
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
  const day = d.getDay(); // 0 = Sun, 1 = Mon, ...
  const diff = day === 0 ? -6 : 1 - day;
  d.setDate(d.getDate() + diff);
  return d;
}
<<<<<<< HEAD

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

=======
function addDays(dateObj, days) {
  const d = new Date(dateObj.getFullYear(), dateObj.getMonth(), dateObj.getDate(), 12, 0, 0);
  d.setDate(d.getDate() + days);
  return d;
}
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
function toIsoDateString(dateObj) {
  const y = dateObj.getFullYear();
  const m = String(dateObj.getMonth() + 1).padStart(2, "0");
  const d = String(dateObj.getDate()).padStart(2, "0");
  return `${y}-${m}-${d}`;
}
<<<<<<< HEAD

=======
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
function formatDayMonth(dateObj) {
  const d = String(dateObj.getDate()).padStart(2, "0");
  const m = String(dateObj.getMonth() + 1).padStart(2, "0");
  return `${d}/${m}`;
}
<<<<<<< HEAD

function getIsoWeekInfo(dateObj) {
  const d = new Date(
    Date.UTC(dateObj.getFullYear(), dateObj.getMonth(), dateObj.getDate()),
  );
=======
function getIsoWeekInfo(dateObj) {
  const d = new Date(Date.UTC(dateObj.getFullYear(), dateObj.getMonth(), dateObj.getDate()));
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
  const dayNum = d.getUTCDay() || 7;
  d.setUTCDate(d.getUTCDate() + 4 - dayNum);
  const yearStart = new Date(Date.UTC(d.getUTCFullYear(), 0, 1));
  const weekNo = Math.ceil(((d - yearStart) / 86400000 + 1) / 7);
<<<<<<< HEAD
  return { week: weekNo, year: d.getUTCFullYear() };
}

=======
  return {
    week: weekNo,
    year: d.getUTCFullYear()
  };
}
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
function getSlotPeriodTag(slot) {
  const raw = slot.startTime || slot.label || "";
  const hour = parseInt(raw.slice(0, 2), 10);
  if (Number.isNaN(hour)) return "";
  if (hour < 11) return "Sáng";
  if (hour < 13) return "Trưa";
  if (hour < 18) return "Chiều";
  return "Tối";
}
<<<<<<< HEAD

=======
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
function formatSlotTimeRange(slot) {
  if (slot.label) return slot.label;
  const start = (slot.startTime || "").slice(0, 5);
  const end = (slot.endTime || "").slice(0, 5);
  return start && end ? `${start} - ${end}` : "";
}
<<<<<<< HEAD

function getSessionStartMs(s) {
  if (!s?.sessionDate) return 0;
  const parts = s.sessionDate.split("-").map((n) => parseInt(n, 10));
  if (parts.length !== 3 || parts.some((n) => Number.isNaN(n))) return 0;
  const rawStart = (s.startTime || s.timeLabel?.split("-")[0] || "00:00")
    .trim()
    .slice(0, 5);
  const [hh, mm] = rawStart.split(":").map((n) => parseInt(n, 10) || 0);
  return new Date(parts[0], parts[1] - 1, parts[2], hh, mm, 0, 0).getTime();
}

=======
function getSessionStartMs(s) {
  if (!s?.sessionDate) return 0;
  const parts = s.sessionDate.split("-").map(n => parseInt(n, 10));
  if (parts.length !== 3 || parts.some(n => Number.isNaN(n))) return 0;
  const rawStart = (s.startTime || s.timeLabel?.split("-")[0] || "00:00").trim().slice(0, 5);
  const [hh, mm] = rawStart.split(":").map(n => parseInt(n, 10) || 0);
  return new Date(parts[0], parts[1] - 1, parts[2], hh, mm, 0, 0).getTime();
}
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
function isSessionPastStart(s, nowMs = Date.now()) {
  const startMs = getSessionStartMs(s);
  return startMs > 0 && startMs <= nowMs;
}
<<<<<<< HEAD

=======
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
function isSessionFull(s) {
  const availableSlots = s.availableSlots ?? s.capacity - s.occupiedPlaces;
  return availableSlots <= 0;
}
<<<<<<< HEAD

=======
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
export function BrowseSessionsView({
  user,
  onSelectSession,
  onNavigate,
<<<<<<< HEAD
  initialSport = "ALL",
=======
  initialSport = "ALL"
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
}) {
  const [sessions, setSessions] = useState([]);
  const [timeSlots, setTimeSlots] = useState(DEFAULT_TIME_SLOTS);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  // View mode: "list" (normal cards) | "timetable" (weekly slot timetable)
  const [viewMode, setViewMode] = useState("list");
<<<<<<< HEAD
  const [weekMonday, setWeekMonday] = useState(() =>
    getMondayOfWeek(new Date()),
  );

  // Filters state
  const [selectedSport, setSelectedSport] = useState(initialSport);
=======
  const [weekMonday, setWeekMonday] = useState(() => getMondayOfWeek(new Date()));

  // Filters state
  const [selectedSport, setSelectedSport] = useState(initialSport);
  const [selectedSessionType, setSelectedSessionType] = useState("ALL");
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
  const [selectedDate, setSelectedDate] = useState("");
  const [selectedSlot, setSelectedSlot] = useState("ALL");
  const [filterEligibleOnly, setFilterEligibleOnly] = useState(false);

<<<<<<< HEAD
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
=======
  // Load time slots once. Session APIs use the signed-in center.
  useEffect(() => {
    let active = true;
    api.getTimeSlots().then(data => {
      if (active && Array.isArray(data) && data.length > 0) {
        setTimeSlots(data);
      }
    }).catch(() => {
      /* Fallback to DEFAULT_TIME_SLOTS when offline */
    });
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
    return () => {
      active = false;
    };
  }, []);
<<<<<<< HEAD

  useEffect(() => {
    loadSessions();
  }, [user, selectedSport, selectedDate, selectedSlot]);

=======
  useEffect(() => {
    loadSessions();
  }, [user, selectedSport, selectedDate, selectedSlot]);
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
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
<<<<<<< HEAD

=======
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
  function handleDateFilterChange(val) {
    setSelectedDate(val);
    if (val) {
      setWeekMonday(getMondayOfWeek(parseLocalDate(val)));
    }
  }
<<<<<<< HEAD

=======
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
  function shiftWeek(offsetWeeks) {
    if (selectedDate) {
      setSelectedDate("");
    }
<<<<<<< HEAD
    setWeekMonday((prev) => addDays(prev, offsetWeeks * 7));
  }

=======
    setWeekMonday(prev => addDays(prev, offsetWeeks * 7));
  }
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
  function jumpToCurrentWeek() {
    if (selectedDate) {
      setSelectedDate("");
    }
    setWeekMonday(getMondayOfWeek(new Date()));
  }
<<<<<<< HEAD

=======
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
  function jumpToDateWeek(dateStr) {
    if (!dateStr) return;
    if (selectedDate && selectedDate !== dateStr) {
      setSelectedDate("");
    }
    setWeekMonday(getMondayOfWeek(parseLocalDate(dateStr)));
  }
<<<<<<< HEAD

  function formatPrice(val) {
    return new Intl.NumberFormat("vi-VN", {
      style: "currency",
      currency: "VND",
    }).format(val);
  }

=======
  function formatPrice(val) {
    return new Intl.NumberFormat("vi-VN", {
      style: "currency",
      currency: "VND"
    }).format(val);
  }
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
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
<<<<<<< HEAD

  function getSessionTitle(s) {
    if (s.sportName === "Badminton") {
      if (s.timeLabel?.includes("18:00") && s.band === "B2")
        return "Cầu lông sau giờ làm";
=======
  function getSessionTitle(s) {
    if (s.sportName === "Badminton") {
      if (s.timeLabel?.includes("18:00") && s.band === "B2") return "Cầu lông sau giờ làm";
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
      if (s.timeLabel?.includes("20:00")) return "Cầu lông phong trào tối";
      if (s.band === "B3") return "Cầu lông nâng cao";
      return "Giao lưu cầu lông";
    } else {
      if (s.timeLabel?.includes("08:00")) return "Pickleball cuối tuần";
      if (!s.band) return "Pickleball giao lưu mở";
      return "Giao lưu Pickleball";
    }
  }
<<<<<<< HEAD

  const displayedSessions = useMemo(
    () =>
      filterEligibleOnly
        ? sessions.filter((s) => s.isEligibleSuggestion === true)
        : sessions,
    [sessions, filterEligibleOnly],
  );
=======
  const displayedSessions = useMemo(() => sessions.filter(s => !filterEligibleOnly || s.isEligibleSuggestion === true), [sessions, filterEligibleOnly]);

  // Counts for Quick Filter Tabs (open sessions in list mode, or all in timetable mode)
  const sessionTypeCounts = useMemo(() => {
    const nowMs = Date.now();
    const basePool = viewMode === "list" ? displayedSessions.filter(s => !isSessionFull(s) && !isSessionPastStart(s, nowMs)) : displayedSessions;
    const social = basePool.length;
    return {
      all: basePool.length,
      social
    };
  }, [displayedSessions, viewMode]);
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c

  // Open sessions for List View: exclude sessions that are already full or past their start time
  const openListSessions = useMemo(() => {
    const nowMs = Date.now();
<<<<<<< HEAD
    return displayedSessions
      .filter((s) => !isSessionFull(s) && !isSessionPastStart(s, nowMs))
      .sort((a, b) => getSessionStartMs(a) - getSessionStartMs(b));
=======
    return displayedSessions.filter(s => !isSessionFull(s) && !isSessionPastStart(s, nowMs)).sort((a, b) => getSessionStartMs(a) - getSessionStartMs(b));
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
  }, [displayedSessions]);

  // Build 7 days of the currently selected week (Monday -> Sunday)
  const todayIso = toIsoDateString(new Date());
  const currentWeekMondayIso = toIsoDateString(getMondayOfWeek(new Date()));
<<<<<<< HEAD
  const isViewingCurrentWeek =
    toIsoDateString(weekMonday) === currentWeekMondayIso;
  const weekInfo = getIsoWeekInfo(weekMonday);

=======
  const isViewingCurrentWeek = toIsoDateString(weekMonday) === currentWeekMondayIso;
  const weekInfo = getIsoWeekInfo(weekMonday);
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
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
<<<<<<< HEAD
        isSelectedDate: Boolean(selectedDate && iso === selectedDate),
=======
        isSelectedDate: Boolean(selectedDate && iso === selectedDate)
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
      };
    });
  }, [weekMonday, todayIso, selectedDate]);

  // Visible time slots of the venue (all slots from dbo.time_slots, or filtered slot)
  const visibleTimeSlots = useMemo(() => {
<<<<<<< HEAD
    if (selectedSlot === "ALL") return timeSlots;
    return timeSlots.filter((ts) => String(ts.id) === String(selectedSlot));
=======
    if (selectedSlot !== "ALL") {
      return timeSlots.filter(ts => String(ts.id) === String(selectedSlot));
    }
    return timeSlots;
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
  }, [timeSlots, selectedSlot]);

  // Sessions inside the currently viewed week
  const weekStartIso = weekDays[0].iso;
  const weekEndIso = weekDays[6].iso;
<<<<<<< HEAD
  const sessionsInSelectedWeek = useMemo(
    () =>
      displayedSessions.filter(
        (s) => s.sessionDate >= weekStartIso && s.sessionDate <= weekEndIso,
      ),
    [displayedSessions, weekStartIso, weekEndIso],
  );
=======
  const sessionsInSelectedWeek = useMemo(() => displayedSessions.filter(s => s.sessionDate >= weekStartIso && s.sessionDate <= weekEndIso), [displayedSessions, weekStartIso, weekEndIso]);
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c

  // Nearest session date if the current week has no sessions (helps quick navigation)
  const nearestSessionWeek = useMemo(() => {
    if (sessionsInSelectedWeek.length > 0 || displayedSessions.length === 0) {
      return null;
    }
<<<<<<< HEAD
    const sorted = [...displayedSessions].sort((a, b) =>
      (a.sessionDate || "").localeCompare(b.sessionDate || ""),
    );
    const target =
      sorted.find((s) => s.sessionDate >= todayIso) ||
      sorted[sorted.length - 1];
=======
    const sorted = [...displayedSessions].sort((a, b) => (a.sessionDate || "").localeCompare(b.sessionDate || ""));
    const target = sorted.find(s => s.sessionDate >= todayIso) || sorted[sorted.length - 1];
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
    if (!target?.sessionDate) return null;
    const targetMonday = getMondayOfWeek(parseLocalDate(target.sessionDate));
    const targetSunday = addDays(targetMonday, 6);
    return {
      dateStr: target.sessionDate,
<<<<<<< HEAD
      label: `${formatDayMonth(targetMonday)} - ${formatDayMonth(targetSunday)}/${targetSunday.getFullYear()}`,
    };
  }, [
    sessionsInSelectedWeek.length,
    displayedSessions,
    todayIso,
  ]);

  return (
    <div className="sessions-page">
=======
      label: `${formatDayMonth(targetMonday)} - ${formatDayMonth(targetSunday)}/${targetSunday.getFullYear()}`
    };
  }, [sessionsInSelectedWeek.length, displayedSessions, todayIso]);
  return <div className="sessions-page">
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
      <header className="page-heading">
        <span className="eyebrow">KHÁM PHÁ · KẾT NỐI · RA SÂN</span>
        <h1>
          Tìm buổi chơi của bạn<span>.</span>
        </h1>
<<<<<<< HEAD
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
=======
      </header>
      <section className="session-filters" aria-label="Bộ lọc buổi chơi">
        <div className="filter-heading">
          <SlidersHorizontal size={18} />
          <strong>Tìm buổi chơi</strong>
        </div>
        <div className="filter-fields">
          <div className="filter-field">
            <label htmlFor="session-type">Loại lịch / Vé mở bán</label>
            <select id="session-type" value={selectedSessionType} onChange={e => {
            setSelectedSessionType(e.target.value);
            if (e.target.value === 'Remaining') setSelectedDate(todayIso);
          }}>
              <option value="ALL">Tất cả</option>
              <option value="Social">Lịch Social</option>
              <option value="Remaining">Vé giờ lẻ</option>
            </select>
          </div>
          <div className="filter-field">
            <label htmlFor="session-sport">Môn thể thao</label>
            <select id="session-sport" value={selectedSport} onChange={e => setSelectedSport(e.target.value)}>
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
              <option value="ALL">Tất cả bộ môn</option>
              <option value="Badminton">Cầu lông</option>
              <option value="Pickleball">Pickleball</option>
            </select>
          </div>
          <div className="filter-field">
            <label htmlFor="session-date">Ngày ra sân</label>
<<<<<<< HEAD
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
=======
            <input id="session-date" type="date" value={selectedDate} onChange={e => handleDateFilterChange(e.target.value)} />
          </div>
          <div className="filter-field">
            <label htmlFor="session-slot">Khung giờ của sân</label>
            <select id="session-slot" value={selectedSlot} onChange={e => setSelectedSlot(e.target.value)}>
              <option value="ALL">Tất cả khung giờ</option>
              {timeSlots.map(slot => {
              const range = formatSlotTimeRange(slot);
              const period = getSlotPeriodTag(slot);
              return <option key={slot.id} value={String(slot.id)}>
                    {range}
                    {period ? ` (${period})` : ""}
                  </option>;
            })}
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
            </select>
          </div>
          <button className="button button-primary" onClick={loadSessions}>
            Tìm buổi chơi <ArrowRight size={16} />
          </button>
        </div>
<<<<<<< HEAD
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
=======

        {/* Session and late-entry filters */}
        <div className="session-quick-pills-row">
          <div className="session-type-pills" role="group" aria-label="Phân loại vé">
            <button type="button" className={`session-type-pill ${selectedSessionType === "ALL" ? "is-active" : ""}`} onClick={() => setSelectedSessionType("ALL")}>
              Tất cả
            </button>
            <button type="button" className={`session-type-pill ${selectedSessionType === "Social" ? "is-active" : ""}`} onClick={() => setSelectedSessionType("Social")}>
              Lịch Social ({sessionTypeCounts.social})
            </button>
            <button type="button" className={`session-type-pill ${selectedSessionType === 'Remaining' ? 'is-active' : ''}`} onClick={() => {
            setSelectedSessionType('Remaining');
            setSelectedDate(todayIso);
          }}>
              <Zap size={15} /> Vé giờ lẻ
            </button>
          </div>

        </div>

        <div className="filter-footer">
          <label className="eligibility-toggle">
            <input type="checkbox" checked={filterEligibleOnly} onChange={() => setFilterEligibleOnly(!filterEligibleOnly)} />
            <span>Chỉ buổi chơi phù hợp với trình độ của tôi</span>
          </label>
          <button className="text-button" onClick={() => {
          setSelectedSport("ALL");
          setSelectedSessionType("ALL");
          setSelectedDate("");
          setSelectedSlot("ALL");
          setFilterEligibleOnly(false);
        }}>
            Đặt lại toàn bộ bộ lọc
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
          </button>
        </div>
      </section>

<<<<<<< HEAD
=======
      {(selectedSessionType === 'ALL' || selectedSessionType === 'Remaining') && <LateEntryOffers sport={selectedSport} sessionIds={loading ? [] : displayedSessions.map(s => s.id)} showEmpty={selectedSessionType === "Remaining"} onSelectSession={onSelectSession} />}
      {selectedSessionType !== 'Remaining' && <>
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
      <div className="results-heading">
        <div className="results-heading-left">
          <h2>Lịch social</h2>
          <span role="status">
<<<<<<< HEAD
            {loading
              ? "Đang tìm buổi chơi…"
              : error
                ? "Chưa tải được dữ liệu"
                : viewMode === "timetable"
                  ? `${sessionsInSelectedWeek.length} buổi trong tuần (${displayedSessions.length} tổng)`
                  : `${openListSessions.length} buổi chơi đang mở`}
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
=======
            {loading ? "Đang tìm buổi chơi…" : error ? "Chưa tải được dữ liệu" : viewMode === "timetable" ? `${sessionsInSelectedWeek.length} buổi trong tuần (${displayedSessions.length} tổng)` : `${openListSessions.length} buổi chơi đang mở`}
          </span>
        </div>
        <div className="view-mode-toggle" role="group" aria-label="Chế độ hiển thị lịch đấu">
          <button type="button" className={viewMode === "list" ? "view-mode-btn is-active" : "view-mode-btn"} aria-pressed={viewMode === "list"} onClick={() => setViewMode("list")}>
            <LayoutGrid size={15} />
            <span>Danh sách</span>
          </button>
          <button type="button" className={viewMode === "timetable" ? "view-mode-btn is-active" : "view-mode-btn"} aria-pressed={viewMode === "timetable"} onClick={() => setViewMode("timetable")}>
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
            <CalendarRange size={15} />
            <span>Thời gian biểu</span>
          </button>
        </div>
      </div>

<<<<<<< HEAD
      {loading ? (
        <div
          className="session-grid"
          role="status"
          aria-label="Đang tải buổi chơi"
          aria-busy="true"
        >
          {[1, 2, 3].map((i) => (
            <div key={i} className="session-skeleton">
=======
      {loading ? <div className="session-grid" role="status" aria-label="Đang tải buổi chơi" aria-busy="true">
          {[1, 2, 3].map(i => <div key={i} className="session-skeleton">
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
              <span />
              <span />
              <span />
              <span />
<<<<<<< HEAD
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
=======
            </div>)}
        </div> : error ? <StatePanel kind="error" title="Kết nối đang gián đoạn" description={error} action={<button className="button button-primary" onClick={loadSessions}>
              Thử lại
            </button>} /> : viewMode === "timetable" ? <section className="timetable-section" aria-label="Thời gian biểu theo tuần của sân">
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
          <div className="timetable-toolbar">
            <div className="timetable-week-picker">
              <label htmlFor="timetable-week-date">Select week / Chọn tuần</label>
              <div className="timetable-week-input-row">
                <span className="timetable-week-badge">
                  Week {weekInfo.week}, {weekInfo.year} (
                  {weekDays[0].dayMonth} - {weekDays[6].dayMonth})
                </span>
<<<<<<< HEAD
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
=======
                <input id="timetable-week-date" type="date" aria-label="Chọn ngày bất kỳ để xem tuần tương ứng" value={toIsoDateString(weekMonday)} onChange={e => {
                if (e.target.value) jumpToDateWeek(e.target.value);
              }} />
              </div>
            </div>
            <div className="timetable-nav-buttons">
              <button type="button" className="timetable-nav-btn" onClick={() => shiftWeek(-1)}>
                <ChevronLeft size={15} />
                <span>Previous week</span>
              </button>
              <button type="button" className={isViewingCurrentWeek ? "timetable-nav-btn is-current" : "timetable-nav-btn"} onClick={jumpToCurrentWeek}>
                <CalendarDays size={15} />
                <span>Current week</span>
              </button>
              <button type="button" className="timetable-nav-btn" onClick={() => shiftWeek(1)}>
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
                <span>Next week</span>
                <ChevronRight size={15} />
              </button>
            </div>
          </div>

<<<<<<< HEAD
          {nearestSessionWeek && (
            <div className="timetable-quick-jump" role="status">
=======
          {nearestSessionWeek && <div className="timetable-quick-jump" role="status">
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
              <span>
                Tuần đang chọn ({weekDays[0].dayMonth} - {weekDays[6].dayMonth})
                chưa có lịch mở trên hệ thống.
              </span>
<<<<<<< HEAD
              <button
                type="button"
                className="text-button"
                onClick={() => jumpToDateWeek(nearestSessionWeek.dateStr)}
              >
                Xem tuần có lịch ({nearestSessionWeek.label}) →
              </button>
            </div>
          )}
=======
              <button type="button" className="text-button" onClick={() => jumpToDateWeek(nearestSessionWeek.dateStr)}>
                Xem tuần có lịch ({nearestSessionWeek.label}) →
              </button>
            </div>}
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c

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
                  <i className="legend-dot is-locked" /> Đã khóa (Qua giờ / Đủ chỗ)
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
<<<<<<< HEAD
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
=======
                    {weekDays.map(day => <th scope="col" key={day.iso} className={day.isToday || day.isSelectedDate ? "timetable-day-col is-today" : "timetable-day-col"}>
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
                        <span className="day-title">
                          {day.short} ({day.dayMonth})
                        </span>
                        <small className="day-subtitle">{day.vi}</small>
<<<<<<< HEAD
                      </th>
                    ))}
=======
                      </th>)}
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
                  </tr>
                </thead>
                <tbody>
                  {visibleTimeSlots.map((slot, slotIdx) => {
<<<<<<< HEAD
                    const rangeLabel = formatSlotTimeRange(slot);
                    const periodTag = getSlotPeriodTag(slot);
                    return (
                      <tr key={slot.id}>
=======
                  const rangeLabel = formatSlotTimeRange(slot);
                  const periodTag = getSlotPeriodTag(slot);
                  return <tr key={slot.id}>
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
                        <th scope="row" className="timetable-slot-cell">
                          <span className="slot-number">
                            Slot {slot.id ?? slotIdx + 1}
                          </span>
                          <span className="slot-range">{rangeLabel}</span>
<<<<<<< HEAD
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
                                    const isPast = isSessionPastStart(s);
                                    const isLocked = isFull || isPast;
                                    const isBadminton =
                                      s.sportName === "Badminton";
                                    const bandLabel =
                                      s.band || "Tự do";

                                    return (
                                      <button
                                        type="button"
                                        key={s.id}
                                        disabled={isLocked}
                                        aria-disabled={isLocked}
                                        className={[
                                          "timetable-event",
                                          isLocked
                                            ? "is-locked"
                                            : isBadminton
                                              ? "is-badminton"
                                              : "is-pickleball",
                                        ]
                                          .filter(Boolean)
                                          .join(" ")}
                                        onClick={() => {
                                          if (!isLocked) onSelectSession(s.id);
                                        }}
                                        title={
                                          isPast
                                            ? "Buổi chơi đã qua thời điểm bắt đầu (Đã khóa)"
                                            : isFull
                                              ? "Buổi chơi đã đủ người (Đã khóa)"
                                              : `Xem chi tiết: ${getSessionTitle(s)} (${s.courtName})`
                                        }
                                      >
                                        <div className="timetable-event-title">
                                          <span>
                                            {isBadminton
                                              ? "Cầu lông"
                                              : "Pickleball"}{" "}
                                            - {bandLabel}
                                          </span>
                                          {!isLocked &&
                                            s.isEligibleSuggestion === true && (
                                              <Sparkles
                                                size={12}
                                                className="timetable-eligible-icon"
                                                aria-label="Phù hợp trình độ"
                                              />
                                            )}
=======
                          {periodTag && <small className="slot-period">Ca {periodTag}</small>}
                        </th>
                        {weekDays.map(day => {
                      const cellSessions = displayedSessions.filter(s => s.sessionDate === day.iso && (Number(s.timeSlotId) === Number(slot.id) || !s.timeSlotId && s.timeLabel === rangeLabel));
                      return <td key={`${slot.id}-${day.iso}`} className={day.isToday || day.isSelectedDate ? "timetable-cell is-today-col" : "timetable-cell"}>
                              {cellSessions.length === 0 ? <span className="timetable-empty" aria-label="Trống">
                                  -
                                </span> : <div className="timetable-cell-list">
                                  {cellSessions.map(s => {
                            const availableSlots = s.availableSlots ?? s.capacity - s.occupiedPlaces;
                            const isFull = availableSlots <= 0;
                            const isPast = isSessionPastStart(s);
                            const isLocked = isFull || isPast;
                            const isBadminton = s.sportName === "Badminton";
                            const bandLabel = s.band || "Tự do";
                            return <button type="button" key={s.id} disabled={isLocked} aria-disabled={isLocked} className={["timetable-event", isLocked ? "is-locked" : isBadminton ? "is-badminton" : "is-pickleball"].filter(Boolean).join(" ")} onClick={() => {
                              if (!isLocked) onSelectSession(s.id);
                            }} title={isPast ? "Buổi chơi đã qua thời điểm bắt đầu (Đã khóa)" : isFull ? "Buổi chơi đã đủ người (Đã khóa)" : `Xem chi tiết: ${getSessionTitle(s)} (${s.courtName})`}>

                                        <div className="timetable-event-title">
                                          <span>
                                            {isBadminton ? "Cầu lông" : "Pickleball"}{" "}
                                            - {bandLabel}
                                          </span>
                                          {!isLocked && s.isEligibleSuggestion === true && <Sparkles size={13} className="timetable-eligible-icon" aria-label="Phù hợp trình độ" />}
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
                                        </div>
                                        <div className="timetable-event-meta">
                                          <MapPin size={12} />
                                          <span>{s.courtName}</span>
                                        </div>
                                        <div className="timetable-event-footer">
<<<<<<< HEAD
                                          <span
                                            className={
                                              isLocked
                                                ? "timetable-places is-locked"
                                                : "timetable-places"
                                            }
                                          >
                                            {isPast
                                              ? "Đã qua giờ"
                                              : isFull
                                                ? "Đã đủ chỗ"
                                                : `Còn ${availableSlots}/${s.capacity} chỗ`}
=======
                                          <span className={isLocked ? "timetable-places is-locked" : "timetable-places"}>
                                            {isPast ? "Đã qua giờ" : isFull ? "Đã đủ chỗ" : `Còn ${availableSlots}/${s.capacity} chỗ`}
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
                                          </span>
                                          <span className="timetable-price">
                                            {formatPrice(s.priceVnd)}
                                          </span>
                                        </div>
<<<<<<< HEAD
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
=======
                                      </button>;
                          })}
                                </div>}
                            </td>;
                    })}
                      </tr>;
                })}
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
                </tbody>
              </table>
            </div>
          </div>
<<<<<<< HEAD
        </section>
      ) : openListSessions.length === 0 ? (
        <StatePanel
          title="Chưa tìm thấy buổi chơi đang mở"
          description="Các buổi chơi đã đủ chỗ hoặc đã qua thời điểm bắt đầu đã được ẩn khỏi danh sách. Hãy chọn ngày khác hoặc chuyển sang Thời gian biểu để xem toàn bộ lịch tuần."
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
          {openListSessions.map((s) => {
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
=======
        </section> : openListSessions.length === 0 ? <StatePanel title={"Chưa tìm thấy buổi chơi đang mở"} description={"Các buổi chơi đã đủ chỗ hoặc đã qua thời điểm bắt đầu đã được ẩn khỏi danh sách. Hãy chọn ngày khác hoặc chuyển sang Thời gian biểu để xem toàn bộ lịch tuần."} action={<button className="button button-primary" onClick={() => {
        setSelectedSport("ALL");
        setSelectedSessionType("ALL");
        setSelectedDate("");
        setSelectedSlot("ALL");
        setFilterEligibleOnly(false);
      }}>
              Xóa bộ lọc
            </button>} /> : <div className="session-grid">
          {openListSessions.map(s => {
          const isEligible = s.isEligibleSuggestion;
          const availableSlots = s.availableSlots ?? s.capacity - s.occupiedPlaces;
          const isFull = availableSlots <= 0;
          const durationMins = getDurationMinutes(s);
          const bandLabel = s.band === "B1" ? "Mới chơi / B1" : s.band === "B2" ? "Trung bình / B2" : s.band === "B3" ? "Khá / B3" : s.band || "Tự do";
          return <article className="session-card" key={s.id}>
                <div className="session-card-top">
                  <div className="session-card-badges">
                    <span className="sport-label">
                      <ActivityMark />
                      {s.sportName === "Badminton" ? "Cầu lông" : "Pickleball"}
                    </span>
                    {<span className="session-duration-badge">
                        Lịch Social · {durationMins} phút
                      </span>}
                  </div>
                  <span className={isFull ? "session-status is-full" : "session-status"}>
                    {isFull ? "Đã hết chỗ" : `Còn ${availableSlots}/${s.capacity} chỗ`}
                  </span>
                </div>
                <h3>{getSessionTitle(s)}</h3>
                {isEligible === true && <span className="session-match">
                      <Sparkles size={14} /> Phù hợp trình độ của bạn
                    </span>}
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
                <ul className="session-meta">
                  <li>
                    <MapPin size={16} />
                    <span>
<<<<<<< HEAD
                      {s.courtName} · {s.venueName}
=======
                      Sân thi đấu: <strong>{s.courtName}</strong>
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
                    </span>
                  </li>
                  <li>
                    <CalendarDays size={16} />
<<<<<<< HEAD
                    <span>{formatSessionDate(s.sessionDate)}</span>
=======
                    <span>Ngày: {formatSessionDate(s.sessionDate)}</span>
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
                  </li>
                  <li>
                    <Clock size={16} />
                    <span>
<<<<<<< HEAD
                      {s.timeLabel || `${s.startTime} - ${s.endTime}`}
=======
                      {s.timeLabel || `${s.startTime} - ${s.endTime}`}{" "}
                      <strong>
                        ({`${durationMins} phút`})
                      </strong>
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
                    </span>
                  </li>
                  <li>
                    <Users size={16} />
                    <span>
<<<<<<< HEAD
                      Trình độ <strong>{bandLabel}</strong>
=======
                      Trình độ: <strong>{bandLabel}</strong>
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
                    </span>
                  </li>
                </ul>
                <div className="session-card-bottom">
                  <div className="session-price">
                    {formatPrice(s.priceVnd)}
<<<<<<< HEAD
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
=======
                    <span>{"/ người"}</span>
                  </div>
                  <button className={`button ${"button-accent"}`} onClick={() => onSelectSession(s.id)}>
                    {"Xem buổi chơi"}{" "}
                    <ArrowRight size={16} />
                  </button>
                </div>
              </article>;
        })}
        </div>}
      </>}
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
      <aside className="tickets-callout">
        <span className="callout-icon">
          <Ticket size={24} />
        </span>
        <div>
          <strong>Lịch ra sân, luôn trong tầm tay.</strong>
          <p>Theo dõi vé và thông tin đăng ký của bạn tại một nơi.</p>
        </div>
<<<<<<< HEAD
        <button
          className="button button-secondary"
          onClick={() => onNavigate && onNavigate("tickets")}
        >
          Xem vé của tôi <ArrowRight size={16} />
        </button>
      </aside>
    </div>
  );
=======
        <button className="button button-secondary" onClick={() => onNavigate && onNavigate("tickets")}>
          Xem vé của tôi <ArrowRight size={16} />
        </button>
      </aside>
    </div>;
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
}
function ActivityMark() {
  return <span className="sport-dot" aria-hidden="true" />;
}
