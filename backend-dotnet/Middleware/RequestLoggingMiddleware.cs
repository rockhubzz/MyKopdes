using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace MyKopdes.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var method = context.Request.Method;
        var path = context.Request.Path;

        // Skip the health probes nginx fires every few seconds — they would
        // otherwise flood the log.
        if (HttpMethods.IsGet(method) && path == "/health")
        {
            await _next(context);
            return;
        }

        // Single log line at completion (was two per request: one at start,
        // one at end) — less I/O on the hot path, same information.
        var start = DateTime.UtcNow;
        await _next(context);

        _logger.LogInformation("{StatusCode} {Method} {Path}{Query} [{User}] {ElapsedMs}ms",
            context.Response.StatusCode,
            method,
            path,
            context.Request.QueryString,
            context.User?.Identity?.Name ?? "anonymous",
            (long)(DateTime.UtcNow - start).TotalMilliseconds);
    }
}
