using Api.Contracts;
using Api.Data;
using Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Api.Services;

public sealed class AssessmentService(
    KawachDbContext dbContext,
    IContentLookupService contentLookup) : IAssessmentService
{
    public async Task<StartAssessmentResponse> StartAsync(
        StartAssessmentRequest request,
        CancellationToken cancellationToken)
    {
        var participantDetails = Normalize(request.Participant);
        var startedOn = DateTime.UtcNow;

        var participant = new Participant
        {
            FullName = participantDetails.FullName,
            Age = participantDetails.Age,
            Gender = participantDetails.Gender,
            Phone = participantDetails.Phone,
            Email = participantDetails.Email,
            Location = participantDetails.Location,
            CreatedOn = startedOn
        };

        var assessment = new AssessmentSession
        {
            AssessmentId = Guid.NewGuid(),
            Participant = participant,
            AssessmentCode = request.AssessmentCode.Trim(),
            LanguageCode = request.LanguageCode.Trim(),
            StartedOn = startedOn,
            Status = "InProgress"
        };

        dbContext.AssessmentSessions.Add(assessment);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new StartAssessmentResponse(
            participant.UserId,
            assessment.AssessmentId,
            assessment.AssessmentCode,
            assessment.LanguageCode,
            participantDetails,
            assessment.StartedOn,
            assessment.Status);
    }

    public async Task<TrackStepResponse> TrackStepAsync(
        TrackStepRequest request,
        CancellationToken cancellationToken)
    {
        var assessment = await FindInProgressAssessmentAsync(
            request.AssessmentId,
            request.UserId,
            cancellationToken);
        var stepCode = request.StepCode.Trim();
        var recordedOn = DateTime.UtcNow;
        var clientOccurredOn = request.ClientOccurredOn?.ToUniversalTime();
        var eventType = request.EventType.Equals("Started", StringComparison.OrdinalIgnoreCase)
            ? "Started"
            : "Completed";

        var duration = eventType == "Completed"
            ? await CalculateDurationAsync(
                assessment.AssessmentId,
                stepCode,
                recordedOn,
                clientOccurredOn,
                cancellationToken)
            : null;

        var tracking = new AssessmentStepTracking
        {
            Assessment = assessment,
            AssessmentId = assessment.AssessmentId,
            UserId = assessment.UserId,
            StepCode = stepCode,
            EventType = eventType,
            PageVersion = Normalize(request.PageVersion),
            ClientOccurredOn = clientOccurredOn,
            RecordedOn = recordedOn,
            DurationMilliseconds = duration,
            MetadataJson = request.Metadata?.GetRawText()
        };

        dbContext.AssessmentStepTracking.Add(tracking);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new TrackStepResponse(
            tracking.TrackingId,
            assessment.UserId,
            assessment.AssessmentId,
            tracking.StepCode,
            tracking.EventType,
            tracking.RecordedOn,
            tracking.DurationMilliseconds);
    }

    public async Task<SubmitAnswerResponse> SubmitAnswerAsync(
        SubmitAnswerRequest request,
        CancellationToken cancellationToken)
    {
        var assessment = await FindInProgressAssessmentAsync(
            request.AssessmentId,
            request.UserId,
            cancellationToken);
        var normalized = contentLookup.Resolve(
            request.LanguageCode.Trim(),
            request.StepCode.Trim(),
            request.StepName,
            request.ModuleCode.Trim(),
            request.ModuleName,
            request.QuestionCode.Trim(),
            request.QuestionText,
            Normalize(request.AnswerCode),
            request.AnswerText,
            request.Score);

        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var previousAnswers = dbContext.AssessmentAnswers.Where(item =>
            item.AssessmentId == assessment.AssessmentId &&
            item.UserId == assessment.UserId &&
            item.StepName == normalized.StepEnglish &&
            item.ModuleName == normalized.ModuleEnglish &&
            item.Question == normalized.QuestionEnglish);

        dbContext.AssessmentAnswers.RemoveRange(previousAnswers);

        var answer = CreateAnswer(assessment, normalized);
        dbContext.AssessmentAnswers.Add(answer);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return ToSubmitAnswerResponse(answer);
    }

    public async Task<SyncAssessmentAnswersResponse> SyncAnswersAsync(
        SyncAssessmentAnswersRequest request,
        CancellationToken cancellationToken)
    {
        var assessment = await FindInProgressAssessmentAsync(
            request.AssessmentId,
            request.UserId,
            cancellationToken);
        var stepName = contentLookup.ResolveStep(
            request.LanguageCode.Trim(),
            request.StepCode.Trim(),
            request.StepName);

        var normalizedAnswers = request.SelectedAnswers
            .Select(item => contentLookup.Resolve(
                request.LanguageCode.Trim(),
                request.StepCode.Trim(),
                request.StepName,
                item.ModuleCode.Trim(),
                item.ModuleName,
                item.QuestionCode.Trim(),
                item.QuestionText,
                item.AnswerCode.Trim(),
                item.AnswerText,
                item.Score))
            .ToArray();

        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var previousAnswers = dbContext.AssessmentAnswers.Where(item =>
            item.AssessmentId == assessment.AssessmentId &&
            item.UserId == assessment.UserId &&
            item.StepName == stepName);

        dbContext.AssessmentAnswers.RemoveRange(previousAnswers);
        dbContext.AssessmentAnswers.AddRange(
            normalizedAnswers.Select(item => CreateAnswer(assessment, item)));
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new SyncAssessmentAnswersResponse(
            assessment.UserId,
            assessment.AssessmentId,
            stepName,
            normalizedAnswers.Length,
            normalizedAnswers.Sum(item => item.Score));
    }

    public async Task<CompleteAssessmentResponse> CompleteAsync(
        CompleteAssessmentRequest request,
        CancellationToken cancellationToken)
    {
        var assessment = await FindAssessmentAsync(
            request.AssessmentId,
            request.UserId,
            cancellationToken);

        if (assessment.Result is not null)
        {
            return ToCompleteResponse(assessment, assessment.Result);
        }

        var score = await dbContext.AssessmentAnswers
            .Where(item =>
                item.AssessmentId == assessment.AssessmentId &&
                item.UserId == assessment.UserId)
            .SumAsync(item => item.Score, cancellationToken);

        var (riskLevel, pathway) = ResolveOutcome(score);
        var completedOn = DateTime.UtcNow;
        var result = new AssessmentResult
        {
            Assessment = assessment,
            AssessmentId = assessment.AssessmentId,
            UserId = assessment.UserId,
            Score = score,
            RiskLevel = riskLevel,
            DecisionPathway = pathway,
            CreatedOn = completedOn
        };

        assessment.Result = result;
        assessment.CompletedOn = completedOn;
        assessment.Status = "Completed";
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToCompleteResponse(assessment, result);
    }

    public async Task<AssessmentSessionResponse> GetAsync(
        Guid assessmentId,
        CancellationToken cancellationToken)
    {
        var assessment = await AssessmentQuery()
            .SingleOrDefaultAsync(
                item => item.AssessmentId == assessmentId,
                cancellationToken)
            ?? throw new KeyNotFoundException($"Assessment '{assessmentId}' was not found.");

        return ToResponse(assessment);
    }

    public async Task<IReadOnlyList<AssessmentSessionResponse>> GetByUserIdAsync(
        long userId,
        CancellationToken cancellationToken)
    {
        var assessments = await AssessmentQuery()
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.StartedOn)
            .ToArrayAsync(cancellationToken);

        if (assessments.Length == 0)
        {
            throw new KeyNotFoundException($"User '{userId}' was not found.");
        }

        return assessments.Select(ToResponse).ToArray();
    }

    public async Task<AdminAssessmentListResponse> GetAdminAssessmentsAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = dbContext.AssessmentSessions
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(item => item.Participant.FullName.Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var assessments = await query
            .OrderByDescending(item => item.StartedOn)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new AdminAssessmentSummaryResponse(
                item.UserId,
                item.AssessmentId,
                item.Participant.FullName,
                item.StartedOn,
                item.CompletedOn,
                item.CompletedOn.HasValue
                    ? (long?)EF.Functions.DateDiffSecond(
                        item.StartedOn,
                        item.CompletedOn.Value) * 1000
                    : null,
                item.Answers.Sum(answer => (decimal?)answer.Score) ?? 0,
                item.Status))
            .ToArrayAsync(cancellationToken);

        return new AdminAssessmentListResponse(
            page,
            pageSize,
            totalCount,
            assessments);
    }

    private IQueryable<AssessmentSession> AssessmentQuery() =>
        dbContext.AssessmentSessions
            .AsNoTracking()
            .AsSplitQuery()
            .Include(item => item.Participant)
            .Include(item => item.StepTracking)
            .Include(item => item.Answers)
            .Include(item => item.Result);

    private async Task<AssessmentSession> FindAssessmentAsync(
        Guid assessmentId,
        long userId,
        CancellationToken cancellationToken) =>
        await dbContext.AssessmentSessions
            .Include(item => item.Result)
            .SingleOrDefaultAsync(
                item =>
                    item.AssessmentId == assessmentId &&
                    item.UserId == userId,
                cancellationToken)
        ?? throw AssessmentNotFound(assessmentId, userId);

    private async Task<AssessmentSession> FindInProgressAssessmentAsync(
        Guid assessmentId,
        long userId,
        CancellationToken cancellationToken)
    {
        var assessment = await FindAssessmentAsync(
            assessmentId,
            userId,
            cancellationToken);
        if (assessment.Status == "Completed")
        {
            throw new InvalidOperationException("A completed assessment cannot be changed.");
        }

        return assessment;
    }

    private async Task<long?> CalculateDurationAsync(
        Guid assessmentId,
        string stepCode,
        DateTime recordedOn,
        DateTime? clientCompletedOn,
        CancellationToken cancellationToken)
    {
        var previousEvent = await dbContext.AssessmentStepTracking
            .AsNoTracking()
            .Where(item =>
                item.AssessmentId == assessmentId &&
                item.StepCode == stepCode)
            .OrderByDescending(item => item.TrackingId)
            .FirstOrDefaultAsync(cancellationToken);

        if (previousEvent is null || previousEvent.EventType != "Started")
        {
            return null;
        }

        var elapsed = previousEvent.ClientOccurredOn.HasValue && clientCompletedOn.HasValue
            ? clientCompletedOn.Value - previousEvent.ClientOccurredOn.Value
            : recordedOn - previousEvent.RecordedOn;

        return Math.Max(0, (long)elapsed.TotalMilliseconds);
    }

    private static AssessmentAnswer CreateAnswer(
        AssessmentSession assessment,
        NormalizedContent normalized) =>
        new()
        {
            Assessment = assessment,
            AssessmentId = assessment.AssessmentId,
            UserId = assessment.UserId,
            StepName = normalized.StepEnglish,
            ModuleName = normalized.ModuleEnglish,
            Question = normalized.QuestionEnglish,
            AnswerText = normalized.AnswerEnglish,
            Score = normalized.Score,
            AnsweredOn = DateTime.UtcNow
        };

    private static SubmitAnswerResponse ToSubmitAnswerResponse(AssessmentAnswer answer) =>
        new(
            answer.AnswerId,
            answer.UserId,
            answer.AssessmentId,
            answer.StepName,
            answer.ModuleName,
            answer.Question,
            answer.AnswerText,
            answer.Score,
            answer.AnsweredOn);

    private static AssessmentSessionResponse ToResponse(AssessmentSession assessment)
    {
        var participant = assessment.Participant;

        return new AssessmentSessionResponse(
            assessment.UserId,
            assessment.AssessmentId,
            assessment.AssessmentCode,
            assessment.LanguageCode,
            new ParticipantDetails(
                participant.FullName,
                participant.Age,
                participant.Gender,
                participant.Phone,
                participant.Email,
                participant.Location),
            assessment.StartedOn,
            assessment.CompletedOn,
            assessment.Status,
            assessment.StepTracking
                .OrderBy(item => item.TrackingId)
                .Select(item => new StepTrackingResponse(
                    item.TrackingId,
                    item.StepCode,
                    item.EventType,
                    item.PageVersion,
                    item.RecordedOn,
                    item.ClientOccurredOn,
                    item.DurationMilliseconds,
                    ParseJson(item.MetadataJson)))
                .ToArray(),
            assessment.Answers
                .OrderBy(item => item.AnswerId)
                .Select(item => new AssessmentAnswerResponse(
                    item.AnswerId,
                    item.StepName,
                    item.ModuleName,
                    item.Question,
                    item.AnswerText,
                    item.Score,
                    item.AnsweredOn))
                .ToArray(),
            assessment.Result is null
                ? null
                : ToCompleteResponse(assessment, assessment.Result));
    }

    private static CompleteAssessmentResponse ToCompleteResponse(
        AssessmentSession assessment,
        AssessmentResult result) =>
        new(
            assessment.UserId,
            assessment.AssessmentId,
            result.Score,
            result.RiskLevel ?? string.Empty,
            result.DecisionPathway ?? string.Empty,
            result.CreatedOn,
            assessment.Status);

    private static JsonElement? ParseJson(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        using var document = JsonDocument.Parse(value);
        return document.RootElement.Clone();
    }

    private static KeyNotFoundException AssessmentNotFound(Guid assessmentId, long userId) =>
        new($"Assessment '{assessmentId}' does not belong to user '{userId}'.");

    private static (string RiskLevel, string DecisionPathway) ResolveOutcome(decimal score) =>
        score switch
        {
            < 5 => ("Low", "SelfCare"),
            < 10 => ("Moderate", "ClinicalReview"),
            _ => ("High", "UrgentCare")
        };

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ParticipantDetails Normalize(ParticipantDetails participant) =>
        new(
            participant.FullName.Trim(),
            participant.Age,
            Normalize(participant.Gender),
            Normalize(participant.Phone),
            Normalize(participant.Email),
            Normalize(participant.Location));
}
