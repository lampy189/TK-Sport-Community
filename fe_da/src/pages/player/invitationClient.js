import { workflowRequest } from '../workflowClient';
export const invitationClient = {
  list: () => workflowRequest('/api/invitations'),
  candidates: id => workflowRequest(`/api/invitations/sessions/${id}/candidates`),
  send: (id, friendPlayerId, paymentChoice) => workflowRequest(`/api/invitations/sessions/${id}`, 'POST', { friendPlayerId, paymentChoice }),
  reply: (id, accept) => workflowRequest(`/api/invitations/${id}/reply`, 'POST', { accept }),
};
export { invitationState, invitationPayable } from './invitationState.js';
