using Application.EventServiceFolder;
using Application.ServerServiceFolder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Application.DefaultConfigFolder;

public class DefaultConfigService : BackgroundService
{
    private readonly ILogger<DefaultConfigService> _logger;
    private readonly EventService _eventService;
    private readonly ServerService _serverService;

    public DefaultConfigService(
        ILogger<DefaultConfigService> logger,
        EventService eventService,
        ServerService serverService)
    {
        _logger = logger;
        _eventService = eventService;
        _serverService = serverService;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _eventService.HibernationEnded += async (_, _) =>
            {
                try
                {
                    const string startConfig = "PRAC.c.cfg";
                    var executeCommand = await _serverService.ExecuteCommand($"exec {startConfig}");
                    if (executeCommand.IsError)
                    {
                        _logger.LogError("Failed to execute start config \"{StartConfig}\"", startConfig);
                    }

                    _logger.LogInformation("Start config \"{StartConfig}\"", startConfig);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Exception while trying to launch default config");
                }
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception while trying to launch default config");
        }

        return Task.CompletedTask;
    }
}