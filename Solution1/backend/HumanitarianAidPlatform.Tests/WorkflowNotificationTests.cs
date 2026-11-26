using HumanitarianAidPlatform.Application.Ai;
using HumanitarianAidPlatform.Application.Common;
using HumanitarianAidPlatform.Application.Notifications;
using HumanitarianAidPlatform.Application.Requests;
using HumanitarianAidPlatform.Application.Resources;
using HumanitarianAidPlatform.Application.Resources.DTOs;
using HumanitarianAidPlatform.Domain.Entities.Notifications;
using HumanitarianAidPlatform.Domain.Entities.Requests;
using HumanitarianAidPlatform.Domain.Entities.Resources;
using HumanitarianAidPlatform.Domain.Entities.Users;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HumanitarianAidPlatform.Tests;

public sealed class WorkflowNotificationTests
{
    [Fact]
    public async Task SubmitSendsStatusNotificationAfterSavingEvenWhenClassificationFails()
    {
        var repository = new RequestRepositoryStub(RequestStatus.Submitted);
        var notifications = new NotificationServiceStub();
        var service = CreateRequestService(repository, notifications);

        var response = await service.SubmitAsync(
            Guid.NewGuid(),
            new CreateRequestDto
            {
                Category = RequestCategory.Food,
                Description = "Food assistance needed"
            });

        Assert.Equal(RequestStatus.Submitted, response.Status);
        var savedRequest = Assert.IsType<AssistanceRequest>(repository.AddedRequest);
        var submittedHistory = Assert.Single(savedRequest.StatusHistory);
        var notification = Assert.IsType<AssistanceRequestStatusNotificationRequest>(
            Assert.Single(notifications.StatusRequests));
        Assert.Equal(submittedHistory.Id, notification.EventId);
        Assert.Equal(RequestStatus.Submitted, notification.Status);
    }

    [Fact]
    public async Task ReviewAndStatusChangesTriggerNotificationsAfterSaving()
    {
        var repository = new RequestRepositoryStub(RequestStatus.PendingReview);
        var notifications = new NotificationServiceStub();
        var service = CreateRequestService(repository, notifications);

        var review = await service.ReviewAsync(
            repository.Request.Id,
            Guid.NewGuid(),
            RoleNames.Coordinator,
            new ReviewRequestDto { Action = ReviewAction.Approved });

        Assert.Equal(ReviewResultCode.Success, review.Code);
        Assert.Equal(RequestStatus.Approved, repository.Request.Status);
        var reviewNotification =
            Assert.IsType<AssistanceRequestStatusNotificationRequest>(
                Assert.Single(notifications.StatusRequests));
        Assert.Equal(repository.SavedHistory!.Id, reviewNotification.EventId);
        Assert.Equal(RequestStatus.Approved, reviewNotification.Status);
        Assert.Equal("citizen@example.test", reviewNotification.Recipient);

        var statusChange = await service.ChangeStatusAsync(
            repository.Request.Id,
            Guid.NewGuid(),
            RoleNames.Coordinator,
            new ChangeRequestStatusDto { Status = RequestStatus.Assigned });

        Assert.Equal(ReviewResultCode.Success, statusChange.Code);
        Assert.Equal(RequestStatus.Assigned, repository.Request.Status);
        var statusNotification = Assert.IsType<AssistanceRequestStatusNotificationRequest>(
            notifications.StatusRequests[1]);
        Assert.Equal(repository.SavedHistory!.Id, statusNotification.EventId);
        Assert.Equal(RequestStatus.Assigned, statusNotification.Status);
    }

    [Fact]
    public async Task MissingRequestRecipientSkipsNotificationWithoutUndoingStatusChange()
    {
        var repository = new RequestRepositoryStub(RequestStatus.PendingReview)
        {
            NotificationRecipient = " "
        };
        var notifications = new NotificationServiceStub();
        var service = CreateRequestService(repository, notifications);

        var result = await service.ChangeStatusAsync(
            repository.Request.Id,
            Guid.NewGuid(),
            RoleNames.Coordinator,
            new ChangeRequestStatusDto { Status = RequestStatus.Approved });

        Assert.Equal(ReviewResultCode.Success, result.Code);
        Assert.Equal(RequestStatus.Approved, repository.Request.Status);
        Assert.Empty(notifications.StatusRequests);
    }

