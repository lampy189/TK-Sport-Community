using System.Data;
using Microsoft.EntityFrameworkCore;
using TKSC.Application.DTOs;
using TKSC.Application.Notifications;
using TKSC.Domain.Entities;
using TKSC.Infrastructure.DbContext;

namespace TKSC.Application.Services;
public sealed class TicketAftercareService(Sep490Context db, TimeProvider clock)
{
 private DateTimeOffset Now => clock.GetUtcNow();
 private static void Require(bool ok,string message,int code=409) { if(!ok) throw new PaymentException(code,message); }
 private static string Evidence(string? text) { text=text?.Trim(); Require(text is {Length:>=10 and <=1000},"Nhập nội dung/bằng chứng từ 10 đến 1000 ký tự.",400); return text!; }
 private static bool Paid(string status)=>status.Equals("paid",StringComparison.OrdinalIgnoreCase);
 private void Audit(long actor,string action,string entity,long id,string? old,string value)=>db.AuditLogs.Add(new AuditLog{ActorId=actor,Action=action,EntityType=entity,EntityId=id,OldValue=old,NewValue=$"{Now:O} | {value}"});
 private void Notify(Ticket t,string kind)=>db.Notifications.AddRange(TicketNotifications.Create(kind,t.Id,t.Player.UserId,t.Order.PayerId));
 private async Task<T> Transaction<T>(Func<Task<T>> work)
 {
  db.ChangeTracker.Clear();
  await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
  var result=await work(); await db.SaveChangesAsync(); await tx.CommitAsync(); return result;
 }
 private async Task LockOrder(long id) => _=await db.SocialBookingOrders.FromSqlInterpolated($"SELECT * FROM social_booking_orders WITH (UPDLOCK,HOLDLOCK) WHERE id={id}").SingleOrDefaultAsync();
 private async Task<Session> Session(long id,bool locked=true)
 {
  if(locked) _=await db.Sessions.FromSqlInterpolated($"SELECT * FROM sessions WITH (UPDLOCK,HOLDLOCK) WHERE id={id}").SingleOrDefaultAsync();
  return await db.Sessions.Include(s=>s.TimeSlot).Include(s=>s.Sport).Include(s=>s.Court).ThenInclude(c=>c.Venue).SingleOrDefaultAsync(s=>s.Id==id)
   ?? throw new PaymentException(404,"Không tìm thấy buổi chơi.");
 }
 private async Task<Ticket> Ticket(long id,long? user=null)
 {
  var orderId=await db.Tickets.Where(t=>t.Id==id).Select(t=>(long?)t.OrderId).SingleOrDefaultAsync()??throw new PaymentException(404,"Không tìm thấy vé.");
  await LockOrder(orderId);
  var t=await db.Tickets.Include(t=>t.Order).ThenInclude(o=>o.Payments).Include(t=>t.Player).ThenInclude(p=>p.User).Include(t=>t.Refunds)
   .SingleAsync(t=>t.Id==id);
  if(user.HasValue) Require(t.Player.UserId==user || t.Order.PayerId==user,"Không tìm thấy vé của bạn.",404);
  t.Session=await Session(t.SessionId);
  return t;
 }
 private IQueryable<Ticket> Occupied(long sessionId,long except=0) => db.Tickets.Where(t=>t.SessionId==sessionId && t.Id!=except &&
  (new[]{"confirmed","checkedin","valid","paid"}.Contains(t.Status.ToLower()) || (new[]{"pending","reserved"}.Contains(t.Status.ToLower()) && t.Order.ExpiresAt>Now)));
 private async Task Eligibility(long playerId,Session session,long except)
 {
  Require(await db.Players.AnyAsync(p=>p.Id==playerId && p.User.DeletedAt==null && p.User.Status.ToLower()=="active"),"Tài khoản người chơi không còn hoạt động.");
  if(!string.IsNullOrWhiteSpace(session.Band) && !await db.SessionExceptions.AnyAsync(e=>e.PlayerId==playerId && e.SessionId==session.Id && e.ApprovedBy!=null))
  {
   var profile=await db.PlayerSportProfiles.SingleOrDefaultAsync(p=>p.PlayerId==playerId && p.SportId==session.SportId);
   Require(profile?.CurrentScore!=null && !string.IsNullOrWhiteSpace(profile.CurrentBand) && profile.RatingStatus?.ToLowerInvariant() is not ("unrated" or "pendingsurvey") && string.Equals(profile.CurrentBand.Trim(),session.Band.Trim(),StringComparison.OrdinalIgnoreCase),"Người chơi không đáp ứng trình độ của buổi mới.");
  }
  Require(!await db.Tickets.AnyAsync(t=>t.Id!=except && t.PlayerId==playerId && t.Session.SessionDate==session.SessionDate
    && t.Session.TimeSlot.StartTime<session.TimeSlot.EndTime && t.Session.TimeSlot.EndTime>session.TimeSlot.StartTime
    && (new[]{"confirmed","checkedin","valid","paid"}.Contains(t.Status.ToLower()) || (new[]{"pending","reserved"}.Contains(t.Status.ToLower()) && t.Order.ExpiresAt>Now))),"Người chơi đã có vé hoặc giữ chỗ trùng giờ.");
 }
 private void Resolvable(Ticket t)
 {
  Require(Paid(t.Order.Status) && t.Status.ToLowerInvariant()=="confirmed" && t.CheckedInBy==null,"Chỉ xử lý vé đã thanh toán, chưa check-in và chưa hủy/đổi.");
  Require(TicketAftercareRules.Disrupted(t.Session.Status),"Buổi chơi chưa được báo sự cố.");
  Require(!t.Refunds.Any(r=>r.Status!="failed"),"Vé đang có yêu cầu hoàn tiền.");
 }
 private async Task<List<TicketFunding>> Funding(Ticket t)
 {
  if(t.AmountVnd==0) return [];
  var funds=await db.TicketFunding.Where(f=>f.TicketId==t.Id && !f.Retired).OrderBy(f=>f.Id).ToListAsync();
  if(funds.Count==0)
  {
   var p=t.Order.Payments.OrderBy(p=>p.Id).FirstOrDefault(p=>Paid(p.Status));
   if(p==null) p=await db.Payments.Where(p=>p.OrderId==t.OrderId && p.BankTransactions.Any(b=>b.MatchStatus=="matched")).FirstOrDefaultAsync();
   Require(p!=null && p.AmountVnd>=t.AmountVnd,"Chưa xác định được nguồn tiền đã trả. Cần đối soát trước khi xử lý.");
   funds.Add(new TicketFunding{TicketId=t.Id,PaymentId=p!.Id,AmountVnd=t.AmountVnd});db.TicketFunding.Add(funds[0]);
  }
  Require(funds.Sum(f=>f.AmountVnd)==t.AmountVnd,"Nguồn tiền vé không khớp, cần kiểm tra.");
  return funds;
 }
 private async Task Refund(Ticket t,long paymentId,int amount,long actor)
 {
  var p=await db.Payments.SingleAsync(p=>p.Id==paymentId);
  var already=await db.Refunds.Where(r=>r.PaymentId==paymentId && r.Status!="failed").SumAsync(r=>(long?)r.AmountVnd)??0;
  already+=db.ChangeTracker.Entries<Refund>().Where(e=>e.State==EntityState.Added && e.Entity.PaymentId==paymentId).Sum(e=>(long)(e.Entity.AmountVnd??0));
  Require(amount>0 && already+amount<=p.AmountVnd,"Số tiền hoàn vượt khoản đã thanh toán.");
  db.Refunds.Add(new Refund{TicketId=t.Id,PaymentId=paymentId,RecipientId=t.Order.PayerId,AmountVnd=amount,Status="requested",InitiatedBy=actor});
 }
 public async Task<object> SessionsAsync()
 {
  var today=DateOnly.FromDateTime(Now.ToOffset(TimeSpan.FromHours(7)).DateTime);
  return await db.Sessions.AsNoTracking().Where(s=>s.SessionDate>=today.AddDays(-1)).OrderBy(s=>s.SessionDate).ThenBy(s=>s.TimeSlot.StartTime).Take(200)
   .Select(s=>new{s.Id,s.SessionDate,s.Status,s.TimeSlot.StartTime,s.TimeSlot.EndTime,Sport=s.Sport.Name,Court=s.Court.Name,Venue=s.Court.Venue.Name}).ToListAsync();
 }
 public Task<object> ReportAsync(long id,long actor,string? reason,bool cancel) => Transaction<object>(async()=>
 {
  var note=Evidence(reason);var s=await Session(id);
  Require(s.Status.ToLowerInvariant()!="completed","Buổi đã kết thúc không thể báo dừng.");
  var status=cancel?"cancelled":"suspended";
  if(s.Status.ToLowerInvariant()==status) return new{status=s.Status};
  Require(s.Status.ToLowerInvariant()!="cancelled","Buổi đã hủy không chuyển về tạm ngưng.");
  var old=s.Status;s.Status=status;
  Audit(actor,"venue_condition_issue","Session",id,old,note);
  var tickets=await db.Tickets.Include(t=>t.Player).Include(t=>t.Order).Where(t=>t.SessionId==id && (t.Status.ToLower()=="confirmed" || t.Status.ToLower()=="pending" || t.Status.ToLower()=="reserved")).ToListAsync();
  foreach(var t in tickets) Notify(t,"ticket_venue_issue");
  return new{status,affected=tickets.Count};
 });
 public async Task<object> OptionsAsync(long id,long user)
 {
  // The final submission rechecks all conditions under session/order locks.
  var ticket=await db.Tickets.Include(t=>t.Player).Include(t=>t.Order).Include(t=>t.Refunds).Include(t=>t.Session).ThenInclude(s=>s.TimeSlot).SingleOrDefaultAsync(t=>t.Id==id && (t.Player.UserId==user || t.Order.PayerId==user))??throw new PaymentException(404,"Không tìm thấy vé.");
  Resolvable(ticket);
  var today=DateOnly.FromDateTime(Now.ToOffset(TimeSpan.FromHours(7)).DateTime);
  var targets=await db.Sessions.Include(s=>s.TimeSlot).Include(s=>s.Court).Include(s=>s.Sport).Where(s=>s.Id!=ticket.SessionId && s.SportId==ticket.Session.SportId && s.SessionDate>=today && (s.Status.ToLower()=="open" || s.Status.ToLower()=="published")).OrderBy(s=>s.SessionDate).ThenBy(s=>s.TimeSlot.StartTime).Take(100).ToListAsync();
  var result=new List<object>();
  foreach(var target in targets)
  {
   try{var count=await Occupied(target.Id).CountAsync();TicketAftercareRules.CheckTarget(ticket.Session,target,count,Now);await Eligibility(ticket.PlayerId,target,ticket.Id);
    result.Add(new{target.Id,target.SessionDate,target.TimeSlot.StartTime,target.TimeSlot.EndTime,Court=target.Court.Name,Sport=target.Sport.Name,target.PriceVnd,DifferenceVnd=target.PriceVnd-ticket.AmountVnd,Available=TicketAftercareRules.Limit(target.Capacity)-count});}
   catch(PaymentException){ }
  }
  return result;
 }
 public Task<object> RequestChangeAsync(long id,long targetId,long user) => Transaction<object>(async()=>
 {
  var t=await Ticket(id,user);Resolvable(t);
  Require(!await db.TicketChanges.AnyAsync(c=>c.TicketId==id && c.Status=="requested"),"Vé đã có yêu cầu đổi đang chờ.");
  var target=await Session(targetId);TicketAftercareRules.CheckTarget(t.Session,target,await Occupied(targetId).CountAsync(),Now);await Eligibility(t.PlayerId,target,id);
  var change=new TicketChange{TicketId=id,TargetSessionId=targetId,OldPriceVnd=t.AmountVnd,NewPriceVnd=target.PriceVnd,RequestedBy=user,CreatedAt=Now};db.TicketChanges.Add(change);
  Audit(user,"ticket_change_requested","Ticket",id,t.SessionId.ToString(),$"target={targetId}; difference={target.PriceVnd-t.AmountVnd}");
  Notify(t,"ticket_change_requested");await db.SaveChangesAsync();
  return new{change.Id,change.Status,DifferenceVnd=target.PriceVnd-t.AmountVnd};
 });
 public Task<object> CancelChangeAsync(long id,long user) => Transaction<object>(async()=>
 {
  var lookup=await db.TicketChanges.AsNoTracking().SingleOrDefaultAsync(c=>c.Id==id)??throw new PaymentException(404,"Không tìm thấy yêu cầu đổi.");
  var t=await Ticket(lookup.TicketId,user);var change=await db.TicketChanges.SingleAsync(c=>c.Id==id);
  Require(change.Status=="requested","Yêu cầu đã được xử lý.");change.Status="cancelled";change.CompletedAt=Now;
  Audit(user,"ticket_change_cancelled","Ticket",t.Id,null,$"change={id}");return new{change.Status};
 });
 public Task<object> RequestRefundAsync(long id,long user) => Transaction<object>(async()=>
 {
  var t=await Ticket(id,user);Resolvable(t);
  Require(!await db.TicketChanges.AnyAsync(c=>c.TicketId==id && c.Status=="requested"),"Hãy rút yêu cầu đổi trước khi yêu cầu hoàn tiền.");
  foreach(var f in await Funding(t)){await Refund(t,f.PaymentId,f.AmountVnd,user);f.Retired=true;}
  t.Status="cancelled";Audit(user,"venue_refund_requested","Ticket",id,"confirmed",$"amount={t.AmountVnd}; recipient={t.Order.PayerId}");Notify(t,"ticket_refund_requested");
  return new{status="requested",amountVnd=t.AmountVnd};
 });
 public async Task<object> ChangesAsync()=>await (from c in db.TicketChanges.AsNoTracking() join t in db.Tickets on c.TicketId equals t.Id join s in db.Sessions on c.TargetSessionId equals s.Id where c.Status=="requested" orderby c.Id descending select new{c.Id,c.TicketId,c.OldPriceVnd,c.NewPriceVnd,c.Status,Holder=t.Player.User.FullName,Payer=t.Order.Payer.FullName,Court=s.Court.Name,s.SessionDate,s.TimeSlot.StartTime,s.TimeSlot.EndTime}).ToListAsync();
 public Task<object> CompleteChangeAsync(long id,long actor,string? evidence,string? bankReference) => Transaction<object>(async()=>
 {
  var note=Evidence(evidence);var snapshot=await db.TicketChanges.AsNoTracking().SingleOrDefaultAsync(c=>c.Id==id)??throw new PaymentException(404,"Không tìm thấy yêu cầu đổi.");
  var t=await Ticket(snapshot.TicketId);var change=await db.TicketChanges.SingleAsync(c=>c.Id==id);Require(change.Status=="requested","Yêu cầu đã xử lý.");Resolvable(t);
  Require(actor!=t.Order.PayerId && actor!=t.Player.UserId,"Không tự duyệt đổi vé của chính mình.",403);
  var target=await Session(change.TargetSessionId);TicketAftercareRules.CheckTarget(t.Session,target,await Occupied(target.Id).CountAsync(),Now);await Eligibility(t.PlayerId,target,t.Id);
  Require(target.PriceVnd==change.NewPriceVnd && t.AmountVnd==change.OldPriceVnd,"Giá đã thay đổi. Hãy rút yêu cầu và chọn lại.");
  var funds=await Funding(t);var delta=change.NewPriceVnd-change.OldPriceVnd;
  if(delta>0){bankReference=bankReference?.Trim();Require(bankReference is {Length:>=6 and <=100},"Nhập mã giao dịch khoản thu thêm.",400);Require(!await db.BankTransactions.AnyAsync(b=>b.ExternalId=="staff:"+bankReference || b.ReferenceCode==bankReference) && !await db.Payments.AnyAsync(p=>p.ReferenceCode==bankReference),"Mã giao dịch đã được sử dụng.");}
  var order=new SocialBookingOrder{PayerId=t.Order.PayerId,OrderCode=$"EX-{Guid.NewGuid():N}",TotalAmount=target.PriceVnd,Status="paid"};db.SocialBookingOrders.Add(order);
  var replacement=new Ticket{Order=order,SessionId=target.Id,PlayerId=t.PlayerId,Player=t.Player,Status="confirmed",AmountVnd=target.PriceVnd,TicketCode=$"TK-{Guid.NewGuid():N}"};db.Tickets.Add(replacement);
  await db.SaveChangesAsync();
  foreach(var part in TicketAftercareRules.Split(funds,target.PriceVnd))
  {
   if(part.Transfer>0) db.TicketFunding.Add(new TicketFunding{TicketId=replacement.Id,PaymentId=part.PaymentId,AmountVnd=part.Transfer});
   if(part.Refund>0) await Refund(t,part.PaymentId,part.Refund,change.RequestedBy);
  }
  if(delta>0)
  {
   var payment=new Payment{OrderId=order.Id,PayerId=order.PayerId,AmountVnd=delta,Method="bank_transfer",Status="paid",ReferenceCode=bankReference!,ConfirmedBy=actor};db.Payments.Add(payment);await db.SaveChangesAsync();
   db.BankTransactions.Add(new BankTransaction{ExternalId="staff:"+bankReference,ReferenceCode=bankReference,AmountVnd=delta,MatchedPaymentId=payment.Id,MatchStatus="matched",RawPayload=note});
   db.TicketFunding.Add(new TicketFunding{TicketId=replacement.Id,PaymentId=payment.Id,AmountVnd=delta});
  }
  foreach(var f in funds)f.Retired=true;
  t.Status="exchanged";change.Status="completed";change.ProcessedBy=actor;change.NewTicketId=replacement.Id;change.CompletedAt=Now;
  var partners=await db.PartnerRequests.Where(p=>p.SessionId==t.SessionId && (p.RequesterId==t.PlayerId || p.PartnerId==t.PlayerId)).ToListAsync();foreach(var p in partners)p.Status="cancelled";
  Audit(actor,"ticket_changed","Ticket",t.Id,$"session={t.SessionId}; code={t.TicketCode}",$"new_ticket={replacement.Id}; new_session={target.Id}; difference={delta}; {note}");
  Notify(replacement,"ticket_changed");if(delta<0)Notify(t,"ticket_difference_refund");
  return new{status="completed",ticketId=replacement.Id,differenceVnd=delta};
 });
 public async Task<object> RefundsAsync()=>await db.Refunds.AsNoTracking().Where(r=>r.Status=="requested" || r.Status=="processing").OrderByDescending(r=>r.Id).Select(r=>new{r.Id,r.TicketId,r.AmountVnd,r.Status,Recipient=r.Recipient.FullName,Email=r.Recipient.Email,r.Payment.ReferenceCode}).ToListAsync();
 public Task<object> CompleteRefundAsync(long id,long actor,string? evidence,string? reference) => Transaction<object>(async()=>
 {
  var note=Evidence(evidence);reference=reference?.Trim();Require(reference is {Length:>=6 and <=100},"Nhập mã giao dịch hoàn tiền.",400);
  var snapshot=await db.Refunds.AsNoTracking().SingleOrDefaultAsync(r=>r.Id==id)??throw new PaymentException(404,"Không tìm thấy khoản hoàn.");
  var ticket=await Ticket(snapshot.TicketId);
  var r=await db.Refunds.SingleAsync(r=>r.Id==id);Require(r.Status is "requested" or "processing","Khoản hoàn đã được xử lý.");
  Require(actor!=r.RecipientId && actor!=r.InitiatedBy,"Không tự xác nhận hoàn tiền của chính mình.",403);
  Require(!await db.AuditLogs.AnyAsync(a=>a.Action=="refund_transfer_reference" && a.NewValue==reference),"Mã hoàn tiền đã được dùng cho khoản khác.");
  var payment=await db.Payments.SingleAsync(p=>p.Id==r.PaymentId);
  var total=await db.Refunds.Where(x=>x.PaymentId==r.PaymentId && x.Status!="failed").SumAsync(x=>(long?)x.AmountVnd)??0;
  Require(r.AmountVnd>0 && total<=payment.AmountVnd,"Số tiền hoàn không hợp lệ.");
  r.Status="completed";r.ApprovedBy=actor;
  db.AuditLogs.Add(new AuditLog{ActorId=actor,Action="refund_transfer_reference",EntityType="Refund",EntityId=id,NewValue=reference});
  Audit(actor,"refund_completed","Refund",id,"requested",$"amount={r.AmountVnd}; recipient={r.RecipientId}; {note}");Notify(ticket,"ticket_refund_completed");return new{status="completed"};
 });
 public async Task<object> ReviewsAsync()=>await db.Payments.AsNoTracking().Where(p=>p.Status=="review_required" && p.OrderId!=null && p.Order!.Status.ToLower()!="paid").OrderByDescending(p=>p.Id)
  .Select(p=>new{p.Id,p.OrderId,p.ReferenceCode,p.AmountVnd,Payer=p.Order!.Payer.FullName,Transactions=p.BankTransactions.Where(b=>b.MatchStatus=="review_required").Select(b=>new{b.Id,b.ExternalId,b.AmountVnd,b.ReferenceCode}).ToList()}).ToListAsync();
 public Task<object> ApprovePaymentAsync(long id,long bankId,long actor,string? evidence) => Transaction<object>(async()=>
 {
  var note=Evidence(evidence);var lookup=await db.Payments.AsNoTracking().SingleOrDefaultAsync(p=>p.Id==id && p.OrderId!=null && p.GoodsOrderId==null)??throw new PaymentException(404,"Không tìm thấy thanh toán vé.");
  await LockOrder(lookup.OrderId!.Value);var order=await db.SocialBookingOrders.Include(o=>o.Payments).SingleAsync(o=>o.Id==lookup.OrderId);var payment=order.Payments.Single(p=>p.Id==id);
  Require(payment.Status=="review_required" && order.Status.ToLower()=="review_required","Đơn không còn chờ duyệt hoặc đã thanh toán.");Require(actor!=order.PayerId,"Không tự duyệt thanh toán của mình.",403);
  var bank=await db.BankTransactions.SingleOrDefaultAsync(b=>b.Id==bankId && b.MatchedPaymentId==id && b.MatchStatus=="review_required");
  Require(bank!=null && bank.AmountVnd==order.TotalAmount && payment.AmountVnd==order.TotalAmount,"Chọn đúng giao dịch đã đối soát và đủ số tiền. Không tự duyệt khoản thiếu/thừa.");
  var tickets=await db.Tickets.Include(t=>t.Player).Where(t=>t.OrderId==order.Id && t.Status.ToLower()!="cancelled").ToListAsync();
  Require(tickets.Count>0 && tickets.All(t=>t.Status.ToLower() is "pending" or "reserved") && tickets.Sum(t=>t.AmountVnd)==order.TotalAmount,"Vé không còn đủ điều kiện kích hoạt.");
  foreach(var group in tickets.GroupBy(t=>t.SessionId).OrderBy(g=>g.Key))
  {
   var session=await Session(group.Key);Require(session.Status.ToLower() is "open" or "published","Buổi chơi đang dừng hoặc chưa mở.");Require(new DateTimeOffset(session.SessionDate.ToDateTime(session.TimeSlot.StartTime),TimeSpan.FromHours(7))>Now,"Buổi chơi đã bắt đầu.");
   var other=await Occupied(session.Id).Where(t=>t.OrderId!=order.Id).CountAsync();Require(other+group.Count()<=TicketAftercareRules.Limit(session.Capacity),"Buổi chơi không còn đủ chỗ.");
   foreach(var t in group){await Eligibility(t.PlayerId,session,t.Id);t.Order=order;}
  }
  payment.Status="paid";payment.ConfirmedBy=actor;order.Status="paid";bank!.MatchStatus="matched";
  foreach(var t in tickets){t.Status="confirmed";t.TicketCode??=$"TK-{Guid.NewGuid():N}";Notify(t,"ticket_paid");}
  Audit(actor,"flagged_payment_approved","Payment",id,"review_required",$"bank={bankId}; {note}");return new{status="paid"};
 });
}
