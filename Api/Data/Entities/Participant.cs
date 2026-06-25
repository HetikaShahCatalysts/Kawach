namespace Api.Data.Entities;

public sealed class Participant
{
    public long UserId { get; set; }
    public required string FullName { get; set; }
    public int? Age { get; set; }
    public string? Gender { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Location { get; set; }
    public DateTime CreatedOn { get; set; }

    public ICollection<AssessmentSession> Assessments { get; set; } = [];
}
