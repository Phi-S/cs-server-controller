using ErrorOr;
using MediatR;

namespace Application.PluginsFolder.CQRS;

public record UpdateOrInstallPluginCommand(string Name, string Version) : IRequest<ErrorOr<Success>>;

public class UpdateOrInstallPluginCommandHandler : IRequestHandler<UpdateOrInstallPluginCommand, ErrorOr<Success>>
{
    private readonly PluginInstallerService _pluginInstallerService;

    public UpdateOrInstallPluginCommandHandler(PluginInstallerService pluginInstallerService)
    {
        _pluginInstallerService = pluginInstallerService;
    }

    public async Task<ErrorOr<Success>> Handle(UpdateOrInstallPluginCommand request,
        CancellationToken cancellationToken)
    {
        var updateOrInstall = await _pluginInstallerService.UpdateOrInstall(request.Name, request.Version);
        return updateOrInstall;
    }
}