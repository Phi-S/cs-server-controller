using ErrorOr;
using MediatR;
using Shared.ApiModels;

namespace Application.PluginsFolder.CQRS;

public record GetPluginsQuery : IRequest<ErrorOr<List<PluginsResponseModel>>>;

public class GetPluginsQueryHandler : IRequestHandler<GetPluginsQuery, ErrorOr<List<PluginsResponseModel>>>
{
    private readonly PluginInstallerService _pluginInstallerService;

    public GetPluginsQueryHandler(PluginInstallerService pluginInstallerService)
    {
        _pluginInstallerService = pluginInstallerService;
    }

    public async Task<ErrorOr<List<PluginsResponseModel>>> Handle(GetPluginsQuery request,
        CancellationToken cancellationToken)
    {
        var plugins = await _pluginInstallerService.GetPlugins();
        return plugins;
    }
}