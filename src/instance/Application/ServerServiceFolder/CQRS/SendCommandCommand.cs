using ErrorOr;
using MediatR;

namespace Application.ServerServiceFolder.CQRS;

public record SendCommandCommand(string Command) : IRequest<ErrorOr<string>>;

public class SendCommandCommandHandler : IRequestHandler<SendCommandCommand, ErrorOr<string>>
{
    private readonly ServerService _serverService;

    public SendCommandCommandHandler(ServerService serverService)
    {
        _serverService = serverService;
    }

    public async Task<ErrorOr<string>> Handle(SendCommandCommand request, CancellationToken cancellationToken)
    {
        var executeCommand = await _serverService.ExecuteCommand(request.Command);
        return executeCommand;
    }
}