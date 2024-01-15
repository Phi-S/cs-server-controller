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
        var info = _statusService.GetStatus();
        return Task.FromResult(info);
    }
}