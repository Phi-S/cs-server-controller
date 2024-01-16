using Application.ServerPluginsFolder;
using Application.ServerServiceFolder;
using Application.StartParameterFolder;
using Application.StatusServiceFolder;
using Application.UpdateOrInstallServiceFolder;
using ErrorOr;
using MediatR;
using Shared;

namespace Application.CQRS.Commands;

public record StartUpdateOrInstallCommand : IRequest<ErrorOr<Guid>>;

public class StartUpdateOrInstallCommandHandler : IRequestHandler<StartUpdateOrInstallCommand, ErrorOr<Guid>>
{
    private readonly ServerService _serverService;
    private readonly UpdateOrInstallService _updateOrInstallService;
    private readonly StartParameterService _startParameterService;
    private readonly StatusService _statusService;
    private readonly ServerPluginsService _serverPluginsService;

    public StartUpdateOrInstallCommandHandler(
        ServerService serverService,
        UpdateOrInstallService updateOrInstallService,
        StartParameterService startParameterService,
        StatusService statusService,
        ServerPluginsService serverPluginsService)
    {
        _serverService = serverService;
        _updateOrInstallService = updateOrInstallService;
        _startParameterService = startParameterService;
        _statusService = statusService;
        _serverPluginsService = serverPluginsService;
    }

    public async Task<ErrorOr<Guid>> Handle(StartUpdateOrInstallCommand request, CancellationToken cancellationToken)
    {
        var startAfterUpdateOrInstall = _statusService.ServerStarted;

        var stop = await _serverService.Stop();
        if (stop.IsError)
        {
            return Errors.Fail($"Failed to start updating or install server. {stop.ErrorMessage()}");
        }

        ErrorOr<Guid> updateOrInstallResult;
        if (startAfterUpdateOrInstall)
        {
            updateOrInstallResult = await _updateOrInstallService.StartUpdateOrInstall(async () =>
            {
                var updateOrInstallPlugins = await _serverPluginsService.UpdateOrInstall();
                if (updateOrInstallPlugins.IsError)
                {
                    throw new Exception($"Failed to update server plugins. {updateOrInstallPlugins.ErrorMessage()}");
                }

                var startParametersResult = _startParameterService.Get();
                if (startParametersResult.IsError)
                {
                    throw new Exception($"Failed to get start parameters. {startParametersResult.ErrorMessage()}");
                }

                await _serverService.Start(startParametersResult.Value);
            });
        }
        else
        {
            updateOrInstallResult = await _updateOrInstallService.StartUpdateOrInstall(async () =>
            {
                var updateOrInstallPlugins = await _serverPluginsService.UpdateOrInstall();
                if (updateOrInstallPlugins.IsError)
                {
                    throw new Exception($"Failed to update server plugins. {updateOrInstallPlugins.ErrorMessage()}");
                }
            });
        }

        return updateOrInstallResult;
    }
}