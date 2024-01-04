using Microsoft.AspNetCore.SignalR;

namespace Instance.SignalR;

public class SignalRHub : Hub
{
    private readonly SignalRUserService _signalRUserService;

    public SignalRHub(SignalRUserService signalRUserService)
    {
        _signalRUserService = signalRUserService;
    }

    public override async Task OnConnectedAsync()
    {
        _signalRUserService.RegisterConnection(Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _signalRUserService.RemoveConnection(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}