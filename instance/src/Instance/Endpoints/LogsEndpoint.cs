using DatabaseLib.Repos;
using Microsoft.AspNetCore.Mvc;
using SharedModelsLib;

namespace Instance.Endpoints;

public static class LogsEndpoint
{
    public static RouteGroupBuilder MapLogsEndpoints(this IEndpointRouteBuilder endpointRouteBuilder)
    {
        const string tag = "logs";
        var group = endpointRouteBuilder
            .MapGroup(tag)
            .WithTags(tag)
            .WithOpenApi();

        group.MapGet("server", async (ServerRepo serverRepo, [FromBody] DateTime logsSince) =>
        {
            var logs = await serverRepo.GetSince(logsSince);
            var response = logs.Select(
                    log => new StartLogResponse(
                        log.ServerStart.Id,
                        log.Message,
                        log.CreatedAtUtc)
                )
                .ToList();
            return Results.Ok(response);
        });

        group.MapGet("update-or-install",
            async (UpdateOrInstallRepo updateOrInstallRepo, [FromBody] DateTime logsSince) =>
            {
                var logs = await updateOrInstallRepo.GetSince(logsSince);
                var response = logs.Select(
                        log => new UpdateOrInstallLogResponse(
                            log.UpdateOrInstallStart.Id,
                            log.Message,
                            log.CreatedAtUtc)
                    )
                    .ToList();

                return Results.Ok(response);
            });

        group.MapGet("events", async (EventLogRepo eventLogRepo, [FromBody] DateTime logsSince) =>
        {
            var logs = await eventLogRepo.GetAllSince(logsSince);
            var response = logs.Select(
                    log => new EventLogResponse(
                        log.Name,
                        log.TriggeredAtUtc)
                )
                .ToList();
            return Results.Ok(response);
        });

        group.MapGet("events/{eventName}", async (EventLogRepo eventLogRepo, [FromBody] DateTime logsSince, string eventName) =>
        {
            var logs = await eventLogRepo.GetAllSince(logsSince, eventName);
            var response = logs.Select(
                    log => new EventLogResponse(
                        log.Name,
                        log.TriggeredAtUtc)
                )
                .ToList();
            return Results.Ok(response);
        });


        return group;
    }
}