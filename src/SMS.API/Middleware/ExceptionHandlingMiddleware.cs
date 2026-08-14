using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SMS.Application;

namespace SMS.API;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            await WriteProblemAsync(context, exception);
        }
    }

    private async Task WriteProblemAsync(HttpContext context, Exception exception)
    {
        var (status, title) = exception switch
        {
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Forbidden"),
            NotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
            ConflictException or DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Conflict"),
            ValidationException or ArgumentException or BadHttpRequestException => (StatusCodes.Status400BadRequest, "Validation Error"),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected Error")
        };

        if (status >= 500)
            logger.LogError(exception, "Unhandled API exception for {Method} {Path} (trace {TraceId})", context.Request.Method, context.Request.Path, context.TraceIdentifier);
        else
            logger.LogWarning("API request {Method} {Path} returned {StatusCode}: {Message}", context.Request.Method, context.Request.Path, status, exception.Message);

        if (context.Response.HasStarted)
            return;
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = status >= 500 ? "An unexpected error occurred." : exception.Message,
            Instance = context.Request.Path
        };
        problem.Extensions["traceId"] = context.TraceIdentifier;
        await context.Response.WriteAsJsonAsync(problem, cancellationToken: context.RequestAborted);
    }
}
