import { useEffect, useRef, useState } from "react";
import ProductPhoto from "../../components/ui/ProductPhoto";
import { stockClient } from "./stockClient";

const formats = ["image/jpeg", "image/png", "image/webp"];
const maxBytes = 2 * 1024 * 1024;

export default function ProductPhotoEditor({ product, revision, onChanged }) {
  const [selected, setSelected] = useState(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const input = useRef(null);
  const previewUrl = selected?.url;

  useEffect(() => () => {
    if (previewUrl) URL.revokeObjectURL(previewUrl);
  }, [previewUrl]);

  function select(event) {
    const file = event.target.files[0];
    setNotice("");
    setError("");
    setSelected(null);
    if (!file) return;
    if (!formats.includes(file.type) || file.size > maxBytes || file.size === 0) {
      setError("Chọn ảnh JPG, PNG hoặc WebP tối đa 2 MB.");
      event.target.value = "";
      return;
    }
    setSelected({ file, url: URL.createObjectURL(file) });
  }

  async function save(restore = false) {
    setBusy(true);
    setNotice("");
    setError("");
    try {
      if (restore) await stockClient.restorePhoto(product.id);
      else await stockClient.savePhoto(product.id, selected.file);
      setSelected(null);
      if (input.current) input.current.value = "";
      onChanged(product.id);
      setNotice(restore ? "Đã khôi phục ảnh mặc định." : "Đã cập nhật ảnh mặt hàng.");
    } catch (error) {
      setError(error.message);
    } finally {
      setBusy(false);
    }
  }

  return (
    <section className="stock-photo-editor" aria-label="Ảnh mặt hàng">
      <ProductPhoto sku={product.sku} productId={product.id} previewUrl={previewUrl} revision={revision} />
      <div className="stock-photo-controls">
        <label>
          <strong>Ảnh mặt hàng</strong>
          <input ref={input} type="file" accept={formats.join(",")} disabled={busy} onChange={select} />
        </label>
        <small>JPG, PNG hoặc WebP, tối đa 2 MB.</small>
        <div className="stock-photo-actions">
          <button type="button" className="button button-secondary" disabled={busy || !selected} onClick={() => save()}>
            Lưu ảnh
          </button>
          <button type="button" className="button button-secondary" disabled={busy} onClick={() => save(true)}>
            Dùng ảnh mặc định
          </button>
        </div>
        {error && <p className="stock-error" role="alert">{error}</p>}
        {notice && <p role="status">{notice}</p>}
      </div>
    </section>
  );
}
