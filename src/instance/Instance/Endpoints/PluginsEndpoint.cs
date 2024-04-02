using Application.PluginsFolder.CQRS;
using Instance.Response;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Shared;
using Shared.ApiModels;

namespace Instance.Endpoints;

public static class PluginsEndpoint
{
    public static void MapEndpointsPlugins(this IEndpointRouteBuilder endpointRouteBuilder)
    {
        const string tag = "plugins";
        var group = endpointRouteBuilder
            .MapGroup(tag)
            .WithTags(tag)
            .WithOpenApi();

        group.MapGet("/", async Task<Results<ErrorResult, Ok<List<PluginsResponseModel>>>>
            (IMediator mediator) =>
        {
            var command = new GetPluginsQuery();
            var result = await mediator.Send(command);
            return result.IsError
                ? Results.Extensions.InternalServerError(result.ErrorMessage())
                : TypedResults.Ok(result.Value);
        });

        group.MapPost("update-or-install",
            async Task<Results<ErrorResult, Ok>> (IMediator mediator, string name, string version) =>
            {
                var command = new UpdateOrInstallPluginCommand(name, version);
                var result = await mediator.Send(command);
                return result.IsError
                    ? Results.Extensions.InternalServerError(result.ErrorMessage())
                    : TypedResults.Ok();
            });
    }
}