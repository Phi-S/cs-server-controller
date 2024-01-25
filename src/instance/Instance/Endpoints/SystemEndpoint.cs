using Application.EventServiceFolder.CQRS;
using Application.ServerServiceFolder.CQRS;
using Application.SystemLogFolder.CQRS;
using Application.UpdateOrInstallServiceFolder.CQRS;
using MediatR;
using Microsoft.AspNetCore.Mvc;

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
        
        group.MapGet("logs", async (IMediator mediator, [FromQuery] long logsSince) =>
        {
            var command = new GetSystemLogsSinceQuery(logsSince);
            var result = await mediator.Send(command);
            return Results.Ok(result);
        });

        return group;
    }
}