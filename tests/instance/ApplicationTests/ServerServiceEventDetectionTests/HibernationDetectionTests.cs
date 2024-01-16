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

public class HibernationDetectionTests
{
    private readonly ITestOutputHelper _output;

    public HibernationDetectionTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task HibernationDetectionTest_StartHibernation()
    {
        // Arrange
        var applicationServices = await ServicesSetup.GetApplicationCollection(_output);
        await using var provider = applicationServices.BuildServiceProvider();

        var eventService = provider.GetRequiredService<EventService>();
        var serverService = provider.GetRequiredService<ServerService>();

        // Act
        CustomEventArg? arg = null;
        eventService.HibernationStarted += (_, customEventArg) => arg = customEventArg;
        serverService.NewOutputHibernationDetection(null, new ServerOutputEventArg(
            new ServerStartDbModel
            {
                Id = Guid.NewGuid(),
                StartParameters = "-startParameter",
                StartedUtc = DateTime.UtcNow,
                CreatedUtc = DateTime.UtcNow
            },
            "Server is hibernating"));
        var waitResult =
            await WaitUtil.WaitUntil(() => arg is not null, _output.WriteLine);

        // Assert
        Assert.True(waitResult.IsError == false, waitResult.IsError ? waitResult.ErrorMessage() : "");
        Assert.True(arg is not null);
        Assert.True(arg.EventName == Events.HibernationStarted);
    }

    [Fact]
    public async Task HibernationDetectionTest_StopHibernation()
    {
        // Arrange
        var applicationServices = await ServicesSetup.GetApplicationCollection(_output);
        await using var provider = applicationServices.BuildServiceProvider();

        var eventService = provider.GetRequiredService<EventService>();
        var serverService = provider.GetRequiredService<ServerService>();

        // Act
        CustomEventArg? arg = null;
        eventService.HibernationEnded += (_, customEventArg) => arg = customEventArg;
        serverService.NewOutputHibernationDetection(null,
            new ServerOutputEventArg(
                new ServerStartDbModel
                {
                    Id = Guid.NewGuid(),
                    StartParameters = "-startParameter",
                    StartedUtc = DateTime.UtcNow,
                    CreatedUtc = DateTime.UtcNow
                },
                "Server waking up from hibernation"));
        var waitResult =
            await WaitUtil.WaitUntil(() => arg is not null, _output.WriteLine);

        // Assert
        Assert.True(waitResult.IsError == false, waitResult.IsError ? waitResult.ErrorMessage() : "");
        Assert.True(arg is not null);
        Assert.True(arg.EventName == Events.HibernationEnded);
    }
}