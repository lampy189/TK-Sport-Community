import { useEffect, useState } from "react";
import {
  ArrowRight,
  ArrowUpRight,
  ArrowDownRight,
  CalendarDays,
  Clock,
  MapPin,
} from "lucide-react";
import { api } from "../../api";

const imageRoot = "/images/community/";

function SportPhoto({
  name,
  alt,
  className = "",
  priority = false,
  sizes = "(max-width: 700px) 100vw, 50vw",
}) {
  return (
    <img
      className={className}
      src={`${imageRoot}${name}-960.webp`}
      srcSet={`${imageRoot}${name}-640.webp 640w, ${imageRoot}${name}-960.webp 960w, ${imageRoot}${name}-1600.webp 1600w`}
      sizes={sizes}
      width="1600"
      height={
        name === "hero"
          ? "2400"
          : name === "pickleball"
            ? "900"
            : name === "community"
              ? "1200"
              : "1067"
      }
      alt={alt}
      loading={priority ? "eager" : "lazy"}
      fetchPriority={priority ? "high" : "auto"}
      decoding="async"
    />
  );
}

// Editorial selection only: the server remains authoritative for availability and registration.
function upcomingSessions(sessions) {
  if (!Array.isArray(sessions)) return [];
  const now = Date.now();
  return sessions
    .filter((session) => {
      const start = new Date(
        `${session.sessionDate}T${session.startTime || session.timeLabel?.split(" - ")[0] || "00:00:00"}+07:00`,
      ).getTime();
      const remaining =
        session.availableSlots ?? session.capacity - session.occupiedPlaces;
      return Number.isFinite(start) && start > now && remaining > 0;
    })
    .sort((a, b) =>
      `${a.sessionDate} ${a.startTime || a.timeLabel}`.localeCompare(
        `${b.sessionDate} ${b.startTime || b.timeLabel}`,
      ),
    )
    .slice(0, 3);
}

function FeaturedSessions({ onBrowse, onSelectSession, preview }) {
  const [sessions, setSessions] = useState([]);
  useEffect(() => {
    if (preview) return;
    let active = true;
    api
      .getSessions()
      .then((data) => {
        if (active) setSessions(upcomingSessions(data));
      })
      .catch(() => {
        // Optional editorial section: show no invented cards when data is unavailable.
      });
    return () => {
      active = false;
    };
  }, [preview]);
  if (!sessions.length) return null;
  return (
    <section
      className="guest-featured guest-container"
      aria-labelledby="featured-title"
    >
      <div className="guest-section-heading">
        <div>
          <span className="guest-kicker">HẸN NHAU TRÊN SÂN</span>
          <h2 id="featured-title">Buổi chơi sắp diễn ra</h2>
        </div>
        <button className="guest-text-link" onClick={() => onBrowse()}>
          Xem toàn bộ lịch <ArrowUpRight size={18} />
        </button>
      </div>
      <div className="guest-session-grid">
        {sessions.map((session) => {
          const sport =
            session.sportName === "Badminton" ? "Cầu lông" : "Pickleball";
          const remaining =
            session.availableSlots ?? session.capacity - session.occupiedPlaces;
          return (
            <article className="guest-session" key={session.id}>
              <div className="guest-session-top">
                <span>{sport}</span>
                <span className="guest-availability">Còn {remaining} chỗ</span>
              </div>
              <h3>{session.courtName}</h3>
              <ul>
                <li>
                  <MapPin size={15} />
                  <span>{session.venueName}</span>
                </li>
                <li>
                  <CalendarDays size={15} />
                  <span>
                    {new Date(
                      `${session.sessionDate}T12:00:00+07:00`,
                    ).toLocaleDateString("vi-VN", {
                      day: "2-digit",
                      month: "2-digit",
                      year: "numeric",
                      timeZone: "Asia/Ho_Chi_Minh",
                    })}
                  </span>
                </li>
                <li>
                  <Clock size={15} />
                  <span>
                    {session.timeLabel ||
                      `${session.startTime} – ${session.endTime}`}
                  </span>
                </li>
              </ul>
              <span className="guest-session-band">
                {session.band ? `Trình độ ${session.band}` : "Mọi trình độ"}
              </span>
              <div className="guest-session-bottom">
                <p>
                  <strong>
                    {new Intl.NumberFormat("vi-VN", {
                      style: "currency",
                      currency: "VND",
                    }).format(session.priceVnd)}
                  </strong>
                  <span>/ người</span>
                </p>
                <button
                  onClick={() => onSelectSession(session.id)}
                  aria-label={`Xem buổi ${sport} tại ${session.courtName}, ngày ${session.sessionDate}`}
                >
                  <ArrowUpRight size={22} />
                </button>
              </div>
            </article>
          );
        })}
      </div>
    </section>
  );
}

