using Application.ServerServiceFolder;
using ErrorOr;
using MediatR;
using Shared.ApiModels;

namespace Application.CQRS.Commands;

public record StartServerCommand(StartParameters StartParameters) : IRequest<ErrorOr<Success>>;

public class StartServerCommandHandler : IRequestHandler<StartServerCommand, ErrorOr<Success>>
{
    private readonly ServerService _serverService;

    public StartServerCommandHandler(ServerService serverService)
    {
        _serverService = serverService;
    }
    
    public async Task<ErrorOr<Success>> Handle(StartServerCommand request, CancellationToken cancellationToken)
    {
        var start = await _serverService.Start(request.StartParameters);
        return start;
    }
}