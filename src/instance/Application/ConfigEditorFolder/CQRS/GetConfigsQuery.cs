using ErrorOr;
using MediatR;

namespace Application.ConfigEditorFolder.CQRS;

public record GetConfigsQuery : IRequest<ErrorOr<List<string>>>;

public class GetConfigsQueryHandler : IRequestHandler<GetConfigsQuery, ErrorOr<List<string>>>
{
    private readonly ConfigEditorService _configEditorService;

    public GetConfigsQueryHandler(ConfigEditorService configEditorService)
    {
        _configEditorService = configEditorService;
    }

    public Task<ErrorOr<List<string>>> Handle(GetConfigsQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_configEditorService.GetExistingConfigs());
    }
}