export default function GuestHome({
  onBrowse,
  onSelectSession,
  onLogin,
  preview = false,
}) {
  return (
    <>
      <section className="guest-hero" aria-labelledby="guest-title">
        <div className="guest-hero-photo">
          <SportPhoto
            name="hero"
            alt="Vận động viên bật nhảy thực hiện cú đánh cầu trên sân trong nhà"
            priority
            sizes="(max-width: 700px) 100vw, 58vw"
          />
        </div>
        <div className="guest-container guest-hero-inner">
          <div className="guest-hero-copy">
            <span className="guest-kicker">
              <span aria-hidden="true" /> CẦU LÔNG & PICKLEBALL
            </span>
            <h1 id="guest-title">
              Một trận hay.
              <br />
              <em>Nhiều kết nối.</em>
            </h1>
            <p>
              Tìm buổi chơi hợp trình. Gặp những người cùng đam mê.
              <br className="guest-desktop-break" /> Và tìm thêm một lý do để ra
              sân mỗi ngày.
            </p>
            <button className="guest-cta" onClick={() => onBrowse()}>
              Khám phá buổi chơi <ArrowUpRight size={20} />
            </button>
            <a className="guest-hero-discover" href="#guest-sports">
              Tìm bộ môn của bạn <ArrowDownRight size={17} />
            </a>
          </div>
        </div>
      </section>

      <section
        className="guest-sports guest-container"
        id="guest-sports"
        tabIndex={-1}
        aria-labelledby="sports-title"
      >
        <div className="guest-section-heading">
          <div>
            <span className="guest-kicker">CHỌN NHỊP CHƠI CỦA BẠN</span>
            <h2 id="sports-title">
              Hai bộ môn.
              <br />
              Một tinh thần ra sân.
            </h2>
          </div>
          <p>
            Những pha cầu tốc độ hay những đường bóng đầy bất ngờ. Tìm niềm vui
            vận động theo cách của bạn.
          </p>
        </div>
        <div className="guest-sport-grid">
          <button
            className="guest-sport guest-badminton"
            aria-label="Khám phá buổi chơi cầu lông"
            onClick={() => onBrowse("Badminton")}
          >
            <SportPhoto
              name="badminton"
              alt="Hai người chơi cầu lông trên sân xanh, nhìn qua khung vợt"
            />
            <span className="guest-sport-type">BADMINTON</span>
            <span className="guest-sport-copy">
              <strong>Cầu lông</strong>
              <span>
                Bứt tốc từng pha cầu.
                <br />
                Ăn ý từng nhịp đánh.
              </span>
              <span className="guest-sport-link">
                Tìm buổi cầu lông <ArrowUpRight size={23} />
              </span>
            </span>
          </button>
          <button
            className="guest-sport guest-pickleball"
            aria-label="Khám phá buổi chơi pickleball"
            onClick={() => onBrowse("Pickleball")}
          >
            <SportPhoto
              name="pickleball"
              alt="Bốn người chơi pickleball trên sân xanh nhìn từ trên cao"
            />
            <span className="guest-sport-type">PICKLEBALL</span>
            <span className="guest-sport-copy">
              <strong>Pickleball</strong>
              <span>
                Nhịp chơi mới.
                <br />
                Niềm vui không khoảng cách.
              </span>
              <span className="guest-sport-link">
                Tìm buổi pickleball <ArrowUpRight size={23} />
              </span>
            </span>
          </button>
        </div>
      </section>

      <FeaturedSessions
        onBrowse={onBrowse}
        onSelectSession={onSelectSession}
        preview={preview}
      />

      <section
        className="guest-community"
        id="guest-community"
        tabIndex={-1}
        aria-labelledby="community-title"
      >
        <div className="guest-container guest-community-inner">
          <div className="guest-community-photo">
            <SportPhoto
              name="community"
              alt="Những người chơi cùng giao lưu pickleball đôi ngoài trời"
            />
          </div>
          <div className="guest-community-copy">
            <span className="guest-kicker">TINH THẦN TK SPORT</span>
            <h2 id="community-title">
              Đến vì thể thao.
              <br />
              <em>Ở lại vì đồng đội.</em>
            </h2>
            <p>
              Có những kết nối bắt đầu từ một đường chuyền. Có những tiến bộ đến
              từ việc cùng nhau thử lại.
            </p>
            <p>
              TK Sport là không gian để bạn tìm bạn chơi, trao đổi kinh nghiệm
              và tận hưởng mỗi lần ra sân — dù mới bắt đầu hay đã có nhiều mùa
              gắn bó.
            </p>
            <button className="guest-text-link" onClick={onLogin}>
              Kết nối cùng TK Sport <ArrowRight size={19} />
            </button>
          </div>
        </div>
      </section>
    </>
  );
}
