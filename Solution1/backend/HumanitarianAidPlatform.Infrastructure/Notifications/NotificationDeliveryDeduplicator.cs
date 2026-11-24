using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using HumanitarianAidPlatform.Application.Notifications;
using HumanitarianAidPlatform.Domain.Entities.Notifications;

namespace HumanitarianAidPlatform.Infrastructure.Notifications;

/// <summary>
/// Keeps a process-local 24-hour idempotency window. Multi-instance deployments
/// require a shared durable store for cross-process duplicate prevention.
/// </summary>
public sealed class NotificationDeliveryDeduplicator
{
    private static readonly TimeSpan RetentionWindow = TimeSpan.FromHours(24);
    private readonly ConcurrentDictionary<string, Entry> _entries = new();
    private long _operationCount;

    public async Task<NotificationDeliveryResult> ExecuteAsync(
        Guid eventId,
        NotificationChannel channel,
        string recipient,
        Func<Task<NotificationDeliveryResult>> delivery,
        CancellationToken cancellationToken)
    {
        var key = CreateKey(eventId, channel, recipient);
        var entry = new Entry();

        while (true)
        {
            if ((Interlocked.Increment(ref _operationCount) & 63) == 0)
            {
                RemoveExpiredEntries();
            }

            if (_entries.TryGetValue(key, out var existing))
            {
                if (!existing.IsExpired(DateTimeOffset.UtcNow))
                {
                    var previous = await existing.Completion.Task.WaitAsync(cancellationToken);
                    return previous with { IsDuplicate = true };
                }

                if (_entries.TryUpdate(key, entry, existing))
                {
                    break;
                }

                continue;
            }

            if (_entries.TryAdd(key, entry))
            {
                break;
            }
        }

        try
        {
            var result = await delivery();
            entry.Completion.TrySetResult(result);

            if (result.Status is not NotificationDeliveryStatus.Cancelled)
            {
                entry.MarkCompleted(DateTimeOffset.UtcNow);
            }
            else
            {
                RemoveEntry(key, entry);
            }

            return result;
        }
        catch (Exception exception)
        {
            entry.Completion.TrySetException(exception);
            RemoveEntry(key, entry);
            throw;
        }
    }

    private void RemoveExpiredEntries()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var (key, entry) in _entries)
        {
            if (entry.IsExpired(now))
            {
                RemoveEntry(key, entry);
            }
        }
    }

    private bool RemoveEntry(string key, Entry entry) =>
        ((ICollection<KeyValuePair<string, Entry>>)_entries)
        .Remove(new KeyValuePair<string, Entry>(key, entry));

    private static string CreateKey(
        Guid eventId,
        NotificationChannel channel,
        string recipient)
    {
        var trimmedRecipient = recipient.Trim();
        var normalizedRecipient = channel switch
        {
            NotificationChannel.Email => trimmedRecipient.ToUpperInvariant(),
            NotificationChannel.Sms => NormalizePhoneRecipient(trimmedRecipient),
            _ => trimmedRecipient
        };
        var recipientHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(normalizedRecipient)));
        return $"{eventId:N}:{(int)channel}:{recipientHash}";
    }

    private static string NormalizePhoneRecipient(string recipient)
    {
        var digits = new string(recipient.Where(char.IsDigit).ToArray());
        return digits.Length > 0 ? digits : recipient.ToUpperInvariant();
    }

    private sealed class Entry
    {
        private long _completedAtUtcTicks;

        public TaskCompletionSource<NotificationDeliveryResult> Completion { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool IsExpired(DateTimeOffset now)
        {
            var completedAtTicks = Interlocked.Read(ref _completedAtUtcTicks);
            return completedAtTicks != 0 &&
                now.UtcTicks - completedAtTicks >= RetentionWindow.Ticks;
        }

        public void MarkCompleted(DateTimeOffset completedAt) =>
            Interlocked.Exchange(
                ref _completedAtUtcTicks,
                completedAt.UtcTicks);
    }
}
