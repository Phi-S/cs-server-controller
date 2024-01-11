using Application.StatusServiceFolder;
using Domain;
using MediatR;
using Microsoft.Extensions.Options;
using Shared.ApiModels;

namespace Application.CQRS.Query;

public record GetServerStatusQuery() : IRequest<ServerInfoResponse>;

public class GetServerStatusQueryHandler : IRequestHandler<GetServerStatusQuery, ServerInfoResponse>
{
    private readonly IOptions<AppOptions> _options;
    private readonly StatusService _statusService;

    public GetServerStatusQueryHandler(IOptions<AppOptions> options, StatusService statusService)
    {
        _options = options;
        _statusService = statusService;
    }

    public Task<ServerInfoResponse> Handle(GetServerStatusQuery request, CancellationToken cancellationToken)
    {
        var info = new ServerInfoResponse(
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