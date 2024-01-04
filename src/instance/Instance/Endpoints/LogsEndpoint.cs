using Application.CQRS.Query;
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