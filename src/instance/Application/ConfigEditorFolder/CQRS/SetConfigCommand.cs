using ErrorOr;
using MediatR;

namespace Application.ConfigEditorFolder.CQRS;

public record SetConfigCommand(string ConfigFile, string Content) : IRequest<ErrorOr<Success>>;

public class SetConfigCommandHandler : IRequestHandler<SetConfigCommand, ErrorOr<Success>>
{
    private readonly ConfigEditorService _configEditorService;

    public SetConfigCommandHandler(ConfigEditorService configEditorService)
    {
        _configEditorService = configEditorService;
    }

    public async Task<ErrorOr<Success>> Handle(SetConfigCommand request, CancellationToken cancellationToken)
    {
        return await _configEditorService.SetConfigFile(request.ConfigFile, request.Content);
    }
}