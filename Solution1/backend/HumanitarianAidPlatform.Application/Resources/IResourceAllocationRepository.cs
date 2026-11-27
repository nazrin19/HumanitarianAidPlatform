
using System;
using System.Threading;
using System.Threading.Tasks;

using HumanitarianAidPlatform.Domain.Entities.Resources;

namespace HumanitarianAidPlatform.Application.Resources;

public interface IResourceAllocationRepository
{
    Task<(ResourceAllocation? Allocation, ResourceAllocationFailure Failure)>
        AllocateAsync(
            Guid resourceId,
            Guid assistanceRequestId,
            int quantity,
            CancellationToken cancellationToken = default);

    Task<(string? Recipient, string? ResourceName)> GetNotificationDetailsAsync(
        Guid resourceId,
        Guid assistanceRequestId,
        CancellationToken cancellationToken = default);
}
