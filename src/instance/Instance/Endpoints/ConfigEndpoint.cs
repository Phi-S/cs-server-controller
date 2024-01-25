using Application.ConfigEditorFolder.CQRS;
using Instance.Response;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared;

namespace Instance.Endpoints;

public static class ConfigEndpoint
{
    public static void MapEndpointsConfig(this IEndpointRouteBuilder endpointRouteBuilder)
    {
        const string tag = "configs";
        var group = endpointRouteBuilder
            .MapGroup(tag)
            .WithTags(tag)
            .WithOpenApi();

        group.MapGet("", async (IMediator mediator) =>
        {
            var command = new GetConfigsQuery();
            var result = await mediator.Send(command);
            return result.IsError
                ? Results.Extensions.InternalServerError(result.ErrorMessage())
                : Results.Ok(result.Value);
        });

        group.MapGet("get-content", async ([FromQuery] string configFile, IMediator mediator) =>
        {
            var command = new GetConfigQuery(configFile);
            var result = await mediator.Send(command);
            return result.IsError
                ? Results.Extensions.InternalServerError(result.ErrorMessage())
                : Results.Ok(result.Value);
        });

        group.MapPost("set-content",
            async ([FromQuery] string configFile, [FromBody] string content, IMediator mediator) =>
            {
                var command = new SetConfigCommand(configFile, content);
                await mediator.Send(command);
                return Results.Ok();
            });
    }
}