using Application.EventServiceFolder;
using Application.EventServiceFolder.EventArgs;
using Application.ServerServiceFolder;
using Domain;
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
    [InlineData("[All Chat][PhiS (194151532)]: ezz", "All Chat", "PhiS", "194151532", "ezz")]
    [InlineData("[All Chat][PhiS (194151532)]: noob", "All Chat", "PhiS", "194151532", "noob")]
    [InlineData("[All Chat][PhiS (194151532)]: [All Chat][alt (43434343434)]", "All Chat", "PhiS", "194151532",
        "[All Chat][alt (43434343434)]")]
    public async Task AllChatDetectionTest(string rawMessage, string shouldBeChat, string shouldBePlayerName,
        string shouldBeSteamId3, string shouldBeMessage)
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
        Assert.True(arg.EventName == Events.ChatMessage);
        Assert.Equal(arg.Chat, shouldBeChat);
        Assert.Equal(arg.PlayerName, shouldBePlayerName);
        Assert.Equal(arg.SteamId3, shouldBeSteamId3);
        Assert.Equal(arg.Message, shouldBeMessage);
    }
}