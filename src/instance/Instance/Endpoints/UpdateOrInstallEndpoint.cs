using Application.CounterStrikeSharpUpdateOrInstallFolder.CQRS;
using Application.ServerUpdateOrInstallServiceFolder.CQRS;
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

        group.MapPost("server/start", async (IMediator mediator) =>
        {
            var command = new ServerStartUpdateOrInstallCommand();
            var result = await mediator.Send(command);
            return result.IsError
                ? Results.Extensions.InternalServerError(result.ErrorMessage())
                : Results.Ok(result.Value);
        });

        group.MapPost("server/cancel", async ([FromQuery] Guid id, IMediator mediator) =>
        {
            var command = new ServerCancelUpdateOrInstallCommand(id);
            var result = await mediator.Send(command);
            return result.IsError
                ? Results.Extensions.InternalServerError(result.ErrorMessage())
                : Results.Ok();
        });

        group.MapGet("server/logs", async ([FromQuery] long logsSince, IMediator mediator) =>
        {
            var command = new GetServerUpdateOrInstallLogsSinceQuery(logsSince);
            var result = await mediator.Send(command);
            return Results.Ok(result);
        });

        group.MapPost("counter-strike-sharp", async (IMediator mediator) =>
        {
            var command = new CounterStrikeSharpUpdateOrInstallCommand();
            var result = await mediator.Send(command);
            return result.IsError
                ? Results.Extensions.InternalServerError(result.ErrorMessage())
                : Results.Ok();
        });
    }
}