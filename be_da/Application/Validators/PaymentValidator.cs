using TKSC.Application.DTOs;
using TKSC.Domain.Entities;

namespace TKSC.Application.Validators;

public static class PaymentValidator
{
    public static void OwnOrder(SocialBookingOrder? order, long userId)
    {
        if (userId <= 0 || order is null || order.PayerId != userId)
            throw new PaymentException(404, "Không tìm thấy đơn của bạn.");
    }
    public static void ReviewReason(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length is < 10 or > 1000)
            throw new PaymentException(400, "Vui lòng mô tả vấn đề từ 10 đến 1000 ký tự.");
    }
}
