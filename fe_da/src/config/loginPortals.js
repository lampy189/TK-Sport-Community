export const roleKeys = { Player: 'player', Referee: 'referee', Staff: 'staff', Manager: 'manager', Owner: 'owner', SystemAdmin: 'system-admin' };
export const portalForRole = role => role === 'SystemAdmin' ? 'admin' : role === 'Player' ? 'player' : 'staff';
export const portalPath = portal => portal === 'player' ? '/player/login' : '/' + portal + '/login';
