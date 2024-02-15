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
        """ 	Client #2 "PhiS > :) < --L" connected @ 172.17.0.1:53668""",
        "2",
        "PhiS > :) < --L",
        "172.17.0.1",
        "53668"
    )]
    [InlineData(
        """ 	Client #2 ""PhiS > :) < --L" connected @ 172.117.0.1:111""",
        "2",
        "\"PhiS > :) < --L",
        "172.117.0.1",
        "111"
    )]
    [InlineData(
        """ 	Client #2 "#4"PhiS > :) < --L" connected @ 172.17.0.150:55""",
        "2",
        "#4\"PhiS > :) < --L",
        "172.17.0.150",
        "55"
    )]
    public async Task PlayerConnectDetectionTest(string log,
        string shouldBeClientId,
        string shouldBeUsername,
        string shouldBeIp,
        string shouldBePort)
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
                new ServerStartDbModel
                {
                    Id = Guid.NewGuid(),
                    StartParameters = "-startParameter",
                    StartedUtc = DateTime.UtcNow,
                    CreatedUtc = DateTime.UtcNow
                },
                log)
        );
        var waitResult =
            await WaitUtil.WaitUntil(TimeSpan.FromSeconds(1), () => arg is not null, _output.WriteLine);

        // Assert
        Assert.True(waitResult.IsError == false, waitResult.IsError ? waitResult.ErrorMessage() : "");
        Assert.True(arg is not null);
        Assert.True(arg.EventName == Events.PlayerConnected);
        Assert.Equal(shouldBeClientId, arg.ConnectionId);
        Assert.Equal(shouldBeUsername, arg.Username);
        Assert.Equal(shouldBeIp, arg.Ip);
        Assert.Equal(shouldBePort, arg.Port);
    }
}