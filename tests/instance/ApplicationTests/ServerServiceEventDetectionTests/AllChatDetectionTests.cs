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

public class AllChatDetectionTests
{
    private readonly ITestOutputHelper _output;

    public AllChatDetectionTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [InlineData(
        "[All Chat][P (51)]: hiS > :) < --L (84675937)]: kjkj",
        Chat.All,
        "P (51)]: hiS > :) < --L",
        "84675937",
        "kjkj"
    )]
    [InlineData(
        "[Allies Chat][PhiS > :) < --L (84675937)]: .kjkj",
        Chat.Team,
        "PhiS > :) < --L",
        "84675937",
        ".kjkj"
    )]
    [InlineData(
        " \t[All Chat][Console (0)]: kekek",
        Chat.All,
        "Console",
        "0",
        "kekek"
    )]
    public async Task AllChatDetectionTest(string rawMessage,
        Chat shouldBeChat,
        string shouldBePlayerName,
        string shouldBeSteamId3,
        string shouldBeMessage)
    {
        // Arrange
        var applicationServices = await ServicesSetup.GetApplicationCollection(_output);
        await using var provider = applicationServices.BuildServiceProvider();
        var eventService = provider.GetRequiredService<EventService>();
        var serverService = provider.GetRequiredService<ServerService>();

        // Act
        CustomEventArgChatMessage? arg = null;
        eventService.ChatMessage += (_, message) => { arg = message; };
        serverService.NewOutputAllChatDetection(
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
        Assert.True(arg.EventName == Events.ChatMessage);
        Assert.Equal(arg.Chat, shouldBeChat);
        Assert.Equal(arg.PlayerName, shouldBePlayerName);
        Assert.Equal(arg.SteamId, shouldBeSteamId3);
        Assert.Equal(arg.Message, shouldBeMessage);
    }
}