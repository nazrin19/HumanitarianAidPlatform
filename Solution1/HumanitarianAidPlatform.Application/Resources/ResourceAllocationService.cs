
using System;
using System.Threading;
using System.Threading.Tasks;

using HumanitarianAidPlatform.Application.Resources.DTOs;
using HumanitarianAidPlatform.Domain.Entities.Resources;

namespace HumanitarianAidPlatform.Application.Resources;

public class ResourceAllocationService
{
    private readonly IResourceAllocationRepository _repository;

    public ResourceAllocationService(
        IResourceAllocationRepository repository)
    {
        _repository = repository;
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

        return (
            MapToDto(result.Allocation),
            ResourceAllocationFailure.None);
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
