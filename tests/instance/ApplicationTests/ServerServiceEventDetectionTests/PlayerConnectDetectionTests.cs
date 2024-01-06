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

public class PlayerConnectDetectionTests
{
    private readonly ITestOutputHelper _output;

    public PlayerConnectDetectionTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [InlineData(
        "Accepting Steam Net connection #3000669907 UDP steamid:76561198044941665@172.17.0.1:54196",
        "3000669907",
        "76561198044941665",
        "172.17.0.1:54196"
    )]
    [InlineData(
        "Accepting Steam Net connection #2413188280 UDP steamid:76561198044941665@172.17.0.1:45632",
        "2413188280",
        "76561198044941665",
        "172.17.0.1:45632"
    )]
    [InlineData(
        "Accepting Steam Net connection #1674795556 UDP steamid:76561198044941665@172.17.0.1:40588",
        "1674795556",
        "76561198044941665",
        "172.17.0.1:40588"
    )]
    public async Task PlayerConnectDetectionTest(string log, string shouldBeConnectionId, string shouldBeStemId,
        string shouldBeIpPort)
    {
        // Arrange
        var applicationServices = await ServicesSetup.GetApplicationCollection(_output);
        await using var provider = applicationServices.BuildServiceProvider();

        var eventService = provider.GetRequiredService<EventService>();
        var serverService = provider.GetRequiredService<ServerService>();

        // Act
        CustomEventArgPlayerConnected? arg = null;
        eventService.PlayerConnected += (_, customEventArg) => arg = customEventArg;
        serverService.NewOutputPlayerConnectDetection(
            null,
            new ServerOutputEventArg(
                new ServerStart
                {
                    Id = Guid.NewGuid(),
                    StartParameters = "-startParameter",
                    StartedAtUtc = DateTime.UtcNow,
                    CreatedAtUtc = DateTime.UtcNow
                },
                log)
        );
        var waitResult =
            await WaitUtil.WaitUntil(TimeSpan.FromSeconds(1), () => arg is not null, _output.WriteLine);

        // Assert
        Assert.True(waitResult.IsError == false, waitResult.IsError ? waitResult.ErrorMessage() : "");
        Assert.True(arg is not null);
        Assert.True(arg.EventName == Events.PlayerConnected);
        Assert.Equal(shouldBeConnectionId, arg.ConnectionId);
        Assert.Equal(shouldBeStemId, arg.SteamId);
        Assert.Equal(shouldBeIpPort, arg.IpPort);
    }
}