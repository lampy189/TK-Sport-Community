import { useEffect, useMemo, useRef, useState } from "react";
import { Camera, CameraOff, Download, FileUp, Plus, ScanLine, Trash2 } from "lucide-react";
import SectionLayout from "../../components/layout/SectionLayout";
import StatePanel from "../../components/ui/StatePanel";
import { stockClient } from "./stockClient";
import {
  buildCodeIndex,
  mergeScannedLine,
  newSourceRef,
  parseImportRows,
  parsePastedText,
  validateLine,
} from "./stockImport";

const number = (value) => new Intl.NumberFormat("vi-VN").format(value);
let nextLineKey = 1;
const withKey = (line) => ({ key: nextLineKey++, ...line });

// UC-27, UC-39 (task split 05/11): receive many items at once from a scanner, the grid, pasted cells or an Excel file.
export default function BulkReceiptView() {
  const [products, setProducts] = useState(null);
  const [loadError, setLoadError] = useState("");
  const [sourceRef, setSourceRef] = useState(newSourceRef);
  const [lines, setLines] = useState([]);
  const [scan, setScan] = useState("");
  const [camera, setCamera] = useState(false);
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const [serverErrors, setServerErrors] = useState({});
  const [busy, setBusy] = useState(false);
  const scanInput = useRef(null);

  useEffect(() => {
    let active = true;
    stockClient
      .products()
      .then((list) => active && setProducts(list))
      .catch((e) => active && setLoadError(e.message));
    return () => {
      active = false;
    };
  }, []);

  const index = useMemo(() => buildCodeIndex(products ?? []), [products]);
  const checked = useMemo(
    () => lines.map((line) => ({ line, result: validateLine(line, index) })),
    [lines, index],
  );
  const invalid = checked.filter((c) => c.result.error).length;

  function addScanned(code) {
    const value = code.trim();
    if (!value) return;
    setError("");
    setNotice("");
    if (!index.find(value)) {
      setError(`Không tìm thấy mặt hàng có mã ${value}.`);
      return;
    }
    setLines((current) => mergeScannedLine(current, value, withKey));
  }

  function appendRows(rows) {
    const parsed = parseImportRows(rows);
    if (parsed.length === 0) {
      setError("Không đọc được dòng hàng nào. Cột cần có: Mã, Đơn vị, Số lượng.");
      return;
    }
    setError("");
    setNotice(`Đã đọc ${number(parsed.length)} dòng. Kiểm tra lại rồi bấm Xác nhận nhập kho.`);
    setServerErrors({});
    setLines((current) => [...current, ...parsed.map(withKey)]);
  }

  async function importFile(file) {
    if (!file) return;
    if (!/\.xlsx$/i.test(file.name)) {
      setError("Chỉ nhận file Excel định dạng .xlsx.");
      return;
    }
    if (file.size > 5 * 1024 * 1024) {
      setError("File Excel tối đa 5 MB.");
      return;
    }
    try {
      const { default: readXlsxFile } = await import("read-excel-file/browser");
      const sheets = await readXlsxFile(file);
      appendRows(sheets[0]?.data ?? []);
    } catch {
      setError("Không đọc được file Excel. Hãy dùng file mẫu và lưu dạng .xlsx.");
    }
  }

  function changeLine(key, patch) {
    setServerErrors({});
    setLines((current) => current.map((l) => (l.key === key ? { ...l, ...patch } : l)));
  }

  async function submit(e) {
    e.preventDefault();
    setBusy(true);
    setError("");
    setNotice("");
    setServerErrors({});
    try {
      const result = await stockClient.batchReceipt({
        sourceRef: sourceRef.trim(),
        lines: lines.map((l) => ({
          code: l.code.trim(),
          unitName: l.unitName || null,
          quantity: Number(l.quantity),
        })),
      });
      setNotice(
        result.alreadyPosted
          ? `Phiếu ${sourceRef} đã nhập trước đó, không cộng thêm vào kho.`
          : `Đã nhập ${number(result.posted.length)} mặt hàng vào kho theo phiếu ${sourceRef}.`,
      );
      setLines([]);
      setSourceRef(newSourceRef());
      setProducts(await stockClient.products());
    } catch (err) {
      setError(err.message);
      const byLine = {};
      (err.data?.lineErrors ?? []).forEach((l) => {
        byLine[l.lineNo] = l.message;
      });
      setServerErrors(byLine);
    } finally {
      setBusy(false);
    }
  }

  if (loadError) return <StatePanel kind="error" title="Không tải được danh mục kho" description={loadError} />;
  if (!products) return <StatePanel kind="loading" title="Đang tải danh mục kho…" />;

  return (
    <SectionLayout
      title="Nhập hàng loạt"
      description="Quét mã vạch, nhập trực tiếp, dán từ Excel hoặc tải file để nhập nhiều mặt hàng cùng lúc."
      className="stock-page"
    >
      <form className="bulk-receipt" onSubmit={submit}>
        <div className="bulk-tools">
          <label className="bulk-ref">
            Mã phiếu nhập
            <input
              value={sourceRef}
              onChange={(e) => setSourceRef(e.target.value)}
              required
              maxLength={100}
              pattern="[A-Za-z0-9][A-Za-z0-9_.\/\-]{1,99}"
              title="2–100 ký tự chữ, số, '-', '_', '.' hoặc '/'"
            />
          </label>
          <label className="bulk-scan">
            Quét hoặc gõ mã vạch, rồi Enter
            <span className="bulk-scan-input">
              <ScanLine size={16} aria-hidden="true" />
              <input
                ref={scanInput}
                value={scan}
                maxLength={100}
                autoComplete="off"
                autoFocus
                onChange={(e) => setScan(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key !== "Enter") return;
                  e.preventDefault();
                  addScanned(scan);
                  setScan("");
                }}
              />
            </span>
          </label>
          <button type="button" className="button button-secondary" onClick={() => setCamera((v) => !v)}>
            {camera ? <CameraOff size={16} aria-hidden="true" /> : <Camera size={16} aria-hidden="true" />}
            {camera ? "Tắt camera" : "Mở camera"}
          </button>
          <label className="button button-secondary bulk-file">
            <FileUp size={16} aria-hidden="true" /> Tải file Excel
            <input
              type="file"
              accept=".xlsx,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
              onChange={(e) => {
                void importFile(e.target.files?.[0]);
                e.target.value = "";
              }}
            />
          </label>
          <a className="button button-secondary" href="/templates/nhap-kho-mau.xlsx" download>
            <Download size={16} aria-hidden="true" /> File mẫu
          </a>
        </div>

        {camera && (
          <BarcodeCamera
            onCode={(code) => addScanned(code)}
            onError={(message) => {
              setError(message);
              setCamera(false);
            }}
          />
        )}

        <label className="bulk-paste">
          <span className="sr-only">Dán từ Excel</span>
          <textarea
            rows={1}
            placeholder="Dán các dòng từ Excel vào đây (Mã, Đơn vị, Số lượng)"
            value=""
            onChange={() => {}}
            onPaste={(e) => {
              e.preventDefault();
              appendRows(parsePastedText(e.clipboardData.getData("text")));
            }}
          />
        </label>

        {notice && (
          <p className="stock-notice" role="status">
            {notice}
          </p>
        )}
        {error && (
          <p className="stock-error" role="alert">
            {error}
          </p>
        )}

        {lines.length === 0 ? (
          <p className="stock-hint">Chưa có dòng hàng nào.</p>
        ) : (
          <div className="stock-table-wrap">
            <table className="stock-table bulk-table">
              <thead>
                <tr>
                  <th scope="col">#</th>
                  <th scope="col">Mã</th>
                  <th scope="col">Mặt hàng</th>
                  <th scope="col">Đơn vị</th>
                  <th scope="col" className="num">Số lượng</th>
                  <th scope="col" className="num">Quy đổi</th>
                  <th scope="col"><span className="sr-only">Thao tác</span></th>
                </tr>
              </thead>
              <tbody>
                {checked.map(({ line, result }, i) => {
                  const message = serverErrors[i + 1] || result.error;
                  return (
                    <tr key={line.key} className={message ? "is-invalid" : ""}>
                      <td data-label="#">{i + 1}</td>
                      <td data-label="Mã">
                        <input
                          aria-label={`Mã dòng ${i + 1}`}
                          value={line.code}
                          maxLength={100}
                          onChange={(e) => changeLine(line.key, { code: e.target.value })}
                        />
                      </td>
                      <td data-label="Mặt hàng">
                        {result.product?.name ?? "—"}
                        {message && <small className="bulk-line-error">{message}</small>}
                      </td>
                      <td data-label="Đơn vị">
                        <select
                          aria-label={`Đơn vị dòng ${i + 1}`}
                          value={line.unitName}
                          disabled={!result.product || result.unitFromBarcode}
                          onChange={(e) => changeLine(line.key, { unitName: e.target.value })}
                        >
                          <option value="">{result.unit?.unitName ?? "Chọn đơn vị"}</option>
                          {(result.product?.units ?? [])
                            .filter((u) => u.unitName !== result.unit?.unitName || line.unitName)
                            .map((u) => (
                              <option key={u.unitName} value={u.unitName}>
                                {u.unitName}
                              </option>
                            ))}
                        </select>
                      </td>
                      <td data-label="Số lượng" className="num">
                        <input
                          aria-label={`Số lượng dòng ${i + 1}`}
                          type="number"
                          inputMode="numeric"
                          min="1"
                          max="100000"
                          value={line.quantity}
                          onChange={(e) => changeLine(line.key, { quantity: e.target.value })}
                        />
                      </td>
                      <td data-label="Quy đổi" className="num">
                        {result.baseQuantity ? `${number(result.baseQuantity)} ${result.product.baseUnit}` : "—"}
                      </td>
                      <td className="stock-actions">
                        <button
                          type="button"
                          className="stock-icon-button"
                          aria-label={`Xóa dòng ${i + 1}`}
                          onClick={() => setLines((current) => current.filter((l) => l.key !== line.key))}
                        >
                          <Trash2 size={16} />
                        </button>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}

        <div className="stock-form-actions bulk-actions">
          <button
            type="button"
            className="button button-secondary"
            onClick={() => setLines((current) => [...current, withKey({ code: "", unitName: "", quantity: "1" })])}
          >
            <Plus size={16} aria-hidden="true" /> Thêm dòng
          </button>
          <button className="button button-primary" disabled={busy || lines.length === 0 || invalid > 0}>
            {busy ? "Đang nhập kho…" : `Xác nhận nhập kho (${number(lines.length)} dòng)`}
          </button>
        </div>
        <p className="stock-hint">
          Cả phiếu được ghi cùng lúc: có dòng lỗi thì chưa nhập dòng nào. Gửi lại cùng mã phiếu không cộng kho lần hai.
        </p>
      </form>
    </SectionLayout>
  );
}

function BarcodeCamera({ onCode, onError }) {
  const video = useRef(null);
  const handlers = useRef({ onCode, onError });
  useEffect(() => {
    handlers.current = { onCode, onError };
  });

  useEffect(() => {
    let stopped = false;
    let controls = null;
    let last = "";
    let lastAt = 0;
    (async () => {
      try {
        if (!navigator.mediaDevices?.getUserMedia) throw new Error("Camera cần HTTPS hoặc localhost.");
        const { BrowserMultiFormatReader } = await import("@zxing/browser");
        if (stopped) return;
        const reader = new BrowserMultiFormatReader();
        const started = await reader.decodeFromConstraints(
          { video: { facingMode: { ideal: "environment" } }, audio: false },
          video.current,
          (result) => {
            if (!result) return;
            const code = result.getText();
            // The same code stays in frame for many frames; count it once per two seconds.
            if (code === last && Date.now() - lastAt < 2000) return;
            last = code;
            lastAt = Date.now();
            handlers.current.onCode(code);
          },
        );
        if (stopped) started.stop();
        else controls = started;
      } catch (e) {
        if (!stopped)
          handlers.current.onError(
            e.name === "NotAllowedError"
              ? "Chưa được cấp quyền camera. Hãy dùng máy quét hoặc gõ mã vạch."
              : e.message,
          );
      }
    })();
    return () => {
      stopped = true;
      controls?.stop();
    };
  }, []);

  return <video ref={video} className="bulk-camera" muted playsInline aria-label="Camera quét mã vạch" />;
}
