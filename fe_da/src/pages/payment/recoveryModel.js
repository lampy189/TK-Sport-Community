export const recoveryLabels={pending:'Chưa xác nhận',failed:'Thất bại / đã đóng',review_required:'Cần đối soát',paid:'Đã xác nhận tiền',PAID:'Đã xác nhận tiền',PENDING:'Đang chờ',CANCELLED:'Đã hủy',EXPIRED:'Hết hạn',FAILED:'Thất bại',matched:'Đã đối chiếu'};
const titles={payment_recovery_requested:'Bắt đầu xử lý',payment_recovery_note:'Ghi chú đối soát',payment_recovery_completed:'Đã xử lý',payment_recovery_failed:'Chưa xử lý được',payment_recovery_closed:'Đóng giao dịch chưa thu tiền',payment_recovery_approved:'Đã duyệt thanh toán',goods_transfer_review:'Giao dịch hàng hóa cần đối soát',goods_transfer_confirmed:'Đã xác nhận chuyển khoản',payos_payment_exception:'Giao dịch vé cần đối soát',payos_payment_confirmed:'Đã xác nhận tiền vé',flagged_payment_approved:'Đã duyệt tiền vé'};
export function historyDetails(history){
 let data;try{data=JSON.parse(history.detail);}catch{data={evidence:history.detail};}
 if(!data||typeof data!=='object')data={evidence:String(data??'')};
 return {title:titles[history.action]||'Cập nhật giao dịch',at:data.at,evidence:data.evidence,reason:data.reason||(typeof data.result==='string'?data.result:data.result?.reason),
  status:recoveryLabels[data.result?.status||data.Status]||'',reference:data.Reference,expected:data.expected,received:data.received};
}
