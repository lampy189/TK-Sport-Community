import TicketOperations from "../payment/TicketOperations";
import AssessmentPage from "../skill/AssessmentPage";
import GoodsPaymentPage from "../payment/GoodsPaymentPage";
import SectionLayout from "../../components/layout/SectionLayout";
import { BrowseSessionsView } from "../../components/BrowseSessionsView";
import { SessionDetailView } from "../../components/SessionDetailView";
import CheckInPage from "../payment/CheckInPage";
import SkillProfileView from "../../components/SkillProfileView";
import PaymentPage from "../payment/PaymentPage";
import StockPage from "../stock/StockPage";
import { Construction, ArrowLeft } from "lucide-react";

export default function HomeWorkspace({
  role,
  active,
  user,
  preview,
  sessionId,
  onSelectSession,
  onOpen,
  onPayment,
  onLogin,
  initialSport = "ALL",
  onBrowseSport,
}) {
  if (!preview && role === "referee" && active === "Đánh giá kỹ năng") return <AssessmentPage />;
  if (!preview && role === "staff" && active === "Bán hàng") return <GoodsPaymentPage />;
  if (!preview && ["referee", "manager"].includes(role) && active === "Điểm danh") return <CheckInPage />;
  if (!preview && ["player", "guest"].includes(role)) {
    if (active === "Lịch social")
      return (
        <BrowseSessionsView
          key={initialSport}
          initialSport={initialSport}
          user={user}
          onSelectSession={onSelectSession}
          onNavigate={(view) => {
            if (view === "tickets") {
              if (role === "guest") onLogin?.();
              else onOpen("Vé của tôi");
            } else if (view === "skills") {
              if (role === "guest") onLogin?.();
              else onOpen("Hồ sơ kỹ năng");
            } else {
              onOpen("Lịch social");
            }
          }}
        />
      );
    if (active === "Chi tiết buổi chơi")
      return (
        <SessionDetailView
          sessionId={sessionId}
          user={user}
          onBack={() => onOpen("Lịch social")}
          onPayment={onPayment}
          onOpenLogin={onLogin}
          onOpenSkills={() =>
            role === "guest" ? onLogin?.() : onOpen("Hồ sơ kỹ năng")
          }
        />
      );
    if (role === "player" && active === "Hồ sơ kỹ năng")
      return (
        <SkillProfileView
          user={user}
          onBrowseSport={(sport) => {
            if (onBrowseSport) onBrowseSport(sport);
            else onOpen("Lịch social");
          }}
        />
      );
    if (role === "player" && ["Thanh toán", "Vé của tôi"].includes(active))
      return (
        <PaymentPage
          onBrowse={() => onOpen("Lịch social")}
          key={active}
          initialView={active === "Vé của tôi" ? "tickets" : "payment"}
        />
      );
  }
  if (
    !preview &&
    ["staff", "manager"].includes(role) &&
    ["Thanh toán", "Sự cố sân", "Đổi & hoàn vé"].includes(active)
  )
    return <TicketOperations key={active} initialTab={active === "Sự cố sân" ? "sessions" : active === "Đổi & hoàn vé" ? "changes" : "reviews"} />;
  if (!preview && role === "staff" && active === "Kho & vật tư")
    return <StockPage />;
  return (
    <SectionLayout
      title={active}
      description="Không gian chức năng theo vai trò của bạn."
    >
      <div className="module-placeholder" role="status">
        <span className="state-icon">
          <Construction size={28} />
        </span>
        <span className="eyebrow">ĐANG HOÀN THIỆN</span>
        <h3>Không gian mới đang được chuẩn bị</h3>
        <p>
          {active} chưa được triển khai. Hẹn gặp bạn khi tính năng sẵn sàng.
        </p>
        <button
          className="button button-secondary"
          onClick={() => onOpen("overview")}
        >
          <ArrowLeft size={16} /> Về trang chủ
        </button>
      </div>
    </SectionLayout>
  );
}
