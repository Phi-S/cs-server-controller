using Application.ServerPluginsFolder;
using Application.ServerServiceFolder;
using Application.StartParameterFolder;
using Application.StatusServiceFolder;
using ErrorOr;
using MediatR;
using Shared;

namespace Application.CQRS.Commands;

public record UpdateOrInstallPluginsCommand() : IRequest<ErrorOr<Success>>;

public class UpdateOrInstallPluginsCommandHandler : IRequestHandler<UpdateOrInstallPluginsCommand, ErrorOr<Success>>
{
    private readonly ServerPluginsService _serverPluginsService;
    private readonly ServerService _serverService;
    private readonly StartParameterService _startParameterService;
    private readonly StatusService _statusService;

    public UpdateOrInstallPluginsCommandHandler(
        ServerPluginsService serverPluginsService,
        ServerService serverService,
        StartParameterService startParameterService,
        StatusService statusService)
    {
        _serverPluginsService = serverPluginsService;
        _serverService = serverService;
        _startParameterService = startParameterService;
        _statusService = statusService;
    }

    public async Task<ErrorOr<Success>> Handle(UpdateOrInstallPluginsCommand request,
        CancellationToken cancellationToken)
    {
        var startAfterUpdate = _statusService.ServerStarted;
        var stop = await _serverService.Stop();
        if (stop.IsError)
        {
            return Errors.Fail($"Failed to start updating or install server. {stop.ErrorMessage()}");
        }

        var updateOrInstallPlugins = await _serverPluginsService.UpdateOrInstall();
        if (updateOrInstallPlugins.IsError)
        {
            return updateOrInstallPlugins.FirstError;
        }

        if (startAfterUpdate)
        {
            var startParameters = _startParameterService.Get();
            if (startParameters.IsError)
            {
                return startParameters.FirstError;
            }

            var start = await _serverService.Start(startParameters.Value);
            if (start.IsError)
            {
                return start.FirstError;
            }
        }

        return Result.Success;
    }
}