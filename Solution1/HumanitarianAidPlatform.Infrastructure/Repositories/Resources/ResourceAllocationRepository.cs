
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

using HumanitarianAidPlatform.Application.Resources;
using HumanitarianAidPlatform.Domain.Entities.Resources;
using HumanitarianAidPlatform.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace HumanitarianAidPlatform.Infrastructure.Repositories.Resources;

public class ResourceAllocationRepository : IResourceAllocationRepository
{
    private readonly AppDbContext _context;

    public ResourceAllocationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(
        ResourceAllocation? Allocation,
        ResourceAllocationFailure Failure)> AllocateAsync(
            Guid resourceId,
            Guid assistanceRequestId,
            int quantity,
            CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
        {
            return (null, ResourceAllocationFailure.InvalidQuantity);
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

        // Check whether the assistance request exists.
        var requestExists = await _context.AssistanceRequests
            .AnyAsync(
                request => request.Id == assistanceRequestId,
                cancellationToken);

        if (!requestExists)
        {
            await transaction.RollbackAsync(cancellationToken);

            return (
                null,
                ResourceAllocationFailure.AssistanceRequestNotFound);
        }

        // Atomically deduct stock only when sufficient quantity exists.
        var updatedRows = await _context.Resources
            .Where(resource =>
                resource.Id == resourceId &&
                resource.QuantityAvailable >= quantity)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    resource => resource.QuantityAvailable,
                    resource => resource.QuantityAvailable - quantity),
                cancellationToken);

        if (updatedRows == 0)
        {
            var resourceExists = await _context.Resources
                .AnyAsync(
                    resource => resource.Id == resourceId,
                    cancellationToken);

            await transaction.RollbackAsync(cancellationToken);

            return resourceExists
                ? (null, ResourceAllocationFailure.InsufficientQuantity)
                : (null, ResourceAllocationFailure.ResourceNotFound);
        }

        // Create allocation record.
        var allocation = new ResourceAllocation
        {
            Id = Guid.NewGuid(),
            ResourceId = resourceId,
            AssistanceRequestId = assistanceRequestId,
            QuantityAllocated = quantity,
            AllocatedAt = DateTime.UtcNow
        };

        await _context.ResourceAllocations.AddAsync(
            allocation,
            cancellationToken);

        // Save allocation and commit the stock deduction together.
        await _context.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return (allocation, ResourceAllocationFailure.None);
    }
}
