using Application.ConfigEditorFolder.CQRS;
using Application.EventServiceFolder.CQRS;
using Application.EventServiceFolder.EventArgs;
using Instance.Response;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared;

namespace Instance.Endpoints;

public static class EventsEndpoint
{
    public static void MapEndpointsEvents(this IEndpointRouteBuilder endpointRouteBuilder)
    {
        const string tag = "events";
        var group = endpointRouteBuilder
            .MapGroup(tag)
            .WithTags(tag)
            .WithOpenApi();

        group.MapGet("", async (IMediator mediator) =>
        {
            var command = new GetEventsQuery();
            var result = await mediator.Send(command);
            return Results.Ok(result);
        });

        group.MapGet("logs", async (
            [FromQuery] string? eventName,
            [FromQuery] long logsSince,
            IMediator mediator) =>
        {
            if (eventName is null)
            {
                var command = new GetEventsLogsSinceQuery(logsSince);
                var result = await mediator.Send(command);
                return Results.Ok(result);
            }
            else
            {
                var command = new GetEventLogsSinceQuery(eventName, logsSince);
                var result = await mediator.Send(command);
                return Results.Ok(result);
            }
        });
    }
}