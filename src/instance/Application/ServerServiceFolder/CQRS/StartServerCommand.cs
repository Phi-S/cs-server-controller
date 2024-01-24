using Application.StartParameterFolder;
using ErrorOr;
using MediatR;
using Shared.ApiModels;

namespace Application.ServerServiceFolder.CQRS;

public record StartServerCommand(StartParameters? StartParameters) : IRequest<ErrorOr<Success>>;

public class StartServerCommandHandler : IRequestHandler<StartServerCommand, ErrorOr<Success>>
{
    private readonly StartParameterService _startParameterService;
    private readonly ServerService _serverService;

    public StartServerCommandHandler(StartParameterService startParameterService, ServerService serverService)
    {
        _startParameterService = startParameterService;
        _serverService = serverService;
    }

    public async Task<ErrorOr<Success>> Handle(StartServerCommand request, CancellationToken cancellationToken)
    {
        StartParameters startParameters;
        if (request.StartParameters is not null)
        {
            startParameters = request.StartParameters;
        }
        else
        {
            var startParametersResult = _startParameterService.Get();
            if (startParametersResult.IsError)
            {
                return startParametersResult.FirstError;
            }

            startParameters = startParametersResult.Value;
        }

        var start = await _serverService.Start(startParameters);
        return start;
    }
}