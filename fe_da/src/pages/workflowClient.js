import { request } from "../api";

export async function workflowRequest(path, method = "GET", body) {
  const response = await request(path, { method, headers: { "X-TKSC-Request": "1" },
    ...(body === undefined ? {} : { body: JSON.stringify(body) }) });
  const data = await response.json().catch(() => ({}));
  if (!response.ok) throw new Error(data.message || `Không thể thực hiện (HTTP ${response.status}).`);
  return data;
}
