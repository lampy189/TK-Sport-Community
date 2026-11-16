import { useEffect, useId, useMemo, useState } from "react";
import {
  ArrowDownToLine,
  ArrowUpFromLine,
  History,
  PackagePlus,
  Pencil,
  Plus,
  Search,
  Trash2,
  TriangleAlert,
  X,
} from "lucide-react";
import SectionLayout from "../../components/layout/SectionLayout";
import StatePanel from "../../components/ui/StatePanel";
import { categoryLabels, movementLabels, stockClient } from "./stockClient";
import SessionSuppliesView from "./SessionSuppliesView";
import BulkReceiptView from "./BulkReceiptView";
import "./stock.css";

const money = (value) =>
  new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND" }).format(
    value,
  );
const number = (value) => new Intl.NumberFormat("vi-VN").format(value);

const tabs = [
  { id: "catalog", label: "Danh mục & tồn kho" },
  { id: "receipts", label: "Nhập hàng loạt" },
  { id: "supplies", label: "Vật tư buổi chơi" },
];

export default function StockPage() {
  const [tab, setTab] = useState("catalog");
  return (
    <div className="stock-workspace">
      <div className="stock-tabs" role="tablist" aria-label="Kho & vật tư">
        {tabs.map((t) => (
          <button
            key={t.id}
            role="tab"
            aria-selected={tab === t.id}
            className={tab === t.id ? "is-active" : ""}
            onClick={() => setTab(t.id)}
          >
            {t.label}
          </button>
        ))}
      </div>
      {tab === "catalog" && <StockCatalog />}
      {tab === "receipts" && <BulkReceiptView />}
      {tab === "supplies" && <SessionSuppliesView />}
    </div>
  );
}

