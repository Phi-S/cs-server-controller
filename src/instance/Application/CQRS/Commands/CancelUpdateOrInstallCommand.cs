using Application.UpdateOrInstallServiceFolder;
using ErrorOr;
using MediatR;

namespace Application.CQRS.Commands;

public record CancelUpdateOrInstallCommand(Guid UpdateOrInstallId) : IRequest<ErrorOr<Success>>;

public class CancelUpdateOrInstallCommandHandler : IRequestHandler<CancelUpdateOrInstallCommand, ErrorOr<Success>>
{
    private readonly UpdateOrInstallService _updateOrInstallService;

    public CancelUpdateOrInstallCommandHandler(UpdateOrInstallService updateOrInstallService)
    {
        _updateOrInstallService = updateOrInstallService;
    }

    public Task<ErrorOr<Success>> Handle(CancelUpdateOrInstallCommand request, CancellationToken cancellationToken)
    {
        var cancelUpdate = _updateOrInstallService.CancelUpdate(request.UpdateOrInstallId);
        return Task.FromResult(cancelUpdate);
    }
}