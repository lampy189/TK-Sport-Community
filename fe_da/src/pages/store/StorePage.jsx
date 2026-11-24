import PaginatedList from '../../components/ui/PaginatedList';
import { useEffect, useMemo, useRef, useState } from 'react';
import { ScanLine, ShoppingBag, Trash2, X } from 'lucide-react';
import { storeClient } from './storeClient';
import MyGoodsOrders from '../stock/MyGoodsOrders';
import './store.css';

const money = (value) => new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(value || 0);

export default function StorePage({ role = 'staff', onOpenOrders }) {
  const [products, setProducts] = useState([]);
  const [quantities, setQuantities] = useState({});
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const [loading, setLoading] = useState(true);
  const [barcode, setBarcode] = useState('');
  const [panel, setPanel] = useState(() => new URLSearchParams(window.location.search).get('goodsOrderId') ? 'orders' : 'cart');
  const [drawerOpen, setDrawerOpen] = useState(() => Boolean(new URLSearchParams(window.location.search).get('goodsOrderId')));
  const [narrow, setNarrow] = useState(false);
  const [ordersVersion, setOrdersVersion] = useState(0);
  const [highlightId, setHighlightId] = useState(() => new URLSearchParams(window.location.search).get('goodsOrderId'));
  const idempotencyKey = useRef(crypto.randomUUID());
  const closeRef = useRef(null);
  useEffect(() => {
    const query = window.matchMedia('(max-width: 980px)');
    const apply = () => setNarrow(query.matches);
    apply();
    query.addEventListener('change', apply);
    return () => query.removeEventListener('change', apply);
  }, []);

  useEffect(() => {
    if (!narrow || !drawerOpen) return undefined;
    const previous = document.body.style.overflow;
    document.body.style.overflow = 'hidden';
    closeRef.current?.focus();
    const onKey = (event) => {
      if (event.key === 'Escape') setDrawerOpen(false);
    };
    window.addEventListener('keydown', onKey);
    return () => {
      document.body.style.overflow = previous;
      window.removeEventListener('keydown', onKey);
    };
  }, [narrow, drawerOpen]);

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
        stock: product.quantityOnHand,
        subtotal: product.unitPriceVnd * (quantities[product.productId] ?? 0),
      })),
    [products, quantities],
  );

  const totalAmount = selectedItems.reduce((sum, item) => sum + item.subtotal, 0);
  const itemCount = selectedItems.reduce((sum, item) => sum + item.quantity, 0);

  function changeQuantity(productId, delta, stockCeiling) {
    const product = products.find((item) => item.productId === productId);
    const max = Math.max(0, Number(stockCeiling ?? product?.quantityOnHand ?? 0));
    const name = product?.name ?? 'sản phẩm';
    setQuantities((current) => {
      const next = Number(current[productId] ?? 0) + delta;
      if (next > max) {
        queueMicrotask(() => setError(`Chỉ còn ${max} «${name}».`));
        return current;
      }
      queueMicrotask(() => {
        setError('');
        idempotencyKey.current = crypto.randomUUID();
      });
      if (next <= 0) {
        const rest = { ...current };
        delete rest[productId];
        return rest;
      }
      return { ...current, [productId]: next };
    });
  }

  function openPanel(next) {
    setPanel(next);
    setDrawerOpen(true);
  }

  async function lookupBarcode(event) {
    event.preventDefault();
    const code = barcode.trim();
    if (!code || busy) return;
    setBusy(true); setError('');
    try {
      const match = await storeClient.lookupBarcode(code);
      if (!match.isActive) throw new Error('Mặt hàng đã ngừng bán.');
      setProducts(current => current.some(product => product.productId === match.productId) ? current : [...current, {
        productId: match.productId, sku: match.sku, name: match.name, category: null, unitPriceVnd: match.unitPriceVnd,
        quantityOnHand: match.quantityOnHand, reorderLevel: 0,
      }]);
      changeQuantity(match.productId, match.factorToBase, match.quantityOnHand);
      setBarcode('');
    } catch (e) { setError(e.message); }
    finally { setBusy(false); }
  }

  async function submitOrder() {
    const items = selectedItems.map((item) => ({
      productId: item.productId,
      quantity: item.quantity,
    }));

    if (!items.length) {
      setError('Chọn ít nhất một món.');
      openPanel('cart');
      return;
    }

    setBusy(true);
    setError('');
    try {
      const result = await storeClient.createCart(items, idempotencyKey.current);
      setQuantities({});
      idempotencyKey.current = crypto.randomUUID();
      if (role === 'player') {
        setHighlightId(String(result.orderId));
        setOrdersVersion((version) => version + 1);
        openPanel('orders');
      } else {
        onOpenOrders?.(result.orderId);
      }
    } catch (e) {
      setError(e.message);
    } finally {
      setBusy(false);
    }
  }

  const player = role === 'player';
  const title = player ? 'Cửa hàng' : 'Bán hàng tại quầy';
  const dialog = narrow && drawerOpen;

  return (
    <section className="store-page">
      {!player && (
        <div className="store-header">
          <div>
            <h2>{title}</h2>
            <p>Chọn món, kiểm tra giỏ và tạo đơn tại quầy.</p>
          </div>
          <button type="button" className="button button-secondary" onClick={() => onOpenOrders?.()}>Xử lý đơn hàng</button>
        </div>
      )}

      {error && !(narrow && drawerOpen) && <div className="store-error" role="alert">{error}</div>}

      <div className="store-mobile-switch">
        <button type="button" className="button button-primary" onClick={() => openPanel('cart')}>
          <ShoppingBag size={16} aria-hidden="true" />
          Giỏ hàng{itemCount ? ` · ${itemCount}` : ''}
        </button>
        {player && <button type="button" className="button button-secondary" onClick={() => openPanel('orders')}>Đơn của tôi</button>}
      </div>

      {loading ? (
        <p>Đang tải danh sách sản phẩm…</p>
      ) : (
        <div className="store-shell">
          <div className="store-catalog">
            {role !== 'player' && <form className="store-barcode-form" onSubmit={lookupBarcode}>
              <label htmlFor="store-barcode"><ScanLine size={18} aria-hidden="true" />Quét hoặc nhập barcode / SKU</label>
              <div><input id="store-barcode" autoComplete="off" value={barcode} onChange={event => setBarcode(event.target.value)} placeholder="Đưa mã vào đây rồi nhấn Enter" disabled={busy} />
                <button type="submit" className="button button-secondary" disabled={busy || !barcode.trim()}>Thêm vào giỏ</button></div>
            </form>}

            {products.length === 0 ? (
              <div className="store-card">
                <h4>Chưa có sản phẩm mở bán</h4>
                <p>Hiện chưa có mặt hàng nào đang được mở bán trong kho.</p>
              </div>
            ) : (
              <div className="store-grid">
                <PaginatedList items={products} hideShortPager>{(product) => {
                  const qty = quantities[product.productId] ?? 0;
                  const out = product.quantityOnHand <= 0;
                  const full = qty >= product.quantityOnHand && product.quantityOnHand > 0;
                  return (
                    <article className={`store-card${qty ? ' is-in-cart' : ''}`} key={product.productId}>
                      <div className="store-card-header">
                        <h4>{product.name}</h4>
                        <div className="store-price">{money(product.unitPriceVnd)}</div>
                      </div>
                      <div className="store-meta">
                        <span>Kho: {product.quantityOnHand}</span>
                        {qty > 0 && <span className="store-in-cart">Trong giỏ: {qty}</span>}
                      </div>
                      <button
                        type="button"
                        className="button button-primary store-add"
                        disabled={out || full}
                        onClick={() => changeQuantity(product.productId, 1)}
                      >
                        {out ? 'Hết hàng' : full ? 'Đủ tồn kho' : 'Thêm vào giỏ'}
                      </button>
                    </article>
                  );
                }}</PaginatedList>
              </div>
            )}
          </div>

          {dialog && <button type="button" className="store-drawer-backdrop" aria-label="Đóng giỏ hàng" onClick={() => setDrawerOpen(false)} />}

          <aside
            className={`store-cart-panel${drawerOpen ? ' is-open' : ''}`}
            role={dialog ? 'dialog' : 'complementary'}
            aria-modal={dialog ? 'true' : undefined}
            aria-label={player ? 'Giỏ hàng và đơn hàng' : 'Giỏ hàng'}
          >
            <div className="store-panel-head">
              {player ? (
                <div className="store-panel-tabs" role="tablist" aria-label="Giỏ và đơn">
                  <button type="button" role="tab" aria-selected={panel === 'cart'} onClick={() => setPanel('cart')}>
                    Giỏ hàng{itemCount ? ` (${itemCount})` : ''}
                  </button>
                  <button type="button" role="tab" aria-selected={panel === 'orders'} onClick={() => setPanel('orders')}>Đơn của tôi</button>
                </div>
              ) : <h3>Giỏ hàng{itemCount ? ` (${itemCount})` : ''}</h3>}
              <button ref={closeRef} type="button" className="store-panel-close" aria-label="Đóng" onClick={() => setDrawerOpen(false)}>
                <X size={18} aria-hidden="true" />
              </button>
            </div>

            {(panel === 'cart' || !player) && (
              <>
                <div className="store-panel-body">
                  {selectedItems.length === 0 ? (
                    <div className="store-cart-empty">
                      <ShoppingBag size={28} aria-hidden="true" />
                      <p>Giỏ đang trống.</p>
                    </div>
                  ) : (
                    <ul className="store-cart-items">
                      {selectedItems.map((item) => (
                        <li className="store-cart-item" key={item.productId}>
                          <div className="store-cart-meta">
                            <strong>{item.name}</strong>
                            <span>{money(item.price)}</span>
                          </div>
                          <div className="store-line-actions">
                            <div className="store-stepper">
                              <button type="button" onClick={() => changeQuantity(item.productId, -1, item.stock)} aria-label={`Giảm ${item.name}`}>−</button>
                              <span>{item.quantity}</span>
                              <button type="button" onClick={() => changeQuantity(item.productId, 1, item.stock)} aria-label={`Tăng ${item.name}`}>+</button>
                            </div>
                            <strong>{money(item.subtotal)}</strong>
                            <button type="button" className="store-remove" aria-label={`Xóa ${item.name}`} onClick={() => changeQuantity(item.productId, -item.quantity, item.stock)}>
                              <Trash2 size={16} aria-hidden="true" />
                            </button>
                          </div>
                        </li>
                      ))}
                    </ul>
                  )}
                </div>
                <div className="store-panel-foot">
                  <div className="store-cart-total">
                    <span>Tổng cộng</span>
                    <strong>{money(totalAmount)}</strong>
                  </div>
                  <p className="store-hold" aria-live="polite">{itemCount ? `${itemCount} món` : 'Chưa chọn món'}</p>
                  {narrow && drawerOpen && error && <p className="store-error" role="alert">{error}</p>}
                  <button type="button" className="button button-primary" disabled={busy || selectedItems.length === 0} onClick={submitOrder}>
                    {busy ? 'Đang tạo đơn…' : player ? 'Đặt hàng' : 'Tạo đơn'}
                  </button>
                </div>
              </>
            )}

            {player && (
              <div className="store-panel-body" hidden={panel !== 'orders'}>
                <MyGoodsOrders
                  embedded
                  refreshKey={ordersVersion}
                  highlightId={highlightId}
                />
              </div>
            )}
          </aside>
        </div>
      )}
    </section>
  );
}
