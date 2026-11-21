import { useEffect, useState } from 'react';
import { workflowRequest } from '../workflowClient';

export default function RuntimeDashboard({ role }) {
  const [state, setState] = useState({ loading: true }), [revision, setRevision] = useState(0);
  useEffect(() => {
    let live = true;
    const extra = role === 'owner' ? '/api/v1/owner-policies' : ['manager', 'system-admin'].includes(role) ? '/api/operations/access' : role === 'referee' ? '/api/v1/skill/sessions' : '/api/v1/commerce/goods-orders';
    Promise.allSettled([workflowRequest('/api/operations/overview'), workflowRequest(extra)]).then(results => {
      if (!live) return;
      const [overview, detail] = results, metrics = [], errors = results.filter(r => r.status === 'rejected').map(r => r.reason.message);
      if (overview.status === 'fulfilled' && role !== 'system-admin') {
        metrics.push([role === 'owner' ? 'Buổi toàn chuỗi hôm nay' : 'Buổi tại cơ sở hôm nay', overview.value.sessions.reduce((n, s) => n + s.count, 0)]);
        metrics.push(['Vé đã điểm danh hôm nay', overview.value.tickets.filter(t => t.status === 'checkedin').reduce((n, s) => n + s.count, 0)]);
      }
      if (detail.status === 'fulfilled') {
        const data = detail.value;
        if (role === 'owner') metrics.push(['Chính sách/giá chờ duyệt', data.filter(p => p.status === 'submitted').length]);
        else if (['manager', 'system-admin'].includes(role)) metrics.push([role === 'system-admin' ? 'Quyền đã duyệt chờ triển khai' : 'Yêu cầu nhân sự chờ duyệt', data.requests.filter(p => p.status === (role === 'system-admin' ? 'approved' : 'pending')).length]);
        else if (Array.isArray(data)) metrics.push([role === 'referee' ? 'Buổi được giao từ hôm nay' : 'Đơn hàng chưa thanh toán', role === 'referee' ? data.length : data.filter(o => String(o.paymentState).toLowerCase() === 'unpaid').length]);
      }
      setState({ metrics, errors, at: new Date() });
    });
    return () => { live = false; };
  }, [role, revision]);
  return <section aria-label="Số liệu thực tế"><button className="button button-secondary" disabled={state.loading} onClick={() => { setState({ loading: true }); setRevision(r => r + 1); }}>Cập nhật số liệu</button>{state.loading && <p role="status">Đang tải số liệu…</p>}{state.errors?.map(error => <p role="alert" key={error}>{error}</p>)}<div className="stats-grid">{state.metrics?.map(([label, value]) => <div className="stat-card" key={label}><strong>{value}</strong><span>{label}</span></div>)}</div>{state.at && <small>Cập nhật {state.at.toLocaleTimeString('vi-VN')}</small>}</section>;
}
