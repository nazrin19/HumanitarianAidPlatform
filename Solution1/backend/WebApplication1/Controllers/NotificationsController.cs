using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HumanitarianAidPlatform.Application.Notifications;
using HumanitarianAidPlatform.Domain.Entities.Users;

namespace HumanitarianAidPlatform.Api.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Coordinator},{RoleNames.CaseManager}")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        /// <summary>Returns recent persisted notification attempts for authorized staff.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyList<NotificationHistoryItem>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IReadOnlyList<NotificationHistoryItem>>> GetRecent(
            [FromQuery] int limit = 50,
            CancellationToken cancellationToken = default)
        {
            var notifications = await _notificationService.GetRecentAsync(
                limit,
                cancellationToken);
            return Ok(notifications);
        }

        [HttpPost("send")]
        [Authorize]
        [ProducesResponseType(typeof(NotificationDeliveryResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(NotificationDeliveryResult), StatusCodes.Status202Accepted)]
        [ProducesResponseType(typeof(NotificationDeliveryResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(NotificationDeliveryResult), StatusCodes.Status502BadGateway)]
        [ProducesResponseType(typeof(NotificationDeliveryResult), 499)]
        public async Task<ActionResult<NotificationDeliveryResult>> Send(
            [FromBody] NotificationRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _notificationService.SendAsync(request, cancellationToken);
            return DeliveryResult(result);
        }

        [HttpPost("assistance-request-status")]
        [Authorize]
        [ProducesResponseType(typeof(NotificationDeliveryResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(NotificationDeliveryResult), StatusCodes.Status202Accepted)]
        [ProducesResponseType(typeof(NotificationDeliveryResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(NotificationDeliveryResult), StatusCodes.Status502BadGateway)]
        [ProducesResponseType(typeof(NotificationDeliveryResult), 499)]
        public async Task<ActionResult<NotificationDeliveryResult>> SendAssistanceRequestStatus(
            [FromBody] AssistanceRequestStatusNotificationRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _notificationService.SendAssistanceRequestStatusAsync(
                request,
                cancellationToken);
            return DeliveryResult(result);
        }

        [HttpPost("allocation-assignment")]
        [Authorize]
        [ProducesResponseType(typeof(NotificationDeliveryResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(NotificationDeliveryResult), StatusCodes.Status202Accepted)]
        [ProducesResponseType(typeof(NotificationDeliveryResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(NotificationDeliveryResult), StatusCodes.Status502BadGateway)]
        [ProducesResponseType(typeof(NotificationDeliveryResult), 499)]
        public async Task<ActionResult<NotificationDeliveryResult>> SendAllocationAssignment(
            [FromBody] AllocationAssignmentNotificationRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _notificationService.SendAllocationAssignmentAsync(
                request,
                cancellationToken);
            return DeliveryResult(result);
        }

        private ActionResult<NotificationDeliveryResult> DeliveryResult(
            NotificationDeliveryResult result)
        {
            if (result.Status == NotificationDeliveryStatus.Cancelled)
            {
                return StatusCode(
                    499,
                    result);
            }

            if (result.Status == NotificationDeliveryStatus.InvalidInput)
            {
                return BadRequest(result);
            }

            if (!result.Succeeded)
            {
                return StatusCode(
                    StatusCodes.Status502BadGateway,
                    result);
            }

            return result.Status == NotificationDeliveryStatus.MockSent
                ? Accepted(result)
                : Ok(result);
        }
    }
}