export const roleHomePaths = {
  Player: '/player/home',
  Referee: '/referee/home',
  Staff: '/staff/home',
  Manager: '/manager/home',
  Owner: '/owner/home',
  SystemAdmin: '/admin/home',
};
export const loginPaths = { player: '/player/login', staff: '/staff/login', admin: '/admin/login' };
export function resolveHomeRoute(pathname, role) {
  const path = pathname.replace(/\/+$/, '') || '/';
  const home = roleHomePaths[role];
  const protectedRole = Object.keys(roleHomePaths).find(key => roleHomePaths[key] === path);
  const login = Object.values(loginPaths).includes(path);
  if (home && (protectedRole || login || path === '/' || path === '/guest/home'))
    return path === home ? { view: 'home' } : { redirect: home };
  if (protectedRole) return { redirect: loginPaths[protectedRole === 'SystemAdmin' ? 'admin' : protectedRole === 'Player' ? 'player' : 'staff'] };
  if (path === '/') return { redirect: '/guest/home' };
  if (path === '/guest/home') return { view: 'guest' };
  if (login) return { view: 'login', portal: path.split('/')[1] };
  return { view: 'not-found' };
}
