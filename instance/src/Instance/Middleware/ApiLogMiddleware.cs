using System.Diagnostics;

namespace Instance.Middleware;

public class ApiLogMiddleware(ILogger<ApiLogMiddleware> logger) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await next.Invoke(context);
        }
        finally
        {
            var elapsedMilliseconds = sw.ElapsedMilliseconds;
            var protocol = context.Request.Protocol;
            var method = context.Request.Method;
            var path = context.Request.Path;
            var statusCode = context.Response.StatusCode;
            logger.LogInformation(
                "[{Protocol} {Method} {Path}] responded {StatusCode} in {ElapsedMilliseconds} ms",
                protocol, method, path, statusCode, elapsedMilliseconds);
        }
    }
}