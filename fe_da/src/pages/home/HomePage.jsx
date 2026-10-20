import { useState } from "react";
import HomeLayout from "../../layouts/HomeLayout";
import SectionLayout from "../../components/layout/SectionLayout";
import { roleHomeConfig } from "../../config/roleHomeConfig";

export default function HomePage({ role = "guest" }) {
  const config = roleHomeConfig[role] || roleHomeConfig.guest;
  const [activeId, setActiveId] = useState("overview");
  const navigation = [
    { id: "overview", label: "Trang chủ" },
    ...config.modules.map((label, index) => ({ id: `module-${index}`, label })),
  ];
  const selected = navigation.find((item) => item.id === activeId);
  const openModule = (id) => {
    setActiveId(id);
    requestAnimationFrame(() =>
      document
        .getElementById("workspace")
        ?.scrollIntoView({
          behavior: window.matchMedia("(prefers-reduced-motion: reduce)")
            .matches
            ? "instant"
            : "smooth",
          block: "start",
        }),
    );
  };
  return (
    <HomeLayout
      roleLabel={config.label}
      navigation={navigation}
      activeId={activeId}
      onNavigate={openModule}
      hero={{
        ...config,
        action: (
          <button
            className="button button-primary"
            onClick={() => openModule("module-0")}
          >
            {config.action}
            <span aria-hidden="true">↗</span>
          </button>
        ),
      }}
    >
      <div className="stats-grid" aria-label="Số liệu minh họa">
        {config.stats.map(([value, label], index) => (
          <div className="stat-card" key={label}>
            <span className="stat-index">0{index + 1} /</span>
            <strong>{value}</strong>
            <span>{label}</span>
          </div>
        ))}
      </div>
      <div className="workspace-grid" id="workspace">
        <SectionLayout
          title={activeId === "overview" ? config.section : selected.label}
          description="Không gian làm việc theo vai trò của bạn."
        >
          {activeId === "overview" ? (
            <div className="module-grid">
              {config.modules.map((label, index) => (
                <button
                  className="module-card"
                  key={label}
                  onClick={() => openModule(`module-${index}`)}
                >
                  <span className="module-number">0{index + 1}</span>
                  <strong>{label}</strong>
                  <span className="module-bottom">
                    Mở không gian <span aria-hidden="true">↗</span>
                  </span>
                </button>
              ))}
            </div>
          ) : (
            <div className="module-placeholder" role="status">
              <span className="eyebrow">KHÔNG GIAN CHỨC NĂNG</span>
              <h3>{selected.label}</h3>
              <p>
                Đây là khu vực dành cho nội dung {selected.label.toLowerCase()}.
                Chức năng nghiệp vụ sẽ được bổ sung trong màn hình tương ứng.
              </p>
              <button
                className="button button-secondary"
                onClick={() => setActiveId("overview")}
              >
                ← Về tổng quan
              </button>
            </div>
          )}
        </SectionLayout>
        <SectionLayout
          title="Gợi ý tiếp theo"
          description="Sẵn sàng cho một ngày hiệu quả."
          className="next-steps"
        >
          <ol className="task-list">
            {config.tasks.map((task) => (
              <li key={task}>{task}</li>
            ))}
          </ol>
          <div className="sport-note">
            <span className="eyebrow">ONE COMMUNITY. TWO SPORTS.</span>
            <h3>Cùng nhau tiến bộ.</h3>
            <p>
              Cầu lông hay pickleball, mỗi lần ra sân đều là một khởi đầu mới.
            </p>
          </div>
        </SectionLayout>
      </div>
      <p className="demo-note">
        Bản mẫu giao diện · Toàn bộ số liệu là minh họa, chưa kết nối dữ liệu
        thực tế.
      </p>
    </HomeLayout>
  );
}
