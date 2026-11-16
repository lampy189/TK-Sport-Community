import { lazy, Suspense, useEffect, useState } from 'react';
import { SignInView } from './components/SignInView';
import { PortalSignIn } from './components/PortalSignIn';
import { roleKeys, portalForRole } from './config/loginPortals';
import { resolveHomeRoute, loginPaths } from './config/homeRoutes';
import { api } from './api';
import HomePage from './pages/home/HomePage';

function Redirect({ to }) {
  useEffect(() => {
    const url = new URL(window.location.href);
    url.pathname = to;
    // Preserve orderId and payment return parameters across authentication routing.
    window.location.replace(url.href);
  }, [to]);
  return <div className="home-loading" role="status">Đang chuyển trang…</div>;
}
function ApplicationApp() {
  const [user, setUser] = useState(null);
  const [loading, setLoading] = useState(true);
  useEffect(() => {
    let active = true;
    api.getMe().then(account => { if (active) setUser(account); }).catch(() => { if (active) setUser(null); }).finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, []);
  async function logout() {
    try {
      if (!await api.logout()) throw new Error('logout_failed');
      window.location.replace(loginPaths[portalForRole(user.role)]);
    } catch { window.alert('Đăng xuất chưa thành công. Vui lòng thử lại.'); }
  }
  if (loading) return <div className="home-loading" role="status"><strong>↗ TKSC</strong><span>Đang kết nối hệ thống…</span></div>;
  if (user && !roleKeys[user.role]) return <p>Vai trò không được hỗ trợ. <button onClick={logout}>Đăng xuất</button></p>;
  const route = !user && window.location.pathname === '/' && new URLSearchParams(window.location.search).has('loginError')
    ? { redirect: '/player/login' } : resolveHomeRoute(window.location.pathname, user?.role);
  if (route.redirect) return <Redirect to={route.redirect} />;
  if (route.view === 'home') return <HomePage key={user.id + ':' + user.role} role={roleKeys[user.role]} user={user} onLogout={logout} />;
  if (route.view === 'guest') return <HomePage role="guest" onLogin={() => { window.location.assign('/player/login' + window.location.search); }} />;
  if (route.view === 'login') return route.portal === 'player'
    ? <SignInView onLoginSuccess={setUser} onContinueAsGuest={() => window.location.assign('/guest/home')} />
    : <PortalSignIn portal={route.portal} onLoginSuccess={setUser} />;
  return <main className="home-loading"><h1>Không tìm thấy trang</h1><a href="/">Về trang chủ</a></main>;
}
const HomePreview = lazy(() => import('./pages/home/HomePreview'));
export default function App() {
  const showPreview = import.meta.env.DEV && new URLSearchParams(window.location.search).get('preview') === '1';
  return showPreview ? <Suspense fallback={<p>Đang tải bản mẫu…</p>}><HomePreview /></Suspense> : <ApplicationApp />;
}
