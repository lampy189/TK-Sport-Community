import { useEffect, useState } from 'react';
import { workflowRequest } from '../workflowClient';
import './revenue-demo.css';

export default function SportCenterPersonnel() {
  const [data, setData] = useState(null);
  const [error, setError] = useState('');
  const [saving, setSaving] = useState(null);
  useEffect(() => {
    let active = true;
    Promise.all([workflowRequest('/api/v1/sport-centers'), workflowRequest('/api/v1/sport-centers/personnel')])
      .then(([scope, people]) => { if (active) setData({ scope, people }); })
      .catch(e => { if (active) setError(e.message); });
    return () => { active = false; };
  }, []);
  async function assign(person, venueId) {
    if (!venueId) return;
    setSaving(person.id); setError('');
    try {
      await workflowRequest(`/api/v1/sport-centers/personnel/${person.id}`, 'PUT', { venueId: Number(venueId) });
      setData(value => ({ ...value, people: value.people.map(p => p.id === person.id ? { ...p, venueId: Number(venueId) } : p) }));
    } catch (e) { setError(e.message); }
    finally { setSaving(null); }
  }
  return <section className="revenue-demo"><h1>Phân quyền Sport Center</h1>{error && <p role="alert">{error}</p>}
    {!data ? <p>Đang tải nhân sự…</p> : <div className="revenue-demo-table"><table><thead><tr><th>Nhân sự</th><th>Vai trò</th><th>Sport Center</th></tr></thead><tbody>{data.people.map(person => <tr key={person.id}><td>{person.name}<small>{person.email}</small></td><td>{person.role}</td><td><select aria-label={`Sport Center của ${person.name}`} value={person.venueId || ''} disabled={saving !== null} onChange={e => assign(person, e.target.value)}><option value="" disabled>Chưa phân công</option>{data.scope.centers.filter(c => c.active || c.id === person.venueId).map(c => <option key={c.id} value={c.id}>{c.name}</option>)}</select></td></tr>)}</tbody></table></div>}
    {saving !== null && <p role="status">Đang lưu phân công…</p>}
  </section>;
}
