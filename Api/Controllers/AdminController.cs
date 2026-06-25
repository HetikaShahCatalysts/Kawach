using Api.Contracts;
using Api.Security;
using Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace Api.Controllers;

[ApiController]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminController(
    IWebHostEnvironment environment,
    AdminCredentialValidator credentialValidator,
    IAssessmentService assessmentService) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("/admin/login")]
    public IActionResult LoginPage()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return Redirect("/admin");
        }

        return AdminFile("login.html");
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("/admin")]
    public IActionResult Dashboard() =>
        AdminFile("dashboard.html");

    [AllowAnonymous]
    [EnableRateLimiting("admin-login")]
    [HttpPost("/api/admin/login")]
    public async Task<IActionResult> Login(
        AdminLoginRequest request)
    {
        if (!credentialValidator.Validate(request.Username, request.Password))
        {
            await Task.Delay(Random.Shared.Next(150, 350));
            return Problem(
                title: "Invalid login",
                detail: "The username or password is incorrect.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, request.Username),
            new Claim(ClaimTypes.Role, "Admin")
        };
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme));

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = false,
                AllowRefresh = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
            });

        return Ok(new { redirectUrl = "/admin" });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("/api/admin/logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("/api/admin/assessments")]
    public async Task<ActionResult<AdminAssessmentListResponse>> Assessments(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default) =>
        Ok(await assessmentService.GetAdminAssessmentsAsync(
            page,
            pageSize,
            search,
            cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpGet("/api/admin/assessments/{assessmentId:guid}")]
    public async Task<ActionResult<AssessmentSessionResponse>> AssessmentDetails(
        Guid assessmentId,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await assessmentService.GetAsync(
                assessmentId,
                cancellationToken));
        }
        catch (KeyNotFoundException exception)
        {
            return Problem(
                detail: exception.Message,
                statusCode: StatusCodes.Status404NotFound);
        }
    }

    private PhysicalFileResult AdminFile(string fileName)
    {
        Response.Headers.CacheControl = "no-store, no-cache";
        Response.Headers.Pragma = "no-cache";
        Response.Headers.ContentSecurityPolicy =
            "default-src 'self'; script-src 'self'; style-src 'self'; " +
            "img-src 'self' data:; connect-src 'self'; frame-ancestors 'none'; " +
            "base-uri 'self'; form-action 'self'";

        return PhysicalFile(
            Path.Combine(environment.ContentRootPath, "AdminUi", fileName),
            "text/html; charset=utf-8");
    }
}
