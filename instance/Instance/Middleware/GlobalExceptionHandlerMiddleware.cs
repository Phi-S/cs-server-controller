using Instance.Response;

namespace Instance.Middleware;

public class GlobalExceptionHandlerMiddleware(ILogger<GlobalExceptionHandlerMiddleware> logger) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next.Invoke(context);
        }
        catch (Exception e)
        {
            var protocol = context.Request.Protocol;
            var method = context.Request.Method;
            var path = context.Request.Path;

            await Results.Extensions.InternalServerError("unknown error").ExecuteAsync(context);
            logger.LogError(e, "[{Protocol} {Method} {Path}] failed with exception", protocol, method, path);
        }
    }
}