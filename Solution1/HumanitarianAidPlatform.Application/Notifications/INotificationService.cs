namespace HumanitarianAidPlatform.Application.Notifications
{
    public interface INotificationService
    {
        Task<bool> SendEmailAsync(string toAddress, string subject, string body);
        Task<bool> SendSmsAsync(string phoneNumber, string message);
    }
}