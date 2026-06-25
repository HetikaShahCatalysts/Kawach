namespace Api.Data.Entities;

public sealed class AssessmentStepTracking
{
    public long TrackingId { get; set; }
    public Guid AssessmentId { get; set; }
    public long UserId { get; set; }
    public required string StepCode { get; set; }
    public required string EventType { get; set; }
    public string? PageVersion { get; set; }
    public DateTime? ClientOccurredOn { get; set; }
    public DateTime RecordedOn { get; set; }
    public long? DurationMilliseconds { get; set; }
    public string? MetadataJson { get; set; }

    public required AssessmentSession Assessment { get; set; }
}
