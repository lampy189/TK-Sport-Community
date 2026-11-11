import { useState } from 'react';
import { useCenterOptions } from './useCenterOptions';
import { api } from '../api';
import './center-selection.css';

export function LoginCenterChoice({ options, value, onChange }) {
  if (options.error) return <p role="alert">{options.error}</p>;
  if (!options.enabled) return null;
  return <div className="center-choice">
    <select value={value} onChange={e => onChange(e.target.value)} aria-label="Cơ sở đăng nhập">
      <option value="">Chọn cơ sở</option>
      {options.centers.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
    </select>
  </div>;
}

export function CenterSwitcher({ user, onLogout }) {
  const options = useCenterOptions();
  const current = String(user.selectedVenueId || '');
  const [selected, setSelected] = useState(current);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const owner = String(user.role).toLowerCase() === 'owner';
  const personnel = ['staff', 'manager', 'referee'].includes(String(user.role).toLowerCase());
  async function change() {
    setBusy(true); setError('');
    try {
      await api.selectCenter(selected ? Number(selected) : null);
      // Drop all cached screens, carts and filter state from the previous center.
      window.location.assign(window.location.pathname);
    } catch (e) { setError(e.message); setBusy(false); }
  }
  return <section className={`center-switcher${user.centerRequired ? ' center-gate' : ''}`} aria-label="Cơ sở đang làm việc">
    <strong>{user.centerRequired ? 'Chọn cơ sở' : 'Cơ sở'}</strong>
    <label><span className="center-sr-only">Cơ sở</span><select value={selected} onChange={e => setSelected(e.target.value)} disabled={busy || options.loading || personnel}>
      <option value="">{owner ? 'Toàn chuỗi' : 'Chọn cơ sở'}</option>
      {options.centers.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
    </select></label>
    {!personnel && <button className="button button-primary" disabled={busy || options.loading || (!owner && !selected) || (!user.centerRequired && selected === current)} onClick={change}>{busy ? 'Đang chuyển…' : user.centerRequired ? 'Tiếp tục' : 'Đổi cơ sở'}</button>}
    {personnel && user.centerRequired && <p role="alert">Chưa có cơ sở hoạt động được phân công. Liên hệ Owner để được hỗ trợ.</p>}
    {(error || options.error) && <p role="alert">{error || options.error}</p>}
    {user.centerRequired && <button className="button button-secondary" onClick={onLogout}>Đăng xuất</button>}
  </section>;
}
