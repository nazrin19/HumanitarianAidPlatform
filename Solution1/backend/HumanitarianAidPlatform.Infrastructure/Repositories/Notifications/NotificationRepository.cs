using System.Data.Common;
using HumanitarianAidPlatform.Application.Notifications;
using HumanitarianAidPlatform.Domain.Entities.Notifications;
using HumanitarianAidPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HumanitarianAidPlatform.Infrastructure.Repositories.Notifications;

public sealed class NotificationRepository : INotificationRepository
{
    private readonly AppDbContext _db;
    private readonly ILogger<NotificationRepository> _logger;

    public NotificationRepository(
        AppDbContext db,
        ILogger<NotificationRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Notification>> GetRecentAsync(
        int limit,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _db.Notifications
                .AsNoTracking()
                .OrderByDescending(notification => notification.CreatedAt)
                .Take(limit)
                .ToListAsync(cancellationToken);
        }
        catch (Exception exception) when (
            exception is DbException or DbUpdateException)
        {
            throw CreatePersistenceException("ReadHistory", exception);
        }
    }

    public async Task AddAsync(
        Notification notification,
        CancellationToken cancellationToken)
    {
        try
        {
            await _db.Notifications.AddAsync(notification, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (
            exception is DbException or DbUpdateException)
        {
            throw CreatePersistenceException("CreateAttempt", exception);
        }
    }

    public async Task UpdateAsync(
        Notification notification,
        CancellationToken cancellationToken)
    {
        try
        {
            _db.Notifications.Update(notification);
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (
            exception is DbException or DbUpdateException)
        {
            throw CreatePersistenceException("UpdateAttempt", exception);
        }
    }

    private NotificationPersistenceException CreatePersistenceException(
        string operation,
        Exception exception)
    {
        _logger.LogError(
            exception,
            "Notification persistence operation {Operation} failed.",
            operation);

        return new NotificationPersistenceException(
            operation,
            $"Notification persistence operation '{operation}' failed.",
            exception);
    }
}
