using HumanitarianAidPlatform.Application.Notifications;
using HumanitarianAidPlatform.Domain.Entities.Notifications;
using HumanitarianAidPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HumanitarianAidPlatform.Infrastructure.Repositories.Notifications;

public sealed class NotificationRepository : INotificationRepository
{
    private readonly AppDbContext _db;

    public NotificationRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Notification>> GetRecentAsync(
        int limit,
        CancellationToken cancellationToken) =>
        await _db.Notifications
            .AsNoTracking()
            .OrderByDescending(notification => notification.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(
        Notification notification,
        CancellationToken cancellationToken)
    {
        await _db.Notifications.AddAsync(notification, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        Notification notification,
        CancellationToken cancellationToken)
    {
        _db.Notifications.Update(notification);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
