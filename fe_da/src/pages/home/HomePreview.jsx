import { useState } from "react";
import HomePage from "./HomePage";
import { roleHomeConfig } from "../../config/roleHomeConfig";

export default function HomePreview() {
  const [role, setRole] = useState("player");
  return (
    <>
      <div className="preview-bar">
        <span>TKSC · Bản mẫu giao diện</span>
        <label htmlFor="preview-role">Xem theo vai trò</label>
        <select
          id="preview-role"
          value={role}
          onChange={(event) => setRole(event.target.value)}
        >
          {Object.entries(roleHomeConfig).map(([key, config]) => (
            <option key={key} value={key}>
              {config.label}
            </option>
          ))}
        </select>
      </div>
      <HomePage key={role} role={role} preview />
    </>
  );
}
