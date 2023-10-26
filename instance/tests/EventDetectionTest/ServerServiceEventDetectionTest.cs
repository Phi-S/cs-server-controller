using AppOptionsLib;
using DatabaseLib;
using DatabaseLib.Models;
using DatabaseLib.Repos;
using EventsServiceLib;
using EventsServiceLib.EventArgs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ServerServiceLib;
using StatusServiceLib;
using TestLoggerLib;
using UtilLib;
using Xunit.Abstractions;

namespace EventDetectionTest;

public class ServerServiceEventDetectionTest
{
    private event EventHandler<ServerOutputEventArg>? TestEvent;

    private readonly EventService _eventService;
    private readonly ITestOutputHelper _output;

    public ServerServiceEventDetectionTest(ITestOutputHelper output)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddDatabaseServices();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        _output = output;
        var eventLogRepo = new EventLogRepo(serviceProvider);
        _eventService = new EventService(new XunitLogger<EventService>(output), eventLogRepo);
        var options = Options.Create(new AppOptions
        {
            APP_NAME = "test",
            STEAM_USERNAME = "test",
            STEAM_PASSWORD = "test",
            PORT = "27015",
            IP_OR_DOMAIN = "localhost"
        });
        var statusService = new StatusService(options, _eventService);

        var serverRepo = new ServerRepo(serviceProvider);
        var serverService =
            new ServerService(new XunitLogger<ServerService>(output), options, statusService, _eventService,
                serverRepo);
        TestEvent += serverService.NewOutputHibernationDetection;
        TestEvent += serverService.NewOutputMapChangeDetection;
        TestEvent += serverService.NewOutputPlayerConnectDetection;
        TestEvent += serverService.NewOutputPlayerDisconnectDetection;
        TestEvent += serverService.NewOutputAllChatDetection;
    }

    [Fact]
    public async Task TestNewOutputHibernationStartedDetection()
    {
        CustomEventArg? arg = null;
        _eventService.HibernationStarted += (_, disconnected) => arg = disconnected;
        TestEvent?.Invoke(null,
            new ServerOutputEventArg(
                new ServerStart
                {
                    Id = Guid.NewGuid(),
                    StartParameters = "-startParameter",
                    StartedAtUtc = DateTime.UtcNow,
                    CreatedAtUtc = DateTime.UtcNow
                },
                "Server is hibernating"));
        var waitResult =
            await WaitUtil.WaitUntil(TimeSpan.FromSeconds(1), () => arg is not null, _output.WriteLine);

        Assert.True(waitResult.IsOk, waitResult.IsFailed ? waitResult.Exception.ToString() : "");
        Assert.True(arg is not null);
        Assert.True(arg.EventName == Events.HIBERNATION_STARTED);
    }

    [Fact]
    public async Task TestNewOutputHibernationEndedDetection()
    {
        CustomEventArg? arg = null;
        _eventService.HibernationEnded += (_, disconnected) => arg = disconnected;
        TestEvent?.Invoke(null,
            new ServerOutputEventArg(
                new ServerStart
                {
                    Id = Guid.NewGuid(),
                    StartParameters = "-startParameter",
                    StartedAtUtc = DateTime.UtcNow,
                    CreatedAtUtc = DateTime.UtcNow
                },
                "Server waking up from hibernation"));
        var waitResult =
            await WaitUtil.WaitUntil(TimeSpan.FromSeconds(1), () => arg is not null, _output.WriteLine);

        Assert.True(waitResult.IsOk, waitResult.IsFailed ? waitResult.Exception.ToString() : "");
        Assert.True(arg is not null);
        Assert.True(arg.EventName == Events.HIBERNATION_ENDED);
    }

    [Fact]
    public async Task TestNewOutputMapChangeDetection()
    {
        CustomEventArgMapChanged? arg = null;
        _eventService.MapChanged += (_, disconnected) => arg = disconnected;
        TestEvent?.Invoke(null,
            new ServerOutputEventArg(
                new ServerStart
                {
                    Id = Guid.NewGuid(),
                    StartParameters = "-startParameter",
                    StartedAtUtc = DateTime.UtcNow,
                    CreatedAtUtc = DateTime.UtcNow
                },
                "Host activate: Changelevel (de_dust2)"));
        var waitResult =
            await WaitUtil.WaitUntil(TimeSpan.FromSeconds(1), () => arg is not null, _output.WriteLine);

        Assert.True(waitResult.IsOk, waitResult.IsFailed ? waitResult.Exception.ToString() : "");
        Assert.True(arg is not null);
        Assert.True(arg.EventName == Events.MAP_CHANGED);
        Assert.Equal("de_dust2", arg.MapName);
    }

    [Fact]
    public async Task TestNewOutputPlayerConnectDetection()
    {
        CustomEventArgPlayerConnected? arg = null;
        _eventService.PlayerConnected += (_, disconnected) => arg = disconnected;
        TestEvent?.Invoke(null,
            new ServerOutputEventArg(
                new ServerStart
                {
                    Id = Guid.NewGuid(),
                    StartParameters = "-startParameter",
                    StartedAtUtc = DateTime.UtcNow,
                    CreatedAtUtc = DateTime.UtcNow
                },
                "CNetworkGameServerBase::ConnectClient( name='PhiS', remote='10.10.20.10:57143' )"));
        var waitResult =
            await WaitUtil.WaitUntil(TimeSpan.FromSeconds(1), () => arg is not null, _output.WriteLine);

        Assert.True(waitResult.IsOk, waitResult.IsFailed ? waitResult.Exception.ToString() : "");
        Assert.True(arg is not null);
        Assert.True(arg.EventName == Events.PLAYER_CONNECTED);
        Assert.Equal("PhiS", arg.PlayerName);
        Assert.Equal("10.10.20.10:57143", arg.PlayerIp);
    }

    [Fact]
    public async Task TestNewOutputPlayerDisconnectDetection()
    {
        CustomEventArgPlayerDisconnected? arg = null;
        _eventService.PlayerDisconnected += (_, disconnected) => arg = disconnected;
        TestEvent?.Invoke(null,
            new ServerOutputEventArg(
                new ServerStart
                {
                    Id = Guid.NewGuid(),
                    StartParameters = "-startParameter",
                    StartedAtUtc = DateTime.UtcNow,
                    CreatedAtUtc = DateTime.UtcNow
                },
                "Disconnect client 'PhiS' from server(59): NETWORK_DISCONNECT_EXITING"));
        var waitResult =
            await WaitUtil.WaitUntil(TimeSpan.FromSeconds(1), () => arg is not null, _output.WriteLine);

        Assert.True(waitResult.IsOk, waitResult.IsFailed ? waitResult.Exception.ToString() : "");
        Assert.True(arg is not null);
        Assert.True(arg.EventName == Events.PLAYER_DISCONNECTED);
        Assert.Equal("PhiS", arg.PlayerName);
        Assert.Equal("NETWORK_DISCONNECT_EXITING", arg.DisconnectReason);
    }

    [Theory]
    [InlineData("[All Chat][PhiS (194151532)]: ezz", "All Chat", "PhiS", "194151532", "ezz")]
    [InlineData("[All Chat][PhiS (194151532)]: noob", "All Chat", "PhiS", "194151532", "noob")]
    [InlineData("[All Chat][PhiS (194151532)]: [All Chat][alt (43434343434)]", "All Chat", "PhiS", "194151532",
        "[All Chat][alt (43434343434)]")]
    public async Task TestNewOutputAllChatDetection(string rawMessage, string shouldBeChat, string shouldBePlayerName,
        string shouldBeSteamId3, string shouldBeMessage)
    {
        CustomEventArgChatMessage? arg = null;
        _eventService.ChatMessage += (_, message) => { arg = message; };
        TestEvent?.Invoke(null,
            new ServerOutputEventArg(
                new ServerStart
                {
                    Id = Guid.NewGuid(),
                    StartParameters = "-startParameter",
                    StartedAtUtc = DateTime.UtcNow,
                    CreatedAtUtc = DateTime.UtcNow
                },
                rawMessage));
        var waitResult =
            await WaitUtil.WaitUntil(TimeSpan.FromSeconds(1), () => arg is not null, _output.WriteLine);

        Assert.True(waitResult.IsOk, waitResult.IsFailed ? waitResult.Exception.ToString() : "");
        Assert.True(arg is not null);
        Assert.True(arg.EventName == Events.CHAT_MESSAGE);
        Assert.Equal(arg.Chat, shouldBeChat);
        Assert.Equal(arg.PlayerName, shouldBePlayerName);
        Assert.Equal(arg.SteamId3, shouldBeSteamId3);
        Assert.Equal(arg.Message, shouldBeMessage);
    }
}