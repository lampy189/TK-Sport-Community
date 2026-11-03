using System.ComponentModel.DataAnnotations;

namespace be_da.Application.Community;

public sealed record SendFriendRequest(
    [Required(ErrorMessage = "Target player ID is required.")]
    long? TargetPlayerId
);

public sealed record RespondFriendRequest(
    [Required(ErrorMessage = "Accept boolean is required.")]
    bool? Accept
);

public sealed record NotifyChatMessageRequest(
    [Required(ErrorMessage = "Recipient player ID is required.")]
    long? RecipientPlayerId,

    [Required(ErrorMessage = "Message preview is required.")]
    [StringLength(500, MinimumLength = 1, ErrorMessage = "Message preview must be between 1 and 500 characters.")]
    string? MessagePreview
);

public sealed record RegisterFcmTokenRequest(
    [Required(ErrorMessage = "FCM token is required.")]
    [StringLength(512, MinimumLength = 10, ErrorMessage = "FCM token must be between 10 and 512 characters.")]
    string? FcmToken,

    [StringLength(50, ErrorMessage = "Device type must not exceed 50 characters.")]
    string? DeviceType
);

public sealed record FriendshipActionResult(
    bool Success,
    string? ErrorCode,
    string? Message,
    long? FriendshipId
);

public sealed record SendMessageRequest(
    [Required(ErrorMessage = "Recipient player ID is required.")]
    long? RecipientPlayerId,

    [Required(ErrorMessage = "Content is required.")]
    [StringLength(2000, MinimumLength = 1, ErrorMessage = "Message content must be between 1 and 2000 characters.")]
    string? Content
);

public sealed record ChatMessageItem(
    long Id,
    long SenderPlayerId,
    long SenderUserId,
    string SenderFullName,
    long RecipientPlayerId,
    long RecipientUserId,
    string Content,
    bool IsRead,
    DateTimeOffset CreatedAt,
    bool IsMine
);

