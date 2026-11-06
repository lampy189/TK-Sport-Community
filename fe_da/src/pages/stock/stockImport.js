// Client-side preview for bulk receipts. The API validates every line again and stays authoritative.
const normalizeCode = (value) => String(value ?? "").trim().toUpperCase();
const sameName = (a, b) => String(a ?? "").trim().toLowerCase() === String(b ?? "").trim().toLowerCase();

export function newSourceRef(now = new Date()) {
  const pad = (n) => String(n).padStart(2, "0");
  return `PN-${now.getFullYear()}${pad(now.getMonth() + 1)}${pad(now.getDate())}-${pad(now.getHours())}${pad(now.getMinutes())}${pad(now.getSeconds())}`;
}

export function buildCodeIndex(products) {
  const barcodes = new Map();
  const skus = new Map();
  products.forEach((product) => {
    skus.set(normalizeCode(product.sku), product);
    (product.units ?? []).forEach((unit) => {
      if (unit.barcode) barcodes.set(normalizeCode(unit.barcode), { product, unit });
    });
  });
  return {
    find(code) {
      const key = normalizeCode(code);
      if (barcodes.has(key)) return { ...barcodes.get(key), fromBarcode: true };
      if (skus.has(key)) return { product: skus.get(key), unit: null, fromBarcode: false };
      return null;
    },
  };
}

export function validateLine(line, index) {
  const code = normalizeCode(line.code);
  if (!code) return { error: "Thiếu mã vạch hoặc mã hàng." };
  const found = index.find(code);
  if (!found) return { error: `Không tìm thấy mặt hàng có mã ${code}.` };
  const { product, fromBarcode } = found;
  const base = (product.units ?? []).find((u) => u.factorToBase === 1);
  let unit = found.unit;
  if (fromBarcode) {
    if (line.unitName && !sameName(line.unitName, unit.unitName))
      return { product, unit, unitFromBarcode: true, error: `Mã vạch này là của đơn vị '${unit.unitName}'.` };
  } else {
    unit = line.unitName ? (product.units ?? []).find((u) => sameName(u.unitName, line.unitName)) : base;
    if (!unit) return { product, unit: base, error: `${product.name} không có đơn vị '${line.unitName}'.` };
  }
  if (!product.isActive) return { product, unit, unitFromBarcode: fromBarcode, error: `${product.name} đã ngừng kinh doanh.` };
  const quantity = Number(line.quantity);
  if (!Number.isInteger(quantity) || quantity < 1 || quantity > 100000)
    return { product, unit, unitFromBarcode: fromBarcode, error: "Số lượng phải là số nguyên từ 1 đến 100.000." };
  return { product, unit, unitFromBarcode: fromBarcode, baseQuantity: quantity * unit.factorToBase };
}

// Scanning the same code again adds one to its line.
export function mergeScannedLine(lines, code, withKey) {
  const key = normalizeCode(code);
  const existing = lines.find((l) => normalizeCode(l.code) === key && !l.unitName);
  if (existing)
    return lines.map((l) => (l === existing ? { ...l, quantity: String(Number(l.quantity || 0) + 1) } : l));
  return [...lines, withKey({ code: key, unitName: "", quantity: "1" })];
}

// Rows are [code, unit, quantity]; a header row and blank rows are skipped.
export function parseImportRows(rows) {
  return rows
    .map((row) => row.map((cell) => (cell == null ? "" : String(cell).trim())))
    .filter((row) => row.some((cell) => cell !== ""))
    .filter((row, i) => !(i === 0 && /^(mã|ma|code|barcode)/i.test(row[0] ?? "")))
    .map((row) => ({ code: row[0] ?? "", unitName: row[1] ?? "", quantity: row[2] ?? "" }));
}

export function parsePastedText(text) {
  return String(text ?? "")
    .split(/\r?\n/)
    .map((line) => line.split("\t"));
}
