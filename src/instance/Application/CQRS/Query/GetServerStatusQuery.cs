using Application.StatusServiceFolder;
using Domain;
using MediatR;
using Microsoft.Extensions.Options;
using Shared.ApiModels;

namespace Application.CQRS.Query;

public record GetServerStatusQuery() : IRequest<ServerStatusResponse>;

public class GetServerStatusQueryHandler : IRequestHandler<GetServerStatusQuery, ServerStatusResponse>
{
    private readonly IOptions<AppOptions> _options;
    private readonly StatusService _statusService;

    public GetServerStatusQueryHandler(IOptions<AppOptions> options, StatusService statusService)
    {
        _options = options;
        _statusService = statusService;
    }

    public Task<ServerStatusResponse> Handle(GetServerStatusQuery request, CancellationToken cancellationToken)
    {
        var info = new ServerStatusResponse(
            _statusService.ServerInstalled,
            _statusService.ServerStartParameters?.ServerHostname,
            _statusService.ServerStartParameters?.ServerPassword,
            _statusService.CurrentMap,
            _statusService.CurrentPlayerCount,
            _statusService.ServerStartParameters?.MaxPlayer,
            _options.Value.IP_OR_DOMAIN,
            _options.Value.PORT,
            _statusService.ServerStarting,
            _statusService.ServerStarted,
            _statusService.ServerStopping,
            _statusService.ServerHibernating,
            _statusService.ServerUpdatingOrInstalling,
            _statusService.DemoUploading,
            DateTime.UtcNow
        );
        return Task.FromResult(info);
    }
}