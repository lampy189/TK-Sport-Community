import { request } from '../../api';
async function call(path, body) {
 const response=await request('/api/ticket-aftercare/'+path,{cache:'no-store',...(body===undefined?{}:{method:'POST',body:JSON.stringify(body)})});
 if(!response.ok){const data=await response.json().catch(()=>({}));throw new Error(data.message||'Không thể xử lý. Hãy tải lại và thử lại.');}
 return response.json();
}
export const aftercare={
<<<<<<< HEAD
=======
 report:(id,category,content,imageBase64)=>call(`tickets/${id}/reports`,{category,content,imageBase64}),
 incidents:()=>call('incidents'),
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
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
