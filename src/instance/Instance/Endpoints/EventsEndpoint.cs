using Application.EventServiceFolder.CQRS;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Shared.ApiModels;

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

        group.MapGet("", async Task<Ok<List<string>>> (IMediator mediator) =>
        {
            var command = new GetEventsQuery();
            var result = await mediator.Send(command);
            return TypedResults.Ok(result);
        });

        group.MapGet("logs", async Task<Ok<List<EventLogResponse>>>
            ([FromQuery] string? eventName, [FromQuery] long logsSince, IMediator mediator) =>
        {
            if (eventName is null)
            {
                var command = new GetEventsLogsSinceQuery(logsSince);
                var result = await mediator.Send(command);
                return TypedResults.Ok(result);
            }
            else
            {
                var command = new GetEventLogsSinceQuery(eventName, logsSince);
                var result = await mediator.Send(command);
                return TypedResults.Ok(result);
            }
        });
    }
}