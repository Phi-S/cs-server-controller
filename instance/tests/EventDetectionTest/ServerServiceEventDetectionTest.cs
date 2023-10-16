using AppOptionsLib;
using EventsServiceLib;
using Microsoft.Extensions.Options;
using ServerServiceLib;
using StatusServiceLib;
using Xunit.Abstractions;

namespace EventDetectionTest;

public class ServerServiceEventDetectionTest
{
    private event EventHandler<string>? TestEvent;

    private readonly EventService _eventService;
    private readonly StatusService _statusService;

    public ServerServiceEventDetectionTest(ITestOutputHelper output)
    {
        _eventService = new EventService(new XunitLogger<EventService>(output));
        _statusService = new StatusService(_eventService);
        var options = Options.Create(new AppOptions
        {
            APP_NAME = "test",
            STEAM_USERNAME = "test",
            STEAM_PASSWORD = "test"
        });
        var serverService =
            new ServerService(new XunitLogger<ServerService>(output), options, _statusService, _eventService);
        TestEvent += serverService.NewOutputHibernationDetection;
        TestEvent += serverService.NewOutputMapChangeDetection;
        TestEvent += serverService.NewOutputPlayerConnectDetection;
        TestEvent += serverService.NewOutputPlayerDisconnectDetection;
    }


    [Fact]
    public void TestNewOutputHibernationDetection()
    {
        Assert.True(_statusService.ServerHibernating == false);
        TestEvent?.Invoke(null, "Server is hibernating");
        Assert.True(_statusService.ServerHibernating);
        TestEvent?.Invoke(null, "Server waking up from hibernation");
        Assert.True(_statusService.ServerHibernating == false);
    }

    [Fact]
    public void TestNewOutputMapChangeDetection()
    {
        Assert.True(_statusService.CurrentMap.Equals("de_dust2") == false);
        TestEvent?.Invoke(null, "Host activate: Changelevel (de_dust2)");
        Assert.True(_statusService.CurrentMap.Equals("de_dust2"));
    }

    [Fact]
    public void TestNewOutputPlayerConnectDetection()
    {
        var currentPlayerCount = _statusService.CurrentPlayerCount;
        TestEvent?.Invoke(null, "CNetworkGameServerBase::ConnectClient( name='PhiS', remote='10.10.20.10:57143' )");
        Assert.True(currentPlayerCount + 1 == _statusService.CurrentPlayerCount);
    }

    [Fact]
    public void TestNewOutputPlayerDisconnectDetection()
    {
        _eventService.OnPlayerConnected("asf", "asdf");
        var currentPlayerCount = _statusService.CurrentPlayerCount;
        Assert.True(currentPlayerCount == 1);
        TestEvent?.Invoke(null, "Disconnect client 'PhiS' from server(59): NETWORK_DISCONNECT_EXITING");
        Assert.True(currentPlayerCount - 1 == _statusService.CurrentPlayerCount);
    }
}