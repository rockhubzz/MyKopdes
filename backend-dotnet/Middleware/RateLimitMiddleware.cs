using System.Collections.Concurrent;

namespace MyKopdes.Middleware;

/// <summary>
/// Simple in-memory sliding-window rate limiter for authentication endpoints.
/// Prevents brute-force and credential-stuffing attacks. Limits are per IP.
/// </summary>
public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitMiddleware> _logger;

    // Track request timestamps per IP per endpoint group
    private static readonly ConcurrentDictionary<string, SlidingWindow> _windows = new();

    // Configuration
    private const int MaxRequests = 10;          // max attempts per window
    private const int WindowSeconds = 60;        // sliding window size
    private const int LockoutSeconds = 300;      // lockout after exceeding limit

    // Endpoints to rate-limit (auth-sensitive)
    private static readonly string[] RateLimitedPaths =
    {
        "/api/auth/staff/login",
        "/api/auth/member/login",
        "/api/auth/member/register"
    };

    public RateLimitMiddleware(RequestDelegate next, ILogger<RateLimitMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";

        if (!RateLimitedPaths.Contains(path))
        {
            await _next(context);
            return;
        }

        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var key = $"{clientIp}:{path}";

        var window = _windows.GetOrAdd(key, _ => new SlidingWindow());
        var now = DateTime.UtcNow;

        // Clean up expired entries periodically
        if (now.Second == 0)
            CleanupExpired(now);

        var (allowed, retryAfter) = window.TryAdd(now, MaxRequests, WindowSeconds, LockoutSeconds);

        if (!allowed)
        {
            _logger.LogWarning("Rate limit exceeded for {Path} from {IP}. Retry after {RetryAfter}s", path, clientIp, retryAfter);
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers["Retry-After"] = retryAfter.ToString();
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                message = $"Too many attempts. Try again in {retryAfter} seconds."
            });
            return;
        }

        await _next(context);
    }

    private static void CleanupExpired(DateTime now)
    {
        var expired = _windows.Where(kvp => kvp.Value.IsEmpty(now, WindowSeconds)).Select(kvp => kvp.Key).ToList();
        foreach (var key in expired)
            _windows.TryRemove(key, out _);
    }

    private class SlidingWindow
    {
        private readonly object _lock = new();
        private readonly List<DateTime> _timestamps = new();
        private DateTime _lockedUntil = DateTime.MinValue;

        public (bool allowed, int retryAfter) TryAdd(DateTime now, int maxRequests, int windowSeconds, int lockoutSeconds)
        {
            lock (_lock)
            {
                // Check if currently locked out
                if (now < _lockedUntil)
                {
                    return (false, (int)(_lockedUntil - now).TotalSeconds + 1);
                }

                // Remove timestamps outside the window
                var windowStart = now.AddSeconds(-windowSeconds);
                _timestamps.RemoveAll(t => t < windowStart);

                if (_timestamps.Count >= maxRequests)
                {
                    // Exceeded limit — apply lockout
                    _lockedUntil = now.AddSeconds(lockoutSeconds);
                    _timestamps.Clear();
                    return (false, lockoutSeconds);
                }

                _timestamps.Add(now);
                return (true, 0);
            }
        }

        public bool IsEmpty(DateTime now, int windowSeconds)
        {
            lock (_lock)
            {
                if (now < _lockedUntil) return false;
                var windowStart = now.AddSeconds(-windowSeconds);
                return !_timestamps.Any(t => t >= windowStart);
            }
        }
    }
}