    [Fact]
    public async Task NotificationExceptionDoesNotUndoSavedReview()
    {
        var repository = new RequestRepositoryStub(RequestStatus.PendingReview);
        var notifications = new NotificationServiceStub { ThrowOnSend = true };
        var service = CreateRequestService(repository, notifications);

        var result = await service.ReviewAsync(
            repository.Request.Id,
            Guid.NewGuid(),
            RoleNames.Coordinator,
            new ReviewRequestDto { Action = ReviewAction.Approved });

        Assert.Equal(ReviewResultCode.Success, result.Code);
        Assert.Equal(RequestStatus.Approved, repository.Request.Status);
        Assert.Single(notifications.StatusRequests);
    }

    [Fact]
    public async Task AllocationNotificationExceptionDoesNotFailSavedAllocation()
    {
        var allocation = new ResourceAllocation
        {
            Id = Guid.NewGuid(),
            ResourceId = Guid.NewGuid(),
            AssistanceRequestId = Guid.NewGuid(),
            QuantityAllocated = 3,
            AllocatedAt = DateTime.UtcNow
        };
        var repository = new ResourceAllocationRepositoryStub(allocation);
        var notifications = new NotificationServiceStub { ThrowOnSend = true };
        var service = new ResourceAllocationService(
            repository,
            notifications,
            NullLogger<ResourceAllocationService>.Instance);

        var result = await service.AllocateAsync(
            new CreateResourceAllocationDto
            {
                ResourceId = allocation.ResourceId,
                AssistanceRequestId = allocation.AssistanceRequestId,
                QuantityAllocated = allocation.QuantityAllocated
            });

        Assert.Equal(ResourceAllocationFailure.None, result.Failure);
        Assert.NotNull(result.Allocation);
        Assert.Equal(allocation.Id, result.Allocation.Id);
        Assert.Equal(1, repository.AllocationCount);
        var request = Assert.IsType<AllocationAssignmentNotificationRequest>(
            Assert.Single(notifications.AllocationRequests));
        Assert.Equal(allocation.Id, request.EventId);
        Assert.Equal(allocation.Id, request.AllocationId);
        Assert.Equal("citizen@example.test", request.Recipient);
        Assert.Equal("Water", request.ResourceName);
    }

    [Fact]
    public async Task MissingAllocationRecipientSkipsNotificationWithoutFailingAllocation()
    {
        var allocation = new ResourceAllocation
        {
            Id = Guid.NewGuid(),
            ResourceId = Guid.NewGuid(),
            AssistanceRequestId = Guid.NewGuid(),
            QuantityAllocated = 3,
            AllocatedAt = DateTime.UtcNow
        };
        var repository = new ResourceAllocationRepositoryStub(allocation)
        {
            NotificationRecipient = null
        };
        var notifications = new NotificationServiceStub();
        var service = new ResourceAllocationService(
            repository,
            notifications,
            NullLogger<ResourceAllocationService>.Instance);

        var result = await service.AllocateAsync(
            new CreateResourceAllocationDto
            {
                ResourceId = allocation.ResourceId,
                AssistanceRequestId = allocation.AssistanceRequestId,
                QuantityAllocated = allocation.QuantityAllocated
            });

        Assert.Equal(ResourceAllocationFailure.None, result.Failure);
        Assert.Equal(1, repository.AllocationCount);
        Assert.Empty(notifications.AllocationRequests);
    }

    private static RequestService CreateRequestService(
        RequestRepositoryStub repository,
        NotificationServiceStub notifications) =>
        new(
            repository,
            new ClassifierStub(),
            notifications,
            NullLogger<RequestService>.Instance);

    private sealed class RequestRepositoryStub : IRequestRepository
    {
        public RequestRepositoryStub(RequestStatus initialStatus)
        {
            Request = new AssistanceRequest
            {
                Id = Guid.NewGuid(),
                CitizenId = Guid.NewGuid(),
                Category = RequestCategory.Food,
                Description = "Food assistance",
                Status = initialStatus
            };
        }

        public AssistanceRequest Request { get; }
        public AssistanceRequest? AddedRequest { get; private set; }
        public string? NotificationRecipient { get; init; } = "citizen@example.test";
        public RequestStatusHistory? SavedHistory { get; private set; }

        public Task AddAsync(AssistanceRequest request)
        {
            AddedRequest = request;
            return Task.CompletedTask;
        }

