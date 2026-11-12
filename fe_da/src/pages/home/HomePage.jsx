import SportCenterBadge from "../revenue/SportCenterBadge";
import { useEffect, useState } from "react";

import HomeLayout from "../../layouts/HomeLayout";
import { roleHomeConfig } from "../../config/roleHomeConfig";
import HomeOverview from "./HomeOverview";
import OperationsOverview from '../operations/OperationsOverview';
import HomeWorkspace from "./HomeWorkspace";
import GuestLayout from "../../components/layout/GuestLayout";
import GuestHome from "./GuestHome";
import PlayerLayout from "../../components/layout/PlayerLayout";
import PlayerHome from "./PlayerHome";
import PlayerChatbot from "../../components/PlayerChatbot";

export default function HomePage({
  role = "guest",
  user,
  onLogout,
  onLogin,
  preview = false,
}) {
  const config = roleHomeConfig[role] || roleHomeConfig.guest;
  const [active, setActive] = useState(() =>
    ["owner", "manager", "staff"].includes(role) && new URLSearchParams(window.location.search).get("view") === "revenue" ? (role === "owner" ? "Kinh doanh" : "Thống kê doanh số") :
    role === 'player' && new URLSearchParams(window.location.search).get('view') === 'invitations' ? 'Lời mời buổi chơi' :
    role !== "guest" && ["messages", "notifications"].includes(new URLSearchParams(window.location.search).get("view")) ? "Thông báo" :
    role === "staff" && new URLSearchParams(window.location.search).has("goodsOrderId") ? "Bán hàng" :
    role === "player" &&
    new URLSearchParams(window.location.search).has("orderId")
      ? "Thanh toán"
      : role === "player" && new URLSearchParams(window.location.search).get("view") === "tickets" ? "Vé của tôi" : window.history.state?.tkscHome?.role === role ? window.history.state.tkscHome.active : "overview",
  );
  const [sessionId, setSessionId] = useState(() => window.history.state?.tkscHome?.role === role ? window.history.state.tkscHome.sessionId : null);
  const [browseSport, setBrowseSport] = useState(() => window.history.state?.tkscHome?.role === role ? window.history.state.tkscHome.browseSport : "ALL");
  const navigation = [
    { id: "overview", label: "Trang chủ" },
    ...config.modules.map((label) => ({ id: label, label })),
    ...(role !== "guest" ? [{ id: "Thông báo", label: "Thông báo" }] : []),
  ];
  useEffect(() => {
    // Keep every visited workspace with its detail and filter context.
    const snapshot = { role, active, sessionId, browseSport };
    window.history.replaceState({ ...window.history.state, tkscHome: snapshot }, "");
    function restore(event) {
      const saved = event.state?.tkscHome;
      if (saved?.role !== role) return;
      setActive(saved.active);
      setSessionId(saved.sessionId);
      setBrowseSport(saved.browseSport || "ALL");
    }
    window.addEventListener("popstate", restore);
    return () => window.removeEventListener("popstate", restore);
  }, [role, active, sessionId, browseSport]);
  function remember(next, nextSession = sessionId, url = window.location.href) {
    if (next === active && nextSession === sessionId && String(url) === window.location.href) return;
    window.history.pushState({ tkscHome: { role, active: next, sessionId: nextSession, browseSport } }, "", url);
  }
  function open(rawLabel) {
    const label = rawLabel === "Tin nhắn" ? "Thông báo" : rawLabel;
    if (
      label !== "overview" &&
      label !== "Chi tiết buổi chơi" &&
      label !== "Thông báo" &&
      label !== "Hồ sơ cá nhân" &&
      label !== "Hồ sơ kỹ năng" &&
      !config.modules.includes(label)
    )
      return;
    if (preview || label === "Thanh toán") remember(label);
    setActive(label);
    if (!preview && label !== "Thanh toán") {
      const url = new URL(window.location.href);
      ["orderId", "code", "id", "cancel", "status", "view"].forEach((key) =>
        url.searchParams.delete(key),
      );
      if (label === "Thông báo" || label === "Tin nhắn") url.searchParams.set("view", "messages");
      if (label === 'Lời mời buổi chơi') url.searchParams.set('view', 'invitations');
      if (label === 'Vé của tôi') url.searchParams.set('view', 'tickets');
      if (["Kinh doanh", "Thống kê doanh số"].includes(label)) url.searchParams.set("view", "revenue");
      remember(label, sessionId, url);
    }
    window.scrollTo({ top: 0, behavior: "instant" });
  }
  function payment(id) {
    const url = new URL(window.location.href);
    if (id) url.searchParams.set("orderId", id);
    url.searchParams.delete('view');
    remember('Thanh toán', sessionId, url);
    setActive("Thanh toán");
    window.scrollTo({ top: 0, behavior: "instant" });
  }
  function selectSession(id) {
    remember("Chi tiết buổi chơi", id);
    setSessionId(id);
    setActive("Chi tiết buổi chơi");
    window.scrollTo({ top: 0, behavior: "instant" });
  }
  useEffect(() => {
    const onOpenSessionEvent = (e) => {
      const targetId = e.detail?.sessionId;
      if (targetId) {
        selectSession(targetId);
      }
    };
    window.addEventListener("tksc:open-session", onOpenSessionEvent);
    const onInvitations = () => open('Lời mời buổi chơi');
    window.addEventListener('tksc:open-invitations', onInvitations);
    return () => { window.removeEventListener("tksc:open-session", onOpenSessionEvent); window.removeEventListener('tksc:open-invitations', onInvitations); };
  }, []);
  if (role === "guest") {
    const browse = (sport = "ALL") => {
      setBrowseSport(sport);
      open("Lịch social");
    };
    const login =
      onLogin ||
      (() => window.location.assign("/player/login" + window.location.search));
    return (
      <GuestLayout
        isOverview={active === "overview"}
        onHome={() => open("overview")}
        onBrowse={browse}
        onLogin={login}
      >
        {active === "overview" ? (
          <GuestHome
            onBrowse={browse}
            onSelectSession={selectSession}
            onLogin={login}
            preview={preview}
          />
        ) : (
          <HomeWorkspace
            role={role}
            active={active}
            user={user}
            preview={false}
            sessionId={sessionId}
            onOpen={open}
            onLogin={login}
            onPayment={payment}
            onSelectSession={selectSession}
            initialSport={browseSport}
            onBrowseSport={browse}
          />
        )}
        <PlayerChatbot guest preview={preview} />
      </GuestLayout>
    );
  }
  if (role === "player") {
    const browse = (sport = "ALL") => {
      setBrowseSport(sport);
      open("Lịch social");
    };
    return (
      <PlayerLayout
        navigation={navigation}
        activeId={active === "Chi tiết buổi chơi" ? "Lịch social" : active}
        onNavigate={open}
        onLogout={onLogout}
        accountName={user?.fullName}
      >
        {active === "overview" ? (
          <PlayerHome
            preview={preview}
            onBrowse={() => browse("ALL")}
            onTickets={() => open("Vé của tôi")}
            onSelectSession={selectSession}
          />
        ) : (
          <div className="player-container player-workspace">
            <div className="home-breadcrumb">
              <button onClick={() => open("overview")}>Trang chủ</button>
              <span>/</span>
              <span>{active}</span>
            </div>
            <HomeWorkspace
              role={role}
              active={active}
              user={user}
              preview={preview}
              sessionId={sessionId}
              onOpen={open}
              onLogin={onLogin}
              onPayment={payment}
              onSelectSession={selectSession}
              initialSport={browseSport}
              onBrowseSport={browse}
            />
          </div>
        )}
        <PlayerChatbot preview={preview} />
      </PlayerLayout>
    );
  }
  return (
    <HomeLayout
      role={role}
      roleLabel={config.label}
      accountName={user?.fullName}
      onLogout={onLogout}
      onLogin={onLogin}
      onHome={() => open("overview")}
      navigation={navigation}
      activeId={active === "Chi tiết buổi chơi" ? "Lịch social" : active}
      onNavigate={open}
      hero={
        active === "overview"
          ? {
              ...config,
              action: (
                <button
                  className="button button-primary"
                  onClick={() => open(config.modules[0])}
                >
                  {config.action}
                  <span aria-hidden="true">↗</span>
                </button>
              ),
            }
          : null
      }
    >
      {!preview && ["owner", "manager", "staff", "referee"].includes(role) && <SportCenterBadge />}
      {active === "overview" ? (
        <>{!preview && <OperationsOverview role={role} />}<HomeOverview config={config} preview={preview} onOpen={open} /></>
      ) : (
        <div className="home-workspace">
          <div className="home-breadcrumb">
            <button onClick={() => open("overview")}>Trang chủ</button>
            <span>/</span>
            <span>{active}</span>
          </div>
          <HomeWorkspace
            role={role}
            active={active}
            user={user}
            preview={preview}
            sessionId={sessionId}
            onOpen={open}
            onLogin={onLogin}
            onPayment={payment}
            onSelectSession={selectSession}
          />
        </div>
      )}
    </HomeLayout>
  );
}
