using System.Web;
using Application.ChatCommandFolder.CQRS;
using Instance.Response;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared;

namespace Instance.Endpoints;

public static class ChatCommandsEndpoint
{
    public static void MapEndpointsChatCommands(this IEndpointRouteBuilder endpointRouteBuilder)
    {
        const string tag = "chat-commands";
        var group = endpointRouteBuilder
            .MapGroup(tag)
            .WithTags(tag)
            .WithOpenApi();

        group.MapGet("", async (IMediator mediator) =>
        {
            var command = new GetAllChatCommandsQuery();
            var result = await mediator.Send(command);
            return Results.Ok(result);
        });

        group.MapPost("new",
            async ([FromQuery] string chatMessage, [FromQuery] string serverCommand, IMediator mediator) =>
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

        group.MapPost("delete", async ([FromQuery] string chatMessage, IMediator mediator) =>
        {
            var urlDecodedChatMessage = HttpUtility.UrlDecode(chatMessage);
            var mediatorCommand = new DeleteChatCommandCommand(urlDecodedChatMessage);
            var result = await mediator.Send(mediatorCommand);
            return result.IsError
                ? Results.Extensions.InternalServerError(result.ErrorMessage())
                : Results.Ok();
        });
    }
}