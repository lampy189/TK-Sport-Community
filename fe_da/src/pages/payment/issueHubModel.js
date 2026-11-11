export function buildIssues({reviews=[],sessions=[],refunds=[],inbox=[]}) {
 return [
  ...inbox.map(item=>({key:`inbox-${item.id}`,kind:'inbox',item,title:`Vé #${item.ticketId} · ${item.lastSender || item.holder}`,type:'Báo cáo / trao đổi Player',summary:item.lastMessage || 'Ảnh đính kèm',status:item.lastIsStaff?'Đã phản hồi':'Chờ phản hồi'})),
  ...reviews.map(item=>({key:`reviews-${item.id}`,kind:'reviews',item,title:item.referenceCode,type:'Thanh toán lỗi',summary:`${item.payer} · ${item.amountVnd.toLocaleString('vi-VN')} ₫`,status:'Cần đối soát'})),
  ...sessions.filter(item=>['suspended','cancelled'].includes(item.status.toLowerCase())).map(item=>({key:`sessions-${item.id}`,kind:'sessions',item,title:`${item.sport} · ${item.court}`,type:'Sự cố sân',summary:`${item.venue} · ${item.sessionDate}`,status:item.status.toLowerCase()==='cancelled'?'Đã hủy buổi':'Tạm ngưng'})),
  ...refunds.map(item=>({key:`refunds-${item.id}`,kind:'refunds',item,title:`Hoàn vé #${item.ticketId} · ${item.recipient}`,type:'Hoàn tiền tồn',summary:`${item.amountVnd.toLocaleString('vi-VN')} ₫ · ${item.referenceCode}`,status:'Cần xử lý hoàn'})),
 ];
}
