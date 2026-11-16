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
  Adjustment: "Điều chỉnh sau kiểm kê",
};

export const countStatusLabels = {
  Pending: "Chờ duyệt",
  Applied: "Đã điều chỉnh",
  Rejected: "Bị từ chối",
  NoDifference: "Khớp sổ",
};

export const requestStatusLabels = {
  Pending: "Chờ cấp",
  Issued: "Đã cấp",
  Rejected: "Bị từ chối",
  Cancelled: "Đã hủy",
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
  reportSupplyUse: (sessionId, productId, usedQuantity, returnQuantity) =>
    send(
      `referee-supplies/${sessionId}/products/${productId}/usage`,
      "POST",
      { usedQuantity, returnQuantity: returnQuantity ?? null },
    ),
  refereeProducts: () => call("referee-supplies/products"),
  requestSupply: (sessionId, body) =>
    send(`referee-supplies/${sessionId}/requests`, "POST", body),
  cancelSupplyRequest: (sessionId, requestId) =>
    send(`referee-supplies/${sessionId}/requests/${requestId}/cancel`, "POST", {}),
  issueSupplyRequest: (sessionId, requestId, body = {}) =>
    send(`session-supplies/${sessionId}/requests/${requestId}/issue`, "POST", body),
  rejectSupplyRequest: (sessionId, requestId, note) =>
    send(`session-supplies/${sessionId}/requests/${requestId}/reject`, "POST", { note }),
  countSheet: (productId) => call(`products/${productId}/count-sheet`),
  createCount: (body) => send("counts", "POST", body),
  counts: (status) => call(`counts${status ? `?status=${encodeURIComponent(status)}` : ""}`),
  approveCount: (id, expectedDifference, note) =>
    send(`counts/${id}/approve`, "POST", { expectedDifference, note: note || null }),
  rejectCount: (id, note) => send(`counts/${id}/reject`, "POST", { note }),
  supplyReport: (from, to) =>
    call(`reports/supplies?from=${encodeURIComponent(from)}&to=${encodeURIComponent(to)}`),
  reconciliation: () => call("reconciliation"),
  reconcileSupply: (sessionId, productId, returnedQuantity) =>
    send(
      `session-supplies/${sessionId}/products/${productId}/reconcile`,
      "POST",
      { returnedQuantity },
    ),
};
