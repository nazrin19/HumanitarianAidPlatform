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

        [HttpPost("send-email")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> SendEmail([FromQuery] string to, [FromQuery] string subject, [FromQuery] string body)
        {
            await Task.CompletedTask;
            return StatusCode(StatusCodes.Status501NotImplemented,
                new { message = "Real email delivery is not configured." });
        }

        [HttpPost("send-sms")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> SendSms([FromQuery] string phoneNumber, [FromQuery] string message)
        {
            await Task.CompletedTask;
            return StatusCode(StatusCodes.Status501NotImplemented,
                new { message = "Real SMS delivery is not configured." });
        }
    }
}