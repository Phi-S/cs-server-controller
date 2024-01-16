using Infrastructure.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Application.SystemLogFolder;

public class SystemLogService
{
    private readonly ILogger<SystemLogService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public SystemLogService(ILogger<SystemLogService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        OnSystemLogEvent += OnOnSystemLogEventToDatabase;
    }

    public event EventHandler<SystemLogEventArgs>? OnSystemLogEvent;

    private async void OnOnSystemLogEventToDatabase(object? sender, SystemLogEventArgs e)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.GetUnitOfWork();
            await unitOfWork.SystemLogRepo.AddLog(e.CreatedUtc, e.Message);
            await unitOfWork.Save();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Exception in OnOnSystemLogEventToDatabase");
        }
    }

    public void Log(string message)
    {
        OnSystemLogEvent?.Invoke(this, new SystemLogEventArgs(DateTime.UtcNow, message));
    }

    public void LogHeader()
    {
        OnSystemLogEvent?.Invoke(this,
            new SystemLogEventArgs(DateTime.UtcNow, "======================================================"));
    }
}