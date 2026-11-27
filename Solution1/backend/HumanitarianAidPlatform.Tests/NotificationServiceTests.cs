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
    public async Task EmailDeliveryUsesExplicitSandboxModeForMock()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Notifications:Providers:Email:Enabled"] = "true",
                ["Notifications:Providers:Email:Mode"] = "Sandbox"
            })
            .Build();
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
    public async Task UnsupportedChannelReturnsInvalidInputAndLogsWarning()
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
        Assert.Equal(NotificationDeliveryStatus.InvalidInput, result.Status);
        Assert.Equal("INVALID_NOTIFICATION_REQUEST", result.ErrorCode);
        Assert.Contains(logger.Messages, message => message.Contains("Rejected notification request"));
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
    public async Task SmsDeliveryUsesExplicitSandboxModeForMock()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Notifications:Providers:Sms:Enabled"] = "true",
                ["Notifications:Providers:Sms:Mode"] = "Sandbox"
            })
            .Build();
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
    public async Task DisabledEmailProviderDoesNotReportMockSuccess()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Notifications:Providers:Email:Enabled"] = "false",
                ["Notifications:Providers:Email:Mode"] = "Sandbox"
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
        Assert.Equal("EMAIL_PROVIDER_DISABLED", result.ErrorCode);
    }

    [Fact]
    public async Task UnknownSmsProviderModeReturnsConfigurationFailure()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Notifications:Providers:Sms:Enabled"] = "true",
                ["Notifications:Providers:Sms:Mode"] = "Demo"
            })
            .Build();
        var sender = new SmsSender(
            configuration,
            new RecordingLogger<SmsSender>());

        var result = await sender.SendAsync(
            "+15555550100",
            "Status update",
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("SMS_PROVIDER_MODE_INVALID", result.ErrorCode);
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
                     entry.Exception is null &&
                     entry.Message.Contains(nameof(InvalidOperationException)) &&
                     !entry.Message.Contains("Provider unavailable."));
    }

    [Fact]
    public async Task ProviderTimeoutIsNotReportedAsCallerCancellation()
    {
        var logger = new RecordingLogger<NotificationService>();
        var service = new NotificationService(
            new TimeoutEmailSender(),
            new StubSmsSender(),
            logger);

        var result = await service.SendAsync(
            new NotificationRequest
            {
                Recipient = "citizen@example.test",
                Message = "Approved",
                Channel = NotificationChannel.Email
            },
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(NotificationDeliveryStatus.Failed, result.Status);
        Assert.Equal("NOTIFICATION_TIMEOUT", result.ErrorCode);
        Assert.Equal(1, result.AttemptCount);
        Assert.Null(result.NextRetryAt);
        Assert.Contains(logger.Entries, entry =>
            entry.Level == LogLevel.Warning &&
            entry.Message.Contains("NOTIFICATION_TIMEOUT"));
        Assert.DoesNotContain(logger.Messages, message =>
            message.Contains("private details"));
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
    public async Task InAppChannelIsLoggedAsSimulationWithoutUserFacingDelivery()
    {
        var repository = new RecordingNotificationRepository();
        var logger = new RecordingLogger<NotificationService>();
        var service = new NotificationService(
            new StubEmailSender(),
            new StubSmsSender(),
            new NotificationDeliveryDeduplicator(),
            repository,
            logger);

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
        Assert.Contains("simulation only", result.ErrorMessage);
        Assert.Contains(logger.Entries, entry =>
            entry.Level == LogLevel.Warning &&
            entry.Message.Contains("no user-facing inbox delivery occurred"));
        Assert.True(Assert.Single(repository.Notifications).IsSent);
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
    public async Task WhitespaceRecipientReturnsInvalidInput(string recipient)
    {
        var service = new NotificationService(
            new StubEmailSender(),
            new StubSmsSender(),
            new RecordingLogger<NotificationService>());

        var result = await service.SendAsync(
            new NotificationRequest
            {
                Recipient = recipient,
                Message = "Test",
                Channel = NotificationChannel.Email
            },
            CancellationToken.None);

        Assert.Equal(NotificationDeliveryStatus.InvalidInput, result.Status);
        Assert.Equal("INVALID_NOTIFICATION_REQUEST", result.ErrorCode);
        Assert.Equal(0, result.AttemptCount);
    }

    [Theory]
    [InlineData(NotificationChannel.Email, "not-an-email")]
    [InlineData(NotificationChannel.Email, "citizen@")]
    [InlineData(NotificationChannel.Sms, "abc5550100")]
    [InlineData(NotificationChannel.Sms, "12345")]
    public async Task InvalidChannelRecipientFormatReturnsInvalidInput(
        NotificationChannel channel,
        string recipient)
    {
        var emailSender = new StubEmailSender();
        var smsSender = new StubSmsSender();
        var service = new NotificationService(
            emailSender,
            smsSender,
            new RecordingLogger<NotificationService>());

        var result = await service.SendAsync(
            new NotificationRequest
            {
                Recipient = recipient,
                Message = "Test",
                Channel = channel
            },
            CancellationToken.None);

        Assert.Equal(NotificationDeliveryStatus.InvalidInput, result.Status);
        Assert.Equal("INVALID_NOTIFICATION_REQUEST", result.ErrorCode);
        Assert.Equal(0, emailSender.SendCount);
        Assert.Equal(0, smsSender.SendCount);
    }

    [Fact]
    public async Task InvalidStatusEnumReturnsInvalidInput()
    {
        var service = new NotificationService(
            new StubEmailSender(),
            new StubSmsSender(),
            new RecordingLogger<NotificationService>());

        var result = await service.SendAssistanceRequestStatusAsync(
            new AssistanceRequestStatusNotificationRequest
            {
                Recipient = "citizen@example.test",
                AssistanceRequestId = Guid.NewGuid(),
                Status = (Domain.Entities.Requests.RequestStatus)999
            },
            CancellationToken.None);

        Assert.Equal(NotificationDeliveryStatus.InvalidInput, result.Status);
        Assert.Equal("INVALID_NOTIFICATION_REQUEST", result.ErrorCode);
    }

    [Theory]
    [InlineData(NotificationChannel.Unknown)]
    [InlineData((NotificationChannel)999)]
    public async Task InvalidNotificationChannelReturnsInvalidInput(
        NotificationChannel channel)
    {
        var sender = new StubEmailSender();
        var service = new NotificationService(
            sender,
            new StubSmsSender(),
            new RecordingLogger<NotificationService>());

        var result = await service.SendAsync(
            new NotificationRequest
            {
                Recipient = "citizen@example.test",
                Message = "Approved",
                Channel = channel
            },
            CancellationToken.None);

        Assert.Equal(NotificationDeliveryStatus.InvalidInput, result.Status);
        Assert.Equal(0, sender.SendCount);
        Assert.Equal(0, result.AttemptCount);
    }

    [Fact]
    public async Task EmptyEventIdIsRejectedInsteadOfBypassingDeduplication()
    {
        var sender = new StubEmailSender();
        var service = new NotificationService(
            sender,
            new StubSmsSender(),
            new RecordingLogger<NotificationService>());

        var result = await service.SendAsync(
            new NotificationRequest
            {
                EventId = Guid.Empty,
                Recipient = "citizen@example.test",
                Message = "Approved",
                Channel = NotificationChannel.Email
            },
            CancellationToken.None);

        Assert.Equal(NotificationDeliveryStatus.InvalidInput, result.Status);
        Assert.Equal(0, sender.SendCount);
    }

    [Fact]
    public async Task EmptyDomainIdReturnsInvalidInput()
    {
        var service = new NotificationService(
            new StubEmailSender(),
            new StubSmsSender(),
            new RecordingLogger<NotificationService>());

        var result = await service.SendAllocationAssignmentAsync(
            new AllocationAssignmentNotificationRequest
            {
                Recipient = "citizen@example.test",
                AllocationId = Guid.Empty,
                AssistanceRequestId = Guid.NewGuid(),
                Quantity = 1
            },
            CancellationToken.None);

        Assert.Equal(NotificationDeliveryStatus.InvalidInput, result.Status);
        Assert.Equal(0, result.AttemptCount);
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
    public async Task SmsLogsMaskRecipientPhoneNumber()
    {
        const string recipient = "+1 (555) 555-0100";
        var logger = new RecordingLogger<SmsSender>();
        var sender = new SmsSender(
            new ConfigurationBuilder().Build(),
            logger);

        await sender.SendAsync(
            recipient,
            "Approved",
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.DoesNotContain(logger.Messages, message => message.Contains(recipient));
        Assert.Contains(logger.Messages, message => message.Contains("***0100"));
    }

    [Fact]
    public async Task RepeatedEventAndChannelReturnsCachedOutcomeWithoutSecondDelivery()
    {
        var sender = new StubEmailSender();
        var deduplicator = new NotificationDeliveryDeduplicator();
        var firstService = new NotificationService(
            sender,
            new StubSmsSender(),
            deduplicator,
            new RecordingLogger<NotificationService>());
        var secondService = new NotificationService(
            sender,
            new StubSmsSender(),
            deduplicator,
            new RecordingLogger<NotificationService>());
        var request = new NotificationRequest
        {
            EventId = Guid.NewGuid(),
            Recipient = "citizen@example.test",
            Message = "Approved",
            Channel = NotificationChannel.Email
        };

        var first = await firstService.SendAsync(request, CancellationToken.None);
        var repeated = await secondService.SendAsync(request, CancellationToken.None);

        Assert.Equal(1, sender.SendCount);
        Assert.True(first.Succeeded);
        Assert.False(first.IsDuplicate);
        Assert.True(repeated.IsDuplicate);
        Assert.Equal(first.CorrelationId, repeated.CorrelationId);
        Assert.Equal(first.AttemptCount, repeated.AttemptCount);
    }

    [Fact]
    public async Task EventDeduplicationIsScopedByChannelAndRecipient()
    {
        var emailSender = new StubEmailSender();
        var smsSender = new StubSmsSender();
        var service = new NotificationService(
            emailSender,
            smsSender,
            new NotificationDeliveryDeduplicator(),
            new RecordingLogger<NotificationService>());
        var eventId = Guid.NewGuid();

        await service.SendAsync(
            new NotificationRequest
            {
                EventId = eventId,
                Recipient = "citizen@example.test",
                Message = "Approved",
                Channel = NotificationChannel.Email
            },
            CancellationToken.None);
        await service.SendAsync(
            new NotificationRequest
            {
                EventId = eventId,
                Recipient = "other@example.test",
                Message = "Approved",
                Channel = NotificationChannel.Email
            },
            CancellationToken.None);
        await service.SendAsync(
            new NotificationRequest
            {
                EventId = eventId,
                Recipient = "+1 (555) 555-0100",
                Message = "Approved",
                Channel = NotificationChannel.Sms
            },
            CancellationToken.None);
        var repeatedSms = await service.SendAsync(
            new NotificationRequest
            {
                EventId = eventId,
                Recipient = "+15555550100",
                Message = "Approved",
                Channel = NotificationChannel.Sms
            },
            CancellationToken.None);

        Assert.Equal(2, emailSender.SendCount);
        Assert.Equal(1, smsSender.SendCount);
        Assert.True(repeatedSms.IsDuplicate);
    }

    [Fact]
    public async Task FailedDeliveryCanRetryAndSuccessfulOutcomeIsThenCached()
    {
        var sender = new StubEmailSender
        {
            Result = new NotificationDeliveryResult(
                false,
                NotificationDeliveryStatus.Failed,
                NotificationChannel.Email,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                "EMAIL_DELIVERY_FAILED",
                "The email provider could not deliver the notification.")
        };
        var service = new NotificationService(
            sender,
            new StubSmsSender(),
            new NotificationDeliveryDeduplicator(),
            new RecordingLogger<NotificationService>());
        var request = new NotificationRequest
        {
            EventId = Guid.NewGuid(),
            Recipient = "citizen@example.test",
            Message = "Approved",
            Channel = NotificationChannel.Email
        };

        var first = await service.SendAsync(request, CancellationToken.None);
        sender.Result = new NotificationDeliveryResult(
            true,
            NotificationDeliveryStatus.Sent,
            NotificationChannel.Email,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            null,
            null);
        var repeated = await service.SendAsync(request, CancellationToken.None);
        var third = await service.SendAsync(request, CancellationToken.None);

        Assert.False(first.Succeeded);
        Assert.False(first.IsDuplicate);
        Assert.True(repeated.Succeeded);
        Assert.False(repeated.IsDuplicate);
        Assert.True(third.IsDuplicate);
        Assert.True(third.Succeeded);
        Assert.Equal(2, sender.SendCount);
    }

    [Fact]
    public async Task ConcurrentDuplicateEventWaitsForOriginalDelivery()
    {
        var sender = new BlockingEmailSender();
        var deduplicator = new NotificationDeliveryDeduplicator();
        var service1 = new NotificationService(
            sender,
            new StubSmsSender(),
            deduplicator,
            new RecordingLogger<NotificationService>());
        var service2 = new NotificationService(
            sender,
            new StubSmsSender(),
            deduplicator,
            new RecordingLogger<NotificationService>());
        var request = new NotificationRequest
        {
            EventId = Guid.NewGuid(),
            Recipient = "citizen@example.test",
            Message = "Approved",
            Channel = NotificationChannel.Email
        };

        var originalTask = service1.SendAsync(request, CancellationToken.None);
        await sender.Started.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var duplicateTask = service2.SendAsync(request, CancellationToken.None);
        sender.Release.TrySetResult();

        var results = await Task.WhenAll(originalTask, duplicateTask);

        Assert.Equal(1, sender.SendCount);
        Assert.Single(results, result => !result.IsDuplicate);
        Assert.Single(results, result => result.IsDuplicate);
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

    [Theory]
    [InlineData(NotificationDeliveryStatus.Sent)]
    [InlineData(NotificationDeliveryStatus.MockSent)]
    public async Task SuccessfulAndMockDeliveriesPersistSentState(
        NotificationDeliveryStatus deliveryStatus)
    {
        var completedAt = DateTimeOffset.UtcNow;
        var repository = new RecordingNotificationRepository();
        var emailSender = new StubEmailSender
        {
            Result = new NotificationDeliveryResult(
                true,
                deliveryStatus,
                NotificationChannel.Email,
                completedAt.AddSeconds(-1),
                completedAt,
                null,
                "Provider result")
        };
        var service = new NotificationService(
            emailSender,
            new StubSmsSender(),
            new NotificationDeliveryDeduplicator(),
            repository,
            new RecordingLogger<NotificationService>());
        var eventId = Guid.NewGuid();

        var result = await service.SendAsync(
            new NotificationRequest
            {
                EventId = eventId,
                Recipient = "citizen@example.test",
                Subject = "Status update",
                Message = "Approved",
                Channel = NotificationChannel.Email
            },
            CancellationToken.None);

        var saved = Assert.Single(repository.Notifications);
        Assert.True(result.Succeeded);
        Assert.True(saved.IsSent);
        Assert.Equal(completedAt.UtcDateTime, saved.SentAt);
        Assert.Equal(eventId, saved.EventId);
        Assert.Equal(deliveryStatus.ToString(), saved.DeliveryStatus);
        Assert.Equal(result.CorrelationId, saved.CorrelationId);
        Assert.Equal(1, saved.AttemptCount);
        Assert.Equal("Provider result", saved.ErrorMessage);
        Assert.Equal(1, repository.AddCount);
        Assert.Equal(1, repository.UpdateCount);
    }

    [Fact]
    public async Task FailedDeliveryPersistsStatusAndErrorWithoutSentAt()
    {
        var completedAt = DateTimeOffset.UtcNow;
        var repository = new RecordingNotificationRepository();
        var service = new NotificationService(
            new StubEmailSender
            {
                Result = new NotificationDeliveryResult(
                    false,
                    NotificationDeliveryStatus.Failed,
                    NotificationChannel.Email,
                    completedAt.AddSeconds(-1),
                    completedAt,
                    "EMAIL_DELIVERY_FAILED",
                    "The email provider could not deliver the notification.")
            },
            new StubSmsSender(),
            new NotificationDeliveryDeduplicator(),
            repository,
            new RecordingLogger<NotificationService>());

        var result = await service.SendAsync(
            new NotificationRequest
            {
                Recipient = "citizen@example.test",
                Message = "Approved",
                Channel = NotificationChannel.Email
            },
            CancellationToken.None);

        var saved = Assert.Single(repository.Notifications);
        Assert.False(result.Succeeded);
        Assert.False(saved.IsSent);
        Assert.Null(saved.SentAt);
        Assert.Equal("Failed", saved.DeliveryStatus);
        Assert.Equal("EMAIL_DELIVERY_FAILED", saved.ErrorCode);
        Assert.Equal(
            "The email provider could not deliver the notification.",
            saved.ErrorMessage);
        Assert.Equal(completedAt.UtcDateTime, saved.DeliveryCompletedAt);
        Assert.Equal(1, saved.AttemptCount);
        Assert.Equal(1, repository.AddCount);
        Assert.Equal(1, repository.UpdateCount);
    }

    [Fact]
    public async Task PendingPersistenceFailurePreventsProviderDelivery()
    {
        var repository = new RecordingNotificationRepository
        {
            AddException = new NotificationPersistenceException(
                "CreateAttempt",
                "Test storage failure.")
        };
        var emailSender = new StubEmailSender();
        var service = new NotificationService(
            emailSender,
            new StubSmsSender(),
            new NotificationDeliveryDeduplicator(),
            repository,
            new RecordingLogger<NotificationService>());

        var result = await service.SendAsync(
            new NotificationRequest
            {
                EventId = Guid.NewGuid(),
                Recipient = "citizen@example.test",
                Message = "Approved",
                Channel = NotificationChannel.Email
            },
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("NOTIFICATION_PERSISTENCE_FAILED", result.ErrorCode);
        Assert.Equal(0, result.AttemptCount);
        Assert.Equal(0, emailSender.SendCount);
        Assert.Equal(0, repository.UpdateCount);
    }

    [Fact]
    public async Task FinalPersistenceFailureDoesNotMisreportSuccessfulDeliveryOrRetryIt()
    {
        var repository = new RecordingNotificationRepository
        {
            UpdateException = new NotificationPersistenceException(
                "UpdateAttempt",
                "Test storage failure.")
        };
        var emailSender = new StubEmailSender();
        var service = new NotificationService(
            emailSender,
            new StubSmsSender(),
            new NotificationDeliveryDeduplicator(),
            repository,
            new RecordingLogger<NotificationService>());
        var request = new NotificationRequest
        {
            EventId = Guid.NewGuid(),
            Recipient = "citizen@example.test",
            Message = "Approved",
            Channel = NotificationChannel.Email
        };

        var result = await service.SendAsync(request, CancellationToken.None);
        var duplicate = await service.SendAsync(request, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(NotificationDeliveryStatus.Sent, result.Status);
        Assert.Equal("NOTIFICATION_STATUS_PERSISTENCE_FAILED", result.ErrorCode);
        Assert.True(duplicate.Succeeded);
        Assert.True(duplicate.IsDuplicate);
        Assert.Equal(1, emailSender.SendCount);
    }

    [Fact]
    public async Task MissingRepositoryMakesHistoryFailureExplicit()
    {
        var service = new NotificationService(
            new StubEmailSender(),
            new StubSmsSender(),
            new RecordingLogger<NotificationService>());

        var exception = await Assert.ThrowsAsync<NotificationPersistenceException>(
            () => service.GetRecentAsync(10, CancellationToken.None));

        Assert.Equal("ReadHistory", exception.Operation);
    }

    [Fact]
    public async Task RecentHistoryReturnsMaskedRecipientAndPersistedDeliveryDetails()
    {
        var createdAt = new DateTime(2026, 11, 26, 12, 0, 0, DateTimeKind.Utc);
        var completedAt = createdAt.AddMinutes(1);
        var notification = new Notification
        {
            Recipient = "citizen@example.test",
            Channel = NotificationChannel.Email,
            CreatedAt = createdAt,
            EventId = Guid.NewGuid(),
            DeliveryStatus = "MockSent",
            IsSent = true,
            DeliveryCompletedAt = completedAt,
            CorrelationId = Guid.NewGuid(),
            AttemptCount = 1
        };
        var repository = new RecordingNotificationRepository();
        repository.Notifications.Add(notification);
        var service = new NotificationService(
            new StubEmailSender(),
            new StubSmsSender(),
            new NotificationDeliveryDeduplicator(),
            repository,
            new RecordingLogger<NotificationService>());

        var history = await service.GetRecentAsync(50, CancellationToken.None);

        var item = Assert.Single(history);
        Assert.Equal(notification.Id, item.Id);
        Assert.Equal(notification.EventId, item.EventId);
        Assert.Equal("ci***@example.test", item.Recipient);
        Assert.Equal(NotificationChannel.Email, item.Channel);
        Assert.Equal(new DateTimeOffset(createdAt), item.CreatedAt);
        Assert.Equal("MockSent", item.Status);
        Assert.True(item.Succeeded);
        Assert.Equal(new DateTimeOffset(completedAt), item.CompletedAt);
        Assert.Equal(notification.CorrelationId, item.CorrelationId);
    }

    private sealed class RecordingNotificationRepository : INotificationRepository
    {
        public List<Notification> Notifications { get; } = [];
        public int AddCount { get; private set; }
        public int UpdateCount { get; private set; }
        public NotificationPersistenceException? AddException { get; init; }
        public NotificationPersistenceException? UpdateException { get; init; }

        public Task<IReadOnlyList<Notification>> GetRecentAsync(
            int limit,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Notification>>(
                Notifications
                    .OrderByDescending(notification => notification.CreatedAt)
                    .Take(limit)
                    .ToArray());

        public Task AddAsync(
            Notification notification,
            CancellationToken cancellationToken)
        {
            AddCount++;
            if (AddException is not null)
            {
                return Task.FromException(AddException);
            }

            Notifications.Add(notification);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(
            Notification notification,
            CancellationToken cancellationToken)
        {
            UpdateCount++;
            return UpdateException is null
                ? Task.CompletedTask
                : Task.FromException(UpdateException);
        }
    }

    private sealed class StubEmailSender : IEmailSender
    {
        public NotificationDeliveryResult Result { get; set; } =
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

        public int SendCount { get; private set; }

        public Task<NotificationDeliveryResult> SendAsync(
            string recipient,
            string message,
            Guid correlationId,
            CancellationToken cancellationToken)
        {
            SendCount++;
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

    private sealed class TimeoutEmailSender : IEmailSender
    {
        public Task<NotificationDeliveryResult> SendAsync(
            string recipient,
            string? subject,
            string message,
            Guid correlationId,
            CancellationToken cancellationToken) =>
            Task.FromException<NotificationDeliveryResult>(
                new OperationCanceledException("Provider timeout with private details."));
    }

    private sealed class BlockingEmailSender : IEmailSender
    {
        public TaskCompletionSource Started { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Release { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int SendCount { get; private set; }

        public async Task<NotificationDeliveryResult> SendAsync(
            string recipient,
            string? subject,
            string message,
            Guid correlationId,
            CancellationToken cancellationToken)
        {
            SendCount++;
            Started.TrySetResult();
            await Release.Task.WaitAsync(cancellationToken);
            return new NotificationDeliveryResult(
                true,
                NotificationDeliveryStatus.Sent,
                NotificationChannel.Email,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                null,
                null,
                correlationId);
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
