namespace Api.Data.Entities;

public sealed class AssessmentSession
{
    public long Id { get; set; }
    public Guid AssessmentId { get; set; }
    public long UserId { get; set; }
    public required string AssessmentCode { get; set; }
    public required string LanguageCode { get; set; }
    public DateTime StartedOn { get; set; }
    public DateTime? CompletedOn { get; set; }
    public required string Status { get; set; }

    public required Participant Participant { get; set; }
    public ICollection<AssessmentStepTracking> StepTracking { get; set; } = [];
    public ICollection<AssessmentAnswer> Answers { get; set; } = [];
    public AssessmentResult? Result { get; set; }
}
