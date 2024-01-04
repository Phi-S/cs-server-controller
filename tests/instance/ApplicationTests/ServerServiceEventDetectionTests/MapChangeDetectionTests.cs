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

public class MapChangeDetectionTests
{
    private readonly ITestOutputHelper _output;

    public MapChangeDetectionTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task MapChangeDetectionTest()
    {
        // Arrange
        var applicationServices = await ServicesSetup.GetApplicationCollection(_output);
        await using var provider = applicationServices.BuildServiceProvider();

        var eventService = provider.GetRequiredService<EventService>();
        var serverService = provider.GetRequiredService<ServerService>();

        // Act
        CustomEventArgMapChanged? arg = null;
        eventService.MapChanged += (_, customEventArg) => arg = customEventArg;
        serverService.NewOutputMapChangeDetection(
            null,
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
            await WaitUtil.WaitUntil(() => arg is not null, _output.WriteLine);

        // Assert
        Assert.True(waitResult.IsError == false, waitResult.IsError ? waitResult.ErrorMessage() : "");
        Assert.True(arg is not null);
        Assert.True(arg.EventName == Events.MapChanged);
        Assert.Equal("de_dust2", arg.MapName);
    }
}