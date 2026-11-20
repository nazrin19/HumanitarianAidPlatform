using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HumanitarianAidPlatform.Application.Notifications;

namespace HumanitarianAidPlatform.Api.Controllers
{
    [ApiController]
    [Route("api/notifications")]
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
            var result = await _notificationService.SendEmailAsync(to, subject, body);
            if (result)
                return Ok(new { message = "Email sent successfully." });

            return BadRequest(new { message = "Failed to send email." });
        }

        [HttpPost("send-sms")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> SendSms([FromQuery] string phoneNumber, [FromQuery] string message)
        {
            var result = await _notificationService.SendSmsAsync(phoneNumber, message);
            if (result)
                return Ok(new { message = "SMS sent successfully." });

            return BadRequest(new { message = "Failed to send SMS." });
        }
    }
}