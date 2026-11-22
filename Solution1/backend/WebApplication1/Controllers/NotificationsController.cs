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

        [HttpPost("send")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        [ProducesResponseType(499)]
        public async Task<ActionResult<NotificationDeliveryResult>> Send(
            [FromBody] NotificationRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _notificationService.SendAsync(request, cancellationToken);
            return DeliveryResult(result);
        }

        [HttpPost("assistance-request-status")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        [ProducesResponseType(499)]
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
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        [ProducesResponseType(499)]
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