using ErrorOr;
using MediatR;
using Shared.ApiModels;

namespace Application.InstalledVersionsFolder.CQRS;

public record GetInstalledVersionsQuery : IRequest<ErrorOr<List<InstalledVersionsModel>>>;

public class GetInstalledVersionsQueryHandler : IRequestHandler<GetInstalledVersionsQuery, ErrorOr<List<InstalledVersionsModel>>>
{
    private readonly InstalledVersionsService _installedVersionsService;

    public GetInstalledVersionsQueryHandler(InstalledVersionsService installedVersionsService)
    {
        _installedVersionsService = installedVersionsService;
    }

    public async Task<ErrorOr<List<InstalledVersionsModel>>> Handle(GetInstalledVersionsQuery request,
        CancellationToken cancellationToken)
    {
        var installedVersions = await _installedVersionsService.GetAll();
        if (installedVersions.IsError)
        {
            return installedVersions.FirstError;
        }

        return installedVersions.Value;
    }
}