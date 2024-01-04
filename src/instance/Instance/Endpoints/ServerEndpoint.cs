using System.Web;
using Application.CQRS.Commands;
using Application.CQRS.Query;
using Domain;
using Instance.Response;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.ApiModels;

namespace Instance.Endpoints;

public static class ServerEndpoint
{
    public static RouteGroupBuilder MapServerEndpoints(this IEndpointRouteBuilder endpointRouteBuilder)
    {
        const string tag = "server";
        var group = endpointRouteBuilder
            .MapGroup(tag)
            .WithTags(tag)
            .WithOpenApi();

        group.MapGet("info", async (IMediator mediator) =>
        {
            var command = new GetServerStatusQuery();
            var result = await mediator.Send(command);
            return Results.Ok(result);
        });

        group.MapGet("events", async (IMediator mediator) =>
        {
            var command = new GetAllAvailableServerEventsQuery();
            var result = await mediator.Send(command);
            return Results.Ok(result);
        });

        // Stops the server and starts the update or install process.
        // If startParameters is set, the server will start automatically after the update or install process
        group.MapPost("start-updating-or-installing", async (
            IMediator mediator,
            [FromBody] StartParameters? startParameters = null) =>
        {
            var command = new StartUpdateOrInstallCommand(startParameters);
            var result = await mediator.Send(command);
            return result.IsError
                ? Results.Extensions.InternalServerError(result.ErrorMessage())
                : Results.Ok(result.Value);
        });

        group.MapPost("cancel-updating-or-installing",
            async (IMediator mediator, [FromQuery] Guid id) =>
            {
                var command = new CancelUpdateOrInstallCommand(id);
                var result = await mediator.Send(command);
                return result.IsError
                    ? Results.Extensions.InternalServerError(result.ErrorMessage())
                    : Results.Ok();
            });

        group.MapPost("start", async (
            IMediator mediator,
            [FromBody] StartParameters startParameters) =>
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

        group.MapGet("maps", async (IMediator mediator) =>
        {
            var command = new GetAllAvailableMapsQuery();
            var result = await mediator.Send(command);
            return result.IsError
                ? Results.Extensions.InternalServerError(result.ErrorMessage())
                : Results.Ok(result.Value);
        });

        group.MapGet("configs", async (IMediator mediator) =>
        {
            var command = new GetAllAvailableServerConfigsQuery();
            var result = await mediator.Send(command);
            return result.IsError
                ? Results.Extensions.InternalServerError(result.ErrorMessage())
                : Results.Ok(result.Value);
        });

        group.MapPost("chat-command",
            async (IMediator mediator, [FromQuery] string chatMessage, [FromQuery] string command) =>
            {
                var mediatorCommand =
                    new AddChatCommandCommand(HttpUtility.UrlDecode(chatMessage), HttpUtility.UrlDecode(command));
                var result = await mediator.Send(mediatorCommand);
                return result.IsError
                    ? Results.Extensions.InternalServerError(result.ErrorMessage())
                    : Results.Ok();
            });

        return group;
    }
}