import { useEffect, useState } from "react";
import {
  ArrowUpRight,
  CalendarDays,
  Clock,
  MapPin,
  Ticket,
} from "lucide-react";
import { api } from "../../api";
import { demoMode, paymentClient } from "../payment/paymentClient";

const startOf = (item) =>
  Date.parse(
    `${item.sessionDate}T${item.startTime || item.timeLabel?.split(" - ")[0] || "00:00:00"}+07:00`,
  );
const dateLabel = (date) =>
  new Date(`${date}T12:00:00+07:00`).toLocaleDateString("vi-VN", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    timeZone: "Asia/Ho_Chi_Minh",
  });
const timeLabel = (item) =>
  item.timeLabel ||
  [item.startTime, item.endTime]
    .filter(Boolean)
    .map((time) => time.slice(0, 5))
    .join(" – ");
const places = (item) =>
  item.availableSlots ?? item.capacity - item.occupiedPlaces;
const upcoming = (items) =>
  Array.isArray(items)
    ? items
        .filter((item) => startOf(item) > Date.now())
        .sort((a, b) => startOf(a) - startOf(b))
    : [];

export default function PlayerHome({
  preview,
  onBrowse,
  onTickets,
  onSelectSession,
}) {
  const [sessions, setSessions] = useState([]);
  const [tickets, setTickets] = useState(null);
  const [ticketError, setTicketError] = useState(false);
  useEffect(() => {
    if (preview) return;
    let active = true;
    api
      .getSessions()
      .then((data) => {
        if (active)
          setSessions(
            upcoming(data)
              .filter((item) => places(item) > 0)
              .slice(0, 3),
          );
      })
      .catch(() => {
        /* Optional discovery content stays absent when unavailable. */
      });
    // Payment demo data must never appear as a player's real participation history.
    if (!demoMode)
      paymentClient
        .tickets()
        .then((data) => {
          if (!Array.isArray(data)) throw new Error("Invalid ticket response");
          if (active)
            setTickets(
              upcoming(data)
                .filter((ticket) => ticket.valid)
                .slice(0, 3),
            );
        })
        .catch(() => {
          if (active) setTicketError(true);
        });
    return () => {
      active = false;
    };
  }, [preview]);
  return (
    <>
      <section className="player-hero" aria-labelledby="player-title">
        <img
          className="player-hero-photo"
          src="/images/community/hero-960.webp"
          srcSet="/images/community/hero-640.webp 640w, /images/community/hero-960.webp 960w, /images/community/hero-1600.webp 1600w"
          sizes="(max-width: 700px) 100vw, 58vw"
          width="1600"
          height="2400"
          fetchPriority="high"
          alt="Vận động viên bật nhảy đánh cầu trên sân trong nhà"
        />
        <div className="player-container player-hero-inner">
          <div className="player-hero-copy">
            <span className="player-eyebrow">CẦU LÔNG & PICKLEBALL</span>
            <h1 id="player-title">
              Sẵn sàng cho
              <br />
              <em>trận đấu tiếp theo?</em>
            </h1>
            <p>
              Một buổi chơi hợp trình, những người bạn cùng đam mê.
              <br className="player-wide-break" /> Ra sân và tận hưởng từng nhịp
              chơi.
            </p>
            <button className="player-primary" onClick={onBrowse}>
              Tìm buổi chơi <ArrowUpRight size={21} aria-hidden="true" />
            </button>
          </div>
        </div>
      </section>
      {sessions.length > 0 && (
        <section
          className="player-container player-section"
          aria-labelledby="player-sessions-title"
        >
          <div className="player-section-heading">
            <h2 id="player-sessions-title">Buổi chơi sắp diễn ra</h2>
            <button className="player-text-link" onClick={onBrowse}>
              Xem lịch social <ArrowUpRight size={18} aria-hidden="true" />
            </button>
          </div>
          <div className="player-session-grid">
            {sessions.map((session) => (
              <article className="player-session" key={session.id}>
                <div className="player-session-top">
                  <span>
                    {session.sportName === "Badminton"
                      ? "Cầu lông"
                      : session.sportName === "Pickleball"
                        ? "Pickleball"
                        : session.sportName}
                  </span>
                  <span>Còn {places(session)} chỗ</span>
                </div>
                <h3>{session.courtName}</h3>
                <ul>
                  <li>
                    <MapPin size={16} aria-hidden="true" />
                    {session.venueName}
                  </li>
                  <li>
                    <CalendarDays size={16} aria-hidden="true" />
                    {dateLabel(session.sessionDate)}
                  </li>
                  <li>
                    <Clock size={16} aria-hidden="true" />
                    {timeLabel(session)}
                  </li>
                </ul>
                {session.band && (
                  <p className="player-session-band">Trình độ {session.band}</p>
                )}
                <div className="player-session-bottom">
                  <span>
                    {Number.isFinite(session.priceVnd) && (
                      <>
                        <strong>
                          {new Intl.NumberFormat("vi-VN", {
                            style: "currency",
                            currency: "VND",
                          }).format(session.priceVnd)}
                        </strong>
                        <small> / người</small>
                      </>
                    )}
                  </span>
                  <button
                    className="player-text-link"
                    onClick={() => onSelectSession(session.id)}
                    aria-label={`Xem chi tiết buổi chơi tại ${session.courtName}, ngày ${session.sessionDate}`}
                  >
                    Chi tiết <ArrowUpRight size={18} aria-hidden="true" />
                  </button>
                </div>
              </article>
            ))}
          </div>
        </section>
      )}
      {(tickets !== null || ticketError) && (
        <section
          className="player-container player-section player-participation"
          aria-labelledby="player-tickets-title"
        >
          <div className="player-section-heading">
            <h2 id="player-tickets-title">Buổi chơi của bạn</h2>
            {tickets?.length > 0 && (
              <button className="player-text-link" onClick={onTickets}>
                Tất cả vé <ArrowUpRight size={18} aria-hidden="true" />
              </button>
            )}
          </div>
          {tickets?.length > 0 ? (
            <ul className="player-ticket-list">
              {tickets.map((ticket) => (
                <li key={ticket.id}>
                  <span className="player-ticket-icon">
                    <Ticket size={24} aria-hidden="true" />
                  </span>
                  <div>
                    <h3>Buổi chơi #{ticket.sessionId}</h3>
                    <p>
                      {dateLabel(ticket.sessionDate)} <span>·</span>{" "}
                      {timeLabel(ticket)}
                    </p>
                  </div>
                  <span className="player-ticket-confirmed">Vé hợp lệ</span>
                  <button
                    className="player-text-link"
                    onClick={onTickets}
                    aria-label={`Xem vé buổi chơi ${ticket.sessionId}`}
                  >
                    Xem vé <ArrowUpRight size={18} aria-hidden="true" />
                  </button>
                </li>
              ))}
            </ul>
          ) : (
            <div className="player-empty">
              <Ticket size={28} aria-hidden="true" />
              <div>
                <h3>
                  {ticketError
                    ? "Chưa tải được lịch chơi của bạn"
                    : "Bạn chưa có vé cho buổi chơi sắp tới"}
                </h3>
                <p>
                  {ticketError
                    ? "Mở Vé của tôi để kiểm tra lại lịch tham gia."
                    : "Tìm một buổi chơi phù hợp để bắt đầu."}
                </p>
              </div>
              <button
                className="player-text-link"
                onClick={ticketError ? onTickets : onBrowse}
              >
                {ticketError ? "Mở vé của tôi" : "Tìm buổi chơi"}
                <ArrowUpRight size={18} aria-hidden="true" />
              </button>
            </div>
          )}
        </section>
      )}
    </>
  );
}
