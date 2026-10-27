import SectionLayout from "../../components/layout/SectionLayout";
import { ArrowUpRight, Layers3, Activity, ShieldCheck } from "lucide-react";
import ModuleIcon from "../../components/ui/ModuleIcon";

export default function HomeOverview({ config, preview, onOpen }) {
  const facts = preview
    ? config.stats
    : [
        [
          String(config.modules.length).padStart(2, "0"),
          "Không gian chức năng",
        ],
        ["02", "Cầu lông & Pickleball"],
        [config.label, "Vai trò truy cập"],
      ];
  return (
    <>
      <div
        className="stats-grid"
        aria-label={preview ? "Số liệu minh họa" : "Thông tin không gian"}
      >
        {facts.map(([value, label], index) => (
          <div className="stat-card" key={label}>
            <span className="stat-index">
              {index === 0 ? (
                <Layers3 size={20} />
              ) : index === 1 ? (
                <Activity size={20} />
              ) : (
                <ShieldCheck size={20} />
              )}
            </span>
            <strong
              className={index === 2 && !preview ? "stat-role" : undefined}
            >
              {value}
            </strong>
            <span>{label}</span>
          </div>
        ))}
      </div>
      <div className="workspace-grid" id="workspace">
        <SectionLayout
          title={config.section}
          description="Chọn công việc bạn muốn thực hiện."
        >
          <div className="module-grid">
            {config.modules.map((label) => (
              <button
                className="module-card"
                key={label}
                onClick={() => onOpen(label)}
              >
                <span className="module-number">
                  <ModuleIcon label={label} size={23} />
                </span>
                <strong>{label}</strong>
                <span className="module-bottom">
                  Khám phá ngay <ArrowUpRight size={18} aria-hidden="true" />
                </span>
              </button>
            ))}
          </div>
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
      {preview && (
        <p className="demo-note">
          Bản mẫu giao diện · Số liệu chỉ để minh họa.
        </p>
      )}
    </>
  );
}
