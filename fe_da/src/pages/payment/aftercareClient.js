import { request } from '../../api';
async function call(path, body) {
 const response=await request('/api/ticket-aftercare/'+path,{cache:'no-store',...(body===undefined?{}:{method:'POST',body:JSON.stringify(body)})});
 if(!response.ok){const data=await response.json().catch(()=>({}));throw new Error(data.message||'Không thể xử lý. Hãy tải lại và thử lại.');}
 return response.json();
}
export const aftercare={
 decideRefund:(id,input)=>call(`refunds/${id}/decision`,input),
 refundPayout:(id,input)=>call(`refunds/${id}/payout`,input),
 failRefund:(id,reason)=>call(`refunds/${id}/fail`,{reason}),
 refundHistory:id=>call(`refunds/${id}/history`),
 report:(id,category,content,imageBase64)=>call(`tickets/${id}/reports`,{category,content,imageBase64}),
 incidents:()=>call('incidents'),
 inbox:()=>call('inbox'),
 messages:id=>call(`tickets/${id}/messages`),
 sendMessage:(id,content,imageBase64)=>call(`tickets/${id}/messages`,{content,imageBase64}),
 sessions:()=>call('sessions'),reviews:()=>call('reviews'),changes:()=>call('changes'),refunds:()=>call('refunds'),
 issue:(id,reason,cancel)=>call(`sessions/${id}/issue`,{reason,cancel}),
 options:id=>call(`tickets/${id}/alternatives`),
 change:(id,sessionId)=>call(`tickets/${id}/change`,{sessionId}),
 cancelChange:id=>call(`changes/${id}/cancel`,{}),
 refund:id=>call(`tickets/${id}/refund`,{}),
 completeChange:(id,evidence,bankReference)=>call(`changes/${id}/complete`,{evidence,bankReference}),
 completeRefund:(id,evidence,bankReference)=>call(`refunds/${id}/complete`,{evidence,bankReference}),
 approve:(id,bankTransactionId,evidence)=>call(`reviews/${id}/approve`,{bankTransactionId,evidence}),
};
