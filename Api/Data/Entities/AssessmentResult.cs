namespace Api.Data.Entities;

public sealed class AssessmentResult
{
    public long Id { get; set; }
    public Guid AssessmentId { get; set; }
    public long UserId { get; set; }
    public decimal Score { get; set; }
    public string? RiskLevel { get; set; }
    public string? DecisionPathway { get; set; }
    public string? ResultJson { get; set; }
    public DateTime CreatedOn { get; set; }

    public required AssessmentSession Assessment { get; set; }
}
