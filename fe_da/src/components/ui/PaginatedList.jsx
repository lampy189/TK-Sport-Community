import { useManagerUI } from './managerUiState';
import { useState } from 'react';
import './pagination.css';

// Keep paging local to each list; totals and business operations retain the full dataset.
export default function PaginatedList({ items = [], children, columns, hideShortPager = false, listItem = false, label = 'Phân trang danh sách', pageSize }) {
  const lockedSize = Number.isFinite(pageSize) && pageSize > 0 ? pageSize : null;
  const manager = useManagerUI();
  const identity = JSON.stringify(items.map((item, index) => item.id ?? item.key ?? item.playerId ?? item.friendPlayerId ?? item.item?.id ?? item.date ?? index));
  const [state, setState] = useState({ identity, page: 1, size: lockedSize || 10 });
  const size = lockedSize || state.size;
  if (state.identity !== identity || state.size !== size) setState({ identity, page: state.identity === identity ? state.page : 1, size });
  const pages = Math.max(1, Math.ceil(items.length / size));
  const page = state.identity === identity ? Math.min(state.page, pages) : 1;
  const start = (page - 1) * size;
  const change = (next, nextSize = size) => setState({ identity, page: next, size: lockedSize || nextSize });
  const numbers = [...new Set([1, page - 1, page, page + 1, pages])].filter(value => value >= 1 && value <= pages).sort((a, b) => a - b);
  const controls = items.length > (lockedSize ?? (hideShortPager ? 10 : 0)) && (!manager || pages > 1 || (!lockedSize && size > 10)) && <nav className="tksc-pagination" aria-label={label}>
    <span className="tksc-pagination-summary" aria-live="polite">{start + 1}–{Math.min(start + size, items.length)} / {items.length} mục</span>
    {!lockedSize && <label>Số mục / trang<select value={size} onChange={event => change(1, Number(event.target.value))}>{[10, 20, 50].map(option => <option key={option} value={option}>{option}</option>)}</select></label>}
    <div className="tksc-pagination-pages">
      <button type="button" aria-label="Trang trước" disabled={page === 1} onClick={() => change(page - 1)}>Trước</button>
      {numbers.map((number, index) => <span className="tksc-pagination-number" key={number}>
        {index > 0 && number - numbers[index - 1] > 1 && <span aria-hidden="true">…</span>}
        <button type="button" aria-label={`Trang ${number}`} aria-current={page === number ? 'page' : undefined} onClick={() => change(number)}>{number}</button>
      </span>)}
      <button type="button" aria-label="Trang sau" disabled={page === pages} onClick={() => change(page + 1)}>Sau</button>
    </div>
  </nav>;
  return <>{items.slice(start, start + size).map((item, index) => children(item, start + index))}{controls && (columns ? <tr className="tksc-pagination-row"><td colSpan={columns}>{controls}</td></tr> : listItem ? <li className="tksc-pagination-list-item">{controls}</li> : controls)}</>;
}
