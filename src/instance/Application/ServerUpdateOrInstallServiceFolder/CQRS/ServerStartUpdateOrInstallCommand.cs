using Application.CounterStrikeSharpUpdateOrInstallFolder;
using Application.EventServiceFolder;
using Application.EventServiceFolder.EventArgs;
using Application.StatusServiceFolder;
using Domain;
using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared;

namespace Application.ServerUpdateOrInstallServiceFolder.CQRS;

public record ServerStartUpdateOrInstallCommand : IRequest<ErrorOr<Guid>>;

public class
    ServerStartUpdateOrInstallCommandHandler : IRequestHandler<ServerStartUpdateOrInstallCommand, ErrorOr<Guid>>
{
    private readonly ILogger<ServerStartUpdateOrInstallCommandHandler> _logger;
    private readonly ServerUpdateOrInstallService _serverUpdateOrInstallService;
    private readonly StatusService _statusService;
    private readonly IOptions<AppOptions> _options;
    private readonly EventService _eventService;

    public ServerStartUpdateOrInstallCommandHandler(
        ILogger<ServerStartUpdateOrInstallCommandHandler> logger,
        ServerUpdateOrInstallService serverUpdateOrInstallService,
        StatusService statusService,
        IOptions<AppOptions> options,
        EventService eventService)
    {
        _logger = logger;
        _serverUpdateOrInstallService = serverUpdateOrInstallService;
        _statusService = statusService;
        _options = options;
        _eventService = eventService;
    }

    public async Task<ErrorOr<Guid>> Handle(ServerStartUpdateOrInstallCommand request,
        CancellationToken cancellationToken)
    {
        var updateOrInstallResult = await _serverUpdateOrInstallService.StartUpdateOrInstall();
        if (updateOrInstallResult.IsError == false && _statusService.CounterStrikeSharpInstalled)
        {
            _eventService.UpdateOrInstallDone += EventServiceOnUpdateOrInstallDone;
            _eventService.UpdateOrInstallFailed += EventServiceOnUpdateOrInstallFailedOrCancelled;
            _eventService.UpdateOrInstallCancelled += EventServiceOnUpdateOrInstallFailedOrCancelled;
        }

        return updateOrInstallResult;
    }

    private void EventServiceOnUpdateOrInstallFailedOrCancelled(object? _, CustomEventArgUpdateOrInstall __)
    {
        try
        {
            _eventService.UpdateOrInstallDone -= EventServiceOnUpdateOrInstallDone;
        }
        finally
        {
            _eventService.UpdateOrInstallFailed -= EventServiceOnUpdateOrInstallFailedOrCancelled;
            _eventService.UpdateOrInstallCancelled -= EventServiceOnUpdateOrInstallFailedOrCancelled;
        }
    }

    private async void EventServiceOnUpdateOrInstallDone(object? _, CustomEventArgUpdateOrInstall __)
    {
        var csgoFolder = Path.Combine(
            _options.Value.SERVER_FOLDER,
            "game",
            "csgo");

        try
        {
            var addMetamodEntryToGameinfoGi =
                await CounterStrikeSharpUpdateOrInstallService.AddMetamodEntryToGameinfoGi(csgoFolder);
            if (addMetamodEntryToGameinfoGi.IsError)
            {
                _logger.LogError(
                    "Failed to add Metamod entry to gameinfo.gi after server update or install finished. {Error}",
                    addMetamodEntryToGameinfoGi.ErrorMessage());
            }
            else
            {
                _logger.LogInformation(
                    "Metamod entry added to gameinfo.gi after server update or install finished");
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e,
                "Exception while trying to add Metamod entry to gameinfo.gi after server update or install finished");
        }
        finally
        {
            _eventService.UpdateOrInstallDone -= EventServiceOnUpdateOrInstallDone;
            _eventService.UpdateOrInstallFailed -= EventServiceOnUpdateOrInstallFailedOrCancelled;
            _eventService.UpdateOrInstallCancelled -= EventServiceOnUpdateOrInstallFailedOrCancelled;
        }
    }
}