// Historical forms kept the four observations in evidence before SliderScores existed.
// Reconstruct detailed display points without rewriting their published total or rating.
export function detailedCriterionPoints(row) {
  if (!row) return null;
  const header = String(row.evidence || '').match(/^\[Chỉ số thành phần:\s*([^\]]+)\]/);
  if (header) {
    const parts = header[1].split('•')[0].split('|');
    const values = parts.map(part => {
      const match = part.match(/:\s*(\d+(?:\.\d+)?)\s*\/\s*10\s*$/);
      return match ? Number(match[1]) : NaN;
    });
    if (values.length === 4 && values.every(v => Number.isInteger(v) && v >= 0 && v <= 10))
      return values.reduce((sum, v) => sum + v, 0) / 2;
  }
  if (row.weightedPoints != null && Number.isFinite(Number(row.weightedPoints)))
    return Number(row.weightedPoints);
  const score = row.score ?? row.rawScore;
  return score == null ? null : Number(score) * 5;
}

export const formatCriterionPoints = value => new Intl.NumberFormat('vi-VN', {
  maximumFractionDigits: 1,
}).format(value);
