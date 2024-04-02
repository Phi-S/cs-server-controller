using Application.EventServiceFolder;
using Application.EventServiceFolder.EventArgs;
using Application.PluginsFolder;
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
    private readonly IOptions<AppOptions> _options;
    private readonly ILogger<ServerStartUpdateOrInstallCommandHandler> _logger;
    private readonly ServerUpdateOrInstallService _serverUpdateOrInstallService;
    private readonly EventService _eventService;
    private readonly InstalledPluginsService _installedPluginsService;

    public ServerStartUpdateOrInstallCommandHandler(
        IOptions<AppOptions> options,
        ILogger<ServerStartUpdateOrInstallCommandHandler> logger,
        ServerUpdateOrInstallService serverUpdateOrInstallService,
        EventService eventService,
        InstalledPluginsService installedPluginsService)
    {
        _options = options;
        _logger = logger;
        _serverUpdateOrInstallService = serverUpdateOrInstallService;
        _eventService = eventService;
        _installedPluginsService = installedPluginsService;
    }

    public async Task<ErrorOr<Guid>> Handle(ServerStartUpdateOrInstallCommand request,
        CancellationToken cancellationToken)
    {
        var updateOrInstallResult = await _serverUpdateOrInstallService.StartUpdateOrInstall();
        if (updateOrInstallResult.IsError)
        {
            return updateOrInstallResult.Errors;
        }

        var isMetamodInstalled = await _installedPluginsService.IsInstalled("Metamod:Source");
        if (isMetamodInstalled.IsError)
        {
            return isMetamodInstalled.Errors;
        }

        if (isMetamodInstalled.Value)
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
        try
        {
            var addMetamodEntryToGameinfoGi =
                await MetaModAdditionalAction.AddMetamodEntryToGameinfoGi(_options.Value.CSGO_FOLDER);
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