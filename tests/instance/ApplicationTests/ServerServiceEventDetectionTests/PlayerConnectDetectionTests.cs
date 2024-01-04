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

public class PlayerConnectDetectionTests
{
    private readonly ITestOutputHelper _output;

    public PlayerConnectDetectionTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task PlayerConnectDetectionTest()
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
                "CNetworkGameServerBase::ConnectClient( name='PhiS', remote='10.10.20.10:57143' )")
        );
        var waitResult =
            await WaitUtil.WaitUntil(TimeSpan.FromSeconds(1), () => arg is not null, _output.WriteLine);

        // Assert
        Assert.True(waitResult.IsError == false, waitResult.IsError ? waitResult.ErrorMessage() : "");
        Assert.True(arg is not null);
        Assert.True(arg.EventName == Events.PlayerConnected);
        Assert.Equal("PhiS", arg.PlayerName);
        Assert.Equal("10.10.20.10:57143", arg.PlayerIp);
    }
}