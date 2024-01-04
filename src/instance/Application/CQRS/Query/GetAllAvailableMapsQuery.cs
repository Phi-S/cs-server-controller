using Application.ServerServiceFolder;
using Domain;
using ErrorOr;
using MediatR;
using Microsoft.Extensions.Options;

namespace Application.CQRS.Query;

public record GetAllAvailableMapsQuery() : IRequest<ErrorOr<List<string>>>;

public class GetAllAvailableMapsQueryHandler : IRequestHandler<GetAllAvailableMapsQuery, ErrorOr<List<string>>>
{
    private readonly IOptions<AppOptions> _options;
    private readonly ServerService _serverService;

    public GetAllAvailableMapsQueryHandler(IOptions<AppOptions> options, ServerService serverService)
    {
        _options = options;
        _serverService = serverService;
    }
    
    public Task<ErrorOr<List<string>>> Handle(GetAllAvailableMapsQuery request, CancellationToken cancellationToken)
    {
        var maps = _serverService.GetAllMaps(_options.Value.SERVER_FOLDER);
        return Task.FromResult(maps);
    }
}