import { useEffect, useMemo, useRef, useState } from 'react';
import { Camera, CameraOff, ScanLine } from 'lucide-react';
import { storeClient } from './storeClient';
import './store.css';

export default function StorePage({ role = 'staff', user }) {
  const [products, setProducts] = useState([]);
  const [quantities, setQuantities] = useState({});
  const [barcodeInput, setBarcodeInput] = useState('');
  const [scanning, setScanning] = useState(false);
  const [error, setError] = useState('');
  const [order, setOrder] = useState(null);
  const [busy, setBusy] = useState(false);
  const [loading, setLoading] = useState(true);
  const videoRef = useRef(null);
  const scannerRef = useRef(null);
  const scanGeneration = useRef(0);

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
      scanGeneration.current += 1;
      scannerRef.current?.stop();
      scannerRef.current = null;
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

  function stopScanner() {
    scanGeneration.current += 1;
    scannerRef.current?.stop();
    scannerRef.current = null;
    setScanning(false);
  }

  function getAvailableQuantity(productId) {
    const product = products.find((entry) => entry.productId === productId);
    return product?.availableQuantity ?? product?.quantityOnHand ?? 0;
  }

  function addItemByBarcode(lookup) {
    const productId = lookup.productId;
    const product = products.find((entry) => entry.productId === productId);
    const unitQuantity = Number(lookup.factorToBase || 1);
    const currentQty = Number(quantities[productId] ?? 0);
    const availableQty = Number(product?.availableQuantity ?? lookup.availableQuantity ?? product?.quantityOnHand ?? lookup.quantityOnHand ?? 0);
    const nextQty = currentQty + unitQuantity;

    if (nextQty > availableQty) {
      const remaining = Math.max(availableQty - currentQty, 0);
      const unitLabel = unitQuantity > 1 ? `(${unitQuantity} đơn vị chuẩn)` : '';
      setError(`Sản phẩm '${lookup.name}' chỉ còn ${availableQty} trong kho. Hiện giỏ hàng đang có ${currentQty}. ${unitLabel}`.trim());
      return;
    }

    setOrder(null);
    setError('');
    setQuantities((current) => ({ ...current, [productId]: nextQty }));
    setBarcodeInput('');
  }

  async function checkBarcode(value) {
    const code = (value ?? barcodeInput).trim();
    if (!code) {
      setError('Vui lòng nhập mã vạch hoặc mã hàng trước khi kiểm tra.');
      return;
    }

    try {
      const lookup = await storeClient.lookupBarcode(code);
      addItemByBarcode(lookup);
    } catch (e) {
      setError(e.message);
    }
  }

  async function startScanner() {
    try {
      if (!navigator.mediaDevices?.getUserMedia) {
        throw new Error('Camera cần HTTPS hoặc localhost để quét mã vạch.');
      }
      stopScanner();
      setError('');
      setScanning(true);
      const generation = scanGeneration.current;
      const { BrowserMultiFormatReader } = await import('@zxing/browser');
      const reader = new BrowserMultiFormatReader();
      const scanner = await reader.decodeFromConstraints(
        { video: { facingMode: { ideal: 'environment' } }, audio: false },
        videoRef.current,
        (result, _error, controls) => {
          if (!result || generation !== scanGeneration.current) return;
          controls.stop();
          stopScanner();
          setBarcodeInput(result.getText());
          void checkBarcode(result.getText());
        },
      );
      if (generation !== scanGeneration.current) {
        scanner.stop();
      } else {
        scannerRef.current = scanner;
      }
    } catch (e) {
      stopScanner();
      setError(e.name === 'NotAllowedError' ? 'Chưa được cấp quyền camera. Hãy cho phép camera hoặc nhập mã bằng bàn phím.' : e.message);
    }
  }

  function updateQuantity(productId, delta) {
    setOrder(null);
    setError('');
    setQuantities((current) => {
      const available = getAvailableQuantity(productId);
      const next = Number(current[productId] ?? 0) + delta;

      if (next <= 0) {
        const { [productId]: _removed, ...rest } = current;
        return rest;
      }

      if (next > available) {
        setError(`Sản phẩm chỉ còn ${available} trong kho. Vui lòng giảm số lượng.`);
        return current;
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

      <div className="store-barcode-panel">
        <label className="store-barcode-field">
          <span>Check / Quét Barcode</span>
          <div className="store-barcode-actions">
            <input
              value={barcodeInput}
              onChange={(event) => setBarcodeInput(event.target.value)}
              onKeyDown={(event) => {
                if (event.key === 'Enter') {
                  event.preventDefault();
                  void checkBarcode(barcodeInput);
                }
              }}
              placeholder="Nhập mã vạch hoặc SKU..."
              autoComplete="off"
            />
            <button type="button" className="button button-primary" onClick={() => void checkBarcode(barcodeInput)}>
              Check
            </button>
            <button type="button" className="button button-secondary" onClick={() => {
              if (scanning) {
                stopScanner();
                return;
              }
              void startScanner();
            }}>
              {scanning ? <CameraOff size={16} aria-hidden="true" /> : <Camera size={16} aria-hidden="true" />}
              {scanning ? 'Tắt camera' : 'Quét'}
            </button>
          </div>
        </label>

        {scanning && (
          <div className="store-camera-wrap">
            <video ref={videoRef} className="store-camera" muted playsInline aria-label="Camera quét mã vạch" />
            <div className="store-camera-overlay"><ScanLine size={18} aria-hidden="true" /> Đang quét...</div>
          </div>
        )}
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
                  <span>Còn bán: {product.availableQuantity ?? product.quantityOnHand}</span>
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
