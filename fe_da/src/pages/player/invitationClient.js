import { workflowRequest } from '../workflowClient';
export const invitationClient = {
  list: () => workflowRequest('/api/invitations'),
  eligibleSessions: friendPlayerId => workflowRequest(`/api/invitations/friends/${friendPlayerId}/eligible-sessions`),
  candidates: id => workflowRequest(`/api/invitations/sessions/${id}/candidates`),
  send: (id, friendPlayerId) => workflowRequest(`/api/invitations/sessions/${id}`, 'POST', { friendPlayerId }),
  reply: (id, accept) => workflowRequest(`/api/invitations/${id}/reply`, 'POST', { accept }),
};
export { invitationState, invitationPayable } from './invitationState.js';
