using Application.EventServiceFolder;
using Application.EventServiceFolder.CQRS;
using Application.ServerServiceFolder;
using Application.ServerServiceFolder.CQRS;
using Application.SystemLogFolder;
using Application.SystemLogFolder.CQRS;
using Application.UpdateOrInstallServiceFolder;
using Application.UpdateOrInstallServiceFolder.CQRS;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Instance.Endpoints;

public static class LogsEndpoint
{
    public static RouteGroupBuilder MapLogsEndpoints(this IEndpointRouteBuilder endpointRouteBuilder)
    {
        const string tag = "logs";
        var group = endpointRouteBuilder
            .MapGroup(tag)
            .WithTags(tag)
            .WithOpenApi();
        
        group.MapGet("system", async (IMediator mediator, [FromQuery] long logsSince) =>
        {
            var command = new GetSystemLogsSinceQuery(logsSince);
            var result = await mediator.Send(command);
            return Results.Ok(result);
        });

        group.MapGet("server", async (IMediator mediator, [FromQuery] long logsSince) =>
        {
            var command = new GetServerLogsSinceQuery(logsSince);
            var result = await mediator.Send(command);
            return Results.Ok(result);
        });

        group.MapGet("update-or-install",
            async (IMediator mediator, [FromQuery] long logsSince) =>
            {
                var command = new GetUpdateOrInstallLogsSinceQuery(logsSince);
                var result = await mediator.Send(command);
                return Results.Ok(result);
            });

        group.MapGet("events", async (IMediator mediator, [FromQuery] long logsSince) =>
        {
            var command = new GetAllTriggeredEventsSinceQuery(logsSince);
            var result = await mediator.Send(command);
            return Results.Ok(result);
        });

        group.MapGet("events/{eventName}",
            async (IMediator mediator, string eventName, [FromQuery] long logsSince) =>
            {
                var command = new GetAllTriggersForEventSinceQuery(eventName, logsSince);
                var result = await mediator.Send(command);
                return Results.Ok(result);
            });

        return group;
    }
}