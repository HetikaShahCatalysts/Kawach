namespace Api.Security;

public sealed class RequestSecurityMiddleware(
    RequestDelegate next,
    RequestSecurityPolicy policy,
    IWebHostEnvironment environment)
{
    public async Task Invoke(HttpContext context)
    {
        if (!environment.IsDevelopment() &&
            policy.HasHostRestrictions &&
            !policy.IsAllowedHost(context.Request.Host))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Title = "Invalid host",
                Detail = "This host is not allowed.",
                Status = StatusCodes.Status400BadRequest
            });
            return;
        }

        if (RequiresOriginValidation(context.Request) &&
            !IsAllowedOriginRequest(context.Request))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Title = "Forbidden origin",
                Detail = "This request origin is not allowed.",
                Status = StatusCodes.Status403Forbidden
            });
            return;
        }

        await next(context);
    }

    private bool RequiresOriginValidation(HttpRequest request) =>
        HttpMethods.IsPost(request.Method) ||
        HttpMethods.IsPut(request.Method) ||
        HttpMethods.IsPatch(request.Method) ||
        HttpMethods.IsDelete(request.Method);

    private bool IsAllowedOriginRequest(HttpRequest request)
    {
        if (!policy.HasOriginRestrictions || environment.IsDevelopment())
        {
            return true;
        }

        if (!request.Headers.TryGetValue("Origin", out var originValues))
        {
            return false;
        }

        var origin = originValues.ToString();
        return Uri.TryCreate(origin, UriKind.Absolute, out var parsedOrigin) &&
               policy.IsAllowedOrigin(parsedOrigin.GetLeftPart(UriPartial.Authority));
    }
}
