using HumanitarianAidPlatform.Domain.Entities.Requests;

namespace HumanitarianAidPlatform.Application.Requests;

public sealed record AuditLogDto(
    Guid Id,
    Guid AssistanceRequestId,
    string AiSnapshotJson,
    double? AiConfidence,
    ReviewAction Action,
    string? ChangedValuesJson,
    string? Reason,
    Guid DecidedByUserId,
    DateTime DecidedAt);

public sealed class AuditLogQuery
{
    public DateTime? Date { get; init; }
    public Guid? User { get; init; }
    public ReviewAction? Action { get; init; }
}
