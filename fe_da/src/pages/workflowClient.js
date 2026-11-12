import { request } from "../api";

export async function workflowRequest(path, method = "GET", body) {
  const response = await request(path, { method, headers: { "X-TKSC-Request": "1" },
    ...(body === undefined ? {} : { body: JSON.stringify(body) }) });
  const data = await response.json().catch(() => ({}));
  if (!response.ok) throw new Error(data.message || ({ 401: 'Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.', 403: 'Bạn không có quyền thực hiện thao tác này.', 404: 'Không tìm thấy dữ liệu.', 409: 'Dữ liệu đã thay đổi. Vui lòng tải lại.', 429: 'Thao tác quá nhanh. Vui lòng thử lại sau.', 503: 'Chức năng tạm thời chưa sẵn sàng. Vui lòng thử lại sau.' }[response.status] || 'Chưa kết nối được dịch vụ. Vui lòng thử lại.'));
  return data;
}
