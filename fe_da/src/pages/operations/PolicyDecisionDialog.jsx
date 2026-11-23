import { useEffect, useRef } from 'react';
export default function PolicyDecisionDialog({ decision, reason, onReason, busy, error, onDecide, onClose }) {
  const ref = useRef(null);
  useEffect(() => {
    const previous = document.activeElement;
    ref.current.showModal();
    return () => { if (previous?.isConnected) previous.focus(); };
  }, []);
  return <dialog ref={ref} className="owner-decision-dialog" aria-labelledby="owner-policy-decision-title" onCancel={e => { e.preventDefault(); if (!busy) onClose(); }}>
    <form className="ops-form" onSubmit={e => e.preventDefault()}>
      <h2 id="owner-policy-decision-title">Duyệt {decision.name} / v{decision.version}</h2><p>{decision.value}</p><p>Lý do đề xuất: {decision.submitReason}</p>
      {error && <p role="alert">{error}</p>}
      <label>Lý do quyết định<textarea autoFocus required maxLength={1000} value={reason} disabled={busy} onChange={e => onReason(e.target.value)} /></label>
      <div className="ops-actions"><button disabled={busy || !reason.trim()} onClick={() => onDecide(true)}>Duyệt</button><button disabled={busy || !reason.trim()} onClick={() => onDecide(false)}>Từ chối</button><button disabled={busy} onClick={onClose}>Đóng</button></div>
    </form>
  </dialog>;
}
