using System.Web;
using Application.ServerServiceFolder.CQRS;
using Instance.Response;
using MediatR;
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


        group.MapPost("start", async (
            IMediator mediator,
            [FromBody] StartParameters? startParameters = null) =>
        {
            var command = new StartServerCommand(startParameters);
            var result = await mediator.Send(command);
            return result.IsError
                ? Results.Extensions.InternalServerError(result.ErrorMessage())
                : Results.Ok();
        });

        group.MapPost("stop", async (IMediator mediator) =>
        {
            var command = new StopServerCommand();
            var result = await mediator.Send(command);
            return result.IsError
                ? Results.Extensions.InternalServerError(result.ErrorMessage())
                : Results.Ok();
        });

        group.MapPost("send-command", async (IMediator mediator, [FromQuery] string command) =>
        {
            var commandUrlDecoded = HttpUtility.UrlDecode(command);
            var mediatorCommand = new SendCommandCommand(commandUrlDecoded);
            var result = await mediator.Send(mediatorCommand);
            return result.IsError
                ? Results.Extensions.InternalServerError(result.ErrorMessage())
                : Results.Ok(result.Value);
        });

        group.MapGet("logs", async ([FromQuery] long logsSince, IMediator mediator) =>
        {
            var command = new GetServerLogsSinceQuery(logsSince);
            var result = await mediator.Send(command);
            return Results.Ok(result);
        });

        return group;
    }
}