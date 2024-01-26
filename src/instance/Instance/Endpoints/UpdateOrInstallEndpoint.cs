using Application.ServerPluginsFolder.CQRS;
using Application.UpdateOrInstallServiceFolder.CQRS;
using Instance.Response;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared;

namespace Instance.Endpoints;

public static class UpdateOrInstallEndpoint
{
    public static void MapEndpointsUpdateOrInstall(this IEndpointRouteBuilder endpointRouteBuilder)
    {
        const string tag = "update-or-install";
        var group = endpointRouteBuilder
            .MapGroup(tag)
            .WithTags(tag)
            .WithOpenApi();

        group.MapPost("start", async (IMediator mediator) =>
        {
            var command = new StartUpdateOrInstallCommand();
            var result = await mediator.Send(command);
            return result.IsError
                ? Results.Extensions.InternalServerError(result.ErrorMessage())
                : Results.Ok(result.Value);
        });

        group.MapPost("cancel", async ([FromQuery] Guid id, IMediator mediator) =>
        {
            var command = new CancelUpdateOrInstallCommand(id);
            var result = await mediator.Send(command);
            return result.IsError
                ? Results.Extensions.InternalServerError(result.ErrorMessage())
                : Results.Ok();
        });

        group.MapPost("plugins", async (IMediator mediator) =>
        {
            var command = new UpdateOrInstallPluginsCommand();
            var result = await mediator.Send(command);
            return result.IsError
                ? Results.Extensions.InternalServerError(result.ErrorMessage())
                : Results.Ok();
        });

        group.MapGet("logs", async ([FromQuery] long logsSince, IMediator mediator) =>
        {
            var command = new GetUpdateOrInstallLogsSinceQuery(logsSince);
            var result = await mediator.Send(command);
            return Results.Ok(result);
        });
    }
}