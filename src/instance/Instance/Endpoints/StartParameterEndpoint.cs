using Application.StartParameterFolder.CQRS;
using Instance.Response;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
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

        group.MapGet("", async Task<Results<ErrorResult, Ok<StartParameters>>> (IMediator mediator) =>
        {
            var command = new GetStartParametersQuery();
            var result = await mediator.Send(command);
            return result.IsError
                ? Results.Extensions.InternalServerError(result.ErrorMessage())
                : TypedResults.Ok(result.Value);
        });

        group.MapPost("set", async Task<Ok>
            ([FromBody] StartParameters startParameters, IMediator mediator) =>
        {
            var command = new SetStartParametersCommand(startParameters);
            await mediator.Send(command);
            return TypedResults.Ok();
        });
    }
}