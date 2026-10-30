import { useState } from "react";
import { ArrowUpRight, ArrowRight, Mail, Lightbulb } from "lucide-react";
import { api } from "../api";
import "./player-signin.css";

const BADMINTON_TIPS = [
  "Khởi động kỹ cổ tay, khớp gối và cổ chân ít nhất 10 phút trước khi vào sân để phòng ngừa chấn thương dây chằng.",
  "Sau mỗi pha đánh cầu, hãy lập tức bật nhẹ một bước chân (Split step) và trở về vị trí trung tâm sân (Center court).",
  "Trong đánh đôi, người đứng trên lưới nên giơ mặt vợt ngang ngực để sẵn sàng phản tạt và chụp lưới bất ngờ.",
  "Cầm vợt thả lỏng theo kiểu bắt tay (Universal Grip); chỉ siết chặt ngón tay đúng vào tích tắc mặt vợt chạm cầu.",
  "Khi đối phương chuẩn bị đập cầu (Smash), hãy hạ thấp trọng tâm, hai chân mở rộng hơn vai và đưa vợt ra phía trước.",
  "Đừng lạm dụng smash liên tục; đan xen những quả chém cầu (Drop shot) sát lưới để ép đối thủ mất thăng bằng.",
  "Bộ chân (Footwork) chuẩn là bí quyết số một giúp bạn đón cầu sớm, vào đúng tư thế và phát lực trọn vẹn nhất.",
  "Giữ mắt quan sát chuyển động cổ tay và vai của đối phương khi chuẩn bị đánh để phán đoán hướng cầu sớm nửa nhịp.",
  "Giữ mặt vợt hơi úp khi thực hiện cú đè cầu (Drive) tầm trung để cầu bay sát mép lưới và cắm nhanh về cuối sân.",
  "Trong Pickleball, làm chủ khu vực Non-Volley Zone (Kitchen) bằng các cú Dinking là chìa khóa để giành quyền chủ động.",
  "Hãy thở đều và uống từng ngụm nước nhỏ có bù điện giải sau mỗi set đấu thay vì uống quá nhiều nước cùng một lúc.",
  "Khi giao cầu ngắn, tập trung vào điểm rơi sát vạch trên và giữ quỹ đạo cầu lướt sát mép lưới đối phương.",
  "Khi bị đối phương ép góc trái tay (Backhand), ưu tiên quả phông cầu bổng về góc xa để có thời gian lui về thủ.",
  "Sau khi kết thúc buổi chơi, dành 5–10 phút ép cơ tĩnh (Static Stretching) để cơ bắp phục hồi nhanh hơn.",
  "Chọn mức căng cước phù hợp với lực cổ tay: Người mới nên căng 20–22 lbs, người chơi phong trào nên chọn 22–24 lbs.",
];

