using Api.Contracts;

namespace Api.Services;

public interface IAssessmentService
{
    Task<StartAssessmentResponse> StartAsync(
        StartAssessmentRequest request,
        CancellationToken cancellationToken);
    Task<TrackStepResponse> TrackStepAsync(
        TrackStepRequest request,
        CancellationToken cancellationToken);
    Task<SubmitAnswerResponse> SubmitAnswerAsync(
        SubmitAnswerRequest request,
        CancellationToken cancellationToken);
    Task<SyncAssessmentAnswersResponse> SyncAnswersAsync(
        SyncAssessmentAnswersRequest request,
        CancellationToken cancellationToken);
    Task<CompleteAssessmentResponse> CompleteAsync(
        CompleteAssessmentRequest request,
        CancellationToken cancellationToken);
    Task<AssessmentSessionResponse> GetAsync(
        Guid assessmentId,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<AssessmentSessionResponse>> GetByUserIdAsync(
        long userId,
        CancellationToken cancellationToken);
    Task<AdminAssessmentListResponse> GetAdminAssessmentsAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken);
}