// UC-26 / UC-27 (team breakdown; RP3 SCR-22): Staff catalogue, receipts/issues and movement history.
function StockCatalog() {
  const [products, setProducts] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const [search, setSearch] = useState("");
  const [category, setCategory] = useState("");
  const [onlyLow, setOnlyLow] = useState(false);
  const [showInactive] = useState(false);
  const [dialog, setDialog] = useState(null);

  const [reloadKey, setReloadKey] = useState(0);
  useEffect(() => {
    let active = true;
    stockClient
      .products()
      .then((list) => active && (setProducts(list), setError("")))
      .catch((err) => active && setError(err.message))
      .finally(() => active && setLoading(false));
    return () => {
      active = false;
    };
  }, [reloadKey]);
  function reload() {
    setLoading(true);
    setReloadKey((key) => key + 1);
  }

  const visible = useMemo(() => {
    const term = search.trim().toLowerCase();
    return products.filter(
      (p) =>
        (showInactive || p.isActive) &&
        (!category || p.category === category) &&
        (!onlyLow || p.isLowStock) &&
        (!term ||
          p.sku.toLowerCase().includes(term) ||
          p.name.toLowerCase().includes(term) ||
          (p.units ?? []).some((u) => u.barcode?.toLowerCase().includes(term))),
    );
  }, [products, search, category, onlyLow, showInactive]);
  const active = products.filter((p) => p.isActive);
  const lowCount = active.filter((p) => p.isLowStock).length;

  function saved(product, message) {
    setProducts((list) =>
      list.some((p) => p.id === product.id)
        ? list.map((p) => (p.id === product.id ? product : p))
        : [...list, product],
    );
    setDialog(null);
    setNotice(message);
  }

  const addButton = (
    <button
      className="button button-primary"
      onClick={() => setDialog({ kind: "product" })}
    >
      <PackagePlus size={16} aria-hidden="true" /> Thêm mặt hàng
    </button>
  );

  return (
    <SectionLayout
      title="Kho & vật tư"
      description="Danh mục hàng hóa, vật tư thể thao và số lượng tồn kho tại sân."
      action={addButton}
      className="stock-page"
    >
      <div className="stock-summary">
        <div>
          <span>Mặt hàng đang bán</span>
          <strong>{loading ? "–" : number(active.length)}</strong>
        </div>
        <button
          className={lowCount ? "is-warning" : ""}
          aria-pressed={onlyLow}
          onClick={() => setOnlyLow((value) => !value)}
          disabled={loading}
        >
          <span>Sắp hết hàng</span>
          <strong>{loading ? "–" : number(lowCount)}</strong>
        </button>
      </div>

      <div className="stock-toolbar">
        <label className="stock-search">
          <Search size={16} aria-hidden="true" />
          <span className="sr-only">Tìm mặt hàng</span>
          <input
            type="search"
            placeholder="Tìm theo mã, mã vạch hoặc tên hàng"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
        </label>
        <label>
          <span className="sr-only">Nhóm hàng</span>
          <select
            value={category}
            onChange={(e) => setCategory(e.target.value)}
          >
            <option value="">Tất cả nhóm hàng</option>
            {Object.entries(categoryLabels).map(([value, label]) => (
              <option key={value} value={value}>
                {label}
              </option>
            ))}
          </select>
        </label>
      </div>

      {notice && (
        <p className="stock-notice" role="status">
          {notice}
          <button aria-label="Đóng thông báo" onClick={() => setNotice("")}>
            <X size={14} />
          </button>
        </p>
      )}

      {loading ? (
        <StatePanel kind="loading" title="Đang tải danh mục kho…" />
      ) : error ? (
        <StatePanel
          kind="error"
          title="Không tải được danh mục kho"
          description={error}
          action={
            <button className="button button-secondary" onClick={reload}>
              Thử lại
            </button>
          }
        />
      ) : visible.length === 0 ? (
        <StatePanel
          title={
            products.length ? "Không có mặt hàng phù hợp" : "Kho chưa có mặt hàng"
          }
          description={
            products.length
              ? "Thử đổi từ khóa hoặc bộ lọc."
              : "Thêm mặt hàng đầu tiên để bắt đầu theo dõi tồn kho."
          }
          action={products.length ? null : addButton}
        />
      ) : (
        <div className="stock-table-wrap">
          <table className="stock-table">
            <thead>
              <tr>
                <th scope="col">Mặt hàng</th>
                <th scope="col">Nhóm</th>
                <th scope="col" className="num">
                  Đơn giá
                </th>
                <th scope="col" className="num">
                  Tồn kho
                </th>
                <th scope="col">Trạng thái</th>
                <th scope="col">
                  <span className="sr-only">Thao tác</span>
                </th>
              </tr>
            </thead>
            <tbody>
              {visible.map((p) => (
                <tr key={p.id} className={p.isActive ? "" : "is-inactive"}>
                  <td data-label="Mặt hàng">
                    <strong>{p.name}</strong>
                    <span className="stock-sku">{p.sku}</span>
                  </td>
                  <td data-label="Nhóm">
                    {categoryLabels[p.category] || p.category || "—"}
                  </td>
                  <td data-label="Đơn giá" className="num">
                    {money(p.unitPriceVnd)}
                  </td>
                  <td data-label="Tồn kho" className="num">
                    <span className="stock-qty-cell">
                      <strong
                        className={
                          p.isLowStock ? "stock-qty is-low" : "stock-qty"
                        }
                      >
                        {p.isLowStock && (
                          <TriangleAlert size={14} aria-hidden="true" />
                        )}
                        {number(p.quantityOnHand)} {p.baseUnit}
                      </strong>
                      {p.quantityBreakdown !== `${p.quantityOnHand} ${p.baseUnit}` && (
                        <span className="stock-reorder">{p.quantityBreakdown}</span>
                      )}
                      <span className="stock-reorder">
                        Cảnh báo ≤ {number(p.reorderLevel)}
                      </span>
                    </span>
                  </td>
                  <td data-label="Trạng thái">
                    {!p.isActive ? (
                      <span className="badge-gray">Ngừng kinh doanh</span>
                    ) : p.isLowStock ? (
                      <span className="stock-badge-warning">Sắp hết</span>
                    ) : (
                      <span className="badge-lime">Đang bán</span>
                    )}
                  </td>
                  <td className="stock-actions">
                    <button
                      className="button button-secondary"
                      disabled={!p.isActive}
                      onClick={() =>
                        setDialog({ kind: "move", type: "Receipt", product: p })
                      }
                    >
                      <ArrowDownToLine size={15} aria-hidden="true" /> Nhập
                    </button>
                    <button
                      className="button button-secondary"
                      disabled={!p.isActive || p.quantityOnHand === 0}
                      onClick={() =>
                        setDialog({ kind: "move", type: "Issue", product: p })
                      }
                    >
                      <ArrowUpFromLine size={15} aria-hidden="true" /> Xuất
                    </button>
                    <button
                      className="stock-icon-button"
                      aria-label={`Sửa ${p.name}`}
                      title="Sửa thông tin"
                      onClick={() => setDialog({ kind: "product", product: p })}
                    >
                      <Pencil size={16} />
                    </button>
                    <button
                      className="stock-icon-button"
                      aria-label={`Lịch sử nhập xuất ${p.name}`}
                      title="Lịch sử nhập/xuất"
                      onClick={() => setDialog({ kind: "history", product: p })}
                    >
                      <History size={16} />
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {dialog?.kind === "product" && (
        <ProductDialog
          product={dialog.product}
          onClose={() => setDialog(null)}
          onSaved={(product) =>
            saved(
              product,
              dialog.product
                ? `Đã cập nhật ${product.name}.`
                : `Đã thêm ${product.name} vào kho.`,
            )
          }
        />
      )}
      {dialog?.kind === "move" && (
        <MovementDialog
          type={dialog.type}
          product={dialog.product}
          onClose={() => setDialog(null)}
          onSaved={(product, quantity, unitName) =>
            saved(
              product,
              `${movementLabels[dialog.type]} ${number(quantity)} ${unitName} × ${product.name}. Tồn hiện tại: ${number(product.quantityOnHand)} ${product.baseUnit}.`,
            )
          }
        />
      )}
      {dialog?.kind === "history" && (
        <HistoryDialog product={dialog.product} onClose={() => setDialog(null)} />
      )}
    </SectionLayout>
  );
}

function Dialog({ title, onClose, children }) {
  const titleId = useId();
  useEffect(() => {
    const onKey = (e) => e.key === "Escape" && onClose();
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [onClose]);
  return (
    <div
      className="modal-overlay"
      onMouseDown={(e) => e.target === e.currentTarget && onClose()}
    >
      <div
        className="modal-content stock-dialog"
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
      >
        <div className="stock-dialog-head">
          <h3 id={titleId}>{title}</h3>
          <button className="stock-icon-button" aria-label="Đóng" onClick={onClose}>
            <X size={18} />
          </button>
        </div>
        {children}
      </div>
    </div>
  );
}

function ProductDialog({ product, onClose, onSaved }) {
  const editing = Boolean(product);
  const [form, setForm] = useState(() => ({
    sku: product?.sku ?? "",
    name: product?.name ?? "",
    category: product?.category ?? "",
    unitPriceVnd: product?.unitPriceVnd ?? "",
    reorderLevel: product?.reorderLevel ?? 10,
    initialQuantity: 0,
    isActive: product?.isActive ?? true,
  }));
  const [units, setUnits] = useState(() =>
    product?.units?.length
      ? [...product.units]
          .sort((a, b) => a.factorToBase - b.factorToBase)
          .map((u) => ({
            key: u.id,
            unitName: u.unitName,
            factorToBase: u.factorToBase,
            barcode: u.barcode ?? "",
            existing: true,
          }))
      : [{ key: "base", unitName: "", factorToBase: 1, barcode: "", existing: false }],
  );
  const locked = Boolean(product?.unitsLocked);
  const baseName = units[0]?.unitName.trim() || "đơn vị chuẩn";
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const set = (key) => (e) =>
    setForm((f) => ({
      ...f,
      [key]: e.target.type === "checkbox" ? e.target.checked : e.target.value,
    }));

  async function submit(e) {
    e.preventDefault();
    setBusy(true);
    setError("");
    const details = {
      name: form.name.trim(),
      category: form.category || null,
      unitPriceVnd: Number(form.unitPriceVnd),
      reorderLevel: Number(form.reorderLevel),
      units: units.map((u) => ({
        unitName: u.unitName.trim(),
        factorToBase: Number(u.factorToBase),
        barcode: u.barcode.trim() || null,
      })),
    };
    try {
      onSaved(
        editing
          ? await stockClient.update(product.id, {
              ...details,
              isActive: form.isActive,
            })
          : await stockClient.create({
              ...details,
              sku: form.sku.trim().toUpperCase(),
              initialQuantity: Number(form.initialQuantity),
            }),
      );
    } catch (err) {
      setError(err.message);
      setBusy(false);
    }
  }

  return (
    <Dialog
      title={editing ? "Sửa thông tin mặt hàng" : "Thêm mặt hàng"}
      onClose={onClose}
    >
      <form className="stock-form" onSubmit={submit}>
        <label>
          Mã hàng (SKU)
          <input
            value={form.sku}
            onChange={set("sku")}
            required
            pattern="[A-Za-z0-9][A-Za-z0-9_\-]{1,49}"
            title="2–50 ký tự chữ, số, '-' hoặc '_'"
            disabled={editing}
            placeholder="VD: CAU-YONEX-AS30"
            autoFocus={!editing}
          />
          {editing && <small>Mã hàng cố định sau khi tạo.</small>}
        </label>
        <label>
          Tên mặt hàng
          <input
            value={form.name}
            onChange={set("name")}
            required
            maxLength={255}
            autoFocus={editing}
          />
        </label>
        <div className="stock-form-row">
          <label>
            Nhóm hàng
            <select value={form.category} onChange={set("category")}>
              <option value="">Chưa phân nhóm</option>
              {Object.entries(categoryLabels).map(([value, label]) => (
                <option key={value} value={value}>
                  {label}
                </option>
              ))}
            </select>
          </label>
          <label>
            Đơn giá (VND)
            <input
              type="number"
              inputMode="numeric"
              min="0"
              max="100000000"
              step="1000"
              value={form.unitPriceVnd}
              onChange={set("unitPriceVnd")}
              required
            />
          </label>
        </div>
        <div className="stock-form-row">
          {!editing && (
            <label>
              Tồn kho ban đầu
              <input
                type="number"
                inputMode="numeric"
                min="0"
                max="100000"
                value={form.initialQuantity}
                onChange={set("initialQuantity")}
                required
              />
            </label>
          )}
          <label>
            Cảnh báo khi tồn ≤
            <input
              type="number"
              inputMode="numeric"
              min="0"
              max="100000"
              value={form.reorderLevel}
              onChange={set("reorderLevel")}
              required
            />
          </label>
        </div>
        <fieldset className="unit-editor">
          <legend>Đơn vị tính &amp; mã vạch</legend>
          <p className="stock-hint">
            Dòng đầu là đơn vị chuẩn, phải là đơn vị nhỏ nhất. Tồn kho luôn tính theo đơn vị này.
            {locked && " Đã có phiếu kho nên không đổi được tên và hệ số của đơn vị cũ."}
          </p>
          {units.map((unit, i) => {
            const fixed = locked && unit.existing;
            const update = (patch) =>
              setUnits((list) => list.map((u) => (u.key === unit.key ? { ...u, ...patch } : u)));
            return (
              <div className="unit-row" key={unit.key}>
                <label>
                  {i === 0 ? "Đơn vị chuẩn" : "Đơn vị"}
                  <input
                    value={unit.unitName}
                    onChange={(e) => update({ unitName: e.target.value })}
                    required
                    maxLength={50}
                    disabled={fixed}
                    placeholder={i === 0 ? "VD: Chai" : "VD: Thùng"}
                  />
                </label>
                <label>
                  {i === 0 ? "Hệ số" : `1 ${unit.unitName.trim() || "đơn vị"} = ? ${baseName}`}
                  <input
                    type="number"
                    inputMode="numeric"
                    min={i === 0 ? 1 : 2}
                    max="100000"
                    value={unit.factorToBase}
                    onChange={(e) => update({ factorToBase: e.target.value })}
                    required
                    disabled={i === 0 || fixed}
                  />
                </label>
                <label>
                  Mã vạch
                  <input
                    value={unit.barcode}
                    onChange={(e) => update({ barcode: e.target.value })}
                    maxLength={64}
                    pattern="[A-Za-z0-9\-]{4,64}"
                    title="4–64 ký tự chữ, số hoặc '-'"
                    placeholder="Không bắt buộc"
                  />
                </label>
                {i > 0 && !fixed && (
                  <button
                    type="button"
                    className="stock-icon-button"
                    aria-label={`Xóa đơn vị ${unit.unitName}`}
                    onClick={() => setUnits((list) => list.filter((u) => u.key !== unit.key))}
                  >
                    <Trash2 size={16} />
                  </button>
                )}
              </div>
            );
          })}
          <button
            type="button"
            className="button button-secondary unit-add"
            onClick={() =>
              setUnits((list) => [
                ...list,
                { key: `new-${Date.now()}`, unitName: "", factorToBase: "", barcode: "", existing: false },
              ])
            }
          >
            <Plus size={15} aria-hidden="true" /> Thêm đơn vị
          </button>
        </fieldset>
        {editing && (
          <label className="stock-check">
            <input
              type="checkbox"
              checked={form.isActive}
              onChange={set("isActive")}
            />
            Đang kinh doanh
          </label>
        )}
        {editing && (
          <p className="stock-hint">
            Số lượng tồn chỉ thay đổi qua phiếu nhập/xuất để giữ lịch sử.
          </p>
        )}
        {error && (
          <p className="stock-error" role="alert">
            {error}
          </p>
        )}
        <div className="stock-form-actions">
          <button type="button" className="button button-secondary" onClick={onClose}>
            Hủy
          </button>
          <button className="button button-primary" disabled={busy}>
            {busy ? "Đang lưu…" : editing ? "Lưu thay đổi" : "Thêm mặt hàng"}
          </button>
        </div>
      </form>
    </Dialog>
  );
}

function MovementDialog({ type, product, onClose, onSaved }) {
  const issue = type === "Issue";
  const units = [...(product.units ?? [])].sort((a, b) => a.factorToBase - b.factorToBase);
  const [unitName, setUnitName] = useState(product.baseUnit);
  const [quantity, setQuantity] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const factor = units.find((u) => u.unitName === unitName)?.factorToBase ?? 1;
  const amount = Number(quantity) || 0;
  const baseAmount = amount * factor;
  const after = product.quantityOnHand + (issue ? -baseAmount : baseAmount);

  async function submit(e) {
    e.preventDefault();
    setBusy(true);
    setError("");
    try {
      onSaved(await stockClient.move(product.id, type, amount, unitName), amount, unitName);
    } catch (err) {
      setError(err.message);
      setBusy(false);
    }
  }

  return (
    <Dialog title={`${movementLabels[type]} – ${product.name}`} onClose={onClose}>
      <form className="stock-form" onSubmit={submit}>
        <div className="stock-balance">
          <div>
            <span>Tồn hiện tại</span>
            <strong>{number(product.quantityOnHand)} {product.baseUnit}</strong>
          </div>
          <div>
            <span>Sau khi {issue ? "xuất" : "nhập"}</span>
            <strong className={after < 0 ? "is-invalid" : ""}>
              {amount ? `${number(after)} ${product.baseUnit}` : "–"}
            </strong>
          </div>
        </div>
        <div className="stock-form-row">
          <label>
            Đơn vị
            <select value={unitName} onChange={(e) => setUnitName(e.target.value)}>
              {units.map((u) => (
                <option key={u.unitName} value={u.unitName}>
                  {u.unitName}
                </option>
              ))}
            </select>
          </label>
          <label>
            Số lượng {issue ? "xuất" : "thực nhận"}
            <input
              type="number"
              inputMode="numeric"
              min="1"
              max={issue ? Math.floor(product.quantityOnHand / factor) : 100000}
              value={quantity}
              onChange={(e) => setQuantity(e.target.value)}
              required
              autoFocus
            />
          </label>
        </div>
        {factor > 1 && (
          <p className="stock-hint">
            1 {unitName} = {number(factor)} {product.baseUnit}
            {amount > 0 && ` → ${number(baseAmount)} ${product.baseUnit}`}
          </p>
        )}
        <p className="stock-hint">
          {issue
            ? "Không thể xuất vượt số tồn. Bán hàng và cấp vật tư buổi chơi sẽ tự trừ kho ở luồng riêng."
            : "Ghi số lượng thực tế đã nhận tại sân. Phiếu đã ghi không thể xóa."}
        </p>
        {error && (
          <p className="stock-error" role="alert">
            {error}
          </p>
        )}
        <div className="stock-form-actions">
          <button type="button" className="button button-secondary" onClick={onClose}>
            Hủy
          </button>
          <button className="button button-primary" disabled={busy || after < 0}>
            {busy ? "Đang ghi…" : `Xác nhận ${issue ? "xuất" : "nhập"}`}
          </button>
        </div>
      </form>
    </Dialog>
  );
}

function HistoryDialog({ product, onClose }) {
  const [state, setState] = useState({ loading: true, items: [], error: "" });
  useEffect(() => {
    let active = true;
    stockClient
      .movements(product.id)
      .then((items) => active && setState({ loading: false, items, error: "" }))
      .catch(
        (err) =>
          active && setState({ loading: false, items: [], error: err.message }),
      );
    return () => {
      active = false;
    };
  }, [product.id]);

  return (
    <Dialog title={`Lịch sử nhập/xuất – ${product.name}`} onClose={onClose}>
      {state.loading ? (
        <p className="stock-hint">Đang tải lịch sử…</p>
      ) : state.error ? (
        <p className="stock-error" role="alert">
          {state.error}
        </p>
      ) : state.items.length === 0 ? (
        <p className="stock-hint">Chưa có phiếu nhập/xuất nào cho mặt hàng này.</p>
      ) : (
        <ol className="stock-history">
          {state.items.map((m) => (
            <li key={m.id}>
              <span>
                <strong>{movementLabels[m.movementType] || m.movementType}</strong>
                <small>
                  Phiếu #{m.id}
                  {m.sourceRef ? ` · ${m.sourceRef}` : ""}
                  {m.enteredUnit && m.enteredQuantity
                    ? ` · ${number(m.enteredQuantity)} ${m.enteredUnit}`
                    : ""}
                  {m.createdByName ? ` · ${m.createdByName}` : ""}
                  {m.createdAt
                    ? ` · ${new Date(m.createdAt).toLocaleString("vi-VN", { timeZone: "Asia/Ho_Chi_Minh" })}`
                    : ""}
                </small>
              </span>
              <strong className={m.quantity < 0 ? "is-out" : "is-in"}>
                {m.quantity > 0 ? "+" : ""}
                {number(m.quantity)} {product.baseUnit}
              </strong>
            </li>
          ))}
        </ol>
      )}
    </Dialog>
  );
}
