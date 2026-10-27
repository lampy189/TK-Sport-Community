import { useState } from "react";
import HomeLayout from "../../layouts/HomeLayout";
import { roleHomeConfig } from "../../config/roleHomeConfig";
import HomeOverview from "./HomeOverview";
import HomeWorkspace from "./HomeWorkspace";
import GuestLayout from "../../components/layout/GuestLayout";
import GuestHome from "./GuestHome";
import PlayerLayout from "../../components/layout/PlayerLayout";
import PlayerHome from "./PlayerHome";

export default function HomePage({
  role = "guest",
  user,
  onLogout,
  onLogin,
  preview = false,
}) {
  const config = roleHomeConfig[role] || roleHomeConfig.guest;
  const [active, setActive] = useState(() =>
    role === "player" &&
    new URLSearchParams(window.location.search).has("orderId")
      ? "Thanh toán"
      : "overview",
  );
  const [sessionId, setSessionId] = useState(null);
  const [browseSport, setBrowseSport] = useState("ALL");
  const navigation = [
    { id: "overview", label: "Trang chủ" },
    ...config.modules.map((label) => ({ id: label, label })),
  ];
  function open(label) {
    if (label !== "overview" && !config.modules.includes(label)) return;
    setActive(label);
    if (!preview && label !== "Thanh toán") {
      const url = new URL(window.location.href);
      ["orderId", "code", "id", "cancel", "status"].forEach((key) =>
        url.searchParams.delete(key),
      );
      window.history.replaceState({}, "", url);
    }
    window.scrollTo({ top: 0, behavior: "instant" });
  }
  function payment(id) {
    const url = new URL(window.location.href);
    if (id) url.searchParams.set("orderId", id);
    window.history.replaceState({}, "", url);
    setActive("Thanh toán");
    window.scrollTo({ top: 0, behavior: "instant" });
  }
  function selectSession(id) {
    setSessionId(id);
    setActive("Chi tiết buổi chơi");
    window.scrollTo({ top: 0, behavior: "instant" });
  }
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
          />
        )}
      </GuestLayout>
    );
  }
  if (role === "player") {
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
            onBrowse={() => open("Lịch social")}
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
            />
          </div>
        )}
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
      {active === "overview" ? (
        <HomeOverview config={config} preview={preview} onOpen={open} />
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
