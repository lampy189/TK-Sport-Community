import { request } from '../../api';
import { workflowRequest } from '../workflowClient';

export const storeClient = {
  getProducts: async () => {
    const response = await request('/api/v1/store/products', {
      cache: 'no-store',
      credentials: 'same-origin',
    });
    if (!response.ok) {
      const data = await response.json().catch(() => ({}));
      throw new Error(data.message || 'Không thể tải danh sách sản phẩm đang mở bán.');
    }
    return response.json();
  },
  lookupBarcode: code => workflowRequest(`/api/stock/lookup?code=${encodeURIComponent(code)}`),
  createCart: async (items, idempotencyKey) => {
    const response = await request('/api/v1/store/orders/cart', {
      method: 'POST',
      credentials: 'same-origin',
      headers: { 'Content-Type': 'application/json', 'Idempotency-Key': idempotencyKey },
      body: JSON.stringify({ items }),
    });
    if (!response.ok) {
      const data = await response.json().catch(() => ({}));
      throw new Error(data.message || 'Không thể tạo giỏ hàng / đơn hàng mới.');
    }
    return response.json();
  },
};