        public Task<AssistanceRequest?> GetByIdAsync(Guid id) =>
            Task.FromResult<AssistanceRequest?>(Request.Id == id ? Request : null);

        public Task<AssistanceRequest?> GetTrackedByIdAsync(Guid id) =>
            GetByIdAsync(id);

        public Task<string?> GetNotificationRecipientAsync(Guid requestId) =>
            Task.FromResult(NotificationRecipient);

        public Task<IReadOnlyList<RequestStatusHistory>> GetHistoryAsync(Guid requestId) =>
            throw new NotSupportedException();

        public Task AttachRecommendationAsync(
            Guid requestId,
            AiRecommendation recommendation,
            RequestStatusHistory history)
        {
            SavedHistory = history;
            if (AddedRequest is not null)
            {
                AddedRequest.Status = history.ToStatus;
            }

            return Task.CompletedTask;
        }

        public Task SaveReviewAsync(RequestStatusHistory history, AuditLog auditLog)
        {
            SavedHistory = history;
            Request.Status = history.ToStatus;
            return Task.CompletedTask;
        }

        public Task SaveStatusChangeAsync(RequestStatusHistory history)
        {
            SavedHistory = history;
            Request.Status = history.ToStatus;
            return Task.CompletedTask;
        }

        public Task<AiRecommendation?> GetRecommendationAsync(Guid requestId) =>
            Task.FromResult<AiRecommendation?>(null);

        public Task<PagedResult<RequestListItemDto>> GetMineAsync(
            Guid citizenId,
            RequestListQuery query) =>
            throw new NotSupportedException();

        public Task<PagedResult<RequestListItemDto>> GetForStaffAsync(
            RequestListQuery query) =>
            throw new NotSupportedException();
    }

    private sealed class ResourceAllocationRepositoryStub(
        ResourceAllocation allocation) : IResourceAllocationRepository
    {
        public int AllocationCount { get; private set; }
        public string? NotificationRecipient { get; init; } = "citizen@example.test";

        public Task<(ResourceAllocation? Allocation, ResourceAllocationFailure Failure)>
            AllocateAsync(
                Guid resourceId,
                Guid assistanceRequestId,
                int quantity,
                CancellationToken cancellationToken = default)
        {
            AllocationCount++;
            return Task.FromResult<(ResourceAllocation?, ResourceAllocationFailure)>(
                (allocation, ResourceAllocationFailure.None));
        }

        public Task<(string? Recipient, string? ResourceName)> GetNotificationDetailsAsync(
            Guid resourceId,
            Guid assistanceRequestId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(string?, string?)>(
                (NotificationRecipient, "Water"));
    }

    private sealed class NotificationServiceStub : INotificationService
    {
        public bool ThrowOnSend { get; init; }
        public List<AssistanceRequestStatusNotificationRequest> StatusRequests { get; } = [];
        public List<AllocationAssignmentNotificationRequest> AllocationRequests { get; } = [];

        public Task<IReadOnlyList<NotificationHistoryItem>> GetRecentAsync(
            int limit,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<NotificationHistoryItem>>([]);

        public Task<NotificationDeliveryResult> SendAsync(
            NotificationRequest request,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<NotificationDeliveryResult> SendAssistanceRequestStatusAsync(
            AssistanceRequestStatusNotificationRequest request,
            CancellationToken cancellationToken)
        {
            StatusRequests.Add(request);
            return GetResultOrThrow(request.Channel);
        }

        public Task<NotificationDeliveryResult> SendAllocationAssignmentAsync(
            AllocationAssignmentNotificationRequest request,
            CancellationToken cancellationToken)
        {
            AllocationRequests.Add(request);
            return GetResultOrThrow(request.Channel);
        }

        private Task<NotificationDeliveryResult> GetResultOrThrow(
            NotificationChannel channel)
        {
            if (ThrowOnSend)
            {
                throw new InvalidOperationException("Notification provider unavailable.");
            }

            var now = DateTimeOffset.UtcNow;
            return Task.FromResult(new NotificationDeliveryResult(
                true,
                NotificationDeliveryStatus.Sent,
                channel,
                now,
                now,
                null,
                null));
        }
    }

    private sealed class ClassifierStub : IUrgencyClassifier
    {
        public string Name => "test";

        public Task<ClassificationResult> ClassifyAsync(
            AssistanceRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
