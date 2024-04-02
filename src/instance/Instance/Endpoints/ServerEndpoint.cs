using System.Web;
using Application.ServerServiceFolder.CQRS;
using Instance.Response;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Shared;
using Shared.ApiModels;

namespace Instance.Endpoints;

public static class ServerEndpoint
{
    public static RouteGroupBuilder MapEndpointsServer(this IEndpointRouteBuilder endpointRouteBuilder)
    {
        const string tag = "server";
        var group = endpointRouteBuilder
            .MapGroup(tag)
            .WithTags(tag)
            .WithOpenApi();

        group.MapPost("start", async Task<Results<ErrorResult, Ok>>
            (IMediator mediator, [FromBody] StartParameters? startParameters = null) =>
        {
            var command = new StartServerCommand(startParameters);
            var result = await mediator.Send(command);
            return result.IsError
                ? Results.Extensions.InternalServerError(result.ErrorMessage())
                : TypedResults.Ok();
        });

        group.MapPost("stop", async Task<Results<ErrorResult, Ok>> (IMediator mediator) =>
        {
            var command = new StopServerCommand();
            var result = await mediator.Send(command);
            return result.IsError
                ? Results.Extensions.InternalServerError(result.ErrorMessage())
                : TypedResults.Ok();
        });

        group.MapPost("send-command", async Task<Results<ErrorResult, Ok<string>>>
            (IMediator mediator, [FromQuery] string command) =>
        {
            var commandUrlDecoded = HttpUtility.UrlDecode(command);
            var mediatorCommand = new SendCommandCommand(commandUrlDecoded);
            var result = await mediator.Send(mediatorCommand);
            return result.IsError
                ? Results.Extensions.InternalServerError(result.ErrorMessage())
                : TypedResults.Ok(result.Value);
        });

        group.MapGet("logs", async Task<Ok<List<ServerLogResponse>>>
            ([FromQuery] long logsSince, IMediator mediator) =>
        {
            var command = new GetServerLogsSinceQuery(logsSince);
            var result = await mediator.Send(command);
            return TypedResults.Ok(result);
        });

        return group;
    }
}