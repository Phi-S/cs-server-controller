using Application.InstalledVersionsFolder.CQRS;
using Application.StatusServiceFolder.CQRS;
using Instance.Response;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Shared;
using Shared.ApiModels;

namespace Instance.Endpoints;

public static class InfoEndpoint
{
    public static void MapEndpointsInfo(this IEndpointRouteBuilder endpointRouteBuilder)
    {
        const string tag = "info";
        var group = endpointRouteBuilder
            .MapGroup(tag)
            .WithTags(tag)
            .WithOpenApi();

        group.MapGet("", async Task<Ok<ServerInfoResponse>> (IMediator mediator) =>
        {
            var command = new GetServerStatusQuery();
            var result = await mediator.Send(command);
            return TypedResults.Ok(result);
        });

        group.MapGet("/installed-versions", async Task<Results<ErrorResult, Ok<List<InstalledVersionsModel>>>>
            (IMediator mediator) =>
        {
            var command = new GetInstalledVersionsQuery();
            var result = await mediator.Send(command);
            return result.IsError
                ? Results.Extensions.InternalServerError(result.ErrorMessage())
                : TypedResults.Ok(result.Value);
        });
    }
}