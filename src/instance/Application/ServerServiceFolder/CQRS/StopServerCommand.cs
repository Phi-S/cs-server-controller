using ErrorOr;
using MediatR;

namespace Application.ServerServiceFolder.CQRS;

public record StopServerCommand() : IRequest<ErrorOr<Success>>;

public class StopServerCommandHandler : IRequestHandler<StopServerCommand, ErrorOr<Success>>
{
    private readonly ServerService _serverService;

    public StopServerCommandHandler(ServerService serverService)
    {
        _serverService = serverService;
    }

    public async Task<ErrorOr<Success>> Handle(StopServerCommand request, CancellationToken cancellationToken)
    {
        var start = await _serverService.Stop();
        return start;
    }
}