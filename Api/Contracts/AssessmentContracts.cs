using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Api.Contracts;

public sealed record StartAssessmentRequest(
    [Required, StringLength(50)] string AssessmentCode,
    [Required, StringLength(10)] string LanguageCode,
    [Required] ParticipantDetails Participant);

public sealed record ParticipantDetails(
    [Required, StringLength(150)] string FullName,
    [Range(1, 120)] int? Age,
    [StringLength(30)] string? Gender,
    [Phone, StringLength(20)] string? Phone,
    [EmailAddress, StringLength(254)] string? Email,
    [StringLength(200)] string? Location);

public sealed record StartAssessmentResponse(
    long UserId,
    Guid AssessmentId,
    string AssessmentCode,
    string LanguageCode,
    ParticipantDetails Participant,
    DateTime StartedOn,
    string Status);

public sealed record TrackStepRequest(
    long UserId,
    Guid AssessmentId,
    [Required, StringLength(100)] string StepCode,
    [Required, RegularExpression("^(Started|Completed)$")] string EventType,
    [StringLength(50)] string? PageVersion,
    DateTime? ClientOccurredOn,
    JsonElement? Metadata);

public sealed record TrackStepResponse(
    long TrackingId,
    long UserId,
    Guid AssessmentId,
    string StepCode,
    string EventType,
    DateTime RecordedOn,
    long? DurationMilliseconds);

public sealed record SubmitAnswerRequest(
    long UserId,
    Guid AssessmentId,
    [Required, StringLength(10)] string LanguageCode,
    [Required, StringLength(100)] string StepCode,
    [Required, StringLength(200)] string StepName,
    [Required, StringLength(100)] string ModuleCode,
    [Required, StringLength(200)] string ModuleName,
    [Required, StringLength(100)] string QuestionCode,
    [Required] string QuestionText,
    [StringLength(100)] string? AnswerCode,
    [Required] string AnswerText,
    decimal Score,
    JsonElement? Metadata);

public sealed record SyncAssessmentAnswersRequest(
    long UserId,
    Guid AssessmentId,
    [Required, StringLength(10)] string LanguageCode,
    [Required, StringLength(100)] string StepCode,
    [Required, StringLength(200)] string StepName,
    [Required] IReadOnlyList<SyncAssessmentAnswerItem> SelectedAnswers);

public sealed record SyncAssessmentAnswerItem(
    [Required, StringLength(100)] string ModuleCode,
    [Required, StringLength(200)] string ModuleName,
    [Required, StringLength(100)] string QuestionCode,
    [Required] string QuestionText,
    [Required, StringLength(100)] string AnswerCode,
    [Required] string AnswerText,
    decimal Score);

public sealed record SyncAssessmentAnswersResponse(
    long UserId,
    Guid AssessmentId,
    string StepName,
    int SelectedAnswerCount,
    decimal SelectedScore);

public sealed record SubmitAnswerResponse(
    long AnswerId,
    long UserId,
    Guid AssessmentId,
    string StepName,
    string ModuleName,
    string Question,
    string AnswerText,
    decimal Score,
    DateTime AnsweredOn);

public sealed record CompleteAssessmentRequest(long UserId, Guid AssessmentId);

public sealed record CompleteAssessmentResponse(
    long UserId,
    Guid AssessmentId,
    decimal Score,
    string RiskLevel,
    string DecisionPathway,
    DateTime CompletedOn,
    string Status);

public sealed record StepTrackingResponse(
    long TrackingId,
    string StepCode,
    string EventType,
    string? PageVersion,
    DateTime RecordedOn,
    DateTime? ClientOccurredOn,
    long? DurationMilliseconds,
    JsonElement? Metadata);

public sealed record AssessmentAnswerResponse(
    long AnswerId,
    string StepName,
    string ModuleName,
    string Question,
    string AnswerText,
    decimal Score,
    DateTime AnsweredOn);

public sealed record AssessmentSessionResponse(
    long UserId,
    Guid AssessmentId,
    string AssessmentCode,
    string LanguageCode,
    ParticipantDetails Participant,
    DateTime StartedOn,
    DateTime? CompletedOn,
    string Status,
    IReadOnlyList<StepTrackingResponse> StepTracking,
    IReadOnlyList<AssessmentAnswerResponse> Answers,
    CompleteAssessmentResponse? Result);

public sealed record AdminLoginRequest(
    [Required, StringLength(100)] string Username,
    [Required, StringLength(200)] string Password);

public sealed record AdminAssessmentSummaryResponse(
    long UserId,
    Guid AssessmentId,
    string UserName,
    DateTime StartedOn,
    DateTime? CompletedOn,
    long? TotalDurationMilliseconds,
    decimal TotalScore,
    string Status);

public sealed record AdminAssessmentListResponse(
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyList<AdminAssessmentSummaryResponse> Items);
