using Application.ConfigEditorFolder.CQRS;
using Application.StartParameterFolder.CQRS;
using Instance.Response;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared;
using Shared.ApiModels;

namespace Instance.Endpoints;

public static class StartParameterEndpoint
{
    public static void MapEndpointsStartParameter(this IEndpointRouteBuilder endpointRouteBuilder)
    {
        const string tag = "start-parameters";
        var group = endpointRouteBuilder
            .MapGroup(tag)
            .WithTags(tag)
            .WithOpenApi();

        group.MapGet("", async (IMediator mediator) =>
        {
            var command = new GetStartParametersQuery();
            var result = await mediator.Send(command);
            return result.IsError
                ? Results.Extensions.InternalServerError(result.ErrorMessage())
                : Results.Ok(result.Value);
        });

        group.MapPost("set", async ([FromBody] StartParameters startParameters, IMediator mediator) =>
        {
            var command = new SetStartParametersCommand(startParameters);
            await mediator.Send(command);
            return Results.Ok();
        });
    }
}