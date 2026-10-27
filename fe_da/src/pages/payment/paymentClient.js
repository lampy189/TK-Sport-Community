import { request as authenticatedRequest } from "../../api";
// Real checkout is the default. Simulation must be enabled explicitly.
export const demoMode = import.meta.env.VITE_PAYMENT_MODE === "demo";
const storageKey = "tksc-payment-demo-v1";
export function newDemoOrder() {
  return {
    id: 1, orderCode: "SOC-DEMO-001", totalAmount: 120000, status: "pending",
    expiresAt: new Date(Date.now() + 15 * 60000).toISOString(), payments: [],
    tickets: [{ id: 1, orderId: 1, playerId: 1, sessionId: 1, amountVnd: 120000, status: "reserved" }],
  };
}
function readDemo() {
  try {
    const value = JSON.parse(localStorage.getItem(storageKey));
    if (value?.id === 1 && Array.isArray(value.payments) && Array.isArray(value.tickets)) return value;
  } catch { /* Corrupted or unavailable storage starts a new preview. */ }
  return newDemoOrder();
}
function saveDemo(order) {
  localStorage.setItem(storageKey, JSON.stringify(order));
  return order;
}
async function request(path, body) {
  const response = await authenticatedRequest(`/api/v1/commerce/payment/${path}`, {
    credentials: "same-origin",
    headers: { "Content-Type": "application/json", "X-TKSC-Request": "1" },
    ...(body === undefined ? {} : { method: "POST", body: JSON.stringify(body) }),
  });
  if (!response.ok) {
    const data = await response.json().catch(() => ({}));
    throw new Error(data.message || ({
      401: "Vui lòng đăng nhập để tiếp tục.", 403: "Bạn không có quyền xem nội dung này.",
      404: "Không tìm thấy đơn của bạn.", 409: "Trạng thái đơn đã thay đổi. Vui lòng tải lại.",
    }[response.status] || "Không kết nối được dịch vụ thanh toán. Vui lòng thử lại."));
  }
  return response.json();
}
export const paymentClient = {
  display: (id) => demoMode ? Promise.resolve(saveDemo(readDemo())) : request(`orders/${id}`),
  refresh: (id) => demoMode ? Promise.resolve(saveDemo(readDemo())) : request(`orders/${id}/refresh`, {}),
  tickets: () => demoMode ? Promise.resolve(readDemo().tickets.map(t => ({
    ...t, valid: t.status === "confirmed" && readDemo().status === "paid",
  }))) : request("tickets"),
  reviews: () => demoMode ? Promise.resolve(readDemo().payments.filter(p => p.status === "review_required")
    .map(p => ({ ...p, orderId: 1, reasons: [{ newValue: readDemo().reviewReason }] }))) : request("reviews"),
  process: async (id) => {
    if (!demoMode) return request(`orders/${id}/process`, {});
    const order = readDemo();
    if (order.status !== "pending" || Date.parse(order.expiresAt) <= Date.now())
      throw new Error("Đơn đã hết hạn hoặc không còn đủ điều kiện thanh toán.");
    if (!order.payments.some(p => p.status === "pending")) order.payments.unshift({
      id: Date.now(), referenceCode: `DEMO${Date.now()}`, status: "pending", amountVnd: order.totalAmount,
    });
    saveDemo(order);
    return order.payments[0];
  },
  review: async (id, reason) => {
    if (!demoMode) return request(`orders/${id}/review`, { reason });
    const order = readDemo();
    if (order.status === "paid" || !order.payments.length) throw new Error("Không có giao dịch cần kiểm tra.");
    order.status = "review_required";
    order.payments[0].status = "review_required";
    order.reviewReason = reason;
    return saveDemo(order);
  },
  simulate: async (outcome) => {
    if (!demoMode) throw new Error("Chỉ khả dụng trong bản minh họa.");
    const order = readDemo();
    if (order.status !== "pending" || order.payments[0]?.status !== "pending")
      throw new Error("Giao dịch đã được xử lý.");
    const status = outcome === "success"
      ? (Date.parse(order.expiresAt) <= Date.now() ? "review_required" : "paid")
      : outcome === "mismatch" ? "review_required" : "failed";
    order.payments[0].status = status;
    order.status = status === "failed" ? "pending" : status;
    if (status === "review_required") order.reviewReason = "Giao dịch sai số tiền hoặc xác nhận sau hạn thanh toán.";
    if (status === "paid") order.tickets = order.tickets.map(t => ({
      ...t, status: "confirmed", ticketCode: `DEMO-TK-${t.id}`,
    }));
    return saveDemo(order);
  },
  reset: () => saveDemo(newDemoOrder()),
};
