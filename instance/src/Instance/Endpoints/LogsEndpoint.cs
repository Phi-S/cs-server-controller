using DatabaseLib.Repos;
using Microsoft.AspNetCore.Mvc;
using SharedModelsLib.ApiModels;

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

        group.MapGet("server", async (ServerRepo serverRepo, [FromQuery] long logsSince) =>
        {
            var logsSinceDateTime = DateTimeOffset.FromUnixTimeMilliseconds(logsSince).DateTime;
            var logs = await serverRepo.GetSince(logsSinceDateTime);
            var response = logs.Select(
                    log => new ServerLogResponse(
                        log.ServerStart.Id,
                        log.Message,
                        log.CreatedAtUtc)
                )
                .ToList();
            return Results.Ok(response);
        });

        group.MapGet("update-or-install",
            async (UpdateOrInstallRepo updateOrInstallRepo, [FromQuery] long logsSince) =>
            {
                var logsSinceDateTime = DateTimeOffset.FromUnixTimeMilliseconds(logsSince).DateTime;
                var logs = await updateOrInstallRepo.GetSince(logsSinceDateTime);
                var response = logs.Select(
                        log => new UpdateOrInstallLogResponse(
                            log.UpdateOrInstallStart.Id,
                            log.Message,
                            log.CreatedAtUtc)
                    )
                    .ToList();

                return Results.Ok(response);
            });

        group.MapGet("events", async (EventLogRepo eventLogRepo, [FromQuery] long logsSince) =>
        {
            var logsSinceDateTime = DateTimeOffset.FromUnixTimeMilliseconds(logsSince).DateTime;
            var logs = await eventLogRepo.GetAllSince(logsSinceDateTime);
            var response = logs.Select(
                    log => new EventLogResponse(
                        log.Name,
                        log.TriggeredAtUtc)
                )
                .ToList();
            return Results.Ok(response);
        });

        group.MapGet("events/{eventName}",
            async (EventLogRepo eventLogRepo, string eventName, [FromQuery] long logsSince) =>
            {
                var logsSinceDateTime = DateTimeOffset.FromUnixTimeMilliseconds(logsSince).DateTime;
                var logs = await eventLogRepo.GetAllSince(logsSinceDateTime, eventName);
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