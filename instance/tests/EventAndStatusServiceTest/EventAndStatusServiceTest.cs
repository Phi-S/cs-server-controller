using EventsServiceLib;
using StatusServiceLib;
using Xunit.Abstractions;

namespace EventAndStatusServiceTest;

public class EventAndStatusServiceTest
{
    private readonly EventService _eventService;
    private readonly StatusService _statusService;

    public EventAndStatusServiceTest(ITestOutputHelper output)
    {
        _eventService = new EventService(new XunitLogger<EventService>(output));
        _statusService = new StatusService(_eventService);
    }

    [Fact]
    public void TestStartingServer()
    {
        _statusService.Reset();
        Assert.True(_statusService.ServerStarting == false);
        _eventService.OnStartingServer();
        Assert.True(_statusService.ServerStarting);
    }

    [Fact]
    public void TestStartingServerDone()
    {
        _statusService.Reset();
        Assert.True(_statusService.ServerStarted == false);
        _eventService.OnStartingServerDone();
        Assert.True(_statusService.ServerStarted);
    }

    [Fact]
    public void TestStartingServerFailed()
    {
        _statusService.Reset();
        _eventService.OnStartingServer();
        Assert.True(_statusService.ServerStarting);
        _eventService.OnStartingServerFailed();
        Assert.True(_statusService.ServerStarting == false);
    }

    [Fact]
    public void TestStoppingServer()
    {
        _statusService.Reset();
        Assert.True(_statusService.ServerStopping == false);
        _eventService.OnStoppingServer();
        Assert.True(_statusService.ServerStopping);
    }

    [Fact]
    public void TestServerExited()
    {
        _statusService.Reset();
        _eventService.OnStartingServerDone();
        Assert.True(_statusService.ServerStarted);
        _eventService.OnServerExited();
        Assert.True(_statusService.ServerStarted == false);
    }

    [Fact]
    public void TestUpdateOrInstallStarted()
    {
        _statusService.Reset();
        Assert.True(_statusService.ServerUpdatingOrInstalling == false);
        _eventService.OnUpdateOrInstallStarted();
        Assert.True(_statusService.ServerUpdatingOrInstalling);
    }


    [Fact]
    public void TestUpdateOrInstallDone()
    {
        _statusService.Reset();
        _eventService.OnUpdateOrInstallStarted();
        Assert.True(_statusService.ServerUpdatingOrInstalling);
        _eventService.OnUpdateOrInstallDone();
        Assert.True(_statusService.ServerUpdatingOrInstalling == false);
    }

    [Fact]
    public void TestUpdateOrInstallCancelled()
    {
        _statusService.Reset();
        _eventService.OnUpdateOrInstallStarted();
        Assert.True(_statusService.ServerUpdatingOrInstalling);
        _eventService.OnUpdateOrInstallCancelled();
        Assert.True(_statusService.ServerUpdatingOrInstalling == false);
    }

    [Fact]
    public void TestUpdateOrInstallFailed()
    {
        _statusService.Reset();
        _eventService.OnUpdateOrInstallStarted();
        Assert.True(_statusService.ServerUpdatingOrInstalling);
        _eventService.OnUpdateOrInstallFailed();
        Assert.True(_statusService.ServerUpdatingOrInstalling == false);
    }

    [Fact]
    public void TestUploadDemoStarted()
    {
        _statusService.Reset();
        Assert.True(_statusService.DemoUploading == false);
        _eventService.OnUploadDemoStarted("test");
        Assert.True(_statusService.DemoUploading);
    }

    [Fact]
    public void TestUploadDemoDone()
    {
        _statusService.Reset();
        _eventService.OnUploadDemoStarted("test");
        Assert.True(_statusService.DemoUploading);
        _eventService.OnUploadDemoDone("test");
        Assert.True(_statusService.DemoUploading == false);
    }

    [Fact]
    public void TestUploadDemoFailed()
    {
        _statusService.Reset();
        _eventService.OnUploadDemoStarted("test");
        Assert.True(_statusService.DemoUploading);
        _eventService.OnUploadDemoFailed("test");
        Assert.True(_statusService.DemoUploading == false);
    }

    [Fact]
    public void TestHibernationStarted()
    {
        _statusService.Reset();
        Assert.True(_statusService.ServerHibernating == false);
        _eventService.OnHibernationStarted();
        Assert.True(_statusService.ServerHibernating);
    }

    [Fact]
    public void TestHibernationEnded()
    {
        _statusService.Reset();
        _eventService.OnHibernationStarted();
        Assert.True(_statusService.ServerHibernating);
        _eventService.OnHibernationEnded();
        Assert.True(_statusService.ServerHibernating == false);
    }

    [Fact]
    public void TestMapChanged()
    {
        _statusService.Reset();
        Assert.True(string.IsNullOrWhiteSpace(_statusService.CurrentMap));
        var map = "asdf";
        _eventService.OnMapChanged(map);
        Assert.True(_statusService.CurrentMap.Equals(map));
    }

    [Fact]
    public void TestPlayerConnected()
    {
        _statusService.Reset();
        Assert.True(_statusService.CurrentPlayerCount == 0);
        _eventService.OnPlayerConnected("test", "test");
        Assert.True(_statusService.CurrentPlayerCount == 1);
    }

    [Fact]
    public void TestPlayerDisconnected()
    {
        _statusService.Reset();
        _eventService.OnPlayerConnected("test", "test");
        Assert.True(_statusService.CurrentPlayerCount == 1);
        _eventService.OnPlayerDisconnected("test", "test");
        Assert.True(_statusService.CurrentPlayerCount == 0);
    }
}