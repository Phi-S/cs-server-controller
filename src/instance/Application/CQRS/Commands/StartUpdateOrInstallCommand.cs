using Application.ServerServiceFolder;
using Application.StartParameterFolder;
using Application.UpdateOrInstallServiceFolder;
using ErrorOr;
using MediatR;
using Shared;

namespace Application.CQRS.Commands;

public record StartUpdateOrInstallCommand(bool StartAfterUpdate) : IRequest<ErrorOr<Guid>>;

public class StartUpdateOrInstallCommandHandler : IRequestHandler<StartUpdateOrInstallCommand, ErrorOr<Guid>>
{
    private readonly ServerService _serverService;
    private readonly UpdateOrInstallService _updateOrInstallService;
    private readonly StartParameterService _startParameterService;

    public StartUpdateOrInstallCommandHandler(
        ServerService serverService,
        UpdateOrInstallService updateOrInstallService,
        StartParameterService startParameterService)
    {
        _serverService = serverService;
        _updateOrInstallService = updateOrInstallService;
        _startParameterService = startParameterService;
    }

    public async Task<ErrorOr<Guid>> Handle(StartUpdateOrInstallCommand request, CancellationToken cancellationToken)
    {
        var stop = await _serverService.Stop();
        if (stop.IsError)
        {
            return Errors.Fail($"Failed to start updating or install server. {stop.ErrorMessage()}");
        }

        ErrorOr<Guid> updateOrInstallResult;
        if (request.StartAfterUpdate)
        {
            var startParametersResult = _startParameterService.Get();
            if (startParametersResult.IsError)
            {
                return startParametersResult.FirstError;
            }

            updateOrInstallResult =
                await _updateOrInstallService.StartUpdateOrInstall(() =>
                    _serverService.Start(startParametersResult.Value));
        }
        else
        {
            updateOrInstallResult = await _updateOrInstallService.StartUpdateOrInstall();
        }

        return updateOrInstallResult;
    }
}