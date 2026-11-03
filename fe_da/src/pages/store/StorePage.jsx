import { useEffect, useMemo, useState } from 'react';
import { storeClient } from './storeClient';
import './store.css';

export default function StorePage({ role = 'staff', user }) {
  const [products, setProducts] = useState([]);
  const [quantities, setQuantities] = useState({});
  const [error, setError] = useState('');
  const [order, setOrder] = useState(null);
  const [busy, setBusy] = useState(false);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let active = true;
    (async () => {
      try {
        const data = await storeClient.getProducts();
        if (active) setProducts(data || []);
      } catch (e) {
        if (active) setError(e.message);
      } finally {
        if (active) setLoading(false);
      }
    })();
    return () => {
      active = false;
    };
  }, []);

  const selectedItems = useMemo(
    () => products
      .filter((product) => (quantities[product.productId] ?? 0) > 0)
      .map((product) => ({
        productId: product.productId,
        name: product.name,
        quantity: quantities[product.productId],
        price: product.unitPriceVnd,
        subtotal: product.unitPriceVnd * (quantities[product.productId] ?? 0),
      })),
    [products, quantities],
  );

  const totalAmount = selectedItems.reduce((sum, item) => sum + item.subtotal, 0);

  function updateQuantity(productId, delta) {
    setOrder(null);
    setError('');
    setQuantities((current) => {
      const next = Number(current[productId] ?? 0) + delta;
      if (next <= 0) {
        const { [productId]: _removed, ...rest } = current;
        return rest;
      }
      return { ...current, [productId]: next };
    });
  }

  async function submitOrder() {
    const items = selectedItems.map((item) => ({
      productId: item.productId,
      quantity: item.quantity,
    }));

    if (!items.length) {
      setError('Vui lòng chọn ít nhất một món hàng trước khi tạo đơn.');
      return;
    }

    setBusy(true);
    setError('');
    try {
      const result = await storeClient.createCart(items);
      setOrder(result);
      setQuantities({});
    } catch (e) {
      setError(e.message);
    } finally {
      setBusy(false);
    }
  }

  const title = role === 'player' ? 'Cửa hàng / Đặt đồ' : 'Bán hàng tại quầy';
  const subtitle = role === 'player'
    ? 'Chọn sản phẩm và tạo đơn hàng cho bước thanh toán sau.'
    : 'Nhân viên tạo đơn hàng tại quầy, kiểm tra tồn kho và lưu giỏ hàng tạm.';

  return (
    <section className="store-page">
      <div className="store-header">
        <div>
          <h2>{title}</h2>
          <p>{subtitle}</p>
        </div>
        <span className="payment-badge pending">{user?.fullName || 'Người dùng'}</span>
      </div>

      {error && <div className="store-error" role="alert">{error}</div>}

      {order && (
        <div className="store-order-result">
          <h4>Đơn hàng đã được tạo</h4>
          <p><strong>Mã đơn:</strong> {order.orderCode}</p>
          <p><strong>Tổng tiền:</strong> {new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(order.totalAmount)}</p>
          <p><strong>Trạng thái:</strong> {order.status} · {order.paymentState}</p>
        </div>
      )}

      {loading ? (
        <p>Đang tải danh sách sản phẩm…</p>
      ) : (
        <div className="store-grid">
          {products.length === 0 ? (
            <div className="store-card">
              <h4>Chưa có sản phẩm mở bán</h4>
              <p>Hiện chưa có mặt hàng nào đang được mở bán trong kho.</p>
            </div>
          ) : (
            products.map((product) => (
              <div className="store-card" key={product.productId}>
                <div className="store-card-header">
                  <div>
                    <h4>{product.name}</h4>
                    <small>{product.sku}</small>
                  </div>
                  <div className="store-price">
                    {new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(product.unitPriceVnd)}
                  </div>
                </div>

                <div className="store-meta">
                  <span>{product.category || 'Chưa phân loại'}</span>
                  <span>Tồn kho: {product.quantityOnHand}</span>
                  <span>Đặt lại: {product.reorderLevel}</span>
                </div>

                <div className="store-qty">
                  <span>Số lượng</span>
                  <div className="store-stepper">
                    <button type="button" onClick={() => updateQuantity(product.productId, -1)} aria-label={`Giảm ${product.name}`}>
                      −
                    </button>
                    <span>{quantities[product.productId] ?? 0}</span>
                    <button type="button" onClick={() => updateQuantity(product.productId, 1)} aria-label={`Tăng ${product.name}`}>
                      +
                    </button>
                  </div>
                </div>
              </div>
            ))
          )}
        </div>
      )}

      <div className="store-cart">
        <h3>Giỏ hàng</h3>
        {selectedItems.length === 0 ? (
          <p>Chưa có mặt hàng nào được chọn.</p>
        ) : (
          <>
            <div className="store-cart-items">
              {selectedItems.map((item) => (
                <div className="store-cart-item" key={item.productId}>
                  <div className="store-cart-meta">
                    <strong>{item.name}</strong>
                    <span>{item.quantity} x {new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(item.price)}</span>
                  </div>
                  <strong>{new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(item.subtotal)}</strong>
                </div>
              ))}
            </div>
            <div className="store-cart-item">
              <strong>Tổng cộng</strong>
              <strong>{new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(totalAmount)}</strong>
            </div>
            <button className="button button-primary" disabled={busy} onClick={submitOrder}>
              {busy ? 'Đang tạo đơn...' : 'Tạo giỏ hàng / đơn hàng'}
            </button>
          </>
        )}
      </div>
    </section>
  );
}
