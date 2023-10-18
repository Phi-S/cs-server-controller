using System.Diagnostics;
using AppOptionsLib;
using DatabaseLib;
using DatabaseLib.Repos;
using EventsServiceLib;
using EventsServiceLib.EventArgs;
using Microsoft.Extensions.Options;
using Moq;
using ServerServiceLib;
using StatusServiceLib;
using TestLoggerLib;
using UtilLib;
using Xunit.Abstractions;

namespace EventDetectionTest;

public class ServerServiceEventDetectionTest
{
    private event EventHandler<string>? TestEvent;

    private readonly EventService _eventService;
    private readonly StatusService _statusService;
    private readonly ITestOutputHelper _output;

    public ServerServiceEventDetectionTest(ITestOutputHelper output)
    {
        _output = output;
        var dbContext = new Mock<ApiDbContext>();
        var eventLogRepo = new EventLogRepo(dbContext.Object);
        _eventService = new EventService(new XunitLogger<EventService>(output), eventLogRepo);
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
        TestEvent += serverService.NewOutputAllChatDetection;
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
        Assert.Equal("de_dust2", _statusService.CurrentMap);
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

    [Theory]
    [InlineData("[All Chat][PhiS (194151532)]: ezz", "All Chat", "PhiS", "194151532", "ezz")]
    [InlineData("[All Chat][PhiS (194151532)]: noob", "All Chat", "PhiS", "194151532", "noob")]
    [InlineData("[All Chat][PhiS (194151532)]: [All Chat][bhbggg (43434343434)]", "All Chat", "PhiS", "194151532",
        "[All Chat][bhbggg (43434343434)]")]
    public async Task TestNewOutputAllChatDetection(string rawMessage, string shouldBeChat, string shouldBePlayerName,
        string shouldBeSteamId3, string shouldBeMessage)
    {
        CustomEventArgChatMessage? messageEventAr = null;
        _eventService.ChatMessage += (_, message) => { messageEventAr = message; };
        TestEvent?.Invoke(null, rawMessage);

        var waitResult = await WaitUtil.WaitUntil(TimeSpan.FromSeconds(1), () => messageEventAr is not null, _output.WriteLine);
        Assert.True(waitResult.IsOk, waitResult.IsFailed ? waitResult.Exception.ToString() : "");
        Assert.True(messageEventAr is not null);
        Assert.True(messageEventAr.Chat.Equals(shouldBeChat));
        Assert.True(messageEventAr.PlayerName.Equals(shouldBePlayerName));
        Assert.True(messageEventAr.SteamId3.Equals(shouldBeSteamId3));
        Assert.True(messageEventAr.Message.Equals(shouldBeMessage));
    }
}