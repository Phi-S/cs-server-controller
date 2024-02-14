using Application.ConfigEditorFolder.CQRS;
using Instance.Response;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
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

        group.MapGet("", async Task<Results<ErrorResult, Ok<List<string>>>> (IMediator mediator) =>
        {
            var command = new GetConfigsQuery();
            var result = await mediator.Send(command);
            return result.IsError
                ? Results.Extensions.InternalServerError(result.ErrorMessage())
                : TypedResults.Ok(result.Value);
        });

        group.MapGet("get-content", async Task<Results<ErrorResult, Ok<string>>>
            ([FromQuery] string configFile, IMediator mediator) =>
        {
            var command = new GetConfigQuery(configFile);
            var result = await mediator.Send(command);
            return result.IsError
                ? Results.Extensions.InternalServerError(result.ErrorMessage())
                : TypedResults.Ok(result.Value);
        });

        group.MapPost("set-content", async Task<Ok>
            ([FromQuery] string configFile, [FromBody] string content, IMediator mediator) =>
        {
            var command = new SetConfigCommand(configFile, content);
            await mediator.Send(command);
            return TypedResults.Ok();
        });
    }
}