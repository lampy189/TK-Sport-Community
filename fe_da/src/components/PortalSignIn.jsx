import { useState } from 'react';
import { ShieldCheck, Users, ArrowUpRight } from 'lucide-react';
import { api } from '../api';
import './portal.css';

import { portalForRole, portalPath } from '../config/loginPortals';
export function PortalSignIn({ portal, user, onLoginSuccess, onLogout }) {
  const admin = portal === 'admin';
  const [email, setEmail] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState(new URLSearchParams(window.location.search).has('loginError') ? 'Không thể đăng nhập. Hãy dùng tài khoản Google đã được liên kết và cấp đúng quyền cho cổng này.' : '');
  async function login(event) {
    event.preventDefault(); setBusy(true); setError('');
    try { onLoginSuccess(await api.devLogin(email.trim(), portal)); }
    catch (err) { setError(err.message); }
    finally { setBusy(false); }
  }
  return <main className={'portal-page ' + (admin ? 'portal-admin' : '')}>
    <div className="portal-card">
      <section className="portal-form">
        <a className="portal-brand" href="/"><ArrowUpRight /> TKSC</a>
        <span className="portal-eyebrow">{admin ? 'SYSTEM ADMIN' : 'ĐỘI NGŨ TK SPORT'}</span>
        <h1>{admin ? 'Đăng nhập quản trị' : 'Đăng nhập nội bộ'}</h1>
        <p>{admin ? 'Cổng riêng dành cho quản trị viên hệ thống.' : 'Không gian chung cho Trọng tài, Nhân viên, Quản lý và Chủ sân.'}</p>
        {error && <div className="portal-error" role="alert">{error}</div>}
        {user ? <div>
          <p>Bạn đang đăng nhập với tài khoản <strong>{user.fullName}</strong> ({user.role}). Tài khoản này không thuộc cổng hiện tại.</p>
          <a href={portalPath(portalForRole(user.role))}>Về cổng của bạn</a>
          <button className="portal-primary" onClick={onLogout}>Đăng xuất để đổi tài khoản</button>
        </div> : <>
          <button className="portal-primary" onClick={() => api.googleLogin(portal)}>Tiếp tục với Google <ArrowUpRight size={18}/></button>
          <p className="portal-note">Sử dụng tài khoản đã được cấp quyền và liên kết Google. Liên hệ quản trị viên nếu bạn chưa có quyền truy cập.</p>
          {import.meta.env.DEV && <details className="portal-dev"><summary>Đăng nhập thử nghiệm (Development)</summary>
            <form onSubmit={login}><label htmlFor="portal-email">Email tài khoản có sẵn</label>
              <input id="portal-email" type="email" autoComplete="email" required value={email} onChange={e => setEmail(e.target.value)} placeholder="name@tksc.local" />
              <button className="portal-primary" disabled={busy || !email.trim()}>{busy ? 'Đang xác thực…' : 'Đăng nhập thử nghiệm'}</button>
            </form>
          </details>}
        </>}
        <nav className="portal-links" aria-label="Cổng đăng nhập"><a href="/player/login">Người chơi</a><a href={admin ? '/staff/login' : '/admin/login'}>{admin ? 'Đội ngũ vận hành' : 'Quản trị hệ thống'}</a></nav>
      </section>
      <aside className="portal-aside">{admin ? <ShieldCheck size={56}/> : <Users size={56}/>}
        <h2>{admin ? 'Quản trị hệ thống.\nKiểm soát truy cập.' : 'Một đội ngũ.\nCùng vận hành.'}</h2>
        <p>{admin ? 'Quản lý quyền truy cập và hỗ trợ kỹ thuật cho TK Sport Community.' : 'Kết nối công việc tại sân trong một không gian dành riêng cho đội ngũ.'}</p>
        <div className="portal-badges">{(admin ? ['System Admin'] : ['Referee', 'Staff', 'Manager', 'Owner']).map(role => <span key={role}>{role}</span>)}</div>
      </aside>
    </div>
  </main>;
}
