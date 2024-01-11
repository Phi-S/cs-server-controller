using Application.StartParameterFolder;
using MediatR;
using Shared.ApiModels;

namespace Application.CQRS.Commands;

public record SetStartParametersCommand(StartParameters StartParameters) : IRequest;

public class SetStartParametersCommandHandler : IRequestHandler<SetStartParametersCommand>
{
    private readonly StartParameterService _startParameterService;

    public SetStartParametersCommandHandler(StartParameterService startParameterService)
    {
        _startParameterService = startParameterService;
    }

    public Task Handle(SetStartParametersCommand request, CancellationToken cancellationToken)
    {
        _startParameterService.Set(request.StartParameters);
        return Task.CompletedTask;
    }
}