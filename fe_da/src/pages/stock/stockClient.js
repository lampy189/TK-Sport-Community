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
    throw new Error(
      data?.message ||
        {
          401: "Phiên đăng nhập đã hết. Vui lòng đăng nhập lại.",
          403: "Tài khoản của bạn không có quyền quản lý kho.",
          404: "Không tìm thấy mặt hàng.",
          429: "Thao tác quá nhanh. Vui lòng thử lại sau ít giây.",
        }[response.status] ||
        "Không kết nối được dịch vụ kho. Vui lòng thử lại.",
    );
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
};

export const stockClient = {
  products: () => call("products"),
  create: (product) => send("products", "POST", product),
  update: (id, product) => send(`products/${id}`, "PUT", product),
  move: (id, type, quantity) =>
    send(`products/${id}/movements`, "POST", { type, quantity }),
  movements: (id) => call(`products/${id}/movements`),
};
