using Application.ServerUpdateOrInstallServiceFolder.CQRS;
using Instance.Response;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Shared;
using Shared.ApiModels;

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

        group.MapPost("start", async Task<Results<ErrorResult, Ok<Guid>>>
            (IMediator mediator) =>
        {
            var command = new ServerStartUpdateOrInstallCommand();
            var result = await mediator.Send(command);
            return result.IsError
                ? Results.Extensions.InternalServerError(result.ErrorMessage())
                : TypedResults.Ok(result.Value);
        });

        group.MapPost("cancel", async Task<Results<ErrorResult, Ok>>
            ([FromQuery] Guid id, IMediator mediator) =>
        {
            var command = new ServerCancelUpdateOrInstallCommand(id);
            var result = await mediator.Send(command);
            return result.IsError
                ? Results.Extensions.InternalServerError(result.ErrorMessage())
                : TypedResults.Ok();
        });

        group.MapGet("logs", async Task<Ok<List<UpdateOrInstallLogResponse>>>
            ([FromQuery] long logsSince, IMediator mediator) =>
        {
            var command = new GetServerUpdateOrInstallLogsSinceQuery(logsSince);
            var result = await mediator.Send(command);
            return TypedResults.Ok(result);
        });
    }
}