export function SignInView({ onLoginSuccess, onContinueAsGuest }) {
  const [customEmail, setCustomEmail] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(
    new URLSearchParams(window.location.search).has("loginError")
      ? "Không thể đăng nhập. Hãy dùng tài khoản Player hoặc chuyển sang cổng đăng nhập đúng vai trò."
      : null,
  );
  const [currentTipIndex, setCurrentTipIndex] = useState(() =>
    Math.floor(Math.random() * BADMINTON_TIPS.length),
  );
  const [tipAnimating, setTipAnimating] = useState(false);

  function handleNextTip() {
    setTipAnimating(true);
    setTimeout(() => {
      let nextIndex;
      do {
        nextIndex = Math.floor(Math.random() * BADMINTON_TIPS.length);
      } while (nextIndex === currentTipIndex && BADMINTON_TIPS.length > 1);
      setCurrentTipIndex(nextIndex);
      setTipAnimating(false);
    }, 150);
  }

  async function handleLogin(email) {
    if (!email) return;
    setLoading(true);
    setError(null);
    try {
      const account = await api.devLogin(email, "player");
      if (onLoginSuccess) {
        onLoginSuccess(account);
      }
    } catch (err) {
      setError(
        err.message ||
          "Đăng nhập không thành công. Hãy chắc chắn Backend đang chạy.",
      );
    } finally {
      setLoading(false);
    }
  }

  return (
    <main className="player-signin">
      <div className="player-signin-card">
        <section className="player-signin-form">
          <button
            className="player-signin-brand"
            onClick={onContinueAsGuest}
            aria-label="TK Sport Community — Trang chủ"
          >
            <span>
              <ArrowUpRight size={25} />
            </span>
            <strong>
              TK SPORT<small>COMMUNITY</small>
            </strong>
          </button>
          <div className="player-login-kicker">KHÔNG GIAN NGƯỜI CHƠI</div>
          <h1>
            Chào mừng trở lại<span>.</span>
          </h1>
          <p className="player-login-intro">
            Buổi chơi tiếp theo bắt đầu từ đây.
            <br />
            Đăng nhập để tìm hội và sẵn sàng ra sân.
          </p>
          {error && (
            <div className="login-error" role="alert">
              {error}
            </div>
          )}
          <div className="login-social">
            <button
              className="button button-secondary"
              onClick={() => api.googleLogin("player")}
            >
              <svg
                width="18"
                height="18"
                viewBox="0 0 24 24"
                aria-hidden="true"
              >
                <path
                  fill="#4285F4"
                  d="M23.745 12.27c0-.7-.06-1.4-.19-2.07H12v4.51h6.6c-.29 1.52-1.14 2.8-2.4 3.65v3h3.88c2.27-2.09 3.665-5.17 3.665-9.09z"
                />
                <path
                  fill="#34A853"
                  d="M12 24c3.24 0 5.95-1.08 7.93-2.91l-3.88-3c-1.08.72-2.45 1.16-4.05 1.16-3.12 0-5.77-2.1-6.72-4.93H1.26v3.09C3.27 21.41 7.35 24 12 24z"
                />
                <path
                  fill="#FBBC05"
                  d="M5.28 14.32c-.25-.72-.38-1.49-.38-2.32s.13-1.6.38-2.32V6.59H1.26C.46 8.19 0 9.99 0 12s.46 3.81 1.26 5.41l4.02-3.09z"
                />
                <path
                  fill="#EA4335"
                  d="M12 4.75c1.77 0 3.35.61 4.6 1.8l3.42-3.42C17.95 1.19 15.24 0 12 0 7.35 0 3.27 2.59 1.26 6.59l4.02 3.09c.95-2.83 3.6-4.93 6.72-4.93z"
                />
              </svg>
              Tiếp tục với Google
            </button>
            <button
              className="button button-secondary"
              onClick={() => api.googleLogin("player")}
            >
              <Mail size={18} /> Tiếp tục với Email
            </button>
          </div>
          <div className="login-divider">
            <span>hoặc đăng nhập bằng email</span>
          </div>
          <form
            className="login-email-form"
            onSubmit={(event) => {
              event.preventDefault();
              handleLogin(customEmail.trim());
            }}
          >
            <label htmlFor="player-email">Địa chỉ email</label>
            <div className="login-input">
              <Mail size={17} aria-hidden="true" />
              <input
                id="player-email"
                autoComplete="email"
                type="email"
                placeholder="Nhập địa chỉ email của bạn"
                value={customEmail}
                onChange={(e) => setCustomEmail(e.target.value)}
              />
            </div>
            <button
              className="button button-primary"
              disabled={loading || !customEmail.trim()}
            >
              {loading ? "Đang xác thực…" : "Đăng nhập vào hệ thống"}
              <ArrowRight size={17} />
            </button>
          </form>
          <button className="player-login-guest" onClick={onContinueAsGuest}>
            Khám phá với tư cách khách <ArrowRight size={15} />
          </button>
          <nav className="player-login-portals" aria-label="Cổng đăng nhập">
            <a href="/staff/login">Đội ngũ vận hành</a>
            <span aria-hidden="true">·</span>
            <a href="/admin/login">Quản trị hệ thống</a>
          </nav>
        </section>
        <aside className="player-signin-story">
          <div className="player-story-content">
            <div className="player-sport-tags">
              <span>Cầu lông</span>
              <span>Pickleball</span>
            </div>
            <div className="player-story-heading">
              <span>YOUR NEXT GAME STARTS HERE</span>
              <h2>
                Ra sân.
                <br />
                Kết nối.
                <br />
                <em>Chơi hết mình.</em>
              </h2>
              <p>
                Một buổi chơi phù hợp.
                <br />
                Những người bạn cùng đam mê.
              </p>
            </div>
          </div>
          <div className="login-court" aria-hidden="true">
            <span />
            <i />
          </div>
          <button
            className={`player-tip-card${tipAnimating ? " is-changing" : ""}`}
            onClick={handleNextTip}
            title="Xem mẹo tiếp theo"
          >
            <span className="tip-heading">
              <Lightbulb size={19} />
              <strong>Mẹo nhỏ trước khi ra sân</strong>
              <ArrowRight size={16} />
            </span>
            <span className="tip-copy">{BADMINTON_TIPS[currentTipIndex]}</span>
            <span className="tip-next">Bấm để xem mẹo tiếp theo</span>
          </button>
        </aside>
      </div>
    </main>
  );
}
