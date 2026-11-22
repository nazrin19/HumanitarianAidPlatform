using HumanitarianAidPlatform.Application.Notifications;
using HumanitarianAidPlatform.Domain.Entities.Notifications;
using HumanitarianAidPlatform.Infrastructure.External;
using HumanitarianAidPlatform.Infrastructure.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;

namespace HumanitarianAidPlatform.Tests;

public class NotificationServiceTests
{
    [Fact]
    public async Task EmailDeliveryWithoutProviderConfigurationFallsBackToMock()
    {
        var configuration = new ConfigurationBuilder().Build();
        var senderLogger = new RecordingLogger<EmailSender>();
        var serviceLogger = new RecordingLogger<NotificationService>();
        var sms = new StubSmsSender();
        var email = new EmailSender(configuration, senderLogger);
        var service = new NotificationService(email, sms, serviceLogger);

        var result = await service.SendAsync(
            new NotificationRequest
            {
                Recipient = "citizen@example.test",
                Subject = "Status update",
                Message = "Your request was approved.",
                Channel = NotificationChannel.Email
            },
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(NotificationDeliveryStatus.MockSent, result.Status);
        Assert.Equal(NotificationChannel.Email, result.Channel);
        Assert.Contains(serviceLogger.Messages, message =>
            message.Contains("Notification delivered"));
    }

    [Fact]
    public async Task SmsDeliveryReturnsProviderFailureAsRichResult()
    {
        var sms = new StubSmsSender
        {
            Result = new NotificationDeliveryResult(
                false,
                NotificationDeliveryStatus.Failed,
                NotificationChannel.Sms,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                "SMS_DELIVERY_FAILED",
                "The SMS provider could not deliver the notification.")
        };
        var service = new NotificationService(
            new StubEmailSender(),
            sms,
            new RecordingLogger<NotificationService>());

        var result = await service.SendAsync(
            new NotificationRequest
            {
                Recipient = "+15555550100",
                Message = "Your allocation is ready.",
                Channel = NotificationChannel.Sms
            },
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(NotificationDeliveryStatus.Failed, result.Status);
        Assert.Equal("SMS_DELIVERY_FAILED", result.ErrorCode);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task UnsupportedChannelReturnsFailureAndLogsWarning()
    {
        var logger = new RecordingLogger<NotificationService>();
        var service = new NotificationService(
            new StubEmailSender(),
            new StubSmsSender(),
            logger);

        var result = await service.SendAsync(
            new NotificationRequest
            {
                Recipient = "unknown",
                Message = "Test",
                Channel = NotificationChannel.Unknown
            },
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(NotificationDeliveryStatus.Failed, result.Status);
        Assert.Equal("UNSUPPORTED_CHANNEL", result.ErrorCode);
        Assert.Contains(logger.Messages, message =>
            message.Contains("Notification failed"));
    }

    [Fact]
    public async Task StatusUpdateAndAllocationNotificationsUseChannelNeutralService()
    {
        var email = new StubEmailSender
        {
            Result = new NotificationDeliveryResult(
                true,
                NotificationDeliveryStatus.Sent,
                NotificationChannel.Email,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                null,
                null)
        };
        var service = new NotificationService(
            email,
            new StubSmsSender(),
            new RecordingLogger<NotificationService>());

        var statusResult = await service.SendAssistanceRequestStatusAsync(
            new AssistanceRequestStatusNotificationRequest
            {
                Recipient = "citizen@example.test",
                AssistanceRequestId = Guid.NewGuid(),
                Status = Domain.Entities.Requests.RequestStatus.Approved
            },
            CancellationToken.None);

        var allocationResult = await service.SendAllocationAssignmentAsync(
            new AllocationAssignmentNotificationRequest
            {
                Recipient = "citizen@example.test",
                AllocationId = Guid.NewGuid(),
                AssistanceRequestId = Guid.NewGuid(),
                ResourceName = "water",
                Quantity = 12,
                AssignmentName = "Team North"
            },
            CancellationToken.None);

        Assert.Equal(2, email.SendCount);
        Assert.True(statusResult.Succeeded);
        Assert.True(allocationResult.Succeeded);
    }

    [Fact]
    public async Task SmsDeliveryWithoutProviderConfigurationFallsBackToMock()
    {
        var configuration = new ConfigurationBuilder().Build();
        var senderLogger = new RecordingLogger<SmsSender>();
        var sender = new SmsSender(configuration, senderLogger);

        var result = await sender.SendAsync(
            "+15555550100",
            "Your allocation is ready.",
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(NotificationDeliveryStatus.MockSent, result.Status);
        Assert.Equal(NotificationChannel.Sms, result.Channel);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains(senderLogger.Entries,
            entry => entry.Level == LogLevel.Information &&
                     entry.Message.Contains("SMS notification attempt"));
        Assert.Contains(senderLogger.Entries,
            entry => entry.Level == LogLevel.Warning &&
                     entry.Message.Contains("sandbox/mock delivery"));
    }

    [Fact]
    public async Task RealProviderModeWithoutSdkReturnsProviderSpecificFailureDetails()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Notifications:Providers:Email:Enabled"] = "true",
                ["Notifications:Providers:Email:Mode"] = "Real"
            })
            .Build();
        var sender = new EmailSender(
            configuration,
            new RecordingLogger<EmailSender>());

        var result = await sender.SendAsync(
            "citizen@example.test",
            "Status update",
            "Approved",
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(NotificationDeliveryStatus.Failed, result.Status);
        Assert.Equal("EMAIL_PROVIDER_NOT_IMPLEMENTED", result.ErrorCode);
        Assert.NotNull(result.ErrorMessage);
        Assert.NotNull(result.CompletedAt);
    }

    [Fact]
    public async Task ProviderExceptionReturnsFailureAndStructuredErrorLog()
    {
        var logger = new RecordingLogger<NotificationService>();
        var email = new ThrowingEmailSender();
        var service = new NotificationService(
            email,
            new StubSmsSender(),
            logger);

        var result = await service.SendAsync(
            new NotificationRequest
            {
                Recipient = "citizen@example.test",
                Subject = "Status update",
                Message = "Approved",
                Channel = NotificationChannel.Email
            },
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(NotificationDeliveryStatus.Failed, result.Status);
        Assert.Equal("NOTIFICATION_DELIVERY_ERROR", result.ErrorCode);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains(logger.Entries,
            entry => entry.Level == LogLevel.Error &&
                     entry.Exception is InvalidOperationException &&
                     entry.Message.Contains("unexpected error"));
    }

    [Fact]
    public async Task CancellationReturnsCancelledOutcomeAndLogsIt()
    {
        var logger = new RecordingLogger<NotificationService>();
        var email = new CancellationEmailSender();
        var service = new NotificationService(
            email,
            new StubSmsSender(),
            logger);

        var result = await service.SendAsync(
            new NotificationRequest
            {
                Recipient = "citizen@example.test",
                Subject = "Status update",
                Message = "Approved",
                Channel = NotificationChannel.Email
            },
            new CancellationToken(canceled: true));

        Assert.False(result.Succeeded);
        Assert.Equal(NotificationDeliveryStatus.Cancelled, result.Status);
        Assert.Equal("NOTIFICATION_CANCELLED", result.ErrorCode);
        Assert.Contains(logger.Entries,
            entry => entry.Level == LogLevel.Information &&
                     entry.Message.Contains("cancelled"));
    }

    [Fact]
    public async Task StatusUpdateAndAllocationResultsContainRoutingStatusDetails()
    {
        var email = new StubEmailSender
        {
            Result = new NotificationDeliveryResult(
                true,
                NotificationDeliveryStatus.Sent,
                NotificationChannel.Email,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                null,
                null)
        };
        var service = new NotificationService(
            email,
            new StubSmsSender(),
            new RecordingLogger<NotificationService>());
        var requestId = Guid.NewGuid();
        var allocationId = Guid.NewGuid();

        var statusResult = await service.SendAssistanceRequestStatusAsync(
            new AssistanceRequestStatusNotificationRequest
            {
                Recipient = "citizen@example.test",
                Channel = NotificationChannel.Email,
                AssistanceRequestId = requestId,
                Status = Domain.Entities.Requests.RequestStatus.Assigned,
                Note = "Awaiting collection"
            },
            CancellationToken.None);

        var allocationResult = await service.SendAllocationAssignmentAsync(
            new AllocationAssignmentNotificationRequest
            {
                Recipient = "citizen@example.test",
                Channel = NotificationChannel.Email,
                AllocationId = allocationId,
                AssistanceRequestId = requestId,
                ResourceName = "water",
                Quantity = 5,
                AssignmentName = "Team North"
            },
            CancellationToken.None);

        Assert.True(statusResult.Succeeded);
        Assert.True(allocationResult.Succeeded);
        Assert.Equal(NotificationDeliveryStatus.Sent, statusResult.Status);
        Assert.Equal(NotificationDeliveryStatus.Sent, allocationResult.Status);
        Assert.Equal(NotificationChannel.Email, statusResult.Channel);
        Assert.Equal(NotificationChannel.Email, allocationResult.Channel);
        Assert.NotNull(statusResult.CompletedAt);
        Assert.NotNull(allocationResult.CompletedAt);
    }

    [Fact]
    public async Task InAppChannelReturnsSandboxOutcomeWithoutProviderCredentials()
    {
        var service = new NotificationService(
            new StubEmailSender(),
            new StubSmsSender(),
            new RecordingLogger<NotificationService>());

        var result = await service.SendAsync(
            new NotificationRequest
            {
                Recipient = "user-123",
                Message = "Status update",
                Channel = NotificationChannel.InApp
            },
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(NotificationDeliveryStatus.MockSent, result.Status);
        Assert.Equal(NotificationChannel.InApp, result.Channel);
        Assert.Contains("persisted", result.ErrorMessage);
    }

    [Fact]
    public void LegacyTypePropertyMapsSmsWithoutDataLoss()
    {
#pragma warning disable CS0618
        var notification = new Notification
        {
            Type = "SMS"
        };

        Assert.Equal(NotificationChannel.Sms, notification.Channel);
        Assert.Equal("SMS", notification.Type);
#pragma warning restore CS0618
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\r\n")]
    public async Task WhitespaceRecipientIsRejected(string recipient)
    {
        var service = new NotificationService(
            new StubEmailSender(),
            new StubSmsSender(),
            new RecordingLogger<NotificationService>());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.SendAsync(
                new NotificationRequest
                {
                    Recipient = recipient,
                    Message = "Test",
                    Channel = NotificationChannel.Email
                },
                CancellationToken.None));
    }

    [Fact]
    public async Task InvalidStatusEnumIsRejected()
    {
        var service = new NotificationService(
            new StubEmailSender(),
            new StubSmsSender(),
            new RecordingLogger<NotificationService>());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.SendAssistanceRequestStatusAsync(
                new AssistanceRequestStatusNotificationRequest
                {
                    Recipient = "citizen@example.test",
                    AssistanceRequestId = Guid.NewGuid(),
                    Status = (Domain.Entities.Requests.RequestStatus)999
                },
                CancellationToken.None));
    }

    [Fact]
    public async Task LogsMaskEmailAndPhoneRecipients()
    {
        var emailLogger = new RecordingLogger<EmailSender>();
        var emailServiceLogger = new RecordingLogger<NotificationService>();
        var email = new EmailSender(
            new ConfigurationBuilder().Build(),
            emailLogger);
        var service = new NotificationService(
            email,
            new StubSmsSender(),
            emailServiceLogger);

        await service.SendAsync(
            new NotificationRequest
            {
                Recipient = "citizen.private@example.test",
                Subject = "Status",
                Message = "Approved",
                Channel = NotificationChannel.Email
            },
            CancellationToken.None);

        Assert.DoesNotContain(
            emailLogger.Messages,
            message => message.Contains("citizen.private@example.test"));
        Assert.Contains(
            emailLogger.Messages,
            message => message.Contains("ci***@example.test"));
        Assert.DoesNotContain(
            emailServiceLogger.Messages,
            message => message.Contains("citizen.private@example.test"));
    }

    [Fact]
    public async Task DeliveryResultIncludesCorrelationAndAttemptMetadata()
    {
        var service = new NotificationService(
            new StubEmailSender(),
            new StubSmsSender(),
            new RecordingLogger<NotificationService>());

        var result = await service.SendAsync(
            new NotificationRequest
            {
                Recipient = "citizen@example.test",
                Message = "Approved",
                Channel = NotificationChannel.Email
            },
            CancellationToken.None);

        Assert.NotNull(result.CorrelationId);
        Assert.NotEqual(Guid.Empty, result.CorrelationId);
        Assert.True(result.AttemptCount >= 1);
        Assert.Null(result.NextRetryAt);
    }

    private sealed class StubEmailSender : IEmailSender
    {
        public NotificationDeliveryResult Result { get; init; } =
            new(
                true,
                NotificationDeliveryStatus.Sent,
                NotificationChannel.Email,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                null,
                null);

        public int SendCount { get; private set; }

        public Task<NotificationDeliveryResult> SendAsync(
            string recipient,
            string? subject,
            string message,
            Guid correlationId,
            CancellationToken cancellationToken)
        {
            SendCount++;
            return Task.FromResult(Result);
        }
    }

    private sealed class StubSmsSender : ISmsSender
    {
        public NotificationDeliveryResult Result { get; init; } =
            new(
                true,
                NotificationDeliveryStatus.Sent,
                NotificationChannel.Sms,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                null,
                null);

        public Task<NotificationDeliveryResult> SendAsync(
            string recipient,
            string message,
            Guid correlationId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Result);
        }
    }

    private sealed class ThrowingEmailSender : IEmailSender
    {
        public Task<NotificationDeliveryResult> SendAsync(
            string recipient,
            string? subject,
            string message,
            Guid correlationId,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Provider unavailable.");
        }
    }

    private sealed class CancellationEmailSender : IEmailSender
    {
        public Task<NotificationDeliveryResult> SendAsync(
            string recipient,
            string? subject,
            string message,
            Guid correlationId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(
                new NotificationDeliveryResult(
                    true,
                    NotificationDeliveryStatus.Sent,
                    NotificationChannel.Email,
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow,
                    null,
                    null));
        }
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];
        public List<LogEntry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull =>
            null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
            Entries.Add(new LogEntry(
                logLevel,
                formatter(state, exception),
                exception));
        }

        public sealed record LogEntry(
            LogLevel Level,
            string Message,
            Exception? Exception);
    }
}
