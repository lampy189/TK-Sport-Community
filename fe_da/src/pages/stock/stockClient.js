import { request } from "../../api";

async function call(path, options = {}) {
  const response = await request(`/api/stock/${path}`, options);
  const text = await response.text();
  let data;
  try {
    data = text ? JSON.parse(text) : null;
  } catch {
    data = null;
  }
  if (!response.ok) {
    const error = new Error(
      data?.message ||
        {
          401: "Phiên đăng nhập đã hết. Vui lòng đăng nhập lại.",
          403: "Tài khoản của bạn không có quyền quản lý kho.",
          404: "Không tìm thấy mặt hàng.",
          429: "Thao tác quá nhanh. Vui lòng thử lại sau ít giây.",
        }[response.status] ||
        "Không kết nối được dịch vụ kho. Vui lòng thử lại.",
    );
    error.data = data;
    throw error;
  }
  return data;
}

const send = (path, method, body) =>
  call(path, { method, body: JSON.stringify(body) });

export const categoryLabels = {
  Drink: "Đồ uống",
  Food: "Đồ ăn",
  Equipment: "Dụng cụ thi đấu",
  Accessory: "Phụ kiện",
  Apparel: "Trang phục",
  Other: "Khác",
};

export const movementLabels = {
  Receipt: "Nhập kho",
  Issue: "Xuất kho",
  OpeningBalance: "Tồn đầu kỳ",
  SaleOut: "Xuất bán (giao hàng)",
  SessionIssue: "Cấp cho buổi chơi",
  SessionReturn: "Buổi chơi trả lại",
};

export const stockClient = {
  products: () => call("products"),
  create: (product) => send("products", "POST", product),
  update: (id, product) => send(`products/${id}`, "PUT", product),
  move: (id, type, quantity, unitName) =>
    send(`products/${id}/movements`, "POST", { type, quantity, unitName: unitName || null }),
  batchReceipt: (body) => send("receipts/batch", "POST", body),
  movements: (id) => call(`products/${id}/movements`),
  supplySessions: (date) =>
    call(`session-supplies${date ? `?date=${encodeURIComponent(date)}` : ""}`),
  supplyDetails: (sessionId) => call(`session-supplies/${sessionId}`),
  issueSupply: (sessionId, body) =>
    send(`session-supplies/${sessionId}/issue`, "POST", body),
  refereeSessions: (date) =>
    call(`referee-supplies${date ? `?date=${encodeURIComponent(date)}` : ""}`),
  refereeDetails: (sessionId) => call(`referee-supplies/${sessionId}`),
  reportSupplyUse: (sessionId, productId, usedQuantity) =>
    send(
      `referee-supplies/${sessionId}/products/${productId}/usage`,
      "POST",
      { usedQuantity },
    ),
  reconcileSupply: (sessionId, productId, returnedQuantity) =>
    send(
      `session-supplies/${sessionId}/products/${productId}/reconcile`,
      "POST",
      { returnedQuantity },
    ),
};
