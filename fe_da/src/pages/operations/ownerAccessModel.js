export function accessDecisionActions(item, actorId, role, requestedRole) {
  if (!['owner', 'manager'].includes(role) || Number(item.requestedBy) === Number(actorId) || Number(item.targetUserId) === Number(actorId)) return [];
  if (role === 'manager' && !['Staff', 'Referee'].includes(requestedRole)) return [];
  if (item.status === 'pending') return ['approve', 'reject'];
  if (item.status === 'approved') return ['withdraw'];
  return [];
}
export function accessTargets(users, query = '') {
  const value = query.trim().toLocaleLowerCase('vi');
  return users.filter(u => u.role?.toLowerCase() !== 'owner' && `${u.name} ${u.email}`.toLocaleLowerCase('vi').includes(value));
}
