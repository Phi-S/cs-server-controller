using Application.ServerServiceFolder;
using Application.UpdateOrInstallServiceFolder;
using Domain;
using ErrorOr;
using MediatR;
using Shared.ApiModels;

namespace Application.CQRS.Commands;

public record StartUpdateOrInstallCommand(StartParameters? StartParameters) : IRequest<ErrorOr<Guid>>;

public class StartUpdateOrInstallCommandHandler : IRequestHandler<StartUpdateOrInstallCommand, ErrorOr<Guid>>
{
    private readonly ServerService _serverService;
    private readonly UpdateOrInstallService _updateOrInstallService;

    public StartUpdateOrInstallCommandHandler(ServerService serverService, UpdateOrInstallService updateOrInstallService)
    {
        _serverService = serverService;
        _updateOrInstallService = updateOrInstallService;
    }
    
    public async Task<ErrorOr<Guid>> Handle(StartUpdateOrInstallCommand request, CancellationToken cancellationToken)
    {
        var stop = await _serverService.Stop();
        if (stop.IsError)
        {
            return Errors.Fail($"Failed to start updating or install server. {stop.ErrorMessage()}");
        }

        var startUpdateOrInstall = request.StartParameters == null
            ? await _updateOrInstallService.StartUpdateOrInstall()
            : await _updateOrInstallService.StartUpdateOrInstall(() => _serverService.Start(request.StartParameters));
        return startUpdateOrInstall;
    }
}