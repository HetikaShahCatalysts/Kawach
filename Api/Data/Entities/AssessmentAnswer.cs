namespace Api.Data.Entities;

public sealed class AssessmentAnswer
{
    public long AnswerId { get; set; }
    public Guid AssessmentId { get; set; }
    public long UserId { get; set; }
    public required string StepName { get; set; }
    public required string ModuleName { get; set; }
    public required string Question { get; set; }
    public required string AnswerText { get; set; }
    public decimal Score { get; set; }
    public DateTime AnsweredOn { get; set; }

    public required AssessmentSession Assessment { get; set; }
}
