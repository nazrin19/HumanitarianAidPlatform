namespace HumanitarianAidPlatform.Application.Notifications;

public sealed class NotificationPersistenceException : Exception
{
    public NotificationPersistenceException(
        string operation,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Operation = operation;
    }

    public string Operation { get; }
}
