using System.Net.Mime;
using System.Text;
using System.Text.Json;

namespace Instance.Response;

internal static class ResultsExtensions
{
    public static IResult InternalServerError(this IResultExtensions resultExtensions, string message)
    {
        ArgumentNullException.ThrowIfNull(resultExtensions);
        return new ErrorResponse(StatusCodes.Status500InternalServerError, message);
    }
}

public class ErrorResponse(int statusCode, string message) : IResult
{
    private string GetJson(string traceId)
    {
        var resultObject = new
        {
            TraceId = traceId,
            Message = message,
        };

        return JsonSerializer.Serialize(resultObject);
    }

    public async Task ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Response.ContentType = MediaTypeNames.Application.Json;
        httpContext.Response.StatusCode = statusCode;
        var responseJson = GetJson(httpContext.TraceIdentifier);
        httpContext.Response.ContentLength = Encoding.UTF8.GetByteCount(responseJson);
        await httpContext.Response.WriteAsync(responseJson);
    }
}