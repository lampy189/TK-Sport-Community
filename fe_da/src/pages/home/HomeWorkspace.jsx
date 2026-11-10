import RevenuePage from "../revenue/RevenuePage";
import SportCenterPersonnel from "../revenue/SportCenterPersonnel";
import MessagesPage from "../messages/MessagesPage";
import SkillReviews from "../skill/SkillReviews";
import TicketOperations from "../payment/TicketOperations";
import AssessmentPage from "../skill/AssessmentPage";
import GoodsPaymentPage from "../payment/GoodsPaymentPage";
import SectionLayout from "../../components/layout/SectionLayout";
import { BrowseSessionsView } from "../../components/BrowseSessionsView";
import { SessionDetailView } from "../../components/SessionDetailView";
import CheckInPage from "../payment/CheckInPage";
import SkillProfileView from "../../components/SkillProfileView";
import PersonalProfileView from "../../components/PersonalProfileView";
import { CommunityView } from "../../components/CommunityView";
import PaymentPage from "../payment/PaymentPage";
import StockPage from "../stock/StockPage";
import RefereeSuppliesView from "../stock/RefereeSuppliesView";
import MyGoodsOrders from "../stock/MyGoodsOrders";
import StorePage from "../store/StorePage";
import { Construction, ArrowLeft } from "lucide-react";
import { LateEntryManagement } from "../late-entry/LateEntrySales";

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
  if (!preview && ['staff','manager'].includes(role) && active === 'Vé giờ lẻ') return <LateEntryManagement/>;
  if (!preview && ["owner", "manager", "staff"].includes(role) && ["Kinh doanh", "Thống kê doanh số"].includes(active)) return <RevenuePage />;
  if (!preview && role === "owner" && active === "Phân quyền Sport Center") return <SportCenterPersonnel />;
  if (active === "Tin nhắn" && role !== "guest") return <MessagesPage preview={preview} />;
  if (!preview && ["referee", "manager"].includes(role) && active === "Ngoại lệ trình độ") return <SkillReviews role={role} kind="exceptions" />;
  if (!preview && ["referee", "manager"].includes(role) && active === "Khiếu nại đánh giá") return <SkillReviews role={role} />;
  if (!preview && role === "referee" && active === "Đánh giá kỹ năng") return <AssessmentPage />;
  if (!preview && role === "staff" && active === "Bán hàng")
    return (
      <>
        <StorePage role="staff" user={user} />
        <GoodsPaymentPage />
      </>
    );
  if (!preview && ["referee", "manager"].includes(role) && active === "Điểm danh") return <CheckInPage canManageLateEntries={role === "manager"} />;
  if (!preview && role === "referee" && active === "Vật tư buổi chơi") return <RefereeSuppliesView />;
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
          onOpenCommunity={() =>
            role === "guest" ? onLogin?.() : onOpen("Cộng đồng")
          }
        />
      );
    if (role === "player" && active === "Hồ sơ cá nhân")
      return (
        <PersonalProfileView
          user={user}
          onNavigateSkill={() => onOpen("Hồ sơ kỹ năng")}
          onNavigateCommunity={() => onOpen("Cộng đồng")}
        />
      );
    if (role === "player" && active === "Cửa hàng")
      return (
        <>
          <StorePage role="player" user={user} />
          <MyGoodsOrders />
        </>
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
    if (role === "player" && active === "Cộng đồng")
      return (
        <CommunityView
          user={user}
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
  if (!preview && ["staff", "manager"].includes(role) && active === "Bán hàng")
    return <StorePage role="staff" user={user} />;
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
