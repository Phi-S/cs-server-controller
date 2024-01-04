using Application.ServerServiceFolder;
using Domain;
using ErrorOr;
using MediatR;
using Microsoft.Extensions.Options;

namespace Application.CQRS.Query;

public record GetAllAvailableServerConfigsQuery() : IRequest<ErrorOr<List<string>>>;

public class
    GetAllAvailableServerConfigsQueryHandler : IRequestHandler<GetAllAvailableServerConfigsQuery,
    ErrorOr<List<string>>>
{
    private readonly IOptions<AppOptions> _options;
    private readonly ServerService _serverService;

    public GetAllAvailableServerConfigsQueryHandler(IOptions<AppOptions> options, ServerService serverService)
    {
        _options = options;
        _serverService = serverService;
    }

    public Task<ErrorOr<List<string>>> Handle(GetAllAvailableServerConfigsQuery request,
        CancellationToken cancellationToken)
    {
        var configs = _serverService.GetAvailableConfigs(_options.Value.SERVER_FOLDER);
        if (configs.IsError)
        {
            return Task.FromResult<ErrorOr<List<string>>>(configs.FirstError);
        }

        return Task.FromResult<ErrorOr<List<string>>>(configs.Value.Keys.ToList());
    }
}