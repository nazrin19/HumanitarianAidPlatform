using System.Security.Claims;
using HumanitarianAidPlatform.Application.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HumanitarianAidPlatform.Api.Controllers;

/// <summary>Provides notification inbox operations scoped to the signed-in user.</summary>
[ApiController]
[Route("api/notifications/inbox")]
[Authorize]
public sealed class NotificationInboxController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationInboxController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>Returns recent notifications addressed to the signed-in user.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<InAppNotificationItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<IReadOnlyList<InAppNotificationItem>>> GetInbox(
        [FromQuery] int limit = 30,
        CancellationToken cancellationToken = default)
    {
        var recipient = GetRecipient();
        if (recipient is null)
        {
            return Forbid();
        }

        return Ok(await _notificationService.GetInboxAsync(
            recipient,
            limit,
            cancellationToken));
    }

    /// <summary>Marks all notifications addressed to the signed-in user as read.</summary>
    [HttpPatch("read-all")]
    [ProducesResponseType(typeof(MarkNotificationsReadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<MarkNotificationsReadResponse>> MarkAllAsRead(
        CancellationToken cancellationToken)
    {
        var recipient = GetRecipient();
        if (recipient is null)
        {
            return Forbid();
        }

        var updatedCount = await _notificationService.MarkAllAsReadAsync(
            recipient,
            cancellationToken);
        return Ok(new MarkNotificationsReadResponse(updatedCount));
    }

    /// <summary>Marks one notification addressed to the signed-in user as read.</summary>
    [HttpPatch("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(
        Guid id,
        CancellationToken cancellationToken)
    {
        var recipient = GetRecipient();
        if (recipient is null)
        {
            return Forbid();
        }

        var updated = await _notificationService.MarkAsReadAsync(
            id,
            recipient,
            cancellationToken);
        return updated ? NoContent() : NotFound();
    }

    private string? GetRecipient()
    {
        var recipient = User.FindFirstValue("email") ??
            User.FindFirstValue(ClaimTypes.Email);
        return string.IsNullOrWhiteSpace(recipient) ? null : recipient;
    }
}

/// <summary>Reports how many inbox notifications were marked as read.</summary>
public sealed record MarkNotificationsReadResponse(int UpdatedCount);
