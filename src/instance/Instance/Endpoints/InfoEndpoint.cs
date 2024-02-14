using Application.InstalledVersionsFolder.CQRS;
using Application.StatusServiceFolder.CQRS;
using Instance.Response;
using MediatR;
using Shared;

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

        group.MapGet("", async (IMediator mediator) =>
        {
            var command = new GetServerStatusQuery();
            var result = await mediator.Send(command);
            return Results.Ok(result);
        });

        group.MapGet("/installed-versions", async (IMediator mediator) =>
        {
            var command = new GetInstalledVersionsQuery();
            var result = await mediator.Send(command);
            return result.IsError
                ? Results.Extensions.InternalServerError(result.ErrorMessage())
                : Results.Ok(result.Value);
        });
    }
}