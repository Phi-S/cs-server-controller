using Application.StatusServiceFolder.CQRS;
using MediatR;

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
    }
}