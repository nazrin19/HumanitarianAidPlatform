
using System;
using System.Threading;
using System.Threading.Tasks;

using HumanitarianAidPlatform.Application.Notifications;
using HumanitarianAidPlatform.Application.Resources.DTOs;
using HumanitarianAidPlatform.Domain.Entities.Notifications;
using HumanitarianAidPlatform.Domain.Entities.Resources;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace HumanitarianAidPlatform.Application.Resources;

public class ResourceAllocationService
{
    private readonly IResourceAllocationRepository _repository;
    private readonly INotificationService? _notificationService;
    private readonly ILogger<ResourceAllocationService> _logger;

    public ResourceAllocationService(
        IResourceAllocationRepository repository)
        : this(
            repository,
            null,
            NullLogger<ResourceAllocationService>.Instance)
    {
    }

    public ResourceAllocationService(
        IResourceAllocationRepository repository,
        INotificationService? notificationService,
        ILogger<ResourceAllocationService> logger)
    {
        _repository = repository;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<(
        ResourceAllocationDto? Allocation,
        ResourceAllocationFailure Failure)> AllocateAsync(
            CreateResourceAllocationDto dto,
            CancellationToken cancellationToken = default)
    {
        if (dto.ResourceId == Guid.Empty)
        {
            return (null, ResourceAllocationFailure.InvalidResourceId);
        }

        if (dto.AssistanceRequestId == Guid.Empty)
        {
            return (null, ResourceAllocationFailure.InvalidAssistanceRequestId);
        }

        if (dto.QuantityAllocated <= 0)
        {
            return (null, ResourceAllocationFailure.InvalidQuantity);
        }

        var result = await _repository.AllocateAsync(
            dto.ResourceId,
            dto.AssistanceRequestId,
            dto.QuantityAllocated,
            cancellationToken);

        if (result.Allocation is null)
        {
            return (null, result.Failure);
        }

        await SendAllocationNotificationSafelyAsync(result.Allocation);

        return (
            MapToDto(result.Allocation),
            ResourceAllocationFailure.None);
    }

    private async Task SendAllocationNotificationSafelyAsync(
        ResourceAllocation allocation)
    {
        if (_notificationService is null)
        {
            _logger.LogWarning(
                "Allocation {AllocationId} was saved, but notification was skipped because the notification service is not configured.",
                allocation.Id);
            return;
        }

        try
        {
            var details = await _repository.GetNotificationDetailsAsync(
                allocation.ResourceId,
                allocation.AssistanceRequestId,
                CancellationToken.None);
            if (string.IsNullOrWhiteSpace(details.Recipient))
            {
                _logger.LogWarning(
                    "Allocation notification was skipped for {AllocationId}: recipient lookup returned null or whitespace for request {RequestId}.",
                    allocation.Id,
                    allocation.AssistanceRequestId);
                return;
            }

            var result = await _notificationService.SendAllocationAssignmentAsync(
                new AllocationAssignmentNotificationRequest
                {
                    EventId = allocation.Id,
                    Recipient = details.Recipient,
                    Channel = NotificationChannel.Email,
                    AllocationId = allocation.Id,
                    AssistanceRequestId = allocation.AssistanceRequestId,
                    ResourceName = details.ResourceName,
                    Quantity = allocation.QuantityAllocated
                },
                // The allocation is committed; attempt delivery despite a cancelled HTTP request.
                CancellationToken.None);

            if (result.Succeeded)
            {
                _logger.LogInformation(
                    "Allocation notification completed for {AllocationId} with {DeliveryStatus}; correlation {CorrelationId}.",
                    allocation.Id,
                    result.Status,
                    result.CorrelationId);
            }
            else
            {
                _logger.LogWarning(
                    "Allocation notification failed for {AllocationId} with {DeliveryStatus}: {ErrorCode} ({ErrorMessage}), correlation {CorrelationId}. The allocation remains saved.",
                    allocation.Id,
                    result.Status,
                    result.ErrorCode,
                    result.ErrorMessage,
                    result.CorrelationId);
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Allocation notification dispatch failed for {AllocationId} after the allocation was saved.",
                allocation.Id);
        }
    }

    private static ResourceAllocationDto MapToDto(
        ResourceAllocation allocation)
    {
        return new ResourceAllocationDto
        {
            Id = allocation.Id,
            ResourceId = allocation.ResourceId,
            AssistanceRequestId = allocation.AssistanceRequestId,
            QuantityAllocated = allocation.QuantityAllocated,
            AllocatedAt = allocation.AllocatedAt
        };
    }
}
