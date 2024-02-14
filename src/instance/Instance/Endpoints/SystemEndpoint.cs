using Application.SystemLogFolder.CQRS;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Shared.ApiModels;

namespace Instance.Endpoints;

public static class SystemEndpoint
{
    public static RouteGroupBuilder MapEndpointsSystem(this IEndpointRouteBuilder endpointRouteBuilder)
    {
        const string tag = "system";
        var group = endpointRouteBuilder
            .MapGroup(tag)
            .WithTags(tag)
            .WithOpenApi();

        group.MapGet("logs", async Task<Ok<List<SystemLogResponse>>>
            (IMediator mediator, [FromQuery] long logsSince) =>
        {
            var command = new GetSystemLogsSinceQuery(logsSince);
            var result = await mediator.Send(command);
            return TypedResults.Ok(result);
        });

        return group;
    }
}