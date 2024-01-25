using Application.ConfigEditorFolder.CQRS;
using Application.ServerPluginsFolder.CQRS;
using Application.StartParameterFolder.CQRS;
using Application.StatusServiceFolder.CQRS;
using Application.UpdateOrInstallServiceFolder.CQRS;
using Instance.Response;
using MediatR;
using Microsoft.AspNetCore.Mvc;
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

        group.MapGet("", async (IMediator mediator) =>
        {
            var command = new GetServerStatusQuery();
            var result = await mediator.Send(command);
            return Results.Ok(result);
        });
    }
}