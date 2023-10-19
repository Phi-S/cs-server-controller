using AppOptionsLib;
using Instance.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ServerServiceLib;
using StatusServiceLib;
using UpdateOrInstallServiceLib;

namespace Instance.Endpoints;

public static class ServerEndpoint
{
    public static RouteGroupBuilder MapServerEndpoints(this IEndpointRouteBuilder endpointRouteBuilder)
    {
        const string tag = "server";
        var group = endpointRouteBuilder
            .MapGroup(tag)
            .WithTags(tag)
            .WithOpenApi();

        group.MapGet("Status", (StatusService statusService) =>
        {
            var status = statusService.GetStatus();
            return Results.Ok(status);
        });


        // Stops the server and starts the update or install process.
        // If startParameters is set, the server will start automatically after the update or install process
        group.MapPost("StartUpdatingOrInstalling", async (
            ServerService serverService,
            UpdateOrInstallService updateOrInstallService,
            [FromBody] StartParameters? startParameters = null) =>
        {
            var stop = await serverService.Stop();
            if (stop.IsFailed)
            {
                return Results.Extensions.InternalServerError(
                    $"Failed to start updating or install server. {stop.Exception.Message}");
            }

            var startUpdateOrInstall = startParameters == null
                ? await updateOrInstallService.StartUpdateOrInstall()
                : await updateOrInstallService.StartUpdateOrInstall(() => serverService.Start(startParameters));
            return startUpdateOrInstall.IsFailed
                ? Results.Extensions.InternalServerError(startUpdateOrInstall.Exception.Message)
                : Results.Ok(startUpdateOrInstall.Value);
        });

        group.MapPost("CancelUpdatingOrInstalling", (UpdateOrInstallService updateOrInstallService, Guid id) =>
        {
            var cancelUpdate = updateOrInstallService.CancelUpdate(id);
            return cancelUpdate.IsFailed
                ? Results.Extensions.InternalServerError(cancelUpdate.Exception.Message)
                : Results.Ok();
        });

        group.MapPost("Start", async (
            ServerService serverService,
            [FromBody] StartParameters startParameters) =>
        {
            var start = await serverService.Start(startParameters);
            return start.IsFailed
                ? Results.Extensions.InternalServerError(start.Exception.Message)
                : Results.Ok();
        });

        group.MapPost("Stop", async (ServerService serverService) =>
        {
            var start = await serverService.Stop();
            return start.IsFailed
                ? Results.Extensions.InternalServerError(start.Exception.Message)
                : Results.Ok();
        });

        group.MapGet("Maps", (IOptions<AppOptions> options, ServerService serverService) =>
        {
            var maps = serverService.GetAllMaps(options.Value.SERVER_FOLDER);
            return Results.Ok(maps);
        });

        group.MapPost("SendCommand", async (ServerService serverService, [FromBody] string command) =>
        {
            var executeCommand = await serverService.ExecuteCommand(command);
            return executeCommand.IsFailed
                ? Results.Extensions.InternalServerError(executeCommand.Exception.Message)
                : Results.Ok(executeCommand.Value);
        });

        return group;
    }
}