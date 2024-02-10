using ErrorOr;
using MediatR;

namespace Application.CounterStrikeSharpUpdateOrInstallFolder.CQRS;

public record CounterStrikeSharpUpdateOrInstallCommand() : IRequest<ErrorOr<Success>>;

public class CounterStrikeSharpUpdateOrInstallCommandHandler : IRequestHandler<CounterStrikeSharpUpdateOrInstallCommand, ErrorOr<Success>>
{
    private readonly CounterStrikeSharpUpdateOrInstallService _counterStrikeSharpUpdateOrInstallService;

    public CounterStrikeSharpUpdateOrInstallCommandHandler(CounterStrikeSharpUpdateOrInstallService counterStrikeSharpUpdateOrInstallService)
    {
        _counterStrikeSharpUpdateOrInstallService = counterStrikeSharpUpdateOrInstallService;
    }

    public async Task<ErrorOr<Success>> Handle(CounterStrikeSharpUpdateOrInstallCommand request,
        CancellationToken cancellationToken)
    {
        var updateOrInstallPlugins = await _counterStrikeSharpUpdateOrInstallService.StartUpdateOrInstall();
        if (updateOrInstallPlugins.IsError)
        {
            return updateOrInstallPlugins.FirstError;
        }

        return Result.Success;
    }
}