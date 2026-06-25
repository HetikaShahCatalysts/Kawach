using Api.Contracts;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Api.Controllers;

[ApiController]
[EnableRateLimiting("api")]
[Route("api/assessment")]
public sealed class AssessmentController(IAssessmentService assessmentService) : ControllerBase
{
    [HttpPost("start")]
    [ProducesResponseType<StartAssessmentResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<StartAssessmentResponse>> Start(
        StartAssessmentRequest request,
        CancellationToken cancellationToken)
    {
        var response = await assessmentService.StartAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { assessmentId = response.AssessmentId }, response);
    }

    [HttpPost("track-step")]
    [ProducesResponseType<TrackStepResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<TrackStepResponse>> TrackStep(
        TrackStepRequest request,
        CancellationToken cancellationToken) =>
        await ExecuteCreatedAsync(
            () => assessmentService.TrackStepAsync(request, cancellationToken));

    [HttpPost("answer")]
    [ProducesResponseType<SubmitAnswerResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<SubmitAnswerResponse>> Answer(
        SubmitAnswerRequest request,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(
            () => assessmentService.SubmitAnswerAsync(request, cancellationToken),
            StatusCodes.Status201Created);

    [HttpPost("answers/sync")]
    [ProducesResponseType<SyncAssessmentAnswersResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<SyncAssessmentAnswersResponse>> SyncAnswers(
        SyncAssessmentAnswersRequest request,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(
            () => assessmentService.SyncAnswersAsync(request, cancellationToken),
            StatusCodes.Status200OK);

    [HttpPost("complete")]
    [ProducesResponseType<CompleteAssessmentResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CompleteAssessmentResponse>> Complete(
        CompleteAssessmentRequest request,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(
            () => assessmentService.CompleteAsync(request, cancellationToken),
            StatusCodes.Status200OK);

    [HttpGet("{assessmentId:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType<AssessmentSessionResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AssessmentSessionResponse>> Get(
        Guid assessmentId,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(
            () => assessmentService.GetAsync(assessmentId, cancellationToken),
            StatusCodes.Status200OK);

    [HttpGet("users/{userId:long}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType<IReadOnlyList<AssessmentSessionResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AssessmentSessionResponse>>> GetByUserId(
        long userId,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(
            () => assessmentService.GetByUserIdAsync(userId, cancellationToken),
            StatusCodes.Status200OK);

    private async Task<ActionResult<T>> ExecuteCreatedAsync<T>(Func<Task<T>> action) =>
        await ExecuteAsync(action, StatusCodes.Status201Created);

    private async Task<ActionResult<T>> ExecuteAsync<T>(
        Func<Task<T>> action,
        int successStatusCode)
    {
        try
        {
            return StatusCode(successStatusCode, await action());
        }
        catch (ContentLookupException exception)
        {
            return Problem(
                detail: exception.Message,
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "Content translation mapping required");
        }
        catch (KeyNotFoundException exception)
        {
            return Problem(
                detail: exception.Message,
                statusCode: StatusCodes.Status404NotFound);
        }
        catch (InvalidOperationException exception)
        {
            return Problem(
                detail: exception.Message,
                statusCode: StatusCodes.Status409Conflict);
        }
    }
}
