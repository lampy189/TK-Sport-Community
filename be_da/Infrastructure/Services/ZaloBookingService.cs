using System.Globalization;
using be_da.Application.DTOs;
using be_da.Infrastructure.Persistence.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using TKSC.Application.Interfaces.Services;
using TKSC.Domain.Exceptions;

namespace be_da.Infrastructure.Services;

public sealed class ZaloBookingService(SqlZaloStore store, IZaloIntentInterpreter interpreter,
    IPaymentService payments, IConfiguration config, IOptions<ZaloOptions> options, TimeProvider clock) : IZaloBookingProcessor
{
    public async Task ProcessAsync(ZaloWork work, CancellationToken ct)
    {
        await using var c=await store.OpenAsync(ct);
        var state = await store.StateAsync(c,work.CustomerId,ct);
        var savedOrder = await store.ExistingOrderAsync(c,work.CustomerId,work.MessageId,ct);
        ZaloOutgoing reply;
        if(savedOrder.HasValue) { state.OrderId=savedOrder; reply=await CheckoutAsync(c,work,state,ct); }
        else
        {
            var intent=await interpreter.InterpretAsync(work.Text,state,ct);
            reply=intent is null ? new("AI hiện chưa sẵn sàng để hiểu yêu cầu. Nhân viên sân có thể hỗ trợ bạn trong cuộc trò chuyện này.")
                : await RespondAsync(c,work,state,intent,ct);
        }
        await store.CompleteAsync(c,work,state,reply,ct);
    }

    private SqlSessionStore Sessions() => new(config.GetConnectionString("Sep490")!);
    private async Task<ZaloOutgoing> RespondAsync(SqlConnection c,ZaloWork work,ZaloConversation state,ZaloIntent intent,CancellationToken ct)
    {
        switch(intent.Action)
        {
            case "search":
                DateOnly? day=null;
                if(intent.Date is not null)
                {
                    if(!DateOnly.TryParseExact(intent.Date,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out var parsed))
                        return new("Bạn muốn chơi ngày nào?");
                    day=parsed;
                }
                var today=DateOnly.FromDateTime(clock.GetUtcNow().ToOffset(TimeSpan.FromHours(7)).DateTime);
                if(day<today || day>today.AddDays(60)) return new("Bạn hãy chọn ngày từ hôm nay đến 60 ngày tới.");
                TimeSpan? from=null;
                if(intent.StartFrom is not null)
                {
                    if(!TimeOnly.TryParseExact(intent.StartFrom,"HH:mm",CultureInfo.InvariantCulture,DateTimeStyles.None,out var time))
                        return new("Bạn muốn chơi lúc mấy giờ?");
                    from=time.ToTimeSpan();
                }
                var rows=await Sessions().GetSessionsAsync(new SessionFilter(Sport:intent.Sport,Date:day,
                    FromDate:day??today,ToDate:day??today.AddDays(6),VenueId:options.Value.VenueId,Venue:intent.Venue),null,ct);
                state.Choices=rows.Where(x=>Available(x) && (!from.HasValue || x.StartTime>=from.Value)).Take(5).ToList();
                state.Selected=null; state.QuoteExpiresAt=null;
                return state.Choices.Count==0 ? new("Chưa tìm thấy vé còn chỗ phù hợp. Bạn muốn đổi ngày, giờ hoặc cơ sở nào?")
                    : new("Các vé đang còn chỗ:\n"+string.Join("\n",state.Choices.Select((x,i)=>$"{i+1}. {Describe(x)} — còn {x.AvailableSlots} chỗ"))+"\nBạn muốn vé nào? Giá và chỗ sẽ được kiểm tra lại lúc xác nhận.");
            case "select":
                var selected=state.Choices.SingleOrDefault(x=>x.Id==intent.SessionId);
                if(selected is null) return new("Bạn muốn vé nào trong danh sách vừa gửi? Hoặc cho tôi ngày và giờ để tìm vé khác.");
                selected=(await Sessions().GetSessionByIdAsync(selected.Id,null,ct))?.Summary;
                if(selected is null || !Available(selected) || (options.Value.VenueId.HasValue && selected.VenueId!=options.Value.VenueId))
                    return new("Vé này không còn mở hoặc đã hết chỗ. Bạn muốn tôi tìm vé khác không?");
                if(state.ScoreSportId.HasValue && state.ScoreSportId!=selected.SportId) state.Score=null;
                state.ScoreSportId=selected.SportId; state.Selected=selected;
                return Quote(state);
            case "profile":
                if(!string.IsNullOrWhiteSpace(intent.Name)) state.Name=intent.Name.Trim();
                if(intent.Score.HasValue) { state.Score=intent.Score; state.ScoreSportId=state.Selected?.SportId; }
                return state.Selected is null ? new("Đã nhận thông tin bạn tự khai. Bạn muốn chơi môn gì, ngày và giờ nào?") : Quote(state);
            case "confirm":
                if(options.Value.VenueId.HasValue && state.Selected?.VenueId!=options.Value.VenueId)
                    return new("Cơ sở phục vụ đã thay đổi. Bạn hãy tìm và chọn lại vé.");
                if(state.Selected is null || state.QuoteExpiresAt is null || state.QuoteExpiresAt<=clock.GetUtcNow())
                    return new("Chưa có báo giá còn hiệu lực để xác nhận. Bạn hãy chọn lại vé.");
                if(state.Name is null || state.Score is null || state.ScoreSportId!=state.Selected.SportId) return Quote(state);
                var user=await store.EnsurePlayerAsync(c,work,state.Name,state.Selected.SportId,state.Score.Value,ct);
                var result=await Sessions().RegisterZaloAsync(user,new(work.CustomerId,work.MessageId,state.Selected,state.QuoteExpiresAt.Value),ct);
                state.QuoteExpiresAt=null;
                if(!result.Success || result.OrderId is null)
                    return new((result.Message??"Chưa thể giữ chỗ.")+" Bạn có thể chọn vé khác hoặc nhờ nhân viên kiểm tra hồ sơ.");
                state.OrderId=result.OrderId;
                return await CheckoutAsync(c,work,state,ct);
            case "status":
                state.OrderId=await store.ExistingOrderAsync(c,work.CustomerId,null,ct);
                if(state.OrderId is null) return new("Bạn chưa có đơn vé qua cuộc trò chuyện này. Bạn muốn tìm vé ngày nào?");
                var owner=await store.UserAsync(c,work.CustomerId,ct);
                if(owner is null) return new("Nhân viên cần kiểm tra hồ sơ của bạn.");
                var order=await payments.RefreshAsync(state.OrderId.Value,owner.Value);
                if(order.Status.Equals("pending",StringComparison.OrdinalIgnoreCase) && order.ExpiresAt>clock.GetUtcNow())
                    return await CheckoutAsync(c,work,state,ct);
                return new(StatusText(order),order.Id);
            case "reset":
                state.Selected=null; state.Choices=[];state.QuoteExpiresAt=null;
                return new("Bạn muốn tìm vé môn gì, ngày và giờ nào? Đơn đã tạo vẫn giữ trạng thái hiện tại.");
            case "clarify": return new(intent.Clarification??"Bạn muốn tìm vé, chọn vé hay kiểm tra thanh toán?");
            default: return new("Tôi hỗ trợ tìm và đăng ký một vé cho bạn, gửi QR và xem thanh toán. Hãy cho tôi môn chơi, ngày và giờ bạn muốn. Thuê nguyên sân, đặt hộ hoặc hủy vé cần nhân viên hỗ trợ.");
        }
    }
    private ZaloOutgoing Quote(ZaloConversation state)
    {
        state.QuoteExpiresAt=null;
        if(string.IsNullOrWhiteSpace(state.Name)) return new("Bạn cho tôi tên người tham gia nhé. Thông tin đặt vé sẽ được lưu để sân kiểm tra khi đến chơi.");
        if(state.Score is null) return new($"Bạn tự chấm trình độ {state.Selected!.SportName} bao nhiêu trên thang 0–100? 0–19: B1; 20–39: B2; 40–59: B3; 60–79: B4; 80–100: B5. Đây là mức tự khai, chưa được trọng tài đánh giá.");
        if(!string.IsNullOrWhiteSpace(state.Selected!.Band) && !state.Selected.IsHourlyCourt
            && !state.Selected.Band.Equals(SqlZaloStore.Band(state.Score.Value),StringComparison.OrdinalIgnoreCase))
            return new($"Mức tự khai thuộc {SqlZaloStore.Band(state.Score.Value)}, vé này yêu cầu {state.Selected.Band}. Bạn muốn tìm vé phù hợp hơn hay nhờ nhân viên hỗ trợ?");
        state.QuoteExpiresAt=clock.GetUtcNow().AddMinutes(5);
        return new($"Xác nhận 1 vé: {Describe(state.Selected)}. Người chơi: {state.Name}; tự khai {state.Score}/100. Báo giá có hiệu lực 5 phút. Sau khi giữ chỗ, thanh toán trong tối đa 15 phút và trước giờ chơi. Bạn đồng ý đăng ký không?");
    }
    public bool Available(SessionSummary s) => s.AvailableSlots>0 && s.PriceVnd>0
        && s.Status.ToLowerInvariant() is ("open" or "published")
        && new DateTimeOffset(s.SessionDate.ToDateTime(TimeOnly.FromTimeSpan(s.StartTime)),TimeSpan.FromHours(7))>clock.GetUtcNow();
    private static string Label(string text,int limit) => text.Replace('\r',' ').Replace('\n',' ') is var clean && clean.Length>limit ? clean[..limit]+"…" : clean;
    public static string Describe(SessionSummary s) => $"Mã {s.Id}: {Label(s.SportName,25)}, {Label(s.VenueName,65)}, {Label(s.CourtName,40)}, {s.SessionDate:dd/MM}, {s.StartTime:hh\\:mm}–{s.EndTime:hh\\:mm}, {s.Band??"không giới hạn band"}, {s.PriceVnd:N0}đ";
    private async Task<ZaloOutgoing> CheckoutAsync(SqlConnection c,ZaloWork work,ZaloConversation state,CancellationToken ct)
    {
        var user=await store.UserAsync(c,work.CustomerId,ct) ?? throw new InvalidOperationException("Missing channel identity");
        var snapshot=await payments.DisplayAsync(state.OrderId!.Value,user);
        if(!snapshot.Status.Equals("pending",StringComparison.OrdinalIgnoreCase) || snapshot.ExpiresAt<=clock.GetUtcNow()) return new(StatusText(snapshot),snapshot.Id);
        try
        {
            var checkout=await payments.ProcessAsync(snapshot.Id,user);
            if(checkout.Status!="pending") return new("Đang đối soát thanh toán. Bạn hãy chờ thông báo xác nhận từ sân.",snapshot.Id);
            await using var query=SqlZaloStore.Command(c,"SELECT TOP(1) new_value FROM dbo.audit_logs WHERE entity_type='Payment' AND entity_id=@id AND action='payos_qr' ORDER BY id DESC",("@id",checkout.Id));
            var qr=await query.ExecuteScalarAsync(ct) as string;
            var text=$"Đơn {snapshot.OrderCode}: {snapshot.TotalAmount:N0}đ. Nội dung chuyển khoản: TKSC {checkout.Id}. Hạn thanh toán {snapshot.ExpiresAt?.ToOffset(TimeSpan.FromHours(7)):HH:mm dd/MM}. Chỉ chuyển đúng tiền theo QR.\n{checkout.PaymentUrl}\nSân sẽ xác nhận sau khi nhận được kết quả thanh toán.";
            return new(text,snapshot.Id,qr,true);
        }
        catch(GatewayException)
        { return new($"Đã giữ chỗ cho đơn {snapshot.OrderCode}, nhưng cổng thanh toán chưa phản hồi. Nhắn 'gửi lại QR' để kiểm tra tiếp trước hạn {snapshot.ExpiresAt?.ToOffset(TimeSpan.FromHours(7)):HH:mm}. Không chuyển tiền theo mã tự nhập.",snapshot.Id); }
    }
    public static string StatusText(TKSC.Application.DTOs.OrderSummary order)
    {
        if(order.Status.Equals("paid",StringComparison.OrdinalIgnoreCase))
        {
            var tickets=order.Tickets.Where(x=>x.Valid).ToList();
            return tickets.Count>0 ? $"Đơn {order.OrderCode} đã thanh toán. Vé: "+string.Join(", ",tickets.Select(x=>$"{x.TicketCode} — {x.VenueName}, {x.CourtName}, {x.SessionDate:dd/MM}, {x.StartTime:HH:mm}"))
                : $"Đơn {order.OrderCode} đã nhận tiền; vé cần nhân viên kiểm tra trước khi xác nhận vào sân.";
        }
        if(order.Status.Equals("review_required",StringComparison.OrdinalIgnoreCase)) return $"Đơn {order.OrderCode} đang được đối soát. Không chuyển thêm tiền; nhân viên sẽ hỗ trợ.";
        return $"Đơn {order.OrderCode} đã đóng hoặc hết hạn giữ chỗ. Nếu đã chuyển tiền, hãy nhắn nhân viên để đối soát; không chuyển thêm tiền.";
    }
}
