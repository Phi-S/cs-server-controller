using System.Web;
using AppOptionsLib;
using EventsServiceLib;
using Instance.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ServerServiceLib;
using SharedModelsLib.ApiModels;
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

        group.MapGet("info", (IOptions<AppOptions> options, StatusService statusService) =>
        {
            var status = statusService.GetStatus();
            var info = new InfoModel(
                statusService.ServerStartParameters?.ServerHostname,
                statusService.ServerStartParameters?.ServerPassword,
                status.CurrentMap,
                status.CurrentPlayerCount,
                statusService.ServerStartParameters?.MaxPlayer,
                options.Value.IP_OR_DOMAIN,
                options.Value.PORT,
                status.ServerStarting,
                status.ServerStarted,
                status.ServerStopping,
                status.ServerHibernating,
                status.ServerUpdatingOrInstalling,
                status.DemoUploading
            );
            return Results.Ok(info);
        });

        group.MapGet("events", () =>
        {
            var events = Enum.GetValues<Events>().Select(e => e.ToString()).ToList();
            return Results.Ok(events);
        });


        // Stops the server and starts the update or install process.
        // If startParameters is set, the server will start automatically after the update or install process
        group.MapPost("start-updating-or-installing", async (
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

        group.MapPost("cancel-updating-or-installing",
            (UpdateOrInstallService updateOrInstallService, [FromQuery] Guid id) =>
            {
                var cancelUpdate = updateOrInstallService.CancelUpdate(id);
                return cancelUpdate.IsFailed
                    ? Results.Extensions.InternalServerError(cancelUpdate.Exception.Message)
                    : Results.Ok();
            });

        group.MapPost("start", async (
            ServerService serverService,
            [FromBody] StartParameters startParameters) =>
        {
            var start = await serverService.Start(startParameters);
            return start.IsFailed
                ? Results.Extensions.InternalServerError(start.Exception.Message)
                : Results.Ok();
        });

        group.MapPost("stop", async (ServerService serverService) =>
        {
            var start = await serverService.Stop();
            return start.IsFailed
                ? Results.Extensions.InternalServerError(start.Exception.Message)
                : Results.Ok();
        });


        group.MapPost("send-command", async (ServerService serverService, [FromQuery] string command) =>
        {
            var commandUrlDecoded = HttpUtility.UrlDecode(command);
            var executeCommand = await serverService.ExecuteCommand(commandUrlDecoded);
            return executeCommand.IsFailed
                ? Results.Extensions.InternalServerError(executeCommand.Exception.Message)
                : Results.Ok(executeCommand.Value);
        });

        group.MapGet("maps", (IOptions<AppOptions> options, ServerService serverService) =>
        {
            var maps = serverService.GetAllMaps(options.Value.SERVER_FOLDER);
            return Results.Ok(maps);
        });

        group.MapGet("configs", (IOptions<AppOptions> options, ServerService serverService) =>
        {
            var configs = serverService.GetAvailableConfigs(options.Value.SERVER_FOLDER).Keys.ToList();
            return Task.FromResult(Results.Ok(configs));
        });

        return group;
    }
}