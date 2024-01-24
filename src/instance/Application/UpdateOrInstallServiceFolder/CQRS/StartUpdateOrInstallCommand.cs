using Application.ServerHelperFolder;
using Application.ServerPluginsFolder;
using Application.ServerServiceFolder;
using Application.StartParameterFolder;
using Application.StatusServiceFolder;
using Domain;
using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared;

namespace Application.UpdateOrInstallServiceFolder.CQRS;

public record StartUpdateOrInstallCommand : IRequest<ErrorOr<Guid>>;

public class StartUpdateOrInstallCommandHandler : IRequestHandler<StartUpdateOrInstallCommand, ErrorOr<Guid>>
{
    private readonly ILogger<StartUpdateOrInstallCommandHandler> _logger;
    private readonly IOptions<AppOptions> _options;
    private readonly ServerService _serverService;
    private readonly UpdateOrInstallService _updateOrInstallService;
    private readonly StartParameterService _startParameterService;
    private readonly StatusService _statusService;
    private readonly ServerPluginsService _serverPluginsService;

    public StartUpdateOrInstallCommandHandler(
        ILogger<StartUpdateOrInstallCommandHandler> logger,
        IOptions<AppOptions> options,
        ServerService serverService,
        UpdateOrInstallService updateOrInstallService,
        StartParameterService startParameterService,
        StatusService statusService,
        ServerPluginsService serverPluginsService)
    {
        _logger = logger;
        _options = options;
        _serverService = serverService;
        _updateOrInstallService = updateOrInstallService;
        _startParameterService = startParameterService;
        _statusService = statusService;
        _serverPluginsService = serverPluginsService;
    }

    public async Task<ErrorOr<Guid>> Handle(StartUpdateOrInstallCommand request, CancellationToken cancellationToken)
    {
        var startAfterUpdateOrInstall = _statusService.ServerStarted;
        var serverPluginsBaseInstalled = ServerHelper.IsServerPluginBaseInstalled(_options.Value.SERVER_FOLDER);

        var stop = await _serverService.Stop();
        if (stop.IsError)
        {
            return Errors.Fail($"Failed to start updating or install server. {stop.ErrorMessage()}");
        }

        async Task AfterUpdateAction()
        {
            try
            {
                if (serverPluginsBaseInstalled)
                {
                    var updateOrInstallPlugins = await _serverPluginsService.UpdateOrInstall();
                    if (updateOrInstallPlugins.IsError)
                    {
                        throw new Exception(
                            $"Failed to update server plugins. {updateOrInstallPlugins.ErrorMessage()}");
                    }
                }

                if (startAfterUpdateOrInstall)
                {
                    var startParametersResult = _startParameterService.Get();
                    if (startParametersResult.IsError)
                    {
                        throw new Exception($"Failed to get start parameters. {startParametersResult.ErrorMessage()}");
                    }

                    await _serverService.Start(startParametersResult.Value);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception in AfterUpdateAction");
            }
        }

        var updateOrInstallResult = await _updateOrInstallService.StartUpdateOrInstall(AfterUpdateAction);
        return updateOrInstallResult;
        
    }
}