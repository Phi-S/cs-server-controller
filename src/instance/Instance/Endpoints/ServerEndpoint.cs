using System.Web;
using Application.ChatCommandFolder;
using Application.ChatCommandFolder.CQRS;
using Application.EventServiceFolder;
using Application.EventServiceFolder.CQRS;
using Application.ServerPluginsFolder;
using Application.ServerPluginsFolder.CQRS;
using Application.ServerServiceFolder;
using Application.ServerServiceFolder.CQRS;
using Application.StartParameterFolder;
using Application.StartParameterFolder.CQRS;
using Application.StatusServiceFolder;
using Application.StatusServiceFolder.CQRS;
using Application.UpdateOrInstallServiceFolder;
using Application.UpdateOrInstallServiceFolder.CQRS;
using Instance.Response;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared;
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
            IMediator mediator) =>
        {
            var command = new StartUpdateOrInstallCommand();
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

        group.MapPost("update-or-install-plugins", async (IMediator mediator) =>
        {
            var command = new UpdateOrInstallPluginsCommand();
            var result = await mediator.Send(command);
            return result.IsError
                ? Results.Extensions.InternalServerError(result.ErrorMessage())
                : Results.Ok();
        });

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

        group.MapGet("chat-command/all", async (IMediator mediator) =>
        {
            var command = new GetAllChatCommandsQuery();
            var result = await mediator.Send(command);
            return Results.Ok(result);
        });

        group.MapPost("chat-command/new",
            async (IMediator mediator, [FromQuery] string chatMessage, [FromQuery] string serverCommand) =>
            {
                var urlDecodedChatMessage = HttpUtility.UrlDecode(chatMessage);
                var urlDecodedServerCommand = HttpUtility.UrlDecode(serverCommand);
                var mediatorCommand =
                    new AddChatCommandCommand(urlDecodedChatMessage, urlDecodedServerCommand);
                var result = await mediator.Send(mediatorCommand);
                return result.IsError
                    ? Results.Extensions.InternalServerError(result.ErrorMessage())
                    : Results.Ok();
            });

        group.MapPost("chat-command/delete",
            async (IMediator mediator, [FromQuery] string chatMessage) =>
            {
                var urlDecodedChatMessage = HttpUtility.UrlDecode(chatMessage);
                var mediatorCommand = new DeleteChatCommandCommand(urlDecodedChatMessage);
                var result = await mediator.Send(mediatorCommand);
                return result.IsError
                    ? Results.Extensions.InternalServerError(result.ErrorMessage())
                    : Results.Ok();
            });

        group.MapGet("start-parameters/get",
            async (IMediator mediator) =>
            {
                var command = new GetStartParametersQuery();
                var result = await mediator.Send(command);
                return result.IsError
                    ? Results.Extensions.InternalServerError(result.ErrorMessage())
                    : Results.Ok(result.Value);
            });

        group.MapPost("start-parameters/set",
            async (IMediator mediator, [FromBody] StartParameters startParameters) =>
            {
                var command = new SetStartParametersCommand(startParameters);
                await mediator.Send(command);
                return Results.Ok();
            });

        return group;
    }
}