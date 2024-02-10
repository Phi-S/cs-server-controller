using ErrorOr;
using MediatR;

namespace Application.ServerUpdateOrInstallServiceFolder.CQRS;

public record ServerCancelUpdateOrInstallCommand(Guid UpdateOrInstallId) : IRequest<ErrorOr<Success>>;

public class ServerCancelUpdateOrInstallCommandHandler : IRequestHandler<ServerCancelUpdateOrInstallCommand, ErrorOr<Success>>
{
    private readonly ServerUpdateOrInstallService _serverUpdateOrInstallService;

    public ServerCancelUpdateOrInstallCommandHandler(ServerUpdateOrInstallService serverUpdateOrInstallService)
    {
        _serverUpdateOrInstallService = serverUpdateOrInstallService;
    }

    public Task<ErrorOr<Success>> Handle(ServerCancelUpdateOrInstallCommand request, CancellationToken cancellationToken)
    {
        var cancelUpdate = _serverUpdateOrInstallService.CancelUpdate(request.UpdateOrInstallId);
        return Task.FromResult(cancelUpdate);
    }
}