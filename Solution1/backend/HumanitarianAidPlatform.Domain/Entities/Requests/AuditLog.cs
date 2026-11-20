namespace HumanitarianAidPlatform.Domain.Entities.Requests;

public class AuditLog
{
    public Guid Id { get; set; }
    public Guid AssistanceRequestId { get; set; }
    public string AiSnapshotJson { get; set; } = "{}";
    public double? AiConfidence { get; set; }
    public ReviewAction Action { get; set; }
    public string? ChangedValuesJson { get; set; }
    public string? Reason { get; set; }
    public Guid DecidedByUserId { get; set; }
    public DateTime DecidedAt { get; set; } = DateTime.UtcNow;
}
