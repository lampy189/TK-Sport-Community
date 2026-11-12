using System.ComponentModel.DataAnnotations;

namespace be_da.Application.DTOs;
public sealed record RegisterFcmTokenRequest([Required(ErrorMessage = "FCM token is required.")][StringLength(512, MinimumLength = 10, ErrorMessage = "FCM token must be between 10 and 512 characters.")] string? FcmToken, [StringLength(50, ErrorMessage = "Device type must not exceed 50 characters.")] string? DeviceType);
