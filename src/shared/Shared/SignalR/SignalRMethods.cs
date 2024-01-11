using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Shared.ApiModels;

namespace Shared.SignalR;

public static class SignalRMethods
{
    public const string ServerStatusMethod = "server-status";
    public const string EventMethod = "event";
    public const string ServerLogMethod = "server-log";
    public const string UpdateOrInstallLogMethod = "update-or-install-log";

    public static Task SendServerStatus(this IClientProxy clientProxy, ServerInfoResponse serverInfoResponse,
        CancellationToken cancellationToken = default)
    {
        return clientProxy.SendAsync(ServerStatusMethod, serverInfoResponse, cancellationToken: cancellationToken);
    }

    public static IDisposable OnServerStatus(this HubConnection hubConnection, Func<ServerInfoResponse, Task> handler)
    {
        return hubConnection.On(ServerStatusMethod,
            new[] { typeof(ServerInfoResponse) },
            args => handler((ServerInfoResponse)args[0]!));
    }

    public static Task SendEvent(this IClientProxy clientProxy, EventLogResponse eventLogResponse,
        CancellationToken cancellationToken = default)
    {
        return clientProxy.SendAsync(EventMethod, eventLogResponse, cancellationToken: cancellationToken);
    }

    public static IDisposable OnEvent(this HubConnection hubConnection, Func<EventLogResponse, Task> handler)
    {
        return hubConnection.On(EventMethod,
            new[] { typeof(EventLogResponse) },
            args => handler((EventLogResponse)args[0]!));
    }

    public static Task SendServerLog(this IClientProxy clientProxy, ServerLogResponse serverLogResponse,
        CancellationToken cancellationToken = default)
    {
        return clientProxy.SendAsync(ServerLogMethod, serverLogResponse, cancellationToken: cancellationToken);
    }

    public static IDisposable OnServerLog(this HubConnection hubConnection, Func<ServerLogResponse, Task> handler)
    {
        return hubConnection.On(ServerLogMethod,
            new[] { typeof(ServerLogResponse) },
            args => handler((ServerLogResponse)args[0]!));
    }

    public static Task SendUpdateOrInstallLog(this IClientProxy clientProxy,
        UpdateOrInstallLogResponse updateOrInstallLogResponse,
        CancellationToken cancellationToken = default)
    {
        return clientProxy.SendAsync(UpdateOrInstallLogMethod, updateOrInstallLogResponse,
            cancellationToken: cancellationToken);
    }

    public static IDisposable OnUpdateOrInstallLog(this HubConnection hubConnection,
        Func<UpdateOrInstallLogResponse, Task> handler)
    {
        return hubConnection.On(UpdateOrInstallLogMethod,
            new[] { typeof(UpdateOrInstallLogResponse) },
            args => handler((UpdateOrInstallLogResponse)args[0]!));
    }
}