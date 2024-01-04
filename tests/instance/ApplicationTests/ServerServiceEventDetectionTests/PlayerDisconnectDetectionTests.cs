using Application.EventServiceFolder;
using Application.EventServiceFolder.EventArgs;
using Application.ServerServiceFolder;
using Domain;
using Infrastructure.Database.Models;
using Microsoft.Extensions.DependencyInjection;
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
        "Steam Net connection #2892143414 UDP steamid:76561198154417260@10.10.20.10:49347 closed by peer, reason 1002: NETWORK_DISCONNECT_DISCONNECT_BY_USER",
        "2892143414",
        "76561198154417260",
        "10.10.20.10:49347",
        "1002",
        "NETWORK_DISCONNECT_DISCONNECT_BY_USER")
    ]
    [InlineData(
        "Steam Net connection #516036333 UDP steamid:76561198044941665@172.17.0.1:54767 closed by peer, reason 2055: NETWORK_DISCONNECT_LOOPDEACTIVATE",
        "516036333",
        "76561198044941665",
        "172.17.0.1:54767",
        "2055",
        "NETWORK_DISCONNECT_LOOPDEACTIVATE")
    ]
    public async Task TestNewOutputPlayerDisconnectDetection(
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