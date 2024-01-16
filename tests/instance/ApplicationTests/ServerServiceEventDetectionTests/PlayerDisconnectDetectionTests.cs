using Application.EventServiceFolder;
using Application.EventServiceFolder.EventArgs;
using Application.ServerServiceFolder;
using Infrastructure.Database.Models;
using Microsoft.Extensions.DependencyInjection;
using Shared;
using TestHelper.TestSetup;
using TestHelper.WaitUtilFolder;
using Xunit.Abstractions;

namespace ApplicationTests.ServerServiceEventDetectionTests;

public class PlayerDisconnectDetectionTests
{
    private readonly ITestOutputHelper _output;

    public PlayerDisconnectDetectionTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [InlineData(
        "[#3000669907 UDP steamid:76561198044941665@172.17.0.1:54196] closed by app, entering linger state (2158) NETWORK_DISCONNECT_KICKED_IDLE",
        "3000669907",
        "76561198044941665",
        "172.17.0.1:54196",
        "2158",
        "NETWORK_DISCONNECT_KICKED_IDLE")
    ]
    [InlineData(
        "[#2292360458 UDP steamid:76561198044941665@172.17.0.1:33160] closed by app, entering linger state (2039) NETWORK_DISCONNECT_KICKED",
        "2292360458",
        "76561198044941665",
        "172.17.0.1:33160",
        "2039",
        "NETWORK_DISCONNECT_KICKED")
    ]
    [InlineData(
        "[#2330728174 UDP steamid:76561198154417260@10.10.20.10:50819] closed by peer (1059): NETWORK_DISCONNECT_EXITING",
        "2330728174",
        "76561198154417260",
        "10.10.20.10:50819",
        "1059",
        "NETWORK_DISCONNECT_EXITING"
    )]
    [InlineData(
        "[#1570318589 UDP steamid:76561198158337634@95.91.227.226:1103] closed by peer (1002): NETWORK_DISCONNECT_DISCONNECT_BY_USER",
        "1570318589",
        "76561198158337634",
        "95.91.227.226:1103",
        "1002",
        "NETWORK_DISCONNECT_DISCONNECT_BY_USER"
    )]
    public async Task TestNewOutputPlayerDisconnectByPeerDetection(
        string rawMessage,
        string shouldBeConnectionId,
        string shouldBeSteamId64,
        string shouldBeIpPort,
        string shouldBeDisconnectReasonCode,
        string shouldBeDisconnectReason)
    {
        // Arrange
        var applicationServices = await ServicesSetup.GetApplicationCollection(_output);
        await using var provider = applicationServices.BuildServiceProvider();
        var eventService = provider.GetRequiredService<EventService>();
        var serverService = provider.GetRequiredService<ServerService>();

        // Act
        CustomEventArgPlayerDisconnected? arg = null;
        eventService.PlayerDisconnected += (_, disconnected) => arg = disconnected;
        serverService.NewOutputPlayerDisconnectDetection(
            null,
            new ServerOutputEventArg(
                new ServerStartDbModel
                {
                    Id = Guid.NewGuid(),
                    StartParameters = "-startParameter",
                    StartedUtc = DateTime.UtcNow,
                    CreatedUtc = DateTime.UtcNow
                },
                rawMessage));
        var waitResult =
            await WaitUtil.WaitUntil(TimeSpan.FromSeconds(1), () => arg is not null, _output.WriteLine);

        // Assert
        Assert.True(waitResult.IsError == false, waitResult.IsError ? waitResult.ErrorMessage() : "");
        Assert.True(arg is not null);
        Assert.True(arg.EventName == Events.PlayerDisconnected);
        Assert.Equal(shouldBeConnectionId, arg.ConnectionId);
        Assert.Equal(shouldBeSteamId64, arg.SteamId64);
        Assert.Equal(shouldBeIpPort, arg.IpPort);
        Assert.Equal(shouldBeDisconnectReasonCode, arg.DisconnectReasonCode);
        Assert.Equal(shouldBeDisconnectReason, arg.DisconnectReason);
    }
}