using ErrorOr;
using MediatR;

namespace Application.ConfigEditorFolder.CQRS;

public record GetConfigQuery(string ConfigName) : IRequest<ErrorOr<string>>;

public class GetConfigQueryHandler : IRequestHandler<GetConfigQuery, ErrorOr<string>>
{
    private readonly ConfigEditorService _configEditorService;

    public GetConfigQueryHandler(ConfigEditorService configEditorService)
    {
        _configEditorService = configEditorService;
    }
    
    public async Task<ErrorOr<string>> Handle(GetConfigQuery request, CancellationToken cancellationToken)
    {
        return await _configEditorService.GetConfigFile(request.ConfigName);
    }
}