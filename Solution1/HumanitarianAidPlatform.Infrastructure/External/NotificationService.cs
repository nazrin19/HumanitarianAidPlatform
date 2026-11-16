using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using HumanitarianAidPlatform.Application.Notifications;

namespace HumanitarianAidPlatform.Infrastructure.External
{
    public class NotificationService : INotificationService
    {
        private readonly EmailSender _emailSender;
        private readonly SmsSender _smsSender;

        public NotificationService(EmailSender emailSender, SmsSender smsSender)
        {
            _emailSender = emailSender;
            _smsSender = smsSender;
        }

        public async Task<bool> SendEmailAsync(string toAddress, string subject, string body)
        {
            return await _emailSender.SendEmailAsync(toAddress, subject, body);
        }

        public async Task<bool> SendSmsAsync(string phoneNumber, string message)
        {
            return await _smsSender.SendSmsAsync(phoneNumber, message);
        }
    }